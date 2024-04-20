using InstallerLib.Packages;
using InstallerLib.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    public record TorrentPackageDownloadOptions(string[] Trackers, TimeSpan Timeout);

    public class TorrentPackageDownloader : IPackageDownloader
    {
        private readonly ILogger<TorrentPackageDownloader> _logger;
        private readonly Session _session;
        private readonly IPackageCache _cache;
        private readonly IPeerCache _peerCache;
        private readonly TorrentPackageDownloadOptions _options;

        public TorrentPackageDownloader(ILogger<TorrentPackageDownloader> logger, Session session, IPackageCache cache, IPeerCache peerCache, TorrentPackageDownloadOptions options)
        {
            _logger = logger;
            _session = session;
            _cache = cache;
            _peerCache = peerCache;
            _options = options;
        }

        public async Task<IPackage> GetPackageAsync(string packageUri, CancellationToken cancellationToken = default)
        {
            string id = TorrentPackage.ExtractInfoHashFromMagnetUri(packageUri);
            IPackage? package = await _cache.GetPackageAsync(id);
            if (package != null)
            {
                _logger.LogInformation($"Using cached package {package.Name}-{package.Version}");
                return package;
            }

            DateTimeOffset startTime = DateTimeOffset.UtcNow;

            Task<IPackage> packageDownloadTask = DownloadPackage(packageUri, cancellationToken);
            Task completedTask = await Task.WhenAny(packageDownloadTask, Task.Delay(_options.Timeout)).ConfigureAwait(false);
            if (completedTask != packageDownloadTask)
            {
                string message = $"Failed to download package after {(DateTimeOffset.UtcNow - startTime).TotalSeconds:0.0} seconds";
                _logger.LogError(message);
                throw new TimeoutException(message);
            }

            await _cache.AddPackageAsync(packageDownloadTask.Result);
            return packageDownloadTask.Result;
        }

        public async Task DownloadPackageContentsAsync(
            IPackage package,
            string downloadDirectory,
            PackageDownloadParameters parameters,
            Action<DownloadProgress>? progress,
            CancellationToken cancellationToken = default)
        {
            using var tmpDir = TempDirectory.Create();

            TorrentFile torrent = ((TorrentPackage)package).Torrent;

            // Build the torrent params
            var torrentParams = new TorrentParams();
            torrentParams.Flags = TorrentFlags.UpdateSubscribe;
            torrentParams.SavePath = tmpDir.FullPath;
            torrentParams.Trackers = _options.Trackers.ToList();

            foreach (Peer peer in _peerCache.GetPeerList())
                torrentParams.Peers.Add(new IPEndPoint(IPAddress.Parse(peer.ip), peer.port));

            downloadDirectory = Path.GetFullPath(downloadDirectory);

            for (int i = 0; i < torrent.Info.Files.Count; i++)
            {
                if (parameters.FilesToDownload != null && !parameters.FilesToDownload.Get(i))
                {
                    torrentParams.FilePriorties[i] = DownloadPriority.DontDownload;
                }

                string path = torrent.Info.Files[i].Path!;
                if (parameters.RenamedFiles != null && parameters.RenamedFiles.TryGetValue(path, out string? remappedPath))
                {
                    _logger.LogTrace($"Remapped file({i}) '{path}' -> '{remappedPath}'");
                    path = remappedPath;
                }
                torrentParams.RenamedFiles[i] = Path.Combine(downloadDirectory, path);
            }

            _logger.LogInformation($"Downloading torrent '{package.Uri}' to '{downloadDirectory}'");

            var download = new TorrentDownload(_logger, _peerCache, _session, downloadDirectory, torrent, torrentParams, progress, cancellationToken);
            await download.DownloadAsync().ConfigureAwait(false);

            _logger.LogInformation($"Download completed successfully");
        }

        private async Task<IPackage> DownloadPackage(string packageUri, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Downloading package '{packageUri}'");
            TorrentFile torrentFile = await DownloadTorrentFileAsync(packageUri, cancellationToken);
            return new TorrentPackage(new Uri(packageUri), torrentFile);
        }

        private async Task<TorrentFile> DownloadTorrentFileAsync(string magnetUri, CancellationToken cancellationToken = default)
        {
            await using var tempDir = TempDirectory.Create();

            var p = new TorrentParams()
            {
                SavePath = tempDir.FullPath,
                Flags = TorrentFlags.UpdateSubscribe | TorrentFlags.DefaultDontDownload,
                Trackers = _options.Trackers.ToList()
            };

            foreach (Peer peer in _peerCache.GetPeerList())
                p.Peers.Add(new IPEndPoint(IPAddress.Parse(peer.ip), peer.port));

            int torrentId = _session.AddTorrentMagnet(magnetUri, p);

            try
            {
                _logger.LogInformation($"Waiting for metadata...");
                // Wait for metadata
                await _session.TrackTorrentStatusAsync(torrentId, status => status.HasMetadata, cancellationToken);
                // Extract the metadata
                var result = (Dictionary<string, object>?)await _session.GetTorrentFileAsync(torrentId);
                if (result == null)
                    throw new Exception($"Failed to retrieve torrent metadata. ${magnetUri}");

                return new TorrentFile(result);
            }
            finally
            {
                // Remove the torrent from the session.
                await _session.RemoveTorrentAsync(torrentId);
            }
        }
    }
}
