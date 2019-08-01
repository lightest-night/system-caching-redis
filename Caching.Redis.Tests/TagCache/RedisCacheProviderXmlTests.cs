using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Serialization;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    public class RedisCacheProviderXmlTests : RedisCacheProviderTests
    {
        protected override CacheConfiguration BuildCacheConfiguration(RedisConnectionManager connection)
            => new CacheConfiguration(connection)
            {
                Serializer = new XmlSerializationProvider()
            };
    }
}