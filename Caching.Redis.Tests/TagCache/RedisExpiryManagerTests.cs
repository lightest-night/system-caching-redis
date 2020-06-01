using System;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Expiry;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using LightestNight.System.Utilities.Extensions;
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
            _redisClient = fixture.ThrowIfNull(nameof(fixture)).RedisClient;
            _sut = new RedisExpiryProvider(new CacheConfiguration(fixture.RedisConnectionManager));

            _setKey = _sut.SetKey;

            _fixture = fixture;
        }

        [Fact]
        public async Task ShouldSetKeyExpirySuccessfully()
        {
            // Arrange
            await _redisClient.Remove(_setKey).ConfigureAwait(false);
            var key = _fixture.FormatKey("expiringkey");
            
            // Act & Assert
            Should.NotThrow(() => _sut.SetKeyExpiry(_redisClient, key, DateTime.Now.AddYears(-1)));
        }

        [Fact]
        public async Task ShouldGetExpiredKeysLessThanMaxDate()
        {
            // Arrange
            await _redisClient.Remove(_setKey).ConfigureAwait(false);
            var key1 = _fixture.FormatKey("expiringkey.1");
            var key2 = _fixture.FormatKey("expiringkey.2");
            var key3 = _fixture.FormatKey("expiringkey.3");

            var minus10Date = DateTime.Now.AddYears(-1).AddMinutes(-10);
            var minus20Date = DateTime.Now.AddYears(-1).AddMinutes(-20);
            var minus30Date = DateTime.Now.AddYears(-1).AddMinutes(-30);

            await Task.WhenAll(_sut.SetKeyExpiry(_redisClient, key1, minus10Date),
                    _sut.SetKeyExpiry(_redisClient, key2, minus20Date),
                    _sut.SetKeyExpiry(_redisClient, key3, minus30Date))
                .ConfigureAwait(false);
            
            // Act
            var result = (await _sut.GetExpiredKeys(_redisClient, minus20Date).ConfigureAwait(false))?.ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result?.Length.ShouldBe(2);
            result.ShouldNotContain(key1, $"{key1} should not exist");
            result.ShouldContain(key2, $"{key2} should exist");
            result.ShouldContain(key3, $"{key3} should exist");
        }

        [Fact]
        public async Task ShouldRemoveExpiredItems()
        {
            // Arrange
            await _redisClient.Remove(_setKey).ConfigureAwait(false);
            var key1 = _fixture.FormatKey("expiringkey.1");
            var key2 = _fixture.FormatKey("expiringkey.2");
            var key3 = _fixture.FormatKey("expiringkey.3");

            await Task.WhenAll(_sut.SetKeyExpiry(_redisClient, key1, DateTime.Today.AddYears(-5)),
                _sut.SetKeyExpiry(_redisClient, key2, DateTime.Today.AddYears(-2)),
                _sut.SetKeyExpiry(_redisClient, key3, DateTime.Today.AddYears(1))).ConfigureAwait(false);

            var keys = (await _sut.GetExpiredKeys(_redisClient, DateTime.Today.AddYears(1).AddMonths(6)).ConfigureAwait(false)).ToArray();
            keys.ShouldNotBeNull();
            keys.Length.ShouldBe(3);
            keys.ShouldContain(key1, $"{key1} should exist");
            keys.ShouldContain(key2, $"{key2} should exist");
            keys.ShouldContain(key3, $"{key3} should exist");

            // Act
            await _sut.RemoveKeyExpiry(_redisClient, keys).ConfigureAwait(false);
            var result = (await _sut.GetExpiredKeys(_redisClient, DateTime.Today.AddYears(1).AddMonths(6)).ConfigureAwait(false))?.ToArray();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }
}