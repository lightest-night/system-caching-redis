namespace LightestNight.System.Caching.Redis
{
    public class CacheConfig
    {
        /// <summary>
        /// The host to connect to the cache
        /// </summary>
        public string Host { get; set; } = string.Empty;
        
        /// <summary>
        /// The port to connect to the cache
        /// </summary>
        public int Port { get; set; }
        
        /// <summary>
        /// The password to connect to the cache
        /// </summary>
        public string? Password { get; set; }
        
        /// <summary>
        /// Denotes whether to use SSL when connecting to the cache
        /// </summary>
        public bool UseSsl { get; set; }
        
        /// <summary>
        /// Any timeout to enforce when attempting to connect to the cache
        /// </summary>
        public int? ConnectTimeout { get; set; }
        
        /// <summary>
        /// Any timeout to enforce when syncing with the cache
        /// </summary>
        public int? SyncTimeout { get; set; }
        
        /// <summary>
        /// Denotes whether to allow admin access when connecting to the cache
        /// </summary>
        public bool AllowAdmin { get; set; }
        
        /// <summary>
        /// Denotes whether to process manual expiration of keys on the 6 hour frequency
        /// </summary>
        public bool ManuallyProcessExpiredKeys { get; set; }
    }
}