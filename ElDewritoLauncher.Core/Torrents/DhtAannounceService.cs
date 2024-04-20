using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TorrentLib;
using static TorrentLib.DHT;

namespace EDLauncher.Core.Torrents
{
    public class DhtAannounceService
    {
        private const int TimeoutMinutes = 2;

        private readonly ILogger<DhtAannounceService> _logger;
        private IServiceProvider _serviceProvider;
        private DhtCache _cache;
        private CancellationTokenSource? _cancellationTokenSource;

        public DhtAannounceService(ILogger<DhtAannounceService> logger, IServiceProvider serviceProvider, DhtCache cache)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        public async void RequestReannounce()
        {
            _logger.LogInformation("Requested reannounce");

            // Cancel an existing task if one has been started
            if (_cancellationTokenSource != null)
            {
                _logger.LogInformation("Cancelling current announce");
                _cancellationTokenSource.Cancel();
            }

            CancellationTokenSource localTokenSource = _cancellationTokenSource = new();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            try
            {
                // Start a background task to reannounce
                await Task.Run(async () => await ReannounceAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Announce failed exception while reannouncing DHT");
            }
            finally
            {
                // back on the ui thread now, dispose of the token source
                localTokenSource.Dispose();
                if (localTokenSource == _cancellationTokenSource)
                    _cancellationTokenSource = null;
            }
        }

        public async Task ReannounceAsync(CancellationToken token)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var session = scope.ServiceProvider.GetRequiredService<Session>();

            // Accessing the cache is thread safe
            var currentDht = _cache.GetCurrentDHT();
            if (currentDht == null || currentDht.Value.Value == null)
            {
                _logger.LogInformation("Nothing to announce");
                return;
            }

            DHTItem dht = currentDht.Value;
            try
            {
                _logger.LogInformation($"Starting announce {JsonSerializer.Serialize(dht, new JsonSerializerOptions() { IncludeFields = true })}");

                Task timeoutTask = Task.Delay(TimeSpan.FromMinutes(TimeoutMinutes), token);
                Task<DHTPutResult> announceTask = session.DHT.AnnounceAsync(dht.Key, dht.Salt, dht.Signature, dht.Value, dht.Sequence, token);

                var completedTask = await Task.WhenAny(announceTask, timeoutTask);
                if (completedTask != announceTask)
                    throw new TimeoutException($"Announce timed out after {TimeoutMinutes} minutes");

                DHTPutResult result = announceTask.Result;
                _logger.LogInformation($"Announced successfully to {result.SuccessCount} nodes");
            }
            catch (OperationCanceledException)
            {
                /* swallow */
            }
        }
    }
}
