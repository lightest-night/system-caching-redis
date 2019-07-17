using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightestNight.System.ServiceResolution;
using Microsoft.Extensions.DependencyInjection;
using TagCache.Redis;

namespace LightestNight.System.Caching.Redis
{
    public delegate Task<bool> Set(string key, string value, DateTime? expires = default, params string[] tags);

    public delegate Task<string> Get(string key);

    public delegate Task<List<string>> GetByTag(string tag);

    public delegate Task Remove(string key);
    
    internal class CachingDelegates : DelegateExposer
    {
        public override IServiceCollection ExposeDelegates(IServiceCollection services)
        {
            return services.AddTransient<Set>(serviceProvider =>
                    (key, value, expires, tags) =>
                        serviceProvider.GetRequiredService<RedisCacheProvider>().SetAsync(key, value, expires.GetValueOrDefault(DateTime.UtcNow.AddYears(1)), tags))
                .AddTransient<Get>(serviceProvider =>
                    key =>
                        serviceProvider.GetRequiredService<RedisCacheProvider>().GetAsync<string>(key))
                .AddTransient<GetByTag>(serviceProvider =>
                    tag =>
                        serviceProvider.GetRequiredService<RedisCacheProvider>().GetByTagAsync<string>(tag))
                .AddTransient<Remove>(serviceProvider =>
                    key =>
                        serviceProvider.GetRequiredService<RedisCacheProvider>().RemoveAsync(key));
        }
    }
}