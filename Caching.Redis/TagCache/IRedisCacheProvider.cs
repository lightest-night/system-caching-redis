using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public interface IRedisCacheProvider
    {
        /// <summary>
        /// Gets the cached item under the given key
        /// </summary>
        /// <param name="key">The key to find the item under</param>
        /// <typeparam name="T">The type of the item</typeparam>
        /// <returns>The item. Null if not found</returns>
        Task<T> Get<T>(string key);

        /// <summary>
        /// Gets all items associated with the given tag
        /// </summary>
        /// <param name="tag">The tag to find the associated items under</param>
        /// <typeparam name="T">The type of the items</typeparam>
        /// <returns>Collection of items found; Empty list if not found</returns>
        Task<IEnumerable<T>> GetByTag<T>(string tag);

        /// <summary>
        /// Sets the given item into the cache with the given key, expiry and tags
        /// </summary>
        /// <param name="key">The key to set the item under</param>
        /// <param name="value">The item to set into the cache</param>
        /// <param name="expiry">The expiry date of the item</param>
        /// <param name="tags">A collection of tags to associate the item with</param>
        /// <typeparam name="T">The type of the item</typeparam>
        Task Set<T>(string key, T value, DateTime? expiry = null, params string[]? tags) where T : notnull;

        /// <summary>
        /// Sets the given item into the cache with the given key, expiry and tags
        /// </summary>
        /// <param name="key">The key to set the item under</param>
        /// <param name="value">The item to set into the cache</param>
        /// <param name="tags">A collection of tags to associate the item with</param>
        /// <typeparam name="T">The type of the item</typeparam>
        Task Set<T>(string key, T value, params string[]? tags) where T : notnull;

        /// <summary>
        /// Removes the item with the given key from the cache
        /// </summary>
        /// <param name="key">The key to remove from the cache</param>
        Task Remove(string key);

        /// <summary>
        /// Removes all items with the given keys from the cache
        /// </summary>
        /// <param name="keys">The keys to remove from the cache</param>
        Task Remove(params string[] keys);

        /// <summary>
        /// Removes the given item from the cache
        /// </summary>
        /// <param name="item">The item to remove</param>
        Task Remove(CacheItem item);

        /// <summary>
        /// Removes the items in the cache associated to the given tag
        /// </summary>
        /// <param name="tag">The tag to remove items by</param>
        Task RemoveByTag(string tag);

        /// <summary>
        /// Determines if an item exists within the cache with the given key
        /// </summary>
        /// <param name="key">The key to check existence for</param>
        /// <returns>Boolean denoting existence of an item with the given key</returns>
        Task<bool> Exists(string key);

        /// <summary>
        /// If the version of Redis does not support expiry subscriptions, this removes any expired keys
        /// </summary>
        /// <remarks>This should be called at regular intervals to avoid pollution of the cache</remarks>
        Task<IEnumerable<string>> RemoveExpiredKeys();
    }
}