using EDLauncher.Core.Install;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    public record DhtReleaseServiceOptions(string PublicKey, Dictionary<string, string> channelSalts, TimeSpan DefaultTimeout);

    public class DhtReleaseService : IReleaseService, IDisposable, IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly Session _session;
        private readonly byte[] _publicKeyBytes;
        private TimeSpan _timeout;

        private readonly DhtCache _dhtCache;
        private DhtAannounceService _dhtReannounceService;
        private DhtReleaseServiceOptions _options;

        public DhtReleaseService(ILogger<DhtReleaseService> logger, Session session, DhtCache dhtCache, DhtAannounceService dhtReannounceService, DhtReleaseServiceOptions options)
        {
            _logger = logger;
            _session = session;
            _publicKeyBytes = Convert.FromHexString(options.PublicKey);

            _timeout = options.DefaultTimeout;

            _dhtCache = dhtCache;
            _dhtReannounceService = dhtReannounceService;
            _options = options;
        }

        public async Task<ReleaseInfo?> GetLatestAsync(string releaseChannel, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Getting latest for channel '{releaseChannel}'...");

                if(!_options.channelSalts.ContainsKey(releaseChannel))
                {
                    _logger.LogError($"Release channel '{releaseChannel}' is not configured!");
                    return null;
                }

                byte[] saltBytes = Encoding.ASCII.GetBytes(_options.channelSalts[releaseChannel]);
                var dhtItem = await _session.DHT.GetLatestAsync(_publicKeyBytes, saltBytes, _timeout, cancellationToken);
                if (dhtItem?.Value == null)
                {
                    //TODO: decide if we should return the cached version as the "latest version" here
                    return null;
                }

                _logger.LogInformation($"DHT Returned: {JsonSerializer.Serialize(dhtItem, new JsonSerializerOptions() { IncludeFields = true })}");

                _dhtCache.ReplaceIfNewer(dhtItem.Value);
                _dhtReannounceService.RequestReannounce();

                return ReleaseInfo.Decode(dhtItem.Value!.Value!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest item from DHT");
                return null;
            }
        }

        public async Task<ReleaseInfo?> GetUpdateAsync(string releaseChannel, string currentVersion, CancellationToken cancellationToken = default)
        {
            if (!_options.channelSalts.ContainsKey(releaseChannel))
            {
                _logger.LogError($"Release channel '{releaseChannel}' is not configured!");
                return null;
            }

            byte[] saltBytes = Encoding.ASCII.GetBytes(_options.channelSalts[releaseChannel]);
            var resultsEnumerable = _session.DHT.SearchAsync(_publicKeyBytes, saltBytes, _timeout, cancellationToken);

            var currentVersionSem = SemanticVersion.Parse(currentVersion);
            await foreach (var result in resultsEnumerable.WithCancellation(cancellationToken))
            {
                try
                {
                    ReleaseInfo candidateRelease = ReleaseInfo.Decode(result.Value!);
                    var candidateVersionSem = SemanticVersion.Parse(candidateRelease.Version);
                    if (candidateVersionSem > currentVersionSem)
                    {
                        _dhtCache.ReplaceIfNewer(result);
                        _dhtReannounceService.RequestReannounce();

                        return candidateRelease;
                    }
                }
                catch
                {
                    // TODO: probably log this
                    continue;
                }
            }
            return null;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
