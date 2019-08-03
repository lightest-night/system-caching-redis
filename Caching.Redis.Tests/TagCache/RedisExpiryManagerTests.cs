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
    public class RedisExpiryManagerTests
    {
        private readonly string _setKey;
        private readonly RedisClient _redisClient;
        private readonly RedisExpiryProvider _sut;

        public RedisExpiryManagerTests()
        {
            var redis = new RedisConnectionManager(ConnectionHelper.IntegrationTestHost, ConnectionHelper.Port, password: ConnectionHelper.Password, useSsl: ConnectionHelper.UseSsl);
            _redisClient = new RedisClient(redis);
            _sut = new RedisExpiryProvider(new CacheConfiguration(redis));

            _setKey = _sut.SetKey;
        }

        [Fact]
        public async Task Should_Set_Key_Expiry_Successfully()
        {
            // Arrange
            await _redisClient.Remove(_setKey);
            const string key = "expiringkey";
            
            // Act & Assert
            Should.NotThrow(() => _sut.SetKeyExpiry(_redisClient, key, DateTime.Now.AddYears(-1)));
        }

        [Fact]
        public async Task Should_Get_Expired_Keys_Less_Than_MaxDate()
        {
            // Arrange
            await _redisClient.Remove(_setKey);
            const string key1 = "expiringkey.1";
            const string key2 = "expiringkey.2";
            const string key3 = "expiringkey.3";

            var minus10Date = DateTime.Now.AddYears(-1).AddMinutes(-10);
            var minus20Date = DateTime.Now.AddYears(-1).AddMinutes(-20);
            var minus30Date = DateTime.Now.AddYears(-1).AddMinutes(-30);

            await Task.WhenAll(_sut.SetKeyExpiry(_redisClient, key1, minus10Date), _sut.SetKeyExpiry(_redisClient, key2, minus20Date), _sut.SetKeyExpiry(_redisClient, key3, minus30Date));
            
            // Act
            var result = (await _sut.GetExpiredKeys(_redisClient, minus20Date))?.ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(2);
            result.ShouldNotContain(key1, $"{key1} should not exist");
            result.ShouldContain(key2, $"{key2} should exist");
            result.ShouldContain(key3, $"{key3} should exist");
        }

        [Fact]
        public async Task Should_Remove_Expired_Items()
        {
            // Arrange
            await _redisClient.Remove(_setKey);
            const string key1 = "expiringkey.1";
            const string key2 = "expiringkey.2";
            const string key3 = "expiringkey.3";
            
            await Task.WhenAll(_sut.SetKeyExpiry(_redisClient, key1, new DateTime(2012, 1, 1, 12, 1, 1)), 
                _sut.SetKeyExpiry(_redisClient, key2, new DateTime(2015, 1, 1, 12, 1, 2)),
                _sut.SetKeyExpiry(_redisClient, key3, new DateTime(2020, 1, 1, 12, 1, 3)));

            var keys = (await _sut.GetExpiredKeys(_redisClient, new DateTime(2020, 1, 1, 12, 1, 5)))?.ToArray();
            keys.ShouldNotBeNull();
            keys.ShouldContain(key1, $"{key1} should exist");
            keys.ShouldContain(key2, $"{key2} should exist");
            keys.ShouldContain(key3, $"{key3} should exist");
            keys.Length.ShouldBe(3);
            
            // Act
            await _sut.RemoveKeyExpiry(_redisClient, keys);
            var result = (await _sut.GetExpiredKeys(_redisClient, new DateTime(2020, 1, 1, 12, 1, 5)))?.ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }
}