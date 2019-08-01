using System;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache.Expiry
{
    public class RedisExpiryHandler
    {
        private readonly CacheConfiguration _configuration;
        private ISubscriber _subscriber;
        
        /// <summary>
        /// The date & time an item was last expired
        /// </summary>
        public DateTime LastExpiredDate { get; set; }
        
        /// <summary>
        /// The method used to remove an item from the cache
        /// </summary>
        public Func<string, Task> RemoveMethod { get; set; }
        
        /// <summary>
        /// The method used to log an entry
        /// </summary>
        public Func<string, string, string, Task> LogMethod { get; set; }

        public RedisExpiryHandler(CacheConfiguration cacheConfiguration)
        {
            _configuration = cacheConfiguration;
            SubscribeToExpiryEvents();
        }

        private void SubscribeToExpiryEvents()
        {
            _subscriber = new RedisSubscriberConnectionManager(_configuration.RedisClientConfiguration.RedisConnectionManager).GetConnection();
            _subscriber.Subscribe(new RedisChannel("*:expired", RedisChannel.PatternMode.Pattern), SubscriberMessageReceived);
        }

        private void SubscriberMessageReceived(RedisChannel redisChannel, RedisValue redisValue)
        {
            if (!redisChannel.ToString().EndsWith("expired")) 
                return;
            
            var key = Encoding.UTF8.GetString(redisValue);

            LogMethod("Expired", key, null);
            RemoveMethod(key);
        }
    }
}