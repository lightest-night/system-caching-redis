using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Utilities.Extensions;

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

        /// <inheritdoc cref="ICache.Save{TItem}(string, TItem, DateTime?, string[])" />
        public Task Save<TItem>(string key, TItem item, DateTime? expiry = null, params string[]? tags)
            where TItem : notnull
            => _cacheProvider.SetItem(GenerateKey<TItem>(key), item, expiry, tags);

        /// <inheritdoc cref="ICache.Save{TItem}(CacheItem{TItem})"/>
        public Task Save<TItem>(CacheItem<TItem> cacheItem) where TItem : notnull
        {
            cacheItem.ThrowIfNull(nameof(cacheItem));
            
            var key = cacheItem.Key;
            if (!key.StartsWith(typeof(TItem).Name, StringComparison.InvariantCultureIgnoreCase))
                key = GenerateKey<TItem>(key);

            return _cacheProvider.SetItem(key, cacheItem.Value, cacheItem.Expiry, cacheItem.Tags);
        }

        /// <inheritdoc cref="ICache.Get{TItem}" />
        public Task<CacheItem<TItem>> Get<TItem>(string key)
            => _cacheProvider.GetItem<TItem>(GenerateKey<TItem>(key));

        /// <inheritdoc cref="ICache.GetByTag{TItem}" />
        public Task<IEnumerable<CacheItem<TItem>>> GetByTag<TItem>(string tag)
            => _cacheProvider.GetByTag<TItem>(tag);

        /// <inheritdoc cref="ICache.Delete{TItem}" />
        public Task Delete<TItem>(string key)
            => _cacheProvider.Remove(GenerateKey<TItem>(key));

        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}