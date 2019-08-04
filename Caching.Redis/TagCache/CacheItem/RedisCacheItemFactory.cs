using System;

namespace LightestNight.System.Caching.Redis.TagCache.CacheItem
{
    public class RedisCacheItemFactory : IRedisCacheItemFactory
    {
        /// <inheritdoc cref="IRedisCacheItemFactory.Create" />
        public RedisCacheItem Create(string key, params string[] tags)
        {
            return new RedisCacheItem
            {
                Key = key,
                Tags = tags
            };
        }

        /// <inheritdoc cref="IRedisCacheItemFactory.Create{T}" />
        public RedisCacheItem<T> Create<T>(string key, T value, DateTime? expiry = null, params string[] tags)
        {
            return new RedisCacheItem<T>
            {
                Key = key,
                Tags = tags,
                Expiry = expiry,
                Value = value
            };
        }
    }
}