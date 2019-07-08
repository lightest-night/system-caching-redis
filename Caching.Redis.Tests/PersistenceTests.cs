using System;
using System.Threading.Tasks;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests
{
    public class PersistenceTests
    {
        private readonly Mock<IDatabase> _redisDatabaseMoq = new Mock<IDatabase>();
        private readonly Cache _sut;

        public PersistenceTests()
        {
            _sut = new Cache(() => _redisDatabaseMoq.Object);
        }

        [Fact]
        public async Task Should_Get_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _redisDatabaseMoq.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.EmptyString);
            
            // Act
            await _sut.Get<object>(cacheKey);
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.StringGetAsync(
                It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_Save_New_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save(cacheKey, new {});
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.StringSetAsync(
                    It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_Update_Existing_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _redisDatabaseMoq.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.EmptyString);
            
            // Act
            await _sut.Save(cacheKey, "Test Object");
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.KeyDeleteAsync(
                It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")), 
                It.IsAny<CommandFlags>()), Times.Once);
            
            _redisDatabaseMoq.Verify(x => x.StringSetAsync(
                    It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }
    }
}