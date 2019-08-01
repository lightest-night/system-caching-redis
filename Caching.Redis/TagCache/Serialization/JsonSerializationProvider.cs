using Newtonsoft.Json;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache.Serialization
{
    public class JsonSerializationProvider : ISerializationProvider
    {
        /// <inheritdoc cref="ISerializationProvider.Deserialize{T}" />
        public T Deserialize<T>(RedisValue value) where T : class
            => JsonConvert.DeserializeObject<T>(value);

        /// <inheritdoc cref="ISerializationProvider.Serialize{T}" />
        public RedisValue Serialize<T>(T value) where T : class
            => JsonConvert.SerializeObject(value);
    }
}