using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests
{
    public class PersistenceTests
    {
        private class TestObject
        {
            public string TestProperty { get; set; } = "Test";

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                return ((TestObject) obj).TestProperty == TestProperty;
            }

            protected bool Equals(TestObject other)
            {
                return string.Equals(TestProperty, other.TestProperty);
            }

            public override int GetHashCode()
            {
                return (TestProperty != null ? TestProperty.GetHashCode() : 0);
            }
        }
        
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

        [Fact]
        public async Task Should_Delete_Item_Under_Key()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Delete<object>(cacheKey);
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.KeyDeleteAsync(It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task Should_Add_Item_To_List()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            var testObject = new TestObject();
            
            // Act
            await _sut.AddToList(cacheKey, testObject);
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.ListRightPushAsync(It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                It.Is<RedisValue>(value => value.ToString() == JsonConvert.SerializeObject(testObject)),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task Should_Remove_Item_From_List_At_Index()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _redisDatabaseMoq.Setup(x => x.ListGetByIndexAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.EmptyString);
            
            // Act
            await _sut.RemoveFromListAt<TestObject>(cacheKey, 1);
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.ListRemoveAsync(It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                    It.IsAny<RedisValue>(),
                    It.IsAny<long>(),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_Remove_Item_From_A_List()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.RemoveFromList(cacheKey, new TestObject());
            
            // Assert
            _redisDatabaseMoq.Verify(x => x.ListRemoveAsync(It.Is<RedisKey>(key => key.ToString().Contains($":{cacheKey}")),
                    It.IsAny<RedisValue>(),
                    It.IsAny<long>(),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_Get_List()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _redisDatabaseMoq.Setup(x => x.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(1);
            _redisDatabaseMoq.Setup(x => x.ListGetByIndexAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisValue)JsonConvert.SerializeObject(new TestObject()));

            // Act
            var result = (await _sut.GetList<TestObject>(cacheKey)).ToArray();
            
            // Assert
            result.Length.ShouldBe(1);
            result[0].ShouldBe(new TestObject());
        }
    }
}