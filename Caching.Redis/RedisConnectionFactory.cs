using System;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis
{
    public class RedisConnectionFactory
    {
        private readonly Lazy<ConnectionMultiplexer> _connection;
        
        /// <summary>
        /// The current instance of <see cref="ConnectionMultiplexer" /> in use
        /// </summary>
        public ConnectionMultiplexer Connection => _connection.Value;
        
        public RedisConnectionFactory(IOptions<ConfigurationOptions> options)
        {
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(options.Value));
        }
    }
}