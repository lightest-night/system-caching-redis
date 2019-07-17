using System;
using LightestNight.System.ServiceResolution;
using Microsoft.Extensions.DependencyInjection;
using TagCache.Redis;

namespace LightestNight.System.Caching.Redis
{
    public static class ExtendsServiceCollection
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisConnectionManager> connectionAction = null)
        {
            if (services.BuildServiceProvider().GetService<RedisCacheProvider>() != null)
                return services;
            
            var connectionManager = new RedisConnectionManager();
            connectionAction?.Invoke(connectionManager);

            return services.AddSingleton(_ => new RedisCacheProvider(connectionManager))
                .AddExposedDelegates()
                .AddSingleton<Cache>();
        }
    }
}