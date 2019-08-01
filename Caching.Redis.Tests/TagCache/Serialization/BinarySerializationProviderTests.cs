using LightestNight.System.Caching.Redis.TagCache.CacheItem;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using Xunit.Abstractions;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Serialization
{
    public class BinarySerializationProviderTests : SerializationProviderTests<RedisCacheItem<TestObject>>
    {
        public BinarySerializationProviderTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override ISerializationProvider GetSerializer()
            => new BinarySerializationProvider();
    }
}