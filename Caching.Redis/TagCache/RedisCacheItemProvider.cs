using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using LightestNight.System.Utilities.Extensions;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisCacheItemProvider
    {
        private readonly ISerializationProvider _serializer;
        private readonly ICacheItemFactory _cacheItemFactory;

        public RedisCacheItemProvider(ISerializationProvider serializer, ICacheItemFactory cacheItemFactory)
        {
            _serializer = serializer;
            _cacheItemFactory = cacheItemFactory;
        }

        /// <summary>
        /// Gets the cache item with the given key
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to communicate with Redis</param>
        /// <param name="key">The key to get the value under</param>
        /// <typeparam name="T">The type of the item to return</typeparam>
        /// <returns>The retrieved cache item; null if nothing found</returns>
        public async Task<CacheItem<T>?> Get<T>(RedisClient client, string key)
        {
            var cacheString = await client.ThrowIfNull(nameof(client)).Get(key).ConfigureAwait(false);
            return cacheString.HasValue
                ? _serializer.Deserialize<CacheItem<T>>(cacheString)
                : null;
        }

        /// <summary>
        /// Gets the items from the cache with the corresponding keys
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to communicate with Redis</param>
        /// <param name="keys">The keys to get the values under</param>
        /// <typeparam name="T">The type of the items to return</typeparam>
        /// <returns>A collection of retrieved cache items</returns>
        public async Task<IEnumerable<CacheItem<T>>> GetMany<T>(RedisClient client, params string[]? keys)
        {
            var result = new List<CacheItem<T>>();

            if (keys == null)
                return result;
            
            foreach (var key in keys)
            {
                var r = await Get<T>(client, key).ConfigureAwait(false);
                if (r != null)
                    result.Add(r);
            }

            return result;
        }

        /// <summary>
        /// Sets an item into the cache under the given key with the associated tags
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to communicate with Redis</param>
        /// <param name="key">The key to set the item under</param>
        /// <param name="value">The value to store</param>
        /// <param name="expiry">When to expire the cached item</param>
        /// <param name="tags">The tags to assign to the cached value</param>
        /// <typeparam name="T">The type of the item being stored</typeparam>
        public Task<bool> Set<T>(RedisClient client, string key, T value, DateTime? expiry = null, params string[]? tags)
            where T : notnull
        {
            var cacheItem = _cacheItemFactory.Create(key, value, expiry, tags);
            var serialized = _serializer.Serialize(cacheItem);
            return client.ThrowIfNull(nameof(client)).Set(key, serialized, expiry);
        }
    }
}