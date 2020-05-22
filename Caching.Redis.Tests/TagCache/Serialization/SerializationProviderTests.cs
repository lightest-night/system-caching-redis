using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightestNight.System.Caching.Redis.TagCache.Serialization;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Serialization
{
    public abstract class SerializationProviderTests<TCacheItem>
        where TCacheItem : CacheItem<TestObject>, new()
    {
        private readonly ITestOutputHelper _output;

        protected SerializationProviderTests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        protected abstract ISerializationProvider GetSerializer();

        private TCacheItem CreateTestObject()
        {
            var value = new TCacheItem
            {
                Expiry = DateTime.UtcNow.AddMinutes(10),
                Key = $"{GetSerializer().GetType().Name}Tests.Key",
                Value = new TestObject
                {
                    Property1 = "Property 1",
                    Property2 = "Property 2",
                    Property3 = 100
                },
                Tags = new []{"tag1", "tag2", "tag3"}
            };
            
            for (var i = 0; i < 20; i++)
                value.Value.List.Add(Guid.NewGuid().ToString());

            return value;
        }

        [Fact]
        public void ShouldSerializeSuccessfully()
        {
            // Arrange
            var value = CreateTestObject();
            var serializer = GetSerializer();
            
            // Act
            var result = serializer.Serialize(value);
            
            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        public void ShouldDeserializeToRedisCacheItemSuccessfully()
        {
            // Arrange
            var value = CreateTestObject();
            var serializer = GetSerializer();
            var serialized = serializer.Serialize(value);
            
            // Act
            var result = serializer.Deserialize<TCacheItem>(serialized);
            
            // Assert
            result.ShouldNotBeNull();
            result.Expiry.ShouldBe(value.Expiry);
            result.Key.ShouldBe(value.Key);
            result.Value.ShouldNotBeNull();
            result.Value.Property1.ShouldBe(value.Value.Property1);
            result.Value.Property2.ShouldBe(value.Value.Property2);
            result.Value.Property3.ShouldBe(value.Value.Property3);
            (result.Tags ?? Array.Empty<string>()).Length.ShouldBe((value.Tags ?? Array.Empty<string>()).Length);
            result.Tags.Except(value.Tags).Any().ShouldBeFalse();
        }

        [Trait("Category", "Benchmarks")]
        [Fact]
        public void BenchmarkSerialize()
        {
            const int count = 1000;
            var value = CreateTestObject();
            var serializer = GetSerializer();
            // ReSharper disable once NotAccessedVariable - this is to warm up
            var serialized = serializer.Serialize(value);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < count; i++)
                // ReSharper disable once RedundantAssignment
                serialized = serializer.Serialize(value);
            stopwatch.Stop();
            
            _output.WriteLine("{0} items serialized in {1}ms = {2}ms/item using {3}", count, stopwatch.ElapsedMilliseconds, (double) stopwatch.ElapsedMilliseconds / count, serializer.GetType().Name);
        }
        
        [Trait("Category", "Benchmarks")]
        [Fact]
        public void BenchmarkDeserialize()
        {
            const int count = 1000;
            var value = CreateTestObject();
            var serializer = GetSerializer();
            // ReSharper disable once NotAccessedVariable - this is to warm up
            var serialized = serializer.Serialize(value);
            // ReSharper disable once NotAccessedVariable - this is to warm up
            var deserialized = serializer.Deserialize<TCacheItem>(serialized);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < count; i++)
                // ReSharper disable once RedundantAssignment
                deserialized = serializer.Deserialize<TCacheItem>(serialized);
            stopwatch.Stop();
            
            _output.WriteLine("{0} items deserialized in {1}ms = {2}ms/item using {3}", count, stopwatch.ElapsedMilliseconds, (double) stopwatch.ElapsedMilliseconds / count, serializer.GetType().Name);
        }
    }
}