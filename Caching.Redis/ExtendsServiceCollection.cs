using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis
{
    public static class ExtendsServiceCollection
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<ConfigurationOptions> optionsAction = null)
        {
            services.AddOptions<ConfigurationOptions>().Configure(optionsAction);
            services.TryAddSingleton(typeof(RedisConnectionFactory));
            if (services.BuildServiceProvider().GetService<Cache>() == null)
                services.AddTransient(serviceProvider =>
                    new Cache(() => serviceProvider.GetRequiredService<RedisConnectionFactory>().Connection.GetDatabase())
                );

            return services;
        }
    }
}