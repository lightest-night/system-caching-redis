using LightestNight.System.Caching.Redis.TagCache.CacheItem;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using Xunit.Abstractions;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Serialization
{
    public class XmlSerializationProviderTests : SerializationProviderTests<RedisCacheItem<TestObject>>
    {
        public XmlSerializationProviderTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override ISerializationProvider GetSerializer()
            => new XmlSerializationProvider();
    }
}