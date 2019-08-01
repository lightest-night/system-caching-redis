using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;

namespace LightestNight.System.Caching.Redis
{
    public class Cache : ICache
    {
        private readonly IRedisCacheProvider _cacheProvider;

        public Cache(IRedisCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        /// <inheritdoc cref="ICache.Save{TItem}" />
        public async Task Save<TItem>(string key, TItem item, DateTime? expiry = default, params string[] tags)
        {
            if (item != null)
            {
                var cacheKey = GenerateKey<TItem>(key);
                await _cacheProvider.Set(cacheKey, item, expiry, tags);
            }
        }
        
        /// <inheritdoc cref="ICache.Get{TItem}" />
        public async Task<TItem> Get<TItem>(string key)
        {
            var cacheItem = await _cacheProvider.Get<TItem>(GenerateKey<TItem>(key));
            return cacheItem;
        }

        /// <inheritdoc cref="ICache.GetByTag{TItem}" />
        public Task<IEnumerable<TItem>> GetByTag<TItem>(string tag)
            => _cacheProvider.GetByTag<TItem>(tag);

        /// <inheritdoc cref="ICache.Delete{TItem}" />
        public async Task Delete<TItem>(string key)
            => await _cacheProvider.Remove(GenerateKey<TItem>(key));

        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}