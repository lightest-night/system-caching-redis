using System;
using LightestNight.System.Caching.Redis.TagCache;
using Microsoft.Extensions.DependencyInjection;

namespace LightestNight.System.Caching.Redis
{
    public static class ExtendsServiceCollection
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisConnectionManager> connectionAction = null)
        {
            if (services.BuildServiceProvider().GetService<IRedisCacheProvider>() != null)
                return services;
            
            var connectionManager = new RedisConnectionManager();
            connectionAction?.Invoke(connectionManager);

            return services.AddSingleton<IRedisCacheProvider>(_ => new RedisCacheProvider(connectionManager))
                .AddSingleton(typeof(ICache), typeof(Cache));
        }
    }
}