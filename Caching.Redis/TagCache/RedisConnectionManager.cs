using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisConnectionManager : IDisposable
    {
        private readonly object _connectionLock = new object();
        private readonly Lazy<IEnumerable<IServer>> _servers;
        private Lazy<ConnectionMultiplexer> _connection;

        private readonly string _connectionString, _password;
        private readonly int? _connectTimeout, _syncTimeout;
        private readonly bool _allowAdmin, _useSsl;

        public RedisConnectionManager(string connectionString = "127.0.0.1", int? connectTimeout = null, string password = null, bool allowAdmin = false, int? syncTimeout = null, bool useSsl = false)
        {
            _connectionString = connectionString;
            _connectTimeout = connectTimeout;
            _password = password;
            _allowAdmin = allowAdmin;
            _syncTimeout = syncTimeout;
            _useSsl = useSsl;

            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(BuildConfigurationOptions()));
            _servers = new Lazy<IEnumerable<IServer>>(() =>
            {
                var connection = GetConnection();
                return connection.GetEndPoints().Select(endpoint => connection.GetServer(endpoint));
            });
        }

        public IEnumerable<IServer> GetServers()
            => _servers.Value;

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

            if (!result.Ssl)
                result.Ssl = _useSsl;
            
            result.AllowAdmin = _allowAdmin;
            
            return result;
        }
    }
}