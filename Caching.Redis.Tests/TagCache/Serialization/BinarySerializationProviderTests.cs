using LightestNight.System.Caching.Redis.TagCache.Serialization;
using Xunit.Abstractions;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Serialization
{
    public class BinarySerializationProviderTests : SerializationProviderTests<CacheItem<TestObject>>
    {
        public BinarySerializationProviderTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override ISerializationProvider GetSerializer()
            => new BinarySerializationProvider();
    }
}