using System;
using System.Linq;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using LightestNight.System.Utilities.Extensions;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [Collection(nameof(TestCollection))]
    public abstract class RedisCacheProviderTests
    {
        private readonly RedisConnectionManager _redis;
        private readonly TestFixture _fixture;
        
        private const int DatabaseIndex = 0;

        protected abstract CacheConfiguration BuildCacheConfiguration(RedisConnectionManager connection);

        protected RedisCacheProviderTests(TestFixture fixture)
        {
            _redis = fixture.ThrowIfNull().RedisConnectionManager;
            _fixture = fixture;
        }

        [Fact]
        public void ShouldConstructSuccessfully()
        {
            // Arrange
            var config = BuildCacheConfiguration(_redis);
            config.RootNamespace = "_TestRootNamespace";
            config.Serializer = new BinarySerializationProvider();
            config.RedisClientConfiguration = new RedisClientConfiguration(config.RedisClientConfiguration.RedisConnectionManager)
            {
                DbIndex = DatabaseIndex,
                TimeoutMs = 50
            };

            // Act
            var cache = new RedisCacheProvider(config) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            var expiry = DateTime.Now.AddSeconds(3);
            const string value = "Test Value";
            
            // Assert
            Should.NotThrow(async () => await cache.SetItem(key, value, expiry).ConfigureAwait(false));
        }

        [Fact]
        public void ShouldConstructUnsuccessfully()
        {
            // Arrange
            using var redis = new RedisConnectionManager("nohost");
            var config = BuildCacheConfiguration(redis);
            config.RedisClientConfiguration = new RedisClientConfiguration(redis)
            {
                TimeoutMs = 500
            };

            // Act
            var exception = Should.Throw<RedisConnectionException>(() => new RedisCacheProvider(config) {Logger = new TestRedisLogger()});

            // Assert
            exception.Message.ShouldContain("nohost");
        }

        [Fact]
        public void SetStringShouldSucceed()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(3);
            
            // Act & Assert
            Should.NotThrow(async () => await cache.SetItem(key, value, expiry).ConfigureAwait(false));
        }

        [Fact]
        public async Task GetMissingKeyShouldReturnNull()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"TagCacheTests:NoValueHere.{DateTime.Now.Ticks}");
            
            // Act
            var result = await cache.GetItem<string>(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task ShouldReturnValueForValidKey()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(30);
            await cache.SetItem(key, value, expiry).ConfigureAwait(false);
            
            // Act
            var result = await cache.GetItem<string>(key).ConfigureAwait(false);
            
            // Arrange
            result.ShouldNotBeNull();
            result.ShouldBe(value);
        }

        [Fact]
        public async Task ShouldReturnObjectValueForValidKey()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            var value = new TestObject
            {
                Property1 = "Foo",
                Property2 = "Bar",
                Property3 = 11
            };
            var expiry = DateTime.Now.AddSeconds(30);
            await cache.SetItem(key, value, expiry).ConfigureAwait(false);
            
            // Act
            var result = await cache.GetItem<TestObject>(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldNotBeNull();
            result.Property1.ShouldBe(value.Property1);
            result.Property2.ShouldBe(value.Property2);
            result.Property3.ShouldBe(value.Property3);
        }

        [Fact]
        public async Task ShouldReturnNullWhenKeyRemoved()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(30);
            await cache.SetItem(key, value, expiry).ConfigureAwait(false);
            
            // Act
            var result = await cache.GetItem<string>(key).ConfigureAwait(false);
            result.ShouldBe(value);

            await cache.Remove(key).ConfigureAwait(false);
            result = await cache.GetItem<string>(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task ShouldReturnNullWhenMultipleKeysRemoved()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key1 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add.First");
            var key2 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add.Second");
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            var expiry = DateTime.Now.AddSeconds(30);

            await Task.WhenAll(cache.SetItem(key1, value1, expiry), cache.SetItem(key2, value2, expiry))
                .ConfigureAwait(false);

            var result1 = await cache.GetItem<string>(key1).ConfigureAwait(false);
            var result2 = await cache.GetItem<string>(key2).ConfigureAwait(false);
            result1.ShouldBe(value1);
            result2.ShouldBe(value2);
            
            // Act
            await cache.Remove(key1, key2).ConfigureAwait(false);
            result1 = await cache.GetItem<string>(key1).ConfigureAwait(false);
            result2 = await cache.GetItem<string>(key2).ConfigureAwait(false);
            
            // Assert
            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        [Fact]
        public async Task ShouldReturnNullWhenGettingExpiredItem()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddYears(-1);
            await cache.SetItem(key, value, expiry).ConfigureAwait(false);
            
            // Act
            var result = await cache.GetItem<string>(key).ConfigureAwait(false);
            
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task ShouldRemoveTagsWhenItemExpired()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            const string value = "Test Value";
            const string tag = "Remove Tag";
            var expiry = DateTime.Now.AddYears(-1);
            await cache.SetItem(key, value, expiry, tag).ConfigureAwait(false);

            // Act
            var result = await RedisTagManager.GetKeysForTag(new RedisClient(_redis), tag).ConfigureAwait(false);

            // Assert
            result.Any(t => t == tag).ShouldBeFalse();
        }

        [Fact]
        public async Task ShouldGetSingleItemByTagWhenMultipleTagsExist()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add");
            const string value = "Test Value";
            var expiry = DateTime.Now.AddSeconds(30);

            var tag1 = $"{Guid.NewGuid()}_tag1";
            var tag2 = $"{Guid.NewGuid()}_tag2";
            var tag3 = $"{Guid.NewGuid()}_tag3";
            await cache.SetItem(key, value, expiry, tag1, tag2, tag3).ConfigureAwait(false);

            // Act
            var result = (await cache.GetByTag<string>(tag2).ConfigureAwait(false)).ToArray();

            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(1);
        }

        [Fact]
        public async Task ShouldGetMultipleItemsByTagWhenMultipleAreAssociatedWithSameTag()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key1 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add1");
            var key2 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add2");
            var key3 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add3");
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            const string value3 = "Test Value 3";
            var tag = $"{Guid.NewGuid()}_tag";
            var expiry = DateTime.Now.AddSeconds(30);

            await Task.WhenAll(cache.SetItem(key1, value1, expiry, tag), cache.SetItem(key2, value2, expiry, tag),
                cache.SetItem(key3, value3, expiry, tag)).ConfigureAwait(false);
            
            // Act
            var result = (await cache.GetByTag<string>(tag).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldNotBeNull();
            result.Length.ShouldBe(3);
        }

        [Fact]
        public async Task ShouldRemoveAllItemsByTag()
        {
            // Arrange
            var cache = new RedisCacheProvider(_redis) {Logger = new TestRedisLogger()};
            var key1 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add1");
            var key2 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add2");
            var key3 = _fixture.FormatKey($"{Guid.NewGuid()}TagCacheTests:Add3");
            const string value1 = "Test Value 1";
            const string value2 = "Test Value 2";
            const string value3 = "Test Value 3";
            var tag = $"{Guid.NewGuid()}_tag";
            var expiry = DateTime.Now.AddSeconds(30);

            await Task.WhenAll(cache.SetItem(key1, value1, expiry, tag), cache.SetItem(key2, value2, expiry, tag),
                cache.SetItem(key3, value3, expiry, tag)).ConfigureAwait(false);
            
            // Act
            var result = (await cache.GetByTag<string>(tag).ConfigureAwait(false)).ToArray();
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();

            await cache.RemoveByTag(tag).ConfigureAwait(false);
            result = (await cache.GetByTag<string>(tag).ConfigureAwait(false)).ToArray();
            
            // Assert
            result.ShouldBeEmpty();
        }
    }
}