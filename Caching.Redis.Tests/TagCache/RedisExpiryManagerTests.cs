using System;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Expiry;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using Shouldly;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [Collection(nameof(TestCollection))]
    public class RedisExpiryManagerTests
    {
        private readonly string _setKey;
        private readonly RedisClient _redisClient;
        private readonly RedisExpiryProvider _sut;

        private readonly TestFixture _fixture;

        public RedisExpiryManagerTests(TestFixture fixture)
        {
            _redisClient = fixture.RedisClient;
            _sut = new RedisExpiryProvider(new CacheConfiguration(fixture.RedisConnectionManager));

            _setKey = _sut.SetKey;

            _fixture = fixture;
        }

        [Fact]
        public async Task Should_Set_Key_Expiry_Successfully()
        {
            // Arrange
            await _redisClient.Remove(_setKey);
            var key = _fixture.FormatKey("expiringkey");
            
            // Act & Assert
            Should.NotThrow(() => _sut.SetKeyExpiry(_redisClient, key, DateTime.Now.AddYears(-1)));
        }

        [Fact]
        public async Task Should_Get_Expired_Keys_Less_Than_MaxDate()
        {
            // Arrange
            await _redisClient.Remove(_setKey);
            var key1 = _fixture.FormatKey("expiringkey.1");
            var key2 = _fixture.FormatKey("expiringkey.2");
            var key3 = _fixture.FormatKey("expiringkey.3");

            var minus10Date = DateTime.Now.AddYears(-1).AddMinutes(-10);
            var minus20Date = DateTime.Now.AddYears(-1).AddMinutes(-20);
            var minus30Date = DateTime.Now.AddYears(-1).AddMinutes(-30);

            await Task.WhenAll(_sut.SetKeyExpiry(_redisClient, key1, minus10Date), _sut.SetKeyExpiry(_redisClient, key2, minus20Date), _sut.SetKeyExpiry(_redisClient, key3, minus30Date));
            
            // Act
            var result = (await _sut.GetExpiredKeys(_redisClient, minus20Date))?.ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result?.Length.ShouldBe(2);
            result.ShouldNotContain(key1, $"{key1} should not exist");
            result.ShouldContain(key2, $"{key2} should exist");
            result.ShouldContain(key3, $"{key3} should exist");
        }

        [Fact]
        public async Task Should_Remove_Expired_Items()
        {
            // Arrange
            await _redisClient.Remove(_setKey);
            var key1 = _fixture.FormatKey("expiringkey.1");
            var key2 = _fixture.FormatKey("expiringkey.2");
            var key3 = _fixture.FormatKey("expiringkey.3");

            await Task.WhenAll(_sut.SetKeyExpiry(_redisClient, key1, DateTime.Today.AddYears(-5)),
                _sut.SetKeyExpiry(_redisClient, key2, DateTime.Today.AddYears(-2)),
                _sut.SetKeyExpiry(_redisClient, key3, DateTime.Today.AddYears(1)));

            var keys = (await _sut.GetExpiredKeys(_redisClient, DateTime.Today.AddYears(1).AddMonths(6))).ToArray();
            keys.ShouldNotBeNull();
            keys.Length.ShouldBe(3);
            keys.ShouldContain(key1, $"{key1} should exist");
            keys.ShouldContain(key2, $"{key2} should exist");
            keys.ShouldContain(key3, $"{key3} should exist");

            // Act
            await _sut.RemoveKeyExpiry(_redisClient, keys);
            var result = (await _sut.GetExpiredKeys(_redisClient, DateTime.Today.AddYears(1).AddMonths(6)))?.ToArray();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }
}