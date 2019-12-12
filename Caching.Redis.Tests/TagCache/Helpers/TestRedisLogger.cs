using System;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Helpers
{
    public class TestRedisLogger : IRedisCacheLogger
    {
        public Task Log(string method, string arg, string? message)
        {
            Console.WriteLine("RedisLog> {0}({1}) : {2}", method, arg, message);
            return Task.CompletedTask;
        }
    }
}