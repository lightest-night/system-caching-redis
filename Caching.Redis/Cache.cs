using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

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

        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}