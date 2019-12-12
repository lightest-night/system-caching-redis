using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace LightestNight.System.Caching.Redis.TagCache.Expiry
{
    public class RedisExpiryManager : IHostedService, IDisposable
    {
        private readonly IRedisCacheProvider _redisCacheProvider;

        private Timer? _timer;

        public RedisExpiryManager(IRedisCacheProvider redisCacheProvider)
        {
            _redisCacheProvider = redisCacheProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start the expiration runner immediately, and then run every 6 hours.
            // The manual expiration process utilises the Redis commands KEYS & SCAN
            // This can be quite processor intensive depending on the database size, hopefully running only 4 times a day
            // will mitigate this
            _timer = new Timer(
                async e => await RemoveExpiredKeys(cancellationToken),
                null,
                TimeSpan.Zero,
                TimeSpan.FromHours(6)
                );

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task RemoveExpiredKeys(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await _redisCacheProvider.RemoveExpiredKeys();
            }
        }
    }
}