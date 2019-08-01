using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightestNight.System.Caching.Redis
{
    public interface ICache
    {
        /// <summary>
        /// Saves an item to the cache
        /// </summary>
        /// <param name="key">The key to save the item under</param>
        /// <param name="item">The object to save</param>
        /// <param name="expiry">If set, the expiry of this item; Default is 1 year</param>
        /// <param name="tags">Any tags to assign this item</param>
        /// <typeparam name="TItem">The type of the object being saved</typeparam>
        Task Save<TItem>(string key, TItem item, DateTime? expiry = default, params string[] tags);

        /// <summary>
        /// Gets an item from the cache
        /// </summary>
        /// <param name="key">The key to get the item</param>
        /// <typeparam name="TItem">The type of the object being retrieved</typeparam>
        /// <returns>The instance of <typeparamref name="TItem" /> found in the cache. If nothing found returns default</returns>
        Task<TItem> Get<TItem>(string key);

        /// <summary>
        /// Gets items from the cache by their associated tag
        /// </summary>
        /// <param name="tag">The tag to find the items under</param>
        /// <typeparam name="TItem">The type of the object being retrieved</typeparam>
        /// <returns>A collection of <typeparamref name="TItem" /> instances found in the cache. If nothing found returns an empty collection</returns>
        Task<IEnumerable<TItem>> GetByTag<TItem>(string tag);

        /// <summary>
        /// Deletes the cache item under the given key
        /// </summary>
        /// <param name="key">The key to delete the item under</param>
        /// <typeparam name="TItem">The type of the object being deleted. If the item is a list, the type of the items within the list</typeparam>
        Task Delete<TItem>(string key);
    }
}