using System;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisConnectionManager : IDisposable
    {
        private readonly object _connectionLock = new object();
        private Lazy<ConnectionMultiplexer> _connection;

        private readonly string _connectionString, _password;
        private readonly int? _connectTimeout, _syncTimeout;
        private readonly bool _allowAdmin;

        public RedisConnectionManager(string connectionString = "127.0.0.1", int? connectTimeout = null, string password = null, bool allowAdmin = false, int? syncTimeout = null)
        {
            _connectionString = connectionString;
            _connectTimeout = connectTimeout;
            _password = password;
            _allowAdmin = allowAdmin;
            _syncTimeout = syncTimeout;

            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(BuildConfigurationOptions()));
        }

        public ConnectionMultiplexer GetConnection()
            => _connection.Value;

        public void Reset(bool allowCommandsToComplete = true)
        {
            lock (_connectionLock)
            {
                _connection.Value.Close(allowCommandsToComplete);
                _connection = null;
            }
        }

        public void Dispose()
        {
            lock (_connectionLock)
            {
                _connection.Value.Dispose();
                _connection = null;
            }
        }

        private ConfigurationOptions BuildConfigurationOptions()
        {
            var result = ConfigurationOptions.Parse(_connectionString);

            if (_syncTimeout.HasValue)
                result.SyncTimeout = _syncTimeout.Value;

            if (_connectTimeout.HasValue)
                result.ConnectTimeout = _connectTimeout.Value;

            if (!string.IsNullOrEmpty(_password))
                result.Password = _password;
            
            result.AllowAdmin = _allowAdmin;
            
            return result;
        }
    }
}