using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using Shouldly;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    public class ExpiryTests
    {
        private readonly RedisCacheProvider _sut;
        private readonly CacheConfiguration _config;
        
        public ExpiryTests()
        {
            var redis = new RedisConnectionManager(ConnectionHelper.IntegrationTestHost, ConnectionHelper.Port, password: ConnectionHelper.Password, useSsl: ConnectionHelper.UseSsl);
            _config = new CacheConfiguration(redis);
            _sut = new RedisCacheProvider(_config)
            {
                Logger = new TestRedisLogger()
            };
        }
        
        [Fact]
        public async Task Should_Remove_Expired_Item_From_Cache()
        {
            // Arrange
            var key = $"TagCacheTests:{nameof(Should_Remove_Expired_Item_From_Cache)}";
            const string value = "Test Value";
            var expires = DateTime.Now.AddSeconds(3);

            const string tag1 = "tag1";
            const string tag2 = "tag2";

            await _sut.Set(key, value, expires, tag1, tag2);
            
            var result = await _sut.Get<string>(key);
            result.ShouldNotBeNull();
            result.ShouldBe(value);
            
            Thread.Sleep(1000);

            result = await _sut.Get<string>(key);
            result.ShouldNotBeNull();
            result.ShouldBe(value);
            
            // Act
            Thread.Sleep(2500);
            await _sut.RemoveExpiredKeys();
            Thread.Sleep(500);
            result = await _sut.Get<string>(key);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Remove_Expired_Tags_From_Cache()
        {
            // Arrange
            var client = new RedisClient(_config.RedisClientConfiguration.RedisConnectionManager);
            
            var key = $"TagCacheTests:{nameof(Should_Remove_Expired_Tags_From_Cache)}";
            const string value = "Test Value";
            var expires = DateTime.Now.AddSeconds(5);
            
            const string tag1 = "tag1001";
            const string tag2 = "tag1002";

            await _sut.Set(key, value, expires, tag1, tag2);
            
            // Check everything has been setup correctly
            var result = await _sut.Get<string>(key);
            result.ShouldNotBeNull();
            result.ShouldBe(value);

            var keysForTag1 = (await client.GetKeysForTag(tag1)).ToArray();
            keysForTag1.ShouldNotBeNull();
            keysForTag1.Any(k => k == key).ShouldBeTrue();

            var tagsForKey = (await client.GetTagsForKey(key)).ToArray();
            tagsForKey.ShouldNotBeNull();
            tagsForKey.Any(t => t == tag1).ShouldBeTrue();
            tagsForKey.Any(t => t == tag2).ShouldBeTrue();
            
            // 40 winks
            Thread.Sleep(1000);

            // Check it hasn't expired already
            await _sut.RemoveExpiredKeys();
            Thread.Sleep(500);
            result = await _sut.Get<string>(key);
            result.ShouldNotBeNull();
            result.ShouldBe(value);

            keysForTag1 = (await client.GetKeysForTag(tag1)).ToArray();
            keysForTag1.ShouldNotBeNull();
            keysForTag1.Any(k => k == key).ShouldBeTrue();

            tagsForKey = (await client.GetTagsForKey(key)).ToArray();
            tagsForKey.ShouldNotBeNull();
            tagsForKey.Any(t => t == tag1).ShouldBeTrue();
            tagsForKey.Any(t => t == tag2).ShouldBeTrue();
            
            // Act
            // 40 more winks - it'll expire in this time
            Thread.Sleep(4500);
            await _sut.RemoveExpiredKeys();
            Thread.Sleep(500);
            result = await _sut.Get<string>(key);
            
            // Assert
            result.ShouldBeNull();

            keysForTag1 = (await client.GetKeysForTag(tag1)).ToArray();
            keysForTag1.ShouldNotBeNull();
            keysForTag1.Any(k => k == key).ShouldBeFalse();

            tagsForKey = (await client.GetTagsForKey(key)).ToArray();
            tagsForKey.ShouldNotBeNull();
            tagsForKey.Any(t => t == tag1).ShouldBeFalse();
            tagsForKey.Any(t => t == tag2).ShouldBeFalse();
        }
    }
}