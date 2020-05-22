using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;

namespace LightestNight.System.Caching.Redis
{
    public class RedisCache : ICache
    {
        private readonly IRedisCacheProvider _cacheProvider;

        public RedisCache(IRedisCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        /// <inheritdoc cref="ICache.Exists{TItem}" />
        public Task<bool> Exists<TItem>(string key)
            => _cacheProvider.Exists(GenerateKey<TItem>(key));

        /// <inheritdoc cref="ICache.Save{TItem}" />
        public Task Save<TItem>(string key, TItem item, DateTime? expiry = null, params string[]? tags)
            where TItem : notnull
            => _cacheProvider.SetItem(GenerateKey<TItem>(key), item, expiry, tags);

        /// <inheritdoc cref="ICache.Get{TItem}" />
        public Task<TItem> Get<TItem>(string key)
            => _cacheProvider.GetItem<TItem>(GenerateKey<TItem>(key));

        /// <inheritdoc cref="ICache.GetByTag{TItem}" />
        public Task<IEnumerable<TItem>> GetByTag<TItem>(string tag)
            => _cacheProvider.GetByTag<TItem>(tag);

        /// <inheritdoc cref="ICache.Delete{TItem}" />
        public Task Delete<TItem>(string key)
            => _cacheProvider.Remove(GenerateKey<TItem>(key));

        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}