using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache.Serialization
{
    public interface ISerializationProvider
    {
        /// <summary>
        /// Deserializes the <see cref="RedisValue" /> into an instance of <typeparamref name="T" />
        /// </summary>
        /// <param name="value">The instance to deserialize</param>
        /// <typeparam name="T">The type to deserialize into</typeparam>
        /// <returns>An object of type <typeparamref name="T" /></returns>
        T Deserialize<T>(RedisValue value) where T : class;

        /// <summary>
        /// Serializes the <typeparamref name="T" /> into a <see cref="RedisValue" />
        /// </summary>
        /// <param name="value">The value to serialize</param>
        /// <typeparam name="T">The type of the object to serialize</typeparam>
        /// <returns>A <see cref="RedisValue" /> implementation of the object serialized</returns>
        RedisValue Serialize<T>(T value) where T : class;
    }
}