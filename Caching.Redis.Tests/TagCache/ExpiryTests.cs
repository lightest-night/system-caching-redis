using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using LightestNight.System.Utilities.Extensions;
using Shouldly;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [Collection(nameof(TestCollection))]
    public class ExpiryTests
    {
        private readonly RedisCacheProvider _sut;
        private readonly CacheConfiguration _config;

        private readonly TestFixture _fixture;
        
        public ExpiryTests(TestFixture fixture)
        {
            _config = new CacheConfiguration(fixture.ThrowIfNull(nameof(fixture)).RedisConnectionManager);
            _sut = new RedisCacheProvider(_config)
            {
                Logger = new TestRedisLogger()
            };

            _fixture = fixture;
        }
        
        [Fact]
        public async Task ShouldRemoveExpiredItemFromCache()
        {
            // Arrange
            await _sut.RemoveExpiredKeys().ConfigureAwait(false);
            var key = _fixture.FormatKey($"TagCacheTests:{nameof(ShouldRemoveExpiredItemFromCache)}");
            const string value = "Test Value";
            var expires = DateTime.Now.AddSeconds(3);

            const string tag1 = "tag1";
            const string tag2 = "tag2";

            await _sut.SetItem(key, value, expires, tag1, tag2).ConfigureAwait(false);
            
            var result = await _sut.GetItem<string>(key).ConfigureAwait(false);
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            
            Thread.Sleep(1000);

            result = await _sut.GetItem<string>(key).ConfigureAwait(false);
            result.ShouldNotBeNull();
            result.Value.ShouldBe(value);
            
            // Act
            Thread.Sleep(2500);
            await _sut.RemoveExpiredKeys().ConfigureAwait(false);
            Thread.Sleep(500);
            result = await _sut.GetItem<string>(key).ConfigureAwait(false);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task ShouldRemoveExpiredTagsFromCache()
        {
            // Arrange
            await _sut.RemoveExpiredKeys().ConfigureAwait(false);
            var client = new RedisClient(_config.RedisClientConfiguration.RedisConnectionManager);
            
            var key = _fixture.FormatKey($"TagCacheTests:{nameof(ShouldRemoveExpiredTagsFromCache)}");
            const string value = "Test Value";
            var expires = DateTime.Now.AddSeconds(2);
            
            const string tag1 = "tag1001";
            const string tag2 = "tag1002";

            await _sut.SetItem(key, value, expires, tag1, tag2).ConfigureAwait(false);
            
            // Forty Winks - Let the item expire
            Thread.Sleep(3500);
            
            // Act
            await _sut.RemoveExpiredKeys().ConfigureAwait(false);
            var result = await _sut.GetItem<string>(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldBeNull();

            var keysForTag1 = (await client.GetKeysForTag(tag1).ConfigureAwait(false)).ToArray();
            keysForTag1.ShouldNotBeNull();
            keysForTag1.Any(k => k == key).ShouldBeFalse();

            var tagsForKey = (await client.GetTagsForKey(key).ConfigureAwait(false)).ToArray();
            tagsForKey.ShouldNotBeNull();
            tagsForKey.Any(t => t == tag1).ShouldBeFalse();
            tagsForKey.Any(t => t == tag2).ShouldBeFalse();
        }
    }
}