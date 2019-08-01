using System;
using System.Collections.Generic;

namespace LightestNight.System.Caching.Redis.TagCache.CacheItem
{
    public interface IRedisCacheItem
    {
        /// <summary>
        /// The key to store the item under
        /// </summary>
        string Key { get; set; }
        
        /// <summary>
        /// Any tags to attach to the item
        /// </summary>
        string[] Tags { get; set; }
        
        /// <summary>
        /// The date and time the item will expire and be removed from the cache
        /// </summary>
        DateTime? Expiry { get; set; }
    }

    public interface IRedisCacheItem<T> : IRedisCacheItem
    {
        /// <summary>
        /// The value to store in the item
        /// </summary>
        T Value { get; set; }
    }
}