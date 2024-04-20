using EDLauncher.Core.Torrents;
using InstallerLib.Packages;
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
    /// <summary>
    /// Manges the download of a single torrent
    /// </summary>
    public class TorrentDownload
    {
        private const int UpdateIntervalMS = 33;
        private const int StallDownloadRateThreshold = 1024; // 1 KB/s
        private const int MaxAttempts = 3; // max consecutive failed _attempts
        private const double BackoffTimeMultiplier = 1.3; // backoff up to 2 minutes over 3 consecutive failed _attempts

        private readonly ILogger _logger;
        private readonly IPeerCache _peerCache;
        private readonly Session _session;
        private readonly string _downloadDirectory;
        private readonly TorrentFile _torrent;
        private readonly TorrentParams _torrentParams;
        private readonly Action<DownloadProgress>? _progressCallback;
        private readonly CancellationToken _cancellationToken;

        private int _torrentId = -1;
        private long _initialWantedDone = 0;
        private List<TorrentFileErrorEventArgs>? _fileErrors;

        private DateTimeOffset _startTime = DateTimeOffset.UtcNow; // the start time of the download
        private DateTimeOffset _lastProgressTime = DateTime.UtcNow; // the time the download last made progress
        private int _attempts = 0; // current number of consecutive failed attempts
        private double _stallTimeSec = 30; // the initial length of time the download rate has to be below the threshold before it is considered stalled

        public TorrentDownload(
            ILogger logger,
            IPeerCache peerCache,
            Session session,
            string downloadDirectory,
            TorrentFile torrent,
            TorrentParams torrentParams,
            Action<DownloadProgress>? progressCallback,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _peerCache = peerCache;
            _session = session;
            _downloadDirectory = downloadDirectory;
            _torrent = torrent;
            _torrentParams = torrentParams;
            _progressCallback = progressCallback;
            _cancellationToken = cancellationToken;
        }

        private void OnFileError(object? sender, TorrentFileErrorEventArgs error)
        {
            (_fileErrors ??= new List<TorrentFileErrorEventArgs>()).Add(error);
            _logger.LogError(error.Message);
        }

        public async Task DownloadAsync()
        {
            try
            {
                _session.TorrentFileError += OnFileError;
                _torrentId = _session.AddTorrentFile(_torrent.ToDict(), _torrentParams);
                await RunDownloadLoop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Torrent download failed");
                throw;
            }
            finally
            {
                _session.TorrentFileError -= OnFileError;

                // Remove the _torrent from the session
                if (_torrentId != -1)
                {
                    await _session.RemoveTorrentAsync(_torrentId, deleteFiles: false);
                }
            }

            // If files in torrent are smaller than those on disk we need to truncate
            await FileTruncator.TruncateFilesAsync(_downloadDirectory, _torrent.Info.Files, _torrentParams.RenamedFiles, _cancellationToken);
        }

        private async Task RunDownloadLoop()
        {
            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                TorrentStatus status = _session.GetTorrentStatus(_torrentId);

                // Store the initial total wanted done when we get it for progress calculcation
                if (_initialWantedDone == 0 && status.State == TorrentState.Downloading)
                    _initialWantedDone = status.TotalWantedDone;

                CheckError(status);
                ReportProgress(status);

                await CheckStalled(status);

                if (status.IsFinished)
                    break;

                await Task.Delay(UpdateIntervalMS, _cancellationToken);
            }
        }

        private void CheckError(TorrentStatus status)
        {
            if (status.ErrorCode != 0)
            {
                throw new TorrentException(status.ErrorCode);
            }
            if (status.Flags.HasFlag(TorrentFlags.UploadMode))
            {
                // If we were put in upload mode there was issue writing, a file error should have been posted
                throw new IOException(_fileErrors?.LastOrDefault()?.Message ?? "Unknown IO error occured");
            }
            if (status.ErrorFileIndex != -1)
            {
                throw new IOException(status.ErrorFileIndex < 0
                     ? $"IO error occured. Error: {status.ErrorFileIndex}"
                     : $"IO error accessing file '{_torrent.Info.Files[status.ErrorFileIndex].Path}'");
            }
        }

        private async Task CheckStalled(TorrentStatus status)
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;

            if (status.State != TorrentState.Downloading || (status.DownloadPayloadRate >= StallDownloadRateThreshold))
            {
                // Reset the consecutive failed attempts if we have made progress downloading
                if (status.State == TorrentState.Downloading)
                    _attempts = 0;

                _lastProgressTime = currentTime;
            }

            if ((currentTime - _lastProgressTime) > TimeSpan.FromSeconds(_stallTimeSec))
            {
                if (++_attempts == MaxAttempts)
                {
                    throw new TimeoutException($"Torrent download timed out after {Math.Round((currentTime - _startTime).TotalSeconds)} seconds");
                }

                _logger.LogError($"Torrent download stalled. Retrying ({_attempts}/{MaxAttempts})...");

                if (_attempts == MaxAttempts - 1)
                {
                    // This is the last attempt, this time try re-adding the torrent
                    await _session.RemoveTorrentAsync(_torrentId);
                    _torrentId = -1;
                    UpdatePeersFromCache();
                    _torrentId = _session.AddTorrentFile(_torrent.ToDict(), _torrentParams);
                }
                else
                {
                    // Pausing and resuming the torrent will disconnect and reconnect all peers
                    _session.PauseTorrent(_torrentId);
                    _session.ResumeTorrent(_torrentId);
                }

                // Increase the length of time for a stall for the next attempt
                _stallTimeSec *= BackoffTimeMultiplier;

                _lastProgressTime = currentTime;
            }
        }

        private void UpdatePeersFromCache()
        {
            _torrentParams.Peers.Clear();
            foreach (Peer peer in _peerCache.GetPeerList())
                _torrentParams.Peers.Add(new IPEndPoint(IPAddress.Parse(peer.ip), peer.port));
        }

        private void ReportProgress(TorrentStatus status)
        {
            TimeSpan eta = status.DownloadPayloadRate != 0
                ? TimeSpan.FromSeconds((status.TotalWanted - status.TotalWantedDone) / (double)status.DownloadPayloadRate)
                : default;

            var prog = new DownloadProgress(
                Status: GetDownloadStatus(status.State),
                Progress: GetProgress(status),
                Current: status.TotalWantedDone,
                Total: status.TotalWanted,
                DownloadRate: status.DownloadPayloadRate,
                ETA: eta);

            _progressCallback?.Invoke(prog);
            //_logger.LogTrace($"Downloading: Status: {prog.Status} {prog.Progress * 100:0.0}% Speed: {prog.DownloadRate / 1024.0:0.0} KB/s ETA: {prog.ETA}");
        }

        private DownloadStatus GetDownloadStatus(TorrentState state)
        {
            switch (state)
            {
                case TorrentState.Allocating:
                    return DownloadStatus.Preparing;
                case TorrentState.Unused:
                    break;
                case TorrentState.CheckingFiles:
                case TorrentState.CheckingResumeData:
                    return DownloadStatus.Checking;
                case TorrentState.DownloadingMetadata:
                    return DownloadStatus.Downloading;
                case TorrentState.Downloading:
                    return DownloadStatus.Downloading;
                case TorrentState.Finished:
                case TorrentState.Seeding:
                    return DownloadStatus.Finished;
            }
            throw new NotImplementedException();
        }

        private double GetProgress(TorrentStatus status)
        {
            // For download we need the Progress of just the pieces that we want
            if (status.State == TorrentState.Downloading)
            {
                return _initialWantedDone == status.TotalWanted
                    ? 1.0
                    : (status.TotalWantedDone - _initialWantedDone) / (double)(status.TotalWanted - _initialWantedDone);
            }
            else
            {
                return status.Progress;
            }
        }
    }
}
