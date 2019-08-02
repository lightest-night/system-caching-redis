using System;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    public class RedisClientTests
    {
        private static readonly RedisConnectionManager Redis = new RedisConnectionManager(ConnectionHelper.IntegrationTestHost, ConnectionHelper.Port, password: ConnectionHelper.Password, useSsl: ConnectionHelper.UseSsl);
        private readonly RedisClient _sut = new RedisClient(Redis);

        [Fact]
        public void Should_Add_Successfully()
        {
            // Arrange
            const string key = "TagCacheTests:Add";
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            
            // Act & Assert
            Should.NotThrow(async () => await _sut.Set(key, value, expiry));
        }

        [Fact]
        public async Task Should_Return_Null_When_Key_Is_Missing()
        {
            // Arrange
            const string key = "TagCacheTests:Missing";
            
            // Act
            var result = await _sut.Get(key);
            
            // Assert
            result.ShouldBe(RedisValue.Null);
        }

        [Fact]
        public async Task Should_Return_Value_When_Key_Is_Found()
        {
            // Arrange
            const string key = "TagCacheTests:Add";
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            await _sut.Set(key, value, expiry);

            // Arrange
            var result = await _sut.Get(key);
            
            // Act
            result.ShouldNotBe(RedisValue.Null);
            result.ToString().ShouldBe(value);
        }

        [Fact]
        public async Task Should_Return_Null_After_Key_Is_Removed()
        {
            // Arrange
            const string key = "TagCacheTests:Add";
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            await _sut.Set(key, value, expiry);
            
            // Act
            await _sut.Remove(key);
            var result = await _sut.Get(key);
            
            // Assert
            result.ShouldBe(RedisValue.Null);
        }

        [Fact]
        public async Task Should_Return_Null_When_Multiple_Keys_Removed()
        {
            // Arrange
            const string key1 = "TagCacheTests:1";
            const string key2 = "TagCacheTests:2";
            const string key3 = "TagCacheTests:3";
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            const string value3 = "Test Value 3";
            var expiry = DateTime.Now.AddSeconds(3);

            await Task.WhenAll(_sut.Set(key1, value1, expiry), _sut.Set(key2, value2, expiry), _sut.Set(key3, value3, expiry));
            
            // Act
            await _sut.Remove(new[] {key1, key2, key3});
            var result1 = await _sut.Get(key1);
            var result2 = await _sut.Get(key2);
            var result3 = await _sut.Get(key3);
            
            // Assert
            result1.ShouldBe(RedisValue.Null);
            result2.ShouldBe(RedisValue.Null);
            result3.ShouldBe(RedisValue.Null);
        }
    }
}