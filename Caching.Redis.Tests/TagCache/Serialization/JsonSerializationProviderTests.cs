using LightestNight.System.Caching.Redis.TagCache.Serialization;
using Xunit.Abstractions;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Serialization
{
    public class JsonSerializationProviderTests : SerializationProviderTests<CacheItem<TestObject>>
    {
        public JsonSerializationProviderTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override ISerializationProvider GetSerializer()
            => new JsonSerializationProvider();
    }
}