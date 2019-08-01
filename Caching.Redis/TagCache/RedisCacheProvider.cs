using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache.CacheItem;
using LightestNight.System.Caching.Redis.TagCache.Expiry;
using LightestNight.System.Utilities.Extensions;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisCacheProvider : IRedisCacheProvider
    {
        private static ConcurrentDictionary<string, RedisExpiryHandler> _redisExpiryHandlersByHost;
        
        private readonly RedisClient _client;
        private readonly RedisCacheItemProvider _redisCacheItemProvider;
        private readonly RedisTagManager _redisTagManager;
        private readonly RedisExpiryManager _redisExpiryManager;

        /// <summary>
        /// An instance of <see cref="IRedisCacheLogger" /> used to log errors and informational messages
        /// </summary>
        public IRedisCacheLogger Logger { get; set; }
        
        public RedisCacheProvider(RedisConnectionManager connectionManager) : this(new CacheConfiguration(connectionManager))
        {}

        public RedisCacheProvider(CacheConfiguration configuration)
        {
            _client = new RedisClient(configuration.RedisClientConfiguration.RedisConnectionManager, configuration.RedisClientConfiguration.DbIndex);
            _redisTagManager = new RedisTagManager(configuration.CacheItemFactory);
            _redisExpiryManager = new RedisExpiryManager(configuration);
            _redisCacheItemProvider = new RedisCacheItemProvider(configuration.Serializer, configuration.CacheItemFactory);

            SetupExpiryHandler(configuration, this);
        }

        /// <inheritdoc cref="IRedisCacheProvider.Get{T}" />
        public async Task<T> Get<T>(string key)
        {
            var cacheItem = await _redisCacheItemProvider.Get<T>(_client, key);
            if (cacheItem != null)
            {
                if (await CacheItemIsValid(cacheItem))
                {
                    await Log(nameof(Get), key, "Found");
                    return cacheItem.Value;
                }
            }

            await Log(nameof(Get), key, "Not Found");
            return default;
        }

        /// <inheritdoc cref="IRedisCacheProvider.GetByTag{T}" />
        public async Task<IEnumerable<T>> GetByTag<T>(string tag)
        {
            var keys = (await _redisTagManager.GetKeysForTag(_client, tag))?.ToArray();
            if (keys.IsNullOrEmpty())
            {
                await Log(nameof(GetByTag), tag, "Not Found");
                return null;
            }

            var result = new ConcurrentBag<T>();
            var items = await _redisCacheItemProvider.GetMany<T>(_client, keys);

            await items.ForEach(async item =>
            {
                if (!await CacheItemIsValid(item))
                    return;
                
                var value = item.Value;
                if (value != null)
                    result.Add(value);
            }, Environment.ProcessorCount);

            await Log(nameof(GetByTag), tag, "Found");
            return result;
        }

        /// <inheritdoc cref="IRedisCacheProvider.Set{T}(string,T,Nullable{DateTime},string)" />
        public Task Set<T>(string key, T value, DateTime? expiry = null, string tag = null)
            => Set(key, value, expiry, tag != null ? new[] {tag} : null);

        /// <inheritdoc cref="IRedisCacheProvider.Set{T}(string,T,string[])" />
        public Task Set<T>(string key, T value, params string[] tags)
            => Set(key, value, null, tags);

        /// <inheritdoc cref="IRedisCacheProvider.Set{T}(string,T,Nullable{DateTime},string[])" />
        public async Task Set<T>(string key, T value, DateTime? expiry = null, params string[] tags)
        {
            await Log(nameof(Set), key, null);

            if (await _redisCacheItemProvider.Set(_client, key, value, expiry, tags))
                await _redisTagManager.UpdateTags(_client, key, tags);
        }

        /// <inheritdoc cref="IRedisCacheProvider.Remove(string)" />
        public async Task Remove(string key)
        {
            await Log(nameof(Remove), key, null);
            await Remove(new[] {key});
        }
        
        /// <inheritdoc cref="IRedisCacheProvider.Remove(string[])" />
        public async Task Remove(params string[] keys)
        {
            await Log(nameof(Remove), string.Join(",", keys), null);
            await _client.Remove(keys);
            await _redisTagManager.RemoveTags(_client, keys);
            await _redisExpiryManager.RemoveKeyExpiry(_client, keys);
        }
        
        /// <inheritdoc cref="IRedisCacheProvider.Remove(IRedisCacheItem)" />
        public async Task Remove(IRedisCacheItem cacheItem)
        {
            await Log(nameof(Remove), cacheItem.Key, "Removed via the `Remove(IRedisCacheItem)` method");
            await Task.WhenAll(_client.Remove(cacheItem.Key), _redisTagManager.RemoveTags(_client, cacheItem));
        }

        /// <inheritdoc cref="IRedisCacheProvider.RemoveByTag" />
        public async Task RemoveByTag(string tag)
        {
            await Log(nameof(RemoveByTag), tag, null);
            var keys = (await _redisTagManager.GetKeysForTag(_client, tag)).ToArray();
            if (!keys.IsNullOrEmpty())
                await Remove(keys);
        }

        /// <inheritdoc cref="IRedisCacheProvider.RemoveExpiredKeys" />
        public async Task<IEnumerable<string>> RemoveExpiredKeys()
        {
            var maxDate = DateTime.UtcNow.AddMinutes(CacheConfiguration.MinutesToRemoveAfterExpiry);
            var keys = (await _redisExpiryManager.GetExpiredKeys(_client, maxDate)).ToArray();
            await Remove(keys);

            return keys;
        }

        private static void SetupExpiryHandler(CacheConfiguration configuration, RedisCacheProvider redisCacheProvider)
        {
            if (_redisExpiryHandlersByHost == null)
                _redisExpiryHandlersByHost = new ConcurrentDictionary<string, RedisExpiryHandler>();

            if (_redisExpiryHandlersByHost.ContainsKey(configuration.RedisClientConfiguration.Host)) 
                return;
            
            var handler = new RedisExpiryHandler(configuration)
            {
                RemoveMethod = redisCacheProvider.Remove, 
                LogMethod = redisCacheProvider.Log
            };

            _redisExpiryHandlersByHost.TryAdd(configuration.RedisClientConfiguration.Host, handler);
        }

        private Task Log(string method, string arg, string message)
            => Logger == null
                ? Task.CompletedTask
                : Logger.Log(method, arg, message);

        private async Task<bool> CacheItemIsValid(IRedisCacheItem item)
        {
            if (!item.Expiry.HasValue || item.Expiry.Value >= DateTime.UtcNow) 
                return true;
            
            await Remove(item);
            
            return false;
        }
    }
}