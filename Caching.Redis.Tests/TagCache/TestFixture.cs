using LightestNight.System.Caching.Redis.TagCache;
using LightestNight.System.Caching.Redis.Tests.TagCache.Helpers;
using System;
using System.Threading.Tasks;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [CollectionDefinition(nameof(TestCollection))]
    public class TestCollection : ICollectionFixture<TestFixture> 
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class TestFixture : IAsyncLifetime
    {
        private readonly string _testGroupKey = "TESTS";

        public RedisClient RedisClient { get; }
        public RedisConnectionManager RedisConnectionManager { get; }

        public TestFixture()
        {
            RedisConnectionManager = new RedisConnectionManager(ConnectionHelper.IntegrationTestHost, ConnectionHelper.Port, password: ConnectionHelper.Password, useSsl: ConnectionHelper.UseSsl);
            RedisClient = new RedisClient(RedisConnectionManager);           
        }

        public Task InitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// Formats the given cache key with the test group so as to be easily separated from any other data
        /// </summary>
        /// <param name="key">The key to format</param>
        /// <returns>A formatted cache key</returns>
        public string FormatKey(string key) => $"{_testGroupKey}:{key}";

        public async Task DisposeAsync()
        {
            await RedisClient.RemoveKey(_testGroupKey);
            await RedisClient.RemoveExpiredKeysFromTags();
        }
    }
}
