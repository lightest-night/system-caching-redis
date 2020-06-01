using System;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Utilities.Extensions;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [Collection(nameof(TestCollection))]
    public class RedisClientTests
    {
        private readonly RedisClient _sut;
        private readonly TestFixture _fixture;

        public RedisClientTests(TestFixture fixture)
        {
            _sut = fixture.ThrowIfNull(nameof(fixture)).RedisClient;
            _fixture = fixture;
        }

        [Fact]
        public void ShouldAddSuccessfully()
        {
            // Arrange
            var key = _fixture.FormatKey("TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            
            // Act & Assert
            Should.NotThrow(async () => await _sut.Set(key, value, expiry).ConfigureAwait(false));
        }

        [Fact]
        public async Task ShouldReturnNullWhenKeyIsMissing()
        {
            // Arrange
            var key = _fixture.FormatKey("TagCacheTests:Missing");
            
            // Act
            var result = await _sut.Get(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldBe(RedisValue.Null);
        }

        [Fact]
        public async Task ShouldReturnValueWhenKeyIsFound()
        {
            // Arrange
            var key = _fixture.FormatKey("TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            await _sut.Set(key, value, expiry).ConfigureAwait(false);

            // Arrange
            var result = await _sut.Get(key).ConfigureAwait(false);
            
            // Act
            result.ShouldNotBe(RedisValue.Null);
            result.ToString().ShouldBe(value);
        }

        [Fact]
        public async Task ShouldReturnNullAfterKeyIsRemoved()
        {
            // Arrange
            var key = _fixture.FormatKey("TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            await _sut.Set(key, value, expiry).ConfigureAwait(false);
            
            // Act
            await _sut.Remove(key).ConfigureAwait(false);
            var result = await _sut.Get(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldBe(RedisValue.Null);
        }

        [Fact]
        public async Task ShouldReturnNullWhenMultipleKeysRemoved()
        {
            // Arrange
            var key1 = _fixture.FormatKey("TagCacheTests:1");
            var key2 = _fixture.FormatKey("TagCacheTests:2");
            var key3 = _fixture.FormatKey("TagCacheTests:3");
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            const string value3 = "Test Value 3";
            var expiry = DateTime.Now.AddSeconds(3);

            await Task.WhenAll(_sut.Set(key1, value1, expiry), _sut.Set(key2, value2, expiry), _sut.Set(key3, value3, expiry)).ConfigureAwait(false);
            
            // Act
            await _sut.Remove(new[] {key1, key2, key3}).ConfigureAwait(false);
            var result1 = await _sut.Get(key1).ConfigureAwait(false);
            var result2 = await _sut.Get(key2).ConfigureAwait(false);
            var result3 = await _sut.Get(key3).ConfigureAwait(false);
            
            // Assert
            result1.ShouldBe(RedisValue.Null);
            result2.ShouldBe(RedisValue.Null);
            result3.ShouldBe(RedisValue.Null);
        }
    }
}