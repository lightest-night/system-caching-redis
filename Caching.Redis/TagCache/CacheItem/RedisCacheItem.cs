using System;
using System.Collections.Generic;

namespace LightestNight.System.Caching.Redis.TagCache.CacheItem
{
    [Serializable]
    public class RedisCacheItem
    {
        /// <summary>
        /// The key to store the item under
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// Any tags to attach to the item
        /// </summary>
        public string[]? Tags { get; set; }
        
        /// <summary>
        /// The date and time the item will expire and be removed from the cache
        /// </summary>
        public DateTime? Expiry { get; set; }
    }

    [Serializable]
    public class RedisCacheItem<T> : RedisCacheItem
    {
        /// <summary>
        /// The value to store in the item
        /// </summary>
        public T Value { get; set; } = default!;
    }
}