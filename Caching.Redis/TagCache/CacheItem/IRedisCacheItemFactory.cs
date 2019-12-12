using System;

namespace LightestNight.System.Caching.Redis.TagCache.CacheItem
{
    public interface IRedisCacheItemFactory
    {
        /// <summary>
        /// Creates a new <see cref="RedisCacheItem" />
        /// </summary>
        /// <param name="key">The cache key to save the item under</param>
        /// <param name="tags">Any tags to attach to the item</param>
        /// <returns>A new instance of <see cref="RedisCacheItem" /></returns>
        RedisCacheItem Create(string key, params string[]? tags);

        /// <summary>
        /// Creates a new <see cref="RedisCacheItem{T}" />
        /// </summary>
        /// <param name="key">The cache key to save the item under</param>
        /// <param name="value">The value to store in the item</param>
        /// <param name="expiry">The date and time this cache item will expire and be removed from the cache</param>
        /// <param name="tags">Any tags to attach to the item</param>
        /// <typeparam name="T">The type of the object being stored</typeparam>
        /// <returns>A new instance of <see cref="RedisCacheItem{T}" /></returns>
        RedisCacheItem<T> Create<T>(string key, T value, DateTime? expiry = null, params string[]? tags)
            where T : notnull;
    }
}