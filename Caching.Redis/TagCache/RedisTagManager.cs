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
        public async Task<IEnumerable<string>> GetKeysForTag(RedisClient client, string tag)
        {
            var keys = (await client.GetKeysForTag(tag)).ToArray();
            return keys.IsNullOrEmpty() ? null : keys;
        }

        /// <summary>
        /// Retrieves all tags for the given key
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="key">They key to get the tags for</param>
        /// <returns>A collection of cache tags; null if none found</returns>
        public async Task<IEnumerable<string>> GetTagsForKey(RedisClient client, string key)
        {
            var tags = (await client.GetTagsForKey(key)).ToArray();
            return tags.IsNullOrEmpty() ? null : tags;
        }

        /// <summary>
        /// Updates the tags for an instance of <see cref="IRedisCacheItem" />
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="cacheItem">The <see cref="IRedisCacheItem" /> to update tags upon</param>
        public Task UpdateTags(RedisClient client, IRedisCacheItem cacheItem)
            => Task.WhenAll(SetTagsForItem(client, cacheItem), AddItemToTags(client, cacheItem));

        /// <summary>
        /// Updates the tags for the given key
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="key">The key to update tags upon</param>
        /// <param name="tags">The tags to update</param>
        public Task UpdateTags(RedisClient client, string key, params string[] tags)
            => UpdateTags(client, _cacheItemFactory.Create(key, tags));

        /// <summary>
        /// Stores all tags for the given item
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="cacheItem">The cache item to set tags on</param>
        public Task SetTagsForItem(RedisClient client, IRedisCacheItem cacheItem)
        {
            if (cacheItem == null || cacheItem.Tags.IsNullOrEmpty())
                return Task.CompletedTask;

            return client.SetTagsForKey(cacheItem.Key, cacheItem.Tags.ToArray());
        }

        /// <summary>
        /// Adds the item's key to the tag list
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="cacheItem">The cache item to add the keys for</param>
        public async Task AddItemToTags(RedisClient client, IRedisCacheItem cacheItem)
        {
            if (cacheItem == null || cacheItem.Tags.IsNullOrEmpty())
                return;

            await RemoveKeyFromTags(client, cacheItem);
            await client.AddKeyToTags(cacheItem.Key, cacheItem.Tags);
        }

        /// <summary>
        /// Removes the tags from the given keys
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="keys">The keys to remove the tags from</param>
        public Task RemoveTags(RedisClient client, params string[] keys)
        {
            return keys.ForEach(async key =>
            {
                var tags = await GetTagsForKey(client, key);
                var cacheItem = _cacheItemFactory.Create(key, tags);

                await RemoveKeyFromTags(client, cacheItem);
                await RemoveTagsForItem(client, cacheItem);
            }, Environment.ProcessorCount);
        }

        /// <summary>
        /// Removes the tags from the given item
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="cacheItem">The item to remove the tags from</param>
        public Task RemoveTags(RedisClient client, IRedisCacheItem cacheItem)
            => Task.WhenAll(RemoveKeyFromTags(client, cacheItem), RemoveTagsForItem(client, cacheItem));

        /// <summary>
        /// Removes the given items key from the tag list for each tag
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="item">The cache item where to get the key from</param>
        public Task RemoveKeyFromTags(RedisClient client, IRedisCacheItem item)
        {
            if (item == null || item.Tags.IsNullOrEmpty())
                return Task.CompletedTask;

            return client.RemoveKeyFromTags(item.Key, item.Tags);
        }

        /// <summary>
        /// Clears the collection of tags for the given cache item
        /// </summary>
        /// <param name="client">The <see cref="RedisClient" /> to use to connect to Redis</param>
        /// <param name="cacheItem">The cache item to clear the tags for</param>
        /// <returns></returns>
        public Task RemoveTagsForItem(RedisClient client, IRedisCacheItem cacheItem)
        {
            if (cacheItem == null || cacheItem.Tags.IsNullOrEmpty())
                return Task.CompletedTask;

            return client.SetTagsForKey(cacheItem.Key, null);
        }
    }
}