using System;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    public abstract class RedisCacheProviderTests
    {
        private const string RedisHost = "localhost";
        private const int RedisDb = 0;

        protected abstract CacheConfiguration BuildCacheConfiguration(RedisConnectionManager connection);

        [Fact]
        public void Should_Construct_Successfully()
        {
            // Arrange
            var redis = new RedisConnectionManager(RedisHost);
            var config = BuildCacheConfiguration(redis);
            config.RootNamespace = "_TestRootNamespace";
            config.Serializer = new BinarySerializationProvider();
            config.RedisClientConfiguration = new RedisClientConfiguration(config.RedisClientConfiguration.RedisConnectionManager)
            {
                Host = RedisHost,
                DbIndex = RedisDb,
                TimeoutMs = 50
            };

            // Act
            var cache = new RedisCacheProvider(config) {Logger = new TestRedisLogger()};
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";
            
            // Assert
            Should.NotThrow(async () => await cache.Set(key, value));
        }

        [Fact]
        public void Should_Construct_Unsuccessfully()
        {
            // Arrange
            var redis = new RedisConnectionManager("nohost");
            var config = BuildCacheConfiguration(redis);
            config.RedisClientConfiguration = new RedisClientConfiguration(redis)
            {
                Host = "nohost",
                TimeoutMs = 500
            };

            // Act
            var exception = Should.Throw<RedisConnectionException>(() => new RedisCacheProvider(config) {Logger = new TestRedisLogger()});

            // Assert
            exception.Message.ShouldContain("nohost");
        }

        [Fact]
        public void Set_String_Should_Succeed()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis);
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";
            
            // Act & Assert
            Should.NotThrow(async () => await cache.Set(key, value));
        }

        [Fact]
        public async Task Get_MissingKey_Should_Return_Null()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key = $"TagCacheTests:NoValueHere.{DateTime.UtcNow.Ticks}";
            
            // Act
            var result = await cache.Get<string>(key);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Return_Value_For_Valid_Key()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";
            await cache.Set(key, value);
            
            // Act
            var result = await cache.Get<string>(key);
            
            // Arrange
            result.ShouldNotBeNull();
            result.ShouldBe(value);
        }

        [Fact]
        public async Task Should_Return_ObjectValue_For_Valid_Key()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            var value = new TestObject
            {
                Property1 = "Foo",
                Property2 = "Bar",
                Property3 = 11
            };
            await cache.Set(key, value);
            
            // Act
            var result = await cache.Get<TestObject>(key);
            
            // Assert
            result.ShouldNotBeNull();
            result.Property1.ShouldBe(value.Property1);
            result.Property2.ShouldBe(value.Property2);
            result.Property3.ShouldBe(value.Property3);
        }

        [Fact]
        public async Task Should_Return_Null_When_Key_Removed()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";
            await cache.Set(key, value);
            
            // Act
            var result = await cache.Get<string>(key);
            result.ShouldBe(value);

            await cache.Remove(key);
            result = await cache.Get<string>(key);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Return_Null_When_Multiple_Keys_Removed()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis){Logger = new TestRedisLogger()};
            var key1 = $"{Guid.NewGuid()}TagCacheTests:Add.First";
            var key2 = $"{Guid.NewGuid()}TagCacheTests:Add.Second";
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";

            await Task.WhenAll(cache.Set(key1, value1), cache.Set(key2, value2));

            var result1 = await cache.Get<string>(key1);
            var result2 = await cache.Get<string>(key2);
            result1.ShouldBe(value1);
            result2.ShouldBe(value2);
            
            // Act
            await cache.Remove(key1, key2);
            result1 = await cache.Get<string>(key1);
            result2 = await cache.Get<string>(key2);
            
            // Assert
            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Return_Null_When_Getting_Expired_Item()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";
            var expiry = DateTime.UtcNow.AddYears(-1);
            await cache.Set(key, value, expiry);
            
            // Act
            var result = await cache.Get<string>(key);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Remove_Tags_When_Item_Expired()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var config = BuildCacheConfiguration(redis);
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";
            const string tag = "Remove Tag";
            var expiry = DateTime.UtcNow.AddYears(-1);
            await cache.Set(key, value, expiry, tag);

            // Act
            var tagManager = new RedisTagManager(config.CacheItemFactory);
            var result = await tagManager.GetKeysForTag(new RedisClient(redis), tag);

            // Assert
            result.Any(t => t == tag).ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Get_Single_Item_By_Tag_When_Multiple_Tags_Exist()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key = $"{Guid.NewGuid()}TagCacheTests:Add";
            const string value = "Test Value";

            var tag1 = $"{Guid.NewGuid()}_tag1";
            var tag2 = $"{Guid.NewGuid()}_tag2";
            var tag3 = $"{Guid.NewGuid()}_tag3";
            await cache.Set(key, value, tag1, tag2, tag3);

            // Act
            var result = (await cache.GetByTag<string>(tag2)).ToArray();

            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Get_Multiple_Items_By_Tag_When_Multiple_Are_Associated_With_Same_Tag()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key1 = $"{Guid.NewGuid()}TagCacheTests:Add1";
            var key2 = $"{Guid.NewGuid()}TagCacheTests:Add2";
            var key3 = $"{Guid.NewGuid()}TagCacheTests:Add3";
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            const string value3 = "Test Value 3";
            var tag = $"{Guid.NewGuid()}_tag";

            await Task.WhenAll(cache.Set(key1, value1, tag), cache.Set(key2, value2, tag), cache.Set(key3, value3, tag));
            
            // Act
            var result = (await cache.GetByTag<string>(tag)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(3);
        }

        [Fact]
        public async Task Should_Remove_All_Items_By_Tag()
        {
            // Arrange
            var redis = new RedisConnectionManager();
            var cache = new RedisCacheProvider(redis) {Logger = new TestRedisLogger()};
            var key1 = $"{Guid.NewGuid()}TagCacheTests:Add1";
            var key2 = $"{Guid.NewGuid()}TagCacheTests:Add2";
            var key3 = $"{Guid.NewGuid()}TagCacheTests:Add3";
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            const string value3 = "Test Value 3";
            var tag = $"{Guid.NewGuid()}_tag";

            await Task.WhenAll(cache.Set(key1, value1, tag), cache.Set(key2, value2, tag), cache.Set(key3, value3, tag));
            
            // Act
            var result = (await cache.GetByTag<string>(tag)).ToArray();
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();

            await cache.RemoveByTag(tag);
            result = (await cache.GetByTag<string>(tag))?.ToArray();
            
            // Assert
            result.ShouldBeEmpty();
        }
    }
}