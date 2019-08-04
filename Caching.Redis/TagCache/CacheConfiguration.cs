using LightestNight.System.Caching.Redis.TagCache.CacheItem;
using LightestNight.System.Caching.Redis.TagCache.Serialization;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class CacheConfiguration
    {
        private const string DefaultRootNamespace = "_redisCache";

        /// <summary>
        /// How many minutes after expiry should the system wait before removing the item
        /// </summary>
        internal const int MinutesToRemoveAfterExpiry = 15;
        
        /// <summary>
        /// The current client configuration used by Redis
        /// </summary>
        public RedisClientConfiguration RedisClientConfiguration { get; set; }

        /// <summary>
        /// The Root Namespace in Redis to use
        /// </summary>
        public string RootNamespace { get; set; } = DefaultRootNamespace;

        /// <summary>
        /// The <see cref="ISerializationProvider" /> to use when serializing data for Redis
        /// </summary>
        public ISerializationProvider Serializer { get; set; } = new JsonSerializationProvider();
        
        /// <summary>
        /// The <see cref="IRedisCacheItemFactory" /> to use
        /// </summary>
        public IRedisCacheItemFactory CacheItemFactory { get; set; } = new RedisCacheItemFactory();

        public CacheConfiguration(RedisConnectionManager connectionManager)
        {
            RedisClientConfiguration = new RedisClientConfiguration(connectionManager);
        }
    }
}