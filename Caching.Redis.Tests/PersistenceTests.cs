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

            public override bool Equals(object? obj)
            {
                if (obj == null)
                    return false;

                return ((TestObject) obj).TestProperty == TestProperty;
            }

            public override int GetHashCode()
            {
                return (TestProperty != null ? TestProperty.GetHashCode(StringComparison.InvariantCultureIgnoreCase) : 0);
            }
        }
        
        private readonly Mock<IRedisCacheProvider> _redisCacheProviderMock = new Mock<IRedisCacheProvider>();
        private readonly ICache _sut;

        public PersistenceTests()
        {
            _sut = new RedisCache(_redisCacheProviderMock.Object);
        }

        [Fact]
        public async Task ShouldGetObject()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _redisCacheProviderMock.Setup(x => x.GetItem<object>(It.IsAny<string>())).ReturnsAsync(new CacheItem<object>());
            
            // Act
            await _sut.Get<object>(cacheKey).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(
                x => x.GetItem<object>(It.Is<string>(key =>
                    key.Contains(cacheKey, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public async Task ShouldSaveObject()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save(cacheKey, new { }).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(
                x => x.SetItem(
                    It.Is<string>(key => key.Contains(cacheKey, StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<object>(), It.IsAny<DateTime?>(), It.IsAny<string[]>()), Times.Once);
        }
        
        [Fact]
        public async Task ShouldNotSaveNullObject()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save<object>(cacheKey, null!).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.SetItem(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ShouldSaveWithExpiryDate()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            var expiry = DateTime.Now.AddMinutes(30);

            // Act
            await _sut.Save(cacheKey, new TestObject(), expiry).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(
                x => x.SetItem(
                    It.Is<string>(key => key.Contains(cacheKey, StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<object>(), expiry, It.IsAny<string[]>()), Times.Once);
        }
        
        [Fact]
        public async Task ShouldSaveWithTags()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            var tags = new[] {"tag1", "tag2"};

            // Act
            await _sut.Save(cacheKey, new TestObject(), tags: tags).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(
                x => x.SetItem(
                    It.Is<string>(key => key.Contains(cacheKey, StringComparison.InvariantCultureIgnoreCase)),
                    It.IsAny<object>(), It.IsAny<DateTime?>(), tags), Times.Once);
        }
        
        [Fact]
        public async Task ShouldDeleteItemUnderKey()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Delete<object>(cacheKey).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(
                x => x.Remove(It.Is<string>(key =>
                    key.Contains(cacheKey, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public async Task ShouldGetItemsUnderTag()
        {
            // Arrange
            const string tag = "TestTag";
            _redisCacheProviderMock.Setup(x => x.GetByTag<TestObject>(It.IsAny<string>())).ReturnsAsync(new[] {new CacheItem<TestObject>()});
            
            // Act
            await _sut.GetByTag<TestObject>(tag).ConfigureAwait(false);
            
            // Assert
            _redisCacheProviderMock.Verify(x => x.GetByTag<TestObject>(tag), Times.Once);
        }
    }
}