using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [Collection(nameof(TestCollection))]
    public class RedisCacheProviderBinaryTests : RedisCacheProviderTests
    {
        public RedisCacheProviderBinaryTests(TestFixture fixture) : base(fixture) { }

        protected override CacheConfiguration BuildCacheConfiguration(RedisConnectionManager connection)
            => new CacheConfiguration(connection)
            {
                Serializer = new BinarySerializationProvider()
            };
    }
}