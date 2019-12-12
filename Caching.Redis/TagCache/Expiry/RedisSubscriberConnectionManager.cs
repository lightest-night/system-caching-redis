using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache.Expiry
{
    public class RedisSubscriberConnectionManager
    {
        private volatile ISubscriber? _connection;
        private readonly object _connectionLock = new object();
        private readonly RedisConnectionManager _connectionManager;

        public RedisSubscriberConnectionManager(RedisConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Gets a subscriber to the current Redis connection
        /// </summary>
        /// <returns>A connected instance of <see cref="ISubscriber" /></returns>
        public ISubscriber? GetConnection()
        {
            var connection = _connection;
            if (connection != null) 
                return connection;
            
            lock (_connectionLock)
            {
                if (_connection == null)
                    _connection = _connectionManager.GetConnection()?.GetSubscriber();

                connection = _connection;
            }

            return connection;
        }
    }
}