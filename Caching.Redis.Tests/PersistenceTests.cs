using System;
using System.Collections.Generic;
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
        
        private readonly Mock<Set> _setMock = new Mock<Set>();
        private readonly Mock<Get> _getMock = new Mock<Get>();
        private readonly Mock<GetByTag> _getByTagMock = new Mock<GetByTag>();
        private readonly Mock<Remove> _removeMock = new Mock<Remove>();
        private readonly Cache _sut;

        public PersistenceTests()
        {
            _sut = new Cache(_setMock.Object, _getMock.Object, _getByTagMock.Object, _removeMock.Object);
        }

        [Fact]
        public async Task Should_Get_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            _getMock.Setup(get => get(It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(new TestObject()));
            
            // Act
            await _sut.Get<object>(cacheKey);
            
            // Assert
            _getMock.Verify(get => get(It.Is<string>(key => key.Contains(cacheKey))), Times.Once);
        }

        [Fact]
        public async Task Should_Save_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save(cacheKey, new { });
            
            // Assert
            _setMock.Verify(set => set(It.Is<string>(key => key.Contains(cacheKey)), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string[]>()), Times.Once);
        }
        
        [Fact]
        public async Task Should_Not_Save_Null_Object()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Save<object>(cacheKey, null);
            
            // Assert
            _setMock.Verify(set => set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string[]>()), Times.Never);
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
            _setMock.Verify(set => set(It.Is<string>(key => key.Contains(cacheKey)), It.IsAny<string>(), expiry, It.IsAny<string[]>()), Times.Once);
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
            _setMock.Verify(set => set(It.Is<string>(key => key.Contains(cacheKey)), It.IsAny<string>(), It.IsAny<DateTime?>(), tags), Times.Once);
        }
        
        [Fact]
        public async Task Should_Delete_Item_Under_Key()
        {
            // Arrange
            const string cacheKey = "TestCacheKey";
            
            // Act
            await _sut.Delete<object>(cacheKey);
            
            // Assert
            _removeMock.Verify(remove => remove(It.Is<string>(key => key.Contains(cacheKey))), Times.Once);
        }

        [Fact]
        public async Task Should_Get_Items_Under_Tag()
        {
            // Arrange
            const string tag = "TestTag";
            _getByTagMock.Setup(x => x(It.IsAny<string>())).ReturnsAsync(new List<string> {JsonConvert.SerializeObject(new TestObject())});
            
            // Act
            await _sut.GetByTag<TestObject>(tag);
            
            // Assert
            _getByTagMock.Verify(getByTag => getByTag(tag), Times.Once);
        }
    }
}