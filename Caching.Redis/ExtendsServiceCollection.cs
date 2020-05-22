using System;
using System.Diagnostics.CodeAnalysis;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Expiry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightestNight.System.Caching.Redis
{
    public static class ExtendsServiceCollection
    {
        [SuppressMessage("ReSharper", "CA2000")]// It's disposed by the IServiceCollection
        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<CacheConfig> configAction)
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction), "Cannot add the Redis Cache if no Configuration specified");
            
            var cacheConfig = new CacheConfig();
            configAction.Invoke(cacheConfig);

            if (services.BuildServiceProvider().GetService<IRedisCacheProvider>() == null)
            {
                var connectionManager = new RedisConnectionManager(
                    cacheConfig.Host,
                    cacheConfig.Port,
                    cacheConfig.ConnectTimeout,
                    cacheConfig.Password,
                    cacheConfig.AllowAdmin,
                    cacheConfig.SyncTimeout,
                    cacheConfig.UseSsl);
                services.AddSingleton<IRedisCacheProvider>(_ => new RedisCacheProvider(connectionManager));
            }

            services.TryAddSingleton(typeof(ICache), typeof(RedisCache));

            if (cacheConfig.ManuallyProcessExpiredKeys)
                services.AddHostedService<RedisExpiryManager>();

            return services;
        }
    }
}