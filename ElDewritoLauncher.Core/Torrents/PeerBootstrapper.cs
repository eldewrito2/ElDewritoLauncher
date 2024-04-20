using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EDLauncher.Core.Torrents
{
    public class PeerBootstrapper
    {
        private readonly ILogger<PeerBootstrapper> _logger;
        private readonly IPeerCache _peerCache;

        public PeerBootstrapper(ILogger<PeerBootstrapper> logger, IPeerCache peerCache)
        {
            _logger = logger;
            _peerCache = peerCache;
        }

        public void StartBackgroundBootstrap((string hostname, int port)[] ips)
        {
            Task.Run(async () =>
            {
                try
                {
                    await BootstrapAsync(_peerCache, ips).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to bootstrap peers");
                }
            });
        }

        public async Task BootstrapAsync(IPeerCache cache, (string hostname, int port)[] ips)
        {
            await Task.WhenAll(ips.Select((endpoint) => ResolveAsync(cache, endpoint.hostname, endpoint.port)));
        }

        private async Task ResolveAsync(IPeerCache cache, string hostname, int port)
        {
            try
            {
                IPAddress[] hostAddresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
                foreach (IPAddress addreess in hostAddresses)
                {
                    if (addreess.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    cache.AddPeer(new Peer(addreess.ToString(), port));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to resolve dns '{hostname}'");
            }
        }
    }
}
