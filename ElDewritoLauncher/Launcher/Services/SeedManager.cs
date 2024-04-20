using EDLauncher.Core;
using EDLauncher.Core.Install;
using EDLauncher.Core.Torrents;
using InstallerLib.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TorrentLib;

namespace EDLauncher.Launcher.Services
{
    public class SeedManager
    {
        private readonly ILogger _logger;
        private int _torrentId = -1;
        private Task? _seedTask;
        private CancellationTokenSource? _seedCancel;
        private DateTime _lastDhtAnnounceTime;
        private int DhtReannounceIntervalMinutes = 30;

        public event EventHandler<TorrentStatusUpdatedEventArgs>? StatusUpdated;
        public event EventHandler<SeedErrorEventArgs>? ErrorOccured;

        public SeedManager(ILogger<SeedManager> logger)
        {
            _logger = logger;
        }

        public async void StartSeeding()
        {
            if (App.LauncherState.IsSeeding)
                return;

            _logger.LogTrace("Starting seed...");

            await WaitForPreviousSeedToFinish();

            _seedCancel = new CancellationTokenSource();
            _seedTask = SeedTorrentAsync(_seedCancel.Token);

            try
            {
                App.LauncherState.IsSeeding = true;
                await _seedTask;
            }
            catch (OperationCanceledException)
            {
                /* do nothing */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding");
                ErrorOccured?.Invoke(this, new SeedErrorEventArgs(ex));
            }
            finally
            {
                App.LauncherState.IsSeeding = false;
            }
        }

        public async Task StopSeedingAsycnc()
        {
            _logger.LogTrace("Stopping seed...");

            if (_seedTask == null)
                return;

            StopSeeding();
            await WaitForPreviousSeedToFinish();
        }

        public void StopSeeding()
        {
            _seedCancel?.Cancel();
        }

        private async Task WaitForPreviousSeedToFinish()
        {
            if (_seedTask == null)
                return;

            _seedCancel!.Cancel();

            // We don't care if it threw an exception
            try { await _seedTask; } catch { }
        }

        private async Task SeedTorrentAsync(CancellationToken cancellationToken)
        {
            // Setup the session
            await using var scope = App.ServiceProvider.CreateAsyncScope();
            var session = scope.ServiceProvider.GetRequiredService<Session>();
            session.TorrentFileError += _session_TorrentFileError;
            try
            {
                
                // Get or try to download the torrent file
                TorrentFile torrent = await AcquiretTorrentFileAsync(scope.ServiceProvider, cancellationToken);

                _logger.LogTrace("Adding torrent to the session...");
                AddTorrentToSession(session, torrent, Environment.CurrentDirectory);

                // Monitor the torrent
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    TorrentStatus status = session!.GetTorrentStatus(_torrentId);
                    Debug.Assert(status.State != TorrentState.Downloading);

                    // Check any errors
                    if (status.ErrorCode != 0)
                    {
                        throw new TorrentException(status.ErrorCode);
                    }
                    if (status.ErrorFileIndex != -1)
                    {
                        throw new IOException(status.ErrorFileIndex < 0
                             ? $"IO error occured. Error: {status.ErrorFileIndex}"
                             : $"IO error accessing file '{torrent.Info!.Files[status.ErrorFileIndex].Path}'");
                    }

                    // If the state is finished piece hashe checks have failed
                    if (status.State == TorrentState.Finished)
                    {
                        throw new IOException("Invalid files found");
                    }

                    // Report the status to clients
                    StatusUpdated?.Invoke(this, new TorrentStatusUpdatedEventArgs(_torrentId, status));

                    if(_lastDhtAnnounceTime == default || (DateTime.UtcNow - _lastDhtAnnounceTime) >= TimeSpan.FromMinutes(DhtReannounceIntervalMinutes))
                    {
                        _lastDhtAnnounceTime = DateTime.UtcNow;
                        scope.ServiceProvider.GetRequiredService<DhtAannounceService>().RequestReannounce();
                    }

                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                /* do nothing */
            }
            finally
            {
                _logger.LogTrace("Seed ended. cleaning up...");
                // Cleanup the session
                session.TorrentFileError -= _session_TorrentFileError;
                if (_torrentId != -1)
                {
                    await session.RemoveTorrentAsync(_torrentId);
                    _torrentId = -1;
                }
            }
        }

        private void AddTorrentToSession(Session session, TorrentFile torrent, string directory)
        {
            Debug.Assert(session != null);

            List<string> trackers =
                ExtractInfoTrackers(torrent)
                .Union(App.ServiceProvider.GetRequiredService<TorrentPackageDownloadOptions>().Trackers)
                .Distinct()
                .ToList();

            int uploadRateLimit = App.LauncherSettings.SeedUploadRateLimit;
            if (uploadRateLimit * 1024 <= 0)
                uploadRateLimit = -1;


            var tempDir = InstallDirectory.GetTempDirectory(directory);
            var torrentParmas = new TorrentParams()
            {
                Flags = TorrentFlags.UploadMode | TorrentFlags.DefaultDontDownload | TorrentFlags.UpdateSubscribe | TorrentFlags.AutoManaged,
                SavePath = tempDir,
                Trackers = trackers
            };

            for (int i = 0; i < torrent.Info!.Files.Count; i++)
            {
                string path = torrent.Info!.Files[i].Path!;
                torrentParmas.RenamedFiles.Add(i, Path.Combine(directory, path));
            }

            torrentParmas.UploadLimit = uploadRateLimit * 1024;
            _torrentId = session.AddTorrentFile(torrent.ToDict(), torrentParmas);
        }

        private static List<string> ExtractInfoTrackers(TorrentFile torrent)
        {
            var trackers = new List<string>();
            if (torrent!.Info!.UnknownExtra.ContainsKey("trackers"))
            {
                var trackerListObj = (List<object>)torrent!.Info!.UnknownExtra["trackers"];
                for (int i = 0; i < trackerListObj.Count; i++)
                {
                    trackers.Add((string)trackerListObj[i]);
                }
            }
            return trackers;
        }

        private async Task<TorrentFile> AcquiretTorrentFileAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var packageCache = serviceProvider.GetRequiredService<IPackageCache>();

            IPackage? package = null;
            string? packageId = await packageCache.FindPackageForVersionAsync(Constants.PackageName, App.LauncherState.CurrentVersion);
            if (packageId == null)
            {
                _logger.LogError($"Missing package for version {App.LauncherState.CurrentVersion}");

                // Attempt the download the package
                var releaseService = serviceProvider.GetRequiredService<IReleaseService>();
                var packageDownloader = serviceProvider.GetRequiredService<IPackageDownloader>();
                string releaseChannel = App.LauncherSettings.ReleaseChannel;
                ReleaseInfo? releaseInfo = await releaseService.GetLatestAsync(releaseChannel, cancellationToken);

                if (releaseInfo!.Version == App.LauncherState.CurrentVersion)
                {
                    package = await packageDownloader.GetPackageAsync(releaseInfo.PackageUri, cancellationToken);
                }
                else
                {
                    _logger.LogError($"Current version is out of date and cannot be reacquired");
                }
            }
            else
            {
                package = await packageCache.GetPackageAsync(packageId);
            }

            if (package == null)
            {
                throw new FileNotFoundException("Package not found");
            }

            return ((TorrentPackage)package).Torrent;
        }

        private void _session_TorrentFileError(object? sender, TorrentFileErrorEventArgs e)
        {
            App.ServiceProvider.GetRequiredService<ILogger<SeedManager>>().LogError(new TorrentException(e.ErrorCode), "A Torrent file error occurred");
        }

        public async Task<string?> GetTorrentFilePathAsync()
        {
            var packageCache = App.ServiceProvider.GetRequiredService<IPackageCache>();

            string? packageId = await packageCache.FindPackageForVersionAsync(Constants.PackageName, App.LauncherState.CurrentVersion).ConfigureAwait(false);
            if (packageId == null)
                return null;

            return await packageCache.GetPackageFilePath(packageId).ConfigureAwait(false);
        }

        public async Task<string?> GetMagnetLinkAsync()
        {
            var packageCache = App.ServiceProvider.GetRequiredService<IPackageCache>();

            string? packageId = await packageCache.FindPackageForVersionAsync(Constants.PackageName, App.LauncherState.CurrentVersion).ConfigureAwait(false);
            if (packageId == null)
                return null;

            IPackage package = await packageCache.GetPackageAsync(packageId).ConfigureAwait(false);
            if (package == null)
                return null;

            return package.Uri;
        }
    }

    public class SeedErrorEventArgs : EventArgs
    {
        public SeedErrorEventArgs(Exception error)
        {
            Error = error;
        }

        public Exception Error { get; set; }
    }
}
