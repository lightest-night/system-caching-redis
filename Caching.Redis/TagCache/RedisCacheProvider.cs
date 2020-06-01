using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache.Expiry;
using LightestNight.System.Utilities.Extensions;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisCacheProvider : IRedisCacheProvider
    {
        private static ConcurrentDictionary<string, RedisExpiryHandler>? _redisExpiryHandlersByHost;
        
        private readonly RedisClient _client;
        private readonly RedisCacheItemProvider _redisCacheItemProvider;
        private readonly RedisTagManager _redisTagManager;
        private readonly RedisExpiryProvider _redisExpiryProvider;

        /// <summary>
        /// An instance of <see cref="IRedisCacheLogger" /> used to log errors and informational messages
        /// </summary>
        public IRedisCacheLogger? Logger { get; set; }
        
        public RedisCacheProvider(RedisConnectionManager connectionManager) : this(new CacheConfiguration(connectionManager))
        {}

        public RedisCacheProvider(CacheConfiguration configuration)
        {
            configuration.ThrowIfNull(nameof(configuration));
            
            _client = new RedisClient(configuration.RedisClientConfiguration.RedisConnectionManager, configuration.RedisClientConfiguration.DbIndex);
            _redisTagManager = new RedisTagManager(configuration.CacheItemFactory);
            _redisExpiryProvider = new RedisExpiryProvider(configuration);
            _redisCacheItemProvider = new RedisCacheItemProvider(configuration.Serializer, configuration.CacheItemFactory);

            SetupExpiryHandler(configuration, this);
        }

        /// <inheritdoc cref="IRedisCacheProvider.GetItem{T}" />
        public async Task<CacheItem<T>> GetItem<T>(string key)
        {
            var cacheItem = await _redisCacheItemProvider.Get<T>(_client, key).ConfigureAwait(false);
            if (cacheItem != null)
            {
                if (await CacheItemIsValid(cacheItem).ConfigureAwait(false))
                {
                    await Log(nameof(GetItem), key, "Found").ConfigureAwait(false);
                    return cacheItem;
                }
            }

            await Log(nameof(GetItem), key, "Not Found").ConfigureAwait(false);
            return default!;
        }

        /// <inheritdoc cref="IRedisCacheProvider.GetByTag{T}" />
        public async Task<IEnumerable<CacheItem<T>>> GetByTag<T>(string tag)
        {
            var keys = (await RedisTagManager.GetKeysForTag(_client, tag).ConfigureAwait(false))?.ToArray();
            if (keys.IsNullOrEmpty())
            {
                await Log(nameof(GetByTag), tag, "Not Found").ConfigureAwait(false);
                return Enumerable.Empty<CacheItem<T>>();
            }

            var result = new ConcurrentBag<CacheItem<T>>();
            var items = await _redisCacheItemProvider.GetMany<T>(_client, keys).ConfigureAwait(false);

            await items.ForEach(async item =>
            {
                if (!await CacheItemIsValid(item).ConfigureAwait(false))
                    return;
                
                var value = item.Value;
                if (value != null)
                    result.Add(item);
            }, Environment.ProcessorCount).ConfigureAwait(false);

            await Log(nameof(GetByTag), tag, "Found").ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="IRedisCacheProvider.SetItem{T}(string,T,string[])" />
        public Task SetItem<T>(string key, T value, params string[]? tags) where T : notnull
            => SetItem(key, value, null, tags);

        /// <inheritdoc cref="IRedisCacheProvider.SetItem{T}(string,T,Nullable{DateTime},string[])" />
        public async Task SetItem<T>(string key, T value, DateTime? expiry = null, params string[]? tags)
            where T : notnull
        {
            await Log(nameof(SetItem), key, null).ConfigureAwait(false);
            if (await _redisCacheItemProvider.Set(_client, key, value, expiry, tags).ConfigureAwait(false))
            {
                if (!tags.IsNullOrEmpty())
                {
                    var updateTagsTask = _redisTagManager.UpdateTags(_client, key, tags!);
                    var setKeyExpiryTask = expiry.HasValue ? _redisExpiryProvider.SetKeyExpiry(_client, key, expiry.Value) : Task.CompletedTask;

                    await Task.WhenAll(updateTagsTask, setKeyExpiryTask).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc cref="IRedisCacheProvider.Remove(string)" />
        public async Task Remove(string key)
        {
            await Log(nameof(Remove), key, null).ConfigureAwait(false);
            await Remove(new[] {key}).ConfigureAwait(false);
        }
        
        /// <inheritdoc cref="IRedisCacheProvider.Remove(string[])" />
        public async Task Remove(params string[] keys)
        {
            if (keys.Length > 0)
            {
                await Log(nameof(Remove), string.Join(",", keys), null).ConfigureAwait(false);
                await _client.Remove(keys).ConfigureAwait(false);
                await _redisTagManager.RemoveTags(_client, keys).ConfigureAwait(false);
                await _redisExpiryProvider.RemoveKeyExpiry(_client, keys).ConfigureAwait(false);
            }
        }
        
        /// <inheritdoc cref="IRedisCacheProvider.Remove(CacheItem)" />
        public async Task Remove(CacheItem cacheItem)
        {
            cacheItem.ThrowIfNull(nameof(cacheItem));
            
            await Log(nameof(Remove), cacheItem.Key, "Removed via the `Remove(IRedisCacheItem)` method").ConfigureAwait(false);
            await Task.WhenAll(_client.Remove(cacheItem.Key), RedisTagManager.RemoveTags(_client, cacheItem),
                _redisExpiryProvider.RemoveKeyExpiry(_client, cacheItem.Key)).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IRedisCacheProvider.RemoveByTag" />
        public async Task RemoveByTag(string tag)
        {
            await Log(nameof(RemoveByTag), tag, null).ConfigureAwait(false);
            var keys = (await RedisTagManager.GetKeysForTag(_client, tag).ConfigureAwait(false)).ToArray();
            if (!keys.IsNullOrEmpty())
                await Remove(keys).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IRedisCacheProvider.Exists" />
        public async Task<bool> Exists(string key)
        {
            await Log(nameof(Exists), key, null).ConfigureAwait(false);
            return await _client.Exists(key).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IRedisCacheProvider.RemoveExpiredKeys" />
        public async Task<IEnumerable<string>> RemoveExpiredKeys()
        {
            var removedKeys = new List<string>();

            var maxDate = DateTime.Now.AddMinutes(CacheConfiguration.MinutesToRemoveAfterExpiry);
            removedKeys.AddRange(await _redisExpiryProvider.GetExpiredKeys(_client, maxDate).ConfigureAwait(false));
            removedKeys.AddRange(await _client.RemoveExpiredKeysFromTags().ConfigureAwait(false));
            removedKeys.AddRange(await _client.RemoveTagsFromExpiredKeys().ConfigureAwait(false));

            return removedKeys.Distinct();
        }

        private static void SetupExpiryHandler(CacheConfiguration configuration, RedisCacheProvider redisCacheProvider)
        {
            _redisExpiryHandlersByHost ??= new ConcurrentDictionary<string, RedisExpiryHandler>();

            var redisConnectionManager = configuration.RedisClientConfiguration.RedisConnectionManager;
            var host = redisConnectionManager.Host ?? redisConnectionManager.ConnectionString;

            if (_redisExpiryHandlersByHost.ContainsKey(host))
                return;

            _redisExpiryHandlersByHost.TryAdd(host, new RedisExpiryHandler(configuration)
            {
                RemoveMethod = redisCacheProvider.Remove,
                LogMethod = redisCacheProvider.Log
            });
        }

        private Task Log(string method, string arg, string? message)
            => Logger == null
                ? Task.CompletedTask
                : Logger.Log(method, arg, message);

        private async Task<bool> CacheItemIsValid(CacheItem item)
        {
            if (!item.Expiry.HasValue || item.Expiry.Value >= DateTime.Now) 
                return true;
            
            await Remove(item).ConfigureAwait(false);
            
            return false;
        }
    }
}