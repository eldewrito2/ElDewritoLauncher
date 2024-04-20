using System.Runtime.InteropServices;
using System.Text;

namespace TorrentLib
{
    using static NativeApi;

    public class Session : IDisposable, IAsyncDisposable
    {
        public event EventHandler<TorrentStatusUpdatedEventArgs>? TorrentStatusUpdated;
        private event Action<TorrentRemovedEventArgs>? TorrentRemovedInternal;
        public event EventHandler<TorrentFileErrorEventArgs>? TorrentFileError;
        public event EventHandler<PeerActivityEventArgs>? PeerActivity;
        public event EventHandler<LogEventArgs>? Logged;

        private unsafe session* _session;
        private readonly SessionLoop _loop;
        private session_callbacks callbacks = default;
        private ResumeDataQueue _resumeDataQueue;
        private ReadPieceQueue _readPieceQueue;

        public Session(SessionSettings settings)
        {
            Settings = settings;
            DHT = new DHT(this);
            _resumeDataQueue = new ResumeDataQueue(this);
            _readPieceQueue = new ReadPieceQueue(this);

            unsafe
            {
                var settingsDict = new Dictionary<string, object>();
                settingsDict["lt_settings"] = settings.LTSettings;
                settingsDict["use_auth"] = settings.UseAuth;
                settingsDict["use_logging"] = settings.UseLogging;

                byte[] encodedSettings = Bencode.Encode(settingsDict);
                CheckResult(session_new(out _session, encodedSettings, encodedSettings.Length));
        
                callbacks.torrent_status_updated = OnTorrentStatusUpdated;
                callbacks.torrent_removed = OnTorrentRemoved;
                callbacks.dht_bootstrap = DHT.OnDHTBootstrapped;
                callbacks.dht_get_item = DHT.OnDHTGetItem;
                callbacks.dht_put_item = DHT.OnDHTPutItem;
                callbacks.save_resume_data = OnTorrentSaveResumeDataCompleted;
                callbacks.read_piece = OnReadPieceCompleted;
                callbacks.torrent_file_Error = OnTorrentFileError;
                callbacks.peer_activity = OnPeerActivity;
                callbacks.log = OnLog;
                session_set_callbacks(_session, ref callbacks);
            }

            _loop = new SessionLoop(this);
            _loop.Start();
        }

        internal unsafe session* Handle => _session;

        internal SessionSettings Settings { get; }

        public DHT DHT { get; }

        //////////////////////////////////////////////////////////////
        // WARNING: Dipose method is not thread safe!!
        //////////////////////////////////////////////////////////////
        public virtual void Dispose()
        {
            CheckSessionDisposed();

            _loop.Stop();
            DestroySession();
        }

        //////////////////////////////////////////////////////////////
        // WARNING: DisposeAsync method is not thread safe!!
        //////////////////////////////////////////////////////////////
        public async ValueTask DisposeAsync()
        {
            CheckSessionDisposed();

            await Task.Run(() => Dispose()).ConfigureAwait(false);
        }

        private void CheckResult(int result)
        {
            if (result != 0)
                throw new TorrentException(result);
        }

        private unsafe void CheckSessionDisposed()
        {
            if (_session == null)
                throw new ObjectDisposedException(nameof(_session));
        }


        public unsafe void Poll(TimeSpan timeout)
        {
            CheckSessionDisposed();

            session_poll(_session, (int)timeout.TotalMilliseconds);
        }

        public unsafe void PostTorrentUpdates()
        {
            CheckSessionDisposed();

            CheckResult(session_post_torrent_updates(_session));
        }

        /// <summary>
        /// Appy settings to session
        /// </summary>
        /// <remarks>
        /// This function is asynchronous. Consider using <see cref="SessionSettings.LTSettings"/>
        /// </remarks>
        /// <param name="settings"></param>
        public unsafe void ApplySettings(Dictionary<string, object> settings)
        {
            CheckSessionDisposed();

            byte[] encodedSettings = Bencode.Encode(settings);
            CheckResult(session_apply_settings_buf(_session, encodedSettings, encodedSettings.Length));
        }

        public unsafe int AddTorrentMagnet(string magnetUri, TorrentParams torrentParams)
        {
            CheckSessionDisposed();

            torrent_add_params* atp;
            CheckResult(torrent_params_new_from_magnet(out atp, magnetUri));
            try
            {
                byte[] encodedParams = torrentParams.Encode();
                torrent_params_apply(atp, encodedParams, encodedParams.Length);

                CheckResult(session_add_torrent(_session, atp, out int torrent_id));
                return torrent_id;
            }
            finally
            {
                // TODO: should really be using some kind of SafeHandle
                torrent_params_delete(atp);
            }
        }

        public unsafe int AddTorrentFile(object torrentFile, TorrentParams torrentParams)
        {
            torrent_add_params* atp = null;

            try
            {
                var encodedTorrent = Bencode.Encode(torrentFile);
                CheckResult(torrent_params_new_from_torrent_buffer(out atp, encodedTorrent, encodedTorrent.Length));

                byte[] encodedParams = torrentParams.Encode();
                torrent_params_apply(atp, encodedParams, encodedParams.Length);

                CheckResult(session_add_torrent(_session, atp, out int torrent_id));
                return torrent_id;
            }
            finally
            {
                if (atp != null) torrent_params_delete(atp);
            }
        }

        public unsafe int AddTorrentFile(string filepath, TorrentParams torrentParams)
        {
            torrent_add_params* atp = null;

            try
            {
                var fileBytes = File.ReadAllBytes(filepath);
                CheckResult(torrent_params_new_from_torrent_buffer(out atp, fileBytes, fileBytes.Length));

                byte[] encodedParams = torrentParams.Encode();
                torrent_params_apply(atp, encodedParams, encodedParams.Length);

                CheckResult(session_add_torrent(_session, atp, out int torrent_id));
                return torrent_id;
            }
            finally
            {
                if (atp != null) torrent_params_delete(atp);
            }
        }

        public unsafe int AddTorrentResumeData(object resumeData, TorrentParams? torrentParams = null)
        {
            torrent_add_params* atp = null;

            try
            {
                var encodedResumeData = Bencode.Encode(resumeData);
                CheckResult(torrent_params_new_from_resume_data(out atp, encodedResumeData, encodedResumeData.Length));

                if (torrentParams != null)
                {
                    byte[] encodedParams = torrentParams.Encode();
                    torrent_params_apply(atp, encodedParams, encodedParams.Length);
                }

                CheckResult(session_add_torrent(_session, atp, out int torrent_id));
                return torrent_id;
            }
            finally
            {
                if (atp != null) torrent_params_delete(atp);
            }
        }

        [Obsolete("Use RemoveTorrentAsync instead to avoid race conditions")]
        public unsafe void RemoveTorrent(int torrentId, bool deleteFiles = false)
        {
            CheckSessionDisposed();

            CheckResult(session_remove_torrent(_session, torrentId, deleteFiles));
        }

        public Task RemoveTorrentAsync(int torrentId, bool deleteFiles = false)
        {
            CheckSessionDisposed();

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            SHA1_Hash? infoHash = null;

            try
            {
                TorrentRemovedInternal += TorrentRemovedHandler;
                infoHash = GetTorrentInfoHash(torrentId);
                unsafe { CheckResult(session_remove_torrent(_session, torrentId, deleteFiles)); }
            }
            catch (TorrentException)
            {
                // torrent was not in the session
                TorrentRemovedInternal -= TorrentRemovedHandler;
                return Task.CompletedTask;
            }

            void TorrentRemovedHandler(TorrentRemovedEventArgs e)
            {
                if (e.InfoHash != infoHash) return;

                if (e.ErrorCode != 0)
                {
                    tcs.SetException(new TorrentException(e.ErrorCode));
                }
                else
                {
                    // if deleteFiles wait for deleted before completing the task
                    if (deleteFiles && !e.Deleted) return;

                    tcs.SetResult();
                }

                TorrentRemovedInternal -= TorrentRemovedHandler;
            }

            return tcs.Task;
        }

        /// <summary>
        /// Find a torrent by the specified info hash
        /// </summary>
        /// <param name="infoHash">The info hash to search for</param>
        /// <returns>The id of the torrent or -1 if not found.</returns>
        public unsafe int FindTorrentByInfoHash(SHA1_Hash infoHash)
        {
            return session_find_torrent_by_info_hash(_session, infoHash.ToArray());
        }

        public unsafe TorrentStatus GetTorrentStatus(int torrentId)
        {
            CheckSessionDisposed();

            CheckResult(torrent_get_status(_session, torrentId, out torrent_status status));
            return TorrentStatus.FromNative(&status);
        }

        public unsafe void SetTorrentFlags(int torrentId, TorrentFlags addFlags, TorrentFlags removeFlags = TorrentFlags.None)
        {
            CheckSessionDisposed();

            CheckResult(torrent_set_flags(_session, torrentId, (ulong)addFlags, (ulong)removeFlags));
        }

        public unsafe string GetTorrentName(int torrentId)
        {
            CheckSessionDisposed();

            byte[] buffer = new byte[1024];
            int nameLength = torrent_get_name(_session, torrentId, buffer, buffer.Length);
            if (nameLength < 0)
                return "";
            return Encoding.UTF8.GetString(buffer, 0, nameLength);
        }

        /// <summary>
        /// Returns the info-hash of the torrent.
        /// </summary>
        /// <remarks>
        /// SHA1 hash for v1 torrents, and truncated SHA256 hash for v2 torrents.
        /// </remarks>
        /// <param name="torrentId"></param>
        /// <returns></returns>
        public unsafe SHA1_Hash GetTorrentInfoHash(int torrentId)
        {
            CheckSessionDisposed();

            var buff = stackalloc byte[SHA1_Hash.Length];
            CheckResult(torrent_get_info_hash(_session, torrentId, buff));
            return new SHA1_Hash(buff);
        }

        /// <summary>
        /// Returns the info-hash of a v1 torrent
        /// </summary>
        /// <param name="torrentId">The torrent id</param>
        /// <returns>The SHA1 hash</returns>
        public unsafe SHA1_Hash GetTorrentInfoHashV1(int torrentId)
        {
            CheckSessionDisposed();

            var buff = stackalloc byte[SHA1_Hash.Length];
            CheckResult(torrent_get_info_hash_v2(_session, torrentId, buff));
            return new SHA1_Hash(buff);
        }

        /// <summary>
        /// Returns the info-hash of a v2 torrent
        /// </summary>
        /// <remarks>
        /// If the torrent is not a v2 torrent, <see cref="SHA256_Hash.Empty"/> will be returned.
        /// </remarks>
        /// <param name="torrentId">The torrent id</param>
        /// <returns>A SHA256 hash</returns>
        public unsafe SHA256_Hash GetTorrentInfoHashV2(int torrentId)
        {
            CheckSessionDisposed();

            var buff = stackalloc byte[SHA256_Hash.Length];
            CheckResult(torrent_get_info_hash_v2(_session, torrentId, buff));
            return new SHA256_Hash(buff);
        }

        /// <summary>
        /// Clears any error and restarts the torrent
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        public unsafe void ClearTorrentError(int torrentId)
        {
            CheckSessionDisposed();

            CheckResult(torrent_clear_error(_session, torrentId));
        }

        /// <summary>
        /// Pauses a torrent
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        public unsafe void PauseTorrent(int torrentId)
        {
            CheckSessionDisposed();

            CheckResult(torrent_pause(_session, torrentId));
        }

        /// <summary>
        /// Resume a torrent
        /// </summary>
        /// <remarks>
        /// Reconnects all peers
        /// </remarks>
        /// <param name="torrentId">The id of the torrent</param>
        public unsafe void ResumeTorrent(int torrentId)
        {
            CheckSessionDisposed();

            CheckResult(torrent_resume(_session, torrentId));
        }

        /// <summary>
        /// Puts the torrent back in a state where it assumes to have no resume data.
        /// </summary>
        /// <remarks>
        /// All peers will be disconnected and the torrent will stop announcing to the tracker. 
        /// The torrent will be added to the checking queue, and will be checked (all the files will be read and
        /// compared to the piece hashes). Once the check is complete, the torrent will start connecting to peers again, as normal.
        /// The torrent will be placed last in queue, i.e. its queue position will be the highest of all torrents in the session.
        /// </remarks>
        /// <param name="torrentId">The id of the torrent to recheck</param>
        public unsafe void ForceRecheckTorrent(int torrentId)
        {
            CheckSessionDisposed();

            CheckResult(torrent_force_recheck(_session, torrentId));
        }

        /// <summary>
        /// Sets the upload rate limit on a torrent.
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        /// <param name="limit">Bytes per second</param>
        public unsafe void SetTorrentUploadLimit(int torrentId, int limit)
        {
            CheckSessionDisposed();

            CheckResult(torrent_set_upload_limit(_session, torrentId, limit));
        }

        /// <summary>
        /// Returns the current upload rate limit set on a torrent.
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        /// <returns>Bytes per second</returns>
        public unsafe int GetTorrentUploadLimit(int torrentId)
        {
            CheckSessionDisposed();

            return torrent_get_upload_limit(_session, torrentId);
        }

        /// <summary>
        /// Sets the the download rate limit on a torrent.
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        /// <param name="limit">Bytes per second</param>
        public unsafe void SetTorrentDownloadLimit(int torrentId, int limit)
        {
            CheckSessionDisposed();

            CheckResult(torrent_set_download_limit(_session, torrentId, limit));
        }

        /// <summary>
        /// Returns the current download rate limit set on a torrent.
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        /// <returns>Bytes per second</returns>
        public unsafe int GetTorrentDownloadLimit(int torrentId)
        {
            CheckSessionDisposed();

            return torrent_get_download_limit(_session, torrentId);
        }

        /// <summary>
        /// Sets the torrent's position in the download queue.
        /// </summary>
        /// <remarks>
        /// The torrents with the smallest numbers are the ones that are
        /// being downloaded. The smaller number, the closer the torrent is to the
        /// front of the line to be started.
        /// </remarks>
        /// <param name="torrentId">The id of the torrent</param>
        /// <param name="torrentId">The new queue position</param>
        public unsafe void SetTorrentQueuePosition(int torrentId, int position)
        {
            CheckSessionDisposed();

            CheckResult(torrent_set_queue_position(_session, torrentId, position));
        }

        /// <summary>
        /// Returns the torrent's position in the download queue.
        /// </summary>
        /// <param name="torrentId">The id of the torrent</param>
        public unsafe int GetTorrentQueuePosition(int torrentId)
        {
            CheckSessionDisposed();

            return torrent_get_queue_position(_session, torrentId);
        }

        public unsafe void SetPieceDeadline(int torrentId, int pieceIndex, int deadline, bool alertWhenAvailable = false)
        {
            CheckSessionDisposed();

            CheckResult(torrent_set_piece_deadline(_session, torrentId, pieceIndex, deadline, alertWhenAvailable));
        }

        public unsafe void ResetPieceDeadline(int torrentId, int pieceIndex)
        {
            CheckSessionDisposed();

            CheckResult(torrent_reset_piece_deadline(_session, pieceIndex, torrentId));
        }

        public unsafe List<FileEntry> GetTorrentFileEntries(int torrentId)
        {
            var files = new List<FileEntry>();

            int result = torrent_enumerate_files(_session, torrentId,
                (string path, long filesize) => files.Add(new FileEntry { Path = path, Length = filesize }));

            if (result != 0)
                throw new TorrentException(result);

            return files;
        }

        public unsafe PeerRequest MapFileRange(int torrentId, int fileIndex, long fileOffset, int size)
        {
            torrent_info* ti = torrent_get_info(_session, torrentId);
            if (ti == null) throw new TorrentException("No metadata");

            peer_request pr;
            CheckResult(torrent_info_map_file(ti, fileIndex, fileOffset, size, out pr));
            return new PeerRequest(pr.piece, pr.start, pr.length);
        }

        public unsafe void TruncateFiles(int torrentId)
        {
            CheckSessionDisposed();

            CheckResult(torrent_truncate_files(_session, torrentId));
        }

        public unsafe string CreateMagnetUri(int torrentId)
        {
            string magnetUri = "";
            CheckResult(make_magnet_uri(_session, torrentId, (int err, byte* buffer, int size) =>
            {
                magnetUri = Encoding.UTF8.GetString(buffer, size);
            }));
            return magnetUri;
        }

        public unsafe string CreateMagnetUri(object torrentFile)
        {
            var encoded = Bencode.Encode(torrentFile);
            string magnetUri = "";
            CheckResult(make_magnet_uri_adp(_session, encoded, encoded.Length, (int err, byte* buffer, int size) =>
            {
                magnetUri = Encoding.UTF8.GetString(buffer, size);
            }));
            return magnetUri;
        }

        public Task WaitForMetadataAsync(int torrentId, CancellationToken cancelToken = default)
        {
            return TorrentStatusTracker.TrackTorrentStatusAsync(this, torrentId, status => status.HasMetadata, cancelToken);
        }

        public unsafe Task<byte[]> ReadPieceAsync(int torrentId, int pieceIndex, CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            _readPieceQueue.Enqueue(torrentId, pieceIndex, (int err, IntPtr buffer, int size) =>
            {
                if (err != 0)
                {
                    tcs.SetException(new TorrentException(err));
                    return;
                }

                if (cancelToken.IsCancellationRequested)
                    tcs.SetCanceled();
                else
                {
                    var data = new byte[size];
                    Marshal.Copy(buffer, data, 0, size);
                    tcs.SetResult(data);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Gets the torrent file for a given torrent
        /// </summary>
        /// <param name="torrentId">The torrent id</param>
        /// <returns>A torrent file dctionary or null if the torrent has no metadata</returns>
        public unsafe Task<object?> GetTorrentFileAsync(int torrentId)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            _resumeDataQueue.Enqueue(torrentId, (int err, IntPtr atp) =>
            {
                if (err != 0)
                {
                    tcs.SetException(new TorrentException(err));
                    return;
                }

                err = write_torrent_file((torrent_add_params*)atp, (int err, byte* buffer, int size) =>
                {
                    if (err != 0)
                        tcs.SetException(new TorrentException(err));
                    else
                        tcs.SetResult(buffer == null ? null : Bencode.Decode(buffer, size)!);
                });

                // TODO: would be better to just always call the callback?
                if (err != 0)
                    tcs.SetException(new TorrentException(err));
            });

            return tcs.Task;
        }

        public unsafe Task<object?> GetResumeDataAsync(int torrentId)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            _resumeDataQueue.Enqueue(torrentId, (int err, IntPtr atp) =>
            {
                if (err != 0)
                {
                    tcs.SetException(new TorrentException(err));
                    return;
                }

                write_resume_data((torrent_add_params*)atp, (int err, byte* buffer, int size) =>
                {
                    if (err != 0)
                        tcs.SetException(new TorrentException(err));
                    else
                        tcs.SetResult(buffer == null ? null : Bencode.Decode(buffer, size)!);
                });
            });

            return tcs.Task;
        }

        internal unsafe void DestroySession()
        {
            CheckSessionDisposed();

            if (_session != null)
            {
                session_delete(_session);
                _session = null;
            }
        }

        private unsafe void OnTorrentStatusUpdated(int torrentId, torrent_status* status)
        {
            TorrentStatusUpdated?.Invoke(
                this,
                new TorrentStatusUpdatedEventArgs(torrentId, TorrentStatus.FromNative(status)),
                Settings.SynchronizationContext);
        }


        private unsafe void OnTorrentRemoved(int err, byte* infohash, bool deleted)
        {
            TorrentRemovedInternal?.Invoke(new TorrentRemovedEventArgs(err, new SHA1_Hash(infohash), deleted));
        }


        private unsafe void OnTorrentSaveResumeDataCompleted(int err, int torrent_id, torrent_add_params* atp)
        {
            _resumeDataQueue.OnTorrentSaveResumeDataCompleted(err, torrent_id, atp);
        }

        private unsafe void OnReadPieceCompleted(int err, int torrent_id, int piece_index, byte* buffer, int size)
        {
            _readPieceQueue.OnReadPieceCompleted(err, torrent_id, piece_index, buffer, size);
        }

        private unsafe void OnTorrentFileError(int torrent_id, int err, string filename, string message)
        {
            var eventArgs = new TorrentFileErrorEventArgs(torrent_id, err, filename, message);
            TorrentFileError?.Invoke(this, eventArgs, Settings.SynchronizationContext);
        }

        private void OnPeerActivity(int type, int torrent_id, string ip, int port, int direction, int err, int close_reason)
        {
            var eventArgs = new PeerActivityEventArgs((PeerActivityType)type, torrent_id, ip, port, (Direction)direction, close_reason, err);
            PeerActivity?.Invoke(this, eventArgs, Settings.SynchronizationContext);
        }

        private void OnLog(int type, string message)
        {
            Logged?.Invoke(this, new LogEventArgs(type, message), Settings.SynchronizationContext);
        }
    }
}
