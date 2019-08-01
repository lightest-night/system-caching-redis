using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Utilities.Extensions;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisClient
    {
        private const string RootName = "_redisCache";

        private readonly int _db;
        private readonly RedisConnectionManager _connectionManager;

        public RedisClient(RedisConnectionManager connectionManager, int db)
            : this(connectionManager)
        {
            _db = db;
        }

        public RedisClient(RedisConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Sets the given <see cref="RedisValue" /> into the cache under the given key and sets the expiry
        /// </summary>
        /// <param name="key">The key to set the value under</param>
        /// <param name="value">The value to save</param>
        /// <param name="expiry">Sets the expiry of this item</param>
        public Task<bool> Set(string key, RedisValue value, DateTime? expiry)
        {
            var connection = _connectionManager.GetConnection();
            return connection.GetDatabase(_db).StringSetAsync(key, value, expiry?.TimeOfDay);
        }

        /// <summary>
        /// Gets the instance of <see cref="RedisValue" /> stored under the given key
        /// </summary>
        /// <param name="key">The key to find the value under</param>
        /// <returns>The found instance of <see cref="RedisValue" />; <see cref="RedisValue.Null" /> if not found</returns>
        public Task<RedisValue> Get(string key)
        {
            var connection = _connectionManager.GetConnection();
            return connection.GetDatabase(_db).StringGetAsync(key);
        }

        /// <summary>
        /// Removes the item from the cache with the given key
        /// </summary>
        /// <param name="key">The key to remove the item under</param>
        public Task<long> Remove(string key)
            => Remove(new[] {key});

        /// <summary>
        /// Removes the items from the cache with the given keys
        /// </summary>
        /// <param name="keys">The keys to remove the items under</param>
        public Task<long> Remove(IEnumerable<string> keys)
        {
            var connection = _connectionManager.GetConnection();
            return connection.GetDatabase(_db).KeyDeleteAsync(keys.Select(key => (RedisKey) key).ToArray());
        }

        /// <summary>
        /// Gets all the keys stored in the cache that are against the given tag
        /// </summary>
        /// <param name="tag">The tag to use as the query parameter</param>
        /// <returns>A collection of the keys found</returns>
        public async Task<IEnumerable<string>> GetKeysForTag(string tag)
        {
            var connection = _connectionManager.GetConnection();
            var setMembers = await connection.GetDatabase(_db).SetMembersAsync(TagKeysListKey(tag));
            return setMembers.Select(r => !r.IsNullOrEmpty ? r.ToString() : null);
        }

        /// <summary>
        /// Adds the given key to the given tags
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="tags">The tags to add the key to</param>
        public async Task<bool> AddKeyToTags(string key, IEnumerable<string> tags)
        {
            var enumeratedTags = tags as string[] ?? tags.ToArray();
            if (key == null || enumeratedTags.IsNullOrEmpty()) 
                return true;
            
            var connection = _connectionManager.GetConnection();
            var transaction = connection.GetDatabase(_db).CreateTransaction();

            foreach (var tag in enumeratedTags)
            {
#pragma warning disable 4014
                // Don't await this, the task will get executed when the transaction is executed
                transaction.SetAddAsync(TagKeysListKey(tag), key);
#pragma warning restore 4014
            }
                
            await transaction.ExecuteAsync();

            return true;
        }

        /// <summary>
        /// Removes the given key from the given tags
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <param name="tags">The tags to remove the key from</param>
        public async Task<bool> RemoveKeyFromTags(string key, IEnumerable<string> tags)
        {
            var enumeratedTags = tags as string[] ?? tags.ToArray();
            if (enumeratedTags.IsNullOrEmpty()) 
                return true;
            
            var connection = _connectionManager.GetConnection();
            var transaction = connection.GetDatabase(_db).CreateTransaction();

            foreach (var tag in enumeratedTags)
            {
#pragma warning disable 4014
                // Don't await this, the task will get executed when the transaction is executed
                transaction.SetRemoveAsync(TagKeysListKey(tag), key);
#pragma warning restore 4014
            }

            await transaction.ExecuteAsync();

            return true;
        }

        /// <summary>
        /// Sets the given tags against the given key
        /// </summary>
        /// <param name="key">The key to set the tags against</param>
        /// <param name="tags">The tags to set against the key</param>
        public async Task<bool> SetTagsForKey(string key, params string[] tags)
        {
            if (key == null) 
                return true;
            
            var connection = _connectionManager.GetConnection();
            var transaction = connection.GetDatabase(_db).CreateTransaction();
                
#pragma warning disable 4014
            // Don't await this, the task will get executed when the transaction is executed
            transaction.KeyDeleteAsync(KeyTagsListKey(key));
#pragma warning restore 4014

            if (!tags.IsNullOrEmpty())
            {
#pragma warning disable 4014
                // Don't await this, the task will get executed when the transaction is executed
                transaction.SetAddAsync(KeyTagsListKey(key), tags.Select(r => (RedisValue)r).ToArray());
            }

            await transaction.ExecuteAsync();

            return true;
        }

        /// <summary>
        /// Removes the tags against the given key
        /// </summary>
        /// <param name="key">The key to remove tags</param>
        public async Task<bool> RemoveTagsForKey(string key)
        {
            if (key == null)
                return true;

            var connection = _connectionManager.GetConnection();
            await connection.GetDatabase(_db).KeyDeleteAsync(KeyTagsListKey(key));

            return true;
        }

        /// <summary>
        /// Gets all tags associated with the given key
        /// </summary>
        /// <param name="key">The key to get tags for</param>
        public async Task<IEnumerable<string>> GetTagsForKey(string key)
        {
            var connection = _connectionManager.GetConnection();
            var result = await connection.GetDatabase(_db).SetMembersAsync(KeyTagsListKey(key));

            return result.Select(r => !r.IsNullOrEmpty ? r.ToString() : null);
        }

        /// <summary>
        /// Sets the given value to a sorted set by date
        /// </summary>
        /// <param name="setKey">The key to store the set against</param>
        /// <param name="value">The value to add to the set</param>
        /// <param name="date">The date to use to sort this item in the set</param>
        public Task<bool> SetTimeSet(string setKey, string value, DateTime date)
        {
            var connection = _connectionManager.GetConnection();
            return connection.GetDatabase(_db).SortedSetAddAsync(setKey, value, Helpers.TimeToRank(date));
        }

        /// <summary>
        /// Removes the set of keys given from the set with the given key
        /// </summary>
        /// <param name="setKey">The key of the set</param>
        /// <param name="keys">The keys to remove from the set</param>
        public async Task<bool> RemoveTimeSet(string setKey, IEnumerable<string> keys)
        {
            var connection = _connectionManager.GetConnection();
            await connection.GetDatabase(_db).SortedSetRemoveAsync(setKey, keys.Select(k => (RedisValue) k).ToArray());

            return true;
        }

        /// <summary>
        /// Gets the keys from the set with the given key
        /// </summary>
        /// <param name="setKey">The key of the set</param>
        /// <param name="maxDate">The date at which to not get keys thereafter</param>
        public async Task<IEnumerable<string>> GetFromTimeSet(string setKey, DateTime maxDate)
        {
            var connection = _connectionManager.GetConnection();
            var timeAsRank = Helpers.TimeToRank(maxDate);
            var keys = await connection.GetDatabase(_db).SortedSetRangeByScoreAsync(setKey, start: 0, stop: timeAsRank);
            return keys.Select(k => k.ToString());
        }

        /// <summary>
        /// Removes a key from the cache
        /// </summary>
        /// <param name="key">The key to remove</param>
        public Task RemoveKey(string key)
        {
            var connection = _connectionManager.GetConnection();
            return connection.GetDatabase(_db).KeyDeleteAsync(key);
        }

        /// <summary>
        /// Sets the object in the cache under the key to expire at the given time
        /// </summary>
        /// <param name="key">The key the object is stored under</param>
        /// <param name="expiry">The date and time the object will expire</param>
        /// <returns></returns>
        public Task Expire(string key, DateTime expiry)
        {
            var connection = _connectionManager.GetConnection();
            return connection.GetDatabase(_db).KeyExpireAsync(key, expiry.TimeOfDay);
        }

        private static string TagKeysListKey(string tag)
            => $"{RootName}:_cacheKeysByTag:{tag}";

        private static string KeyTagsListKey(string key)
            => $"{RootName}:_cacheTagsByKey:{key}";
    }
}