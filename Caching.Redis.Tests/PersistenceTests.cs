using System;
using System.Threading.Tasks;
using LightestNight.System.Caching.Redis.TagCache;
using Moq;
using Xunit;

namespace LightestNight.System.Caching.Redis.Tests
{
    public class PersistenceTests
    {
        private class TestObject
        {
            private string TestProperty { get; } = "Test";

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                return ((TestObject) obj).TestProperty == TestProperty;
            }

            public override int GetHashCode()
            {
                return (TestProperty != null ? TestProperty.GetHashCode() : 0);
            }
        }
        
        private readonly Mock<IRedisCacheProvider> _redisCacheProviderMock = new Mock<IRedisCacheProvider>();
        private readonly ICache _sut;

        public PersistenceTests()
        {
            _sut = new Cache(_redisCacheProviderMock.Object);
        }

        [Fact]
        public async Task Should_Get_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _redisCacheProviderMock.Setup(x => x.Get<object>(It.IsAny<string>())).ReturnsAsync(new object());
            
            // Act
            await _sut.Get<object>(cacheKey);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.Get<object>(It.Is<string>(key => key.Contains(cacheKey))), Times.Once);
        }

        [Fact]
        public async Task Should_Save_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save(cacheKey, new { });
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.Set(It.Is<string>(key => key.Contains(cacheKey)), It.IsAny<object>(), It.IsAny<DateTime?>(), It.IsAny<string[]>()), Times.Once);
        }
        
        [Fact]
        public async Task Should_Not_Save_Null_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save<object>(cacheKey, null);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task Should_Save_With_Expiry_Date()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            var expiry = DateTime.Now.AddMinutes(30);

            // Act
            await _sut.Save(cacheKey, new TestObject(), expiry);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.Set(It.Is<string>(key => key.Contains(cacheKey)), It.IsAny<object>(), expiry, It.IsAny<string[]>()), Times.Once);
        }
        
        [Fact]
        public async Task Should_Save_With_Tags()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            var tags = new[] {"tag1", "tag2"};

            // Act
            await _sut.Save(cacheKey, new TestObject(), tags: tags);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.Set(It.Is<string>(key => key.Contains(cacheKey)), It.IsAny<object>(), It.IsAny<DateTime?>(), tags), Times.Once);
        }
        
        [Fact]
        public async Task Should_Delete_Item_Under_Key()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Delete<object>(cacheKey);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.Remove(It.Is<string>(key => key.Contains(cacheKey))), Times.Once);
        }

        [Fact]
        public async Task Should_Get_Items_Under_Tag()
        {
            // Arrange
            const string tag = "TestTag";
            _redisCacheProviderMock.Setup(x => x.GetByTag<TestObject>(It.IsAny<string>())).ReturnsAsync(new[] {new TestObject()});
            
            // Act
            await _sut.GetByTag<TestObject>(tag);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.GetByTag<TestObject>(tag), Times.Once);
        }
    }
}