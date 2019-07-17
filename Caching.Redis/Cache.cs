using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Utilities.Extensions;
using Newtonsoft.Json;

namespace LightestNight.System.Caching.Redis
{
    public class Cache
    {
        private readonly Set _set;
        private readonly Get _get;
        private readonly GetByTag _getByTag;
        private readonly Remove _remove;

        public Cache(Set set, Get get, GetByTag getByTag, Remove remove)
        {
            _set = set;
            _get = get;
            _getByTag = getByTag;
            _remove = remove;
        }

        /// <summary>
        /// Saves an item to the cache
        /// </summary>
        /// <param name="key">The key to save the item under</param>
        /// <param name="objectToSave">The object to save</param>
        /// <param name="expires">If set, the expiry of this item; Default is 1 year</param>
        /// <param name="tags">Any tags to assign this item</param>
        /// <typeparam name="TItem">The type of the object being saved</typeparam>
        public async Task Save<TItem>(object key, TItem objectToSave, DateTime? expires = default, params string[] tags)
        {
            if (objectToSave != null)
            {
                var cacheKey = GenerateKey<TItem>(key.ToString());
                var cacheItem = JsonConvert.SerializeObject(objectToSave);
                await _set(cacheKey, cacheItem, expires, tags);
            }
        }

        /// <summary>
        /// Gets an item from the cache
        /// </summary>
        /// <param name="key">The key to get the item</param>
        /// <typeparam name="TItem">The type of the object being retrieved</typeparam>
        /// <returns>The instance of <typeparamref name="TItem" /> found in the cache. If nothing found returns default</returns>
        public async Task<TItem> Get<TItem>(object key)
        {
            var cacheItem = await _get(GenerateKey<TItem>(key.ToString()));
            return cacheItem == default
                ? default
                : JsonConvert.DeserializeObject<TItem>(cacheItem);
        }

        /// <summary>
        /// Gets items from the cache by their associated tag
        /// </summary>
        /// <param name="tag">The tag to find the items under</param>
        /// <typeparam name="TItem">The type of the object being retrieved</typeparam>
        /// <returns>A collection of <typeparamref name="TItem" /> instances found in the cache. If nothing found returns an empty collection</returns>
        public async Task<IEnumerable<TItem>> GetByTag<TItem>(string tag)
        {
            var cacheItems = await _getByTag(tag);
            return cacheItems.IsNullOrEmpty() 
                ? Enumerable.Empty<TItem>() 
                : cacheItems.Select(JsonConvert.DeserializeObject<TItem>);
        }
        
        /// <summary>
        /// Deletes the cache item under the given key
        /// </summary>
        /// <param name="key">The key to delete the item under</param>
        /// <typeparam name="TItem">The type of the object being deleted. If the item is a list, the type of the items within the list</typeparam>
        public async Task Delete<TItem>(object key)
        {
            var cacheKey = GenerateKey<TItem>(key.ToString());
            await _remove(cacheKey);
        }
        
        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}