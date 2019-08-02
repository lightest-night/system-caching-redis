using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public class RedisConnectionManager : IDisposable
    {
        private readonly object _connectionLock = new object();
        
        private readonly string _password;
        private readonly int? _connectTimeout, _syncTimeout;
        private readonly bool _allowAdmin, _useSsl;
        
        private Lazy<IEnumerable<IServer>> _servers;
        private Lazy<ConnectionMultiplexer> _connection;
        
        /// <summary>
        /// The host we are connecting to Redis via
        /// </summary>
        public string Host { get; }
        
        /// <summary>
        /// The connection string that was used to connect to redis
        /// </summary>
        public string ConnectionString { get; }

        public RedisConnectionManager(string connectionString)
        {
            ConnectionString = connectionString ?? "localhost";
            
            Initialize();
        }

        public RedisConnectionManager(string host = "localhost", int port = 6319, int? connectTimeout = null, string password = null, bool allowAdmin = false, int? syncTimeout = null,
            bool useSsl = false)
        {
            Host = host;
            ConnectionString = $"{host}:{port}";
            _connectTimeout = connectTimeout;
            _password = password;
            _allowAdmin = allowAdmin;
            _syncTimeout = syncTimeout;
            _useSsl = useSsl;
            
            Initialize();
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
        
        private void Initialize()
        {
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(BuildConfigurationOptions()));
            _servers = new Lazy<IEnumerable<IServer>>(() =>
            {
                var connection = GetConnection();
                return connection.GetEndPoints().Select(endpoint => connection.GetServer(endpoint));
            });
        }

        private ConfigurationOptions BuildConfigurationOptions()
        {
            var result = ConfigurationOptions.Parse(ConnectionString);

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