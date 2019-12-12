using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache.Expiry
{
    public class RedisExpiryHandler
    {
        /// <summary>
        /// The method used to remove an item from the cache
        /// </summary>
        public Func<string, Task>? RemoveMethod { get; set; }

        /// <summary>
        /// The method used to log an entry
        /// </summary>
        public Func<string, string, string?, Task>? LogMethod { get; set; }

        public RedisExpiryHandler(CacheConfiguration cacheConfiguration)
        {
            var subscriber = new RedisSubscriberConnectionManager(cacheConfiguration.RedisClientConfiguration.RedisConnectionManager).GetConnection();
            subscriber?.Subscribe(new RedisChannel("*:expired", RedisChannel.PatternMode.Pattern), SubscriberMessageReceived);
        }

        private void SubscriberMessageReceived(RedisChannel redisChannel, RedisValue redisValue)
        {
            if (!redisChannel.ToString().EndsWith("expired")) 
                return;
            
            var key = Encoding.UTF8.GetString(redisValue);

            LogMethod?.Invoke("Expired", key, null);
            RemoveMethod?.Invoke(key);
        }
    }
}