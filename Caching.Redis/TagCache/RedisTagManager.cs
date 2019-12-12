using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache.CacheItem;
using LightestNight.System.Utilities.Extensions;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisTagManager
    {
        private readonly IRedisCacheItemFactory _cacheItemFactory;

        public RedisTagManager(IRedisCacheItemFactory cacheItemFactory)
        {
            _cacheItemFactory = cacheItemFactory;
        }

        /// <summary>
        /// Retrieves all keys from the tag lists for the given tag
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="tag">The tag to get the keys for</param>
        /// <returns>A collection of cache keys; null if none found</returns>
        public static async Task<IEnumerable<string>?> GetKeysForTag(RedisClient client, string tag)
        {
            var keys = (await client.GetKeysForTag(tag)).ToArray();
            return keys.IsNullOrEmpty() ? null : keys;
        }
        
        /// <summary>
        /// Removes the tags from the given item
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="cacheItem">The item to remove the tags from</param>
        public static Task RemoveTags(RedisClient client, RedisCacheItem cacheItem)
            => Task.WhenAll(RemoveKeyFromTags(client, cacheItem), RemoveTagsForItem(client, cacheItem));

        /// <summary>
        /// Updates the tags for the given key
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="key">The key to update tags upon</param>
        /// <param name="tags">The tags to update</param>
        public Task UpdateTags(RedisClient client, string key, params string[] tags)
            => UpdateTags(client, _cacheItemFactory.Create(key, tags));

        /// <summary>
        /// Removes the tags from the given keys
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="keys">The keys to remove the tags from</param>
        public Task RemoveTags(RedisClient client, params string[] keys)
        {
            return keys.ForEach(async key =>
            {
                var tags = (await GetTagsForKey(client, key))?.ToArray();
                var cacheItem = _cacheItemFactory.Create(key, tags);

                await RemoveKeyFromTags(client, cacheItem);
                await RemoveTagsForItem(client, cacheItem);
            }, Environment.ProcessorCount);
        }

        private static Task RemoveKeyFromTags(RedisClient client, RedisCacheItem item)
        {
            if (item == null || item.Tags.IsNullOrEmpty())
                return Task.CompletedTask;

            return client.RemoveKeyFromTags(item.Key, item.Tags);
        }

        private static Task RemoveTagsForItem(RedisClient client, RedisCacheItem cacheItem)
        {
            if (cacheItem == null || cacheItem.Tags.IsNullOrEmpty())
                return Task.CompletedTask;

            return client.SetTagsForKey(cacheItem.Key, null);
        }
        
        private static async Task<IEnumerable<string>?> GetTagsForKey(RedisClient client, string key)
        {
            var tags = (await client.GetTagsForKey(key)).ToArray();
            return tags.IsNullOrEmpty() ? null : tags;
        }

        private static Task UpdateTags(RedisClient client, RedisCacheItem cacheItem)
            => Task.WhenAll(SetTagsForItem(client, cacheItem), AddItemToTags(client, cacheItem));
        
        private static Task SetTagsForItem(RedisClient client, RedisCacheItem cacheItem)
        {
            if (cacheItem == null || cacheItem.Tags.IsNullOrEmpty())
                return Task.CompletedTask;

            return client.SetTagsForKey(cacheItem.Key, cacheItem.Tags.ToArray());
        }

        private static async Task AddItemToTags(RedisClient client, RedisCacheItem cacheItem)
        {
            if (cacheItem == null || cacheItem.Tags.IsNullOrEmpty())
                return;

            await RemoveKeyFromTags(client, cacheItem);
            await client.AddKeyToTags(cacheItem.Key, cacheItem.Tags!);
        }
    }
}