using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
// ReSharper disable UnusedMethodReturnValue.Global

namespace LightestNight.System.Caching.Redis
{
    public class Cache
    {
        private readonly IDatabase _redisDatabase;

        public Cache(Func<IDatabase> databaseFunc)
        {
            _redisDatabase = databaseFunc();
        }

        /// <summary>
        /// Saves an item to the cache
        /// </summary>
        /// <param name="key">The key to save the item under</param>
        /// <param name="objectToSave">The object to save</param>
        /// <param name="expiry">If set, the expiry of this item</param>
        /// <typeparam name="TItem">The type of the object being saved</typeparam>
        public async Task Save<TItem>(object key, TItem objectToSave, TimeSpan? expiry = default)
        {
            if (objectToSave != null)
            {
                var cacheKey = GenerateKey<TItem>(key.ToString());
                
                if (await _redisDatabase.StringGetAsync(cacheKey) != RedisValue.Null)
                    await _redisDatabase.KeyDeleteAsync(cacheKey);

                var cacheItem = JsonConvert.SerializeObject(objectToSave);
                await _redisDatabase.StringSetAsync(cacheKey, cacheItem, expiry);
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
            var cacheItem = await _redisDatabase.StringGetAsync(GenerateKey<TItem>(key.ToString()));
            return cacheItem == RedisValue.Null 
                ? default
                : JsonConvert.DeserializeObject<TItem>(cacheItem);
        }

        /// <summary>
        /// Deletes the cache item under the given key
        /// </summary>
        /// <param name="key">The key to delete the item under</param>
        /// <typeparam name="TItem">The type of the object being deleted. If the item is a list, the type of the items within the list</typeparam>
        public async Task Delete<TItem>(object key)
        {
            var cacheKey = GenerateKey<TItem>(key.ToString());
            await _redisDatabase.KeyDeleteAsync(cacheKey);
        }

        /// <summary>
        /// Adds an item to a list within the cache
        /// </summary>
        /// <param name="key">The key to add the item under</param>
        /// <param name="objectToAdd">The object to add</param>
        /// <typeparam name="TItem">The type of the object being added</typeparam>
        public async Task AddToList<TItem>(object key, TItem objectToAdd)
        {
            if (objectToAdd != null)
            {
                var cacheKey = GenerateKey<TItem>(key.ToString());
                var cacheItem = JsonConvert.SerializeObject(objectToAdd);
                await _redisDatabase.ListRightPushAsync(cacheKey, cacheItem);
            }
        }

        /// <summary>
        /// Removes an item from a list within the cache
        /// </summary>
        /// <param name="key">The key to remove the item from</param>
        /// <param name="index">The index of the item to be removed</param>
        /// <typeparam name="TItem">The type of the object being removed</typeparam>
        public async Task RemoveFromListAt<TItem>(object key, int index)
        {
            var cacheKey = GenerateKey<TItem>(key.ToString());
            var cacheValue = await _redisDatabase.ListGetByIndexAsync(cacheKey, index);

            if (cacheValue != RedisValue.Null)
                await _redisDatabase.ListRemoveAsync(cacheKey, cacheValue);
        }

        /// <summary>
        /// Removes an item from a list within the cache
        /// </summary>
        /// <param name="key">The key to remove the item from</param>
        /// <param name="objectToRemove">The object to remove</param>
        /// <typeparam name="TItem">The type of the object being removed</typeparam>
        public async Task RemoveFromList<TItem>(object key, TItem objectToRemove)
        {
            var cacheKey = GenerateKey<TItem>(key.ToString());
            var cacheValue = JsonConvert.SerializeObject(objectToRemove);

            await _redisDatabase.ListRemoveAsync(cacheKey, cacheValue);
        }

        /// <summary>
        /// Gets an item from a list within the cache at a given index
        /// </summary>
        /// <param name="key">The key to get the item from</param>
        /// <param name="index">The index of the item to get</param>
        /// <typeparam name="TItem">The type of the object</typeparam>
        /// <returns>An instance of <see cref="TItem" /> found in the list at the given index. If not found, the default value is returned</returns>
        public async Task<TItem> GetFromListAt<TItem>(object key, int index)
        {
            var cacheKey = GenerateKey<TItem>(key.ToString());
            var cacheValue = await _redisDatabase.ListGetByIndexAsync(cacheKey, index);

            return cacheValue == RedisValue.Null 
                ? default 
                : JsonConvert.DeserializeObject<TItem>(cacheValue);
        }

        /// <summary>
        /// Gets an entire list from the cache
        /// </summary>
        /// <param name="key">The key to get the list under</param>
        /// <typeparam name="TItem">The type of the objects stored in the list</typeparam>
        /// <returns>A collection of <see cref="TItem" /> objects found in the cache</returns>
        public async Task<IEnumerable<TItem>> GetList<TItem>(object key)
        {
            var cacheKey = GenerateKey<TItem>(key.ToString());
            var listCount = await _redisDatabase.ListLengthAsync(cacheKey);

            var result = new List<TItem>();
            for (var i = 0; i < listCount; i++)
            {
                var cacheItem = await _redisDatabase.ListGetByIndexAsync(cacheKey, i);
                result.Add(JsonConvert.DeserializeObject<TItem>(cacheItem.ToString()));
            }

            return result;
        }

        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}