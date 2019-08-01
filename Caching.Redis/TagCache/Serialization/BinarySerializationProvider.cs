using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache.Serialization
{
    public class BinarySerializationProvider : ISerializationProvider
    {
        private readonly BinaryFormatter _formatter = new BinaryFormatter();
        
        /// <inheritdoc cref="ISerializationProvider.Deserialize{T}" />
        public T Deserialize<T>(RedisValue value) where T : class
        {
            using (var memoryStream = new MemoryStream(value))
                return (T) _formatter.Deserialize(memoryStream);
        }

        /// <inheritdoc cref="ISerializationProvider.Serialize{T}" />
        public RedisValue Serialize<T>(T value) where T : class
        {
            using (var memoryStream = new MemoryStream())
            {
                _formatter.Serialize(memoryStream, value);
                return memoryStream.ToArray();
            }
        }
    }
}