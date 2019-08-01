using System;
using System.Collections.Generic;

namespace LightestNight.System.Caching.Redis.TagCache.CacheItem
{
    [Serializable]
    public class RedisCacheItem : IRedisCacheItem
    {
        /// <inheritdoc cref="IRedisCacheItem.Key" />
        public string Key { get; set; }
        
        /// <inheritdoc cref="IRedisCacheItem.Tags" />
        public string[] Tags { get; set; }
        
        /// <inheritdoc cref="IRedisCacheItem.Expiry" />
        public DateTime? Expiry { get; set; }
    }

    [Serializable]
    public class RedisCacheItem<T> : RedisCacheItem, IRedisCacheItem<T>
    {
        /// <inheritdoc cref="IRedisCacheItem{T}.Value" />
        public T Value { get; set; }
    }
}