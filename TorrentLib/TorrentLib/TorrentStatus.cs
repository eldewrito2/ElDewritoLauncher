namespace TorrentLib
{
    public record TorrentStatus
    {
        /// <remarks>
        /// If the torrent is in an error state ErrorCode will be non zero. 
        /// You can use <see cref="ErrorHelper.GetErrorMessage(int)"/> to get the error message
        /// or alternatively construct a <see cref="TorrentException"/>.
        /// </remarks>
        public int ErrorCode { get; init; }
        public int ErrorFileIndex { get; init; }
        public long TotalDownload { get; init; }
        public long TotalUpload { get; init; }
        public long TotalPayloadDownload { get; init; }
        public long TotalPayloadUpload { get; init; }
        public long TotalFailedBytes { get; init; }
        public long TotalRedundantBytes { get; init; }
        public long TotalDone { get; init; }
        public long Total { get; init; }
        public long TotalWantedDone { get; init; }
        public long TotalWanted { get; init; }
        public long AllTimeUpload { get; init; }
        public long AllTimeDownload { get; init; }
        public DateTimeOffset AddedTime { get; init; }
        public DateTimeOffset CompletedTime { get; init; }
        public DateTimeOffset LastSeenComplete { get; init; }
        public float Progress { get; init; }
        public int ProgressPpm { get; init; }
        public int QueuePosition { get; init; }
        public int DownloadRate { get; init; }
        public int UploadRate { get; init; }
        public int DownloadPayloadRate { get; init; }
        public int UploadPayloadRate { get; init; }
        public int NumSeeds { get; init; }
        public int NumPeers { get; init; }
        public int NumComplete { get; init; }
        public int NumIncomplete { get; init; }
        public int ListSeeds { get; init; }
        public int ListPeers { get; init; }
        public int ConnectCandidates { get; init; }
        public int NumPieces { get; init; }
        public int DistributedFullCopies { get; init; }
        public int DistributedFraction { get; init; }
        public float DstributedCopies { get; init; }
        public int BlockSize { get; init; }
        public int NumUploads { get; init; }
        public int NumConnections { get; init; }
        public int UploadsLimit { get; init; }
        public int ConnectionsLimit { get; init; }
        public int UpBandwidthQueue { get; init; }
        public int DownBandwidthQueue { get; init; }
        public int SeedRank { get; init; }
        public TorrentState State { get; init; }
        public bool NeedSaveResume { get; init; }
        public bool IsSeeding { get; init; }
        public bool IsFinished { get; init; }
        public bool HasMetadata { get; init; }
        public bool HasIncoming { get; init; }
        public bool MovingStorage { get; init; }
        public bool AnnouncingToTrackers { get; init; }
        public bool AnnouncingToLSD { get; init; }
        public bool AnnouncingToDHT { get; init; }
        public DateTimeOffset LastUpload { get; init; }
        public DateTimeOffset LastDownload { get; init; }
        public TimeSpan ActiveDuration { get; init; }
        public TimeSpan FinishedDuration { get; init; }
        public TimeSpan SeedingDuration { get; init; }
        public TorrentFlags Flags { get; init; }

        internal unsafe static TorrentStatus FromNative(NativeApi.torrent_status* status) => new TorrentStatus
        {
            ErrorCode = status->errc,
            ErrorFileIndex = status->error_file,
            TotalDownload = status->total_download,
            TotalUpload = status->total_upload,
            TotalPayloadDownload = status->total_payload_download,
            TotalPayloadUpload = status->total_payload_upload,
            TotalFailedBytes = status->total_failed_bytes,
            TotalRedundantBytes = status->total_redundant_bytes,
            TotalDone = status->total_done,
            Total = status->total,
            TotalWantedDone = status->total_wanted_done,
            TotalWanted = status->total_wanted,
            AllTimeUpload = status->all_time_upload,
            AllTimeDownload = status->all_time_download,
            AddedTime = DateTimeOffset.FromUnixTimeSeconds(status->added_time),
            CompletedTime = DateTimeOffset.FromUnixTimeSeconds(status->completed_time),
            LastSeenComplete = DateTimeOffset.FromUnixTimeSeconds(status->last_seen_complete),
            Progress = status->progress,
            ProgressPpm = status->progress_ppm,
            QueuePosition = status->queue_position,
            DownloadRate = status->download_rate,
            UploadRate = status->upload_rate,
            DownloadPayloadRate = status->download_payload_rate,
            UploadPayloadRate = status->upload_payload_rate,
            NumSeeds = status->num_seeds,
            NumPeers = status->num_peers,
            NumComplete = status->num_complete,
            NumIncomplete = status->num_incomplete,
            ListSeeds = status->list_seeds,
            ListPeers = status->list_peers,
            ConnectCandidates = status->connect_candidates,
            NumPieces = status->num_pieces,
            DistributedFullCopies = status->distributed_full_copies,
            DistributedFraction = status->distributed_fraction,
            DstributedCopies = status->distributed_copies,
            BlockSize = status->block_size,
            NumUploads = status->num_uploads,
            NumConnections = status->num_connections,
            UploadsLimit = status->uploads_limit,
            ConnectionsLimit = status->connections_limit,
            UpBandwidthQueue = status->up_bandwidth_queue,
            DownBandwidthQueue = status->down_bandwidth_queue,
            SeedRank = status->seed_rank,
            State = (TorrentState)status->state,
            NeedSaveResume = status->need_save_resume,
            IsSeeding = status->is_seeding,
            IsFinished = status->is_finished,
            HasMetadata = status->has_metadata,
            HasIncoming = status->has_incoming,
            MovingStorage = status->moving_storage,
            AnnouncingToTrackers = status->announcing_to_trackers,
            AnnouncingToLSD = status->announcing_to_lsd,
            AnnouncingToDHT = status->announcing_to_dht,
            LastUpload = DateTimeOffset.FromUnixTimeSeconds(status->last_upload),
            LastDownload = DateTimeOffset.FromUnixTimeSeconds(status->last_download),
            ActiveDuration = TimeSpan.FromSeconds(status->active_duration),
            FinishedDuration = TimeSpan.FromSeconds(status->finished_duration),
            SeedingDuration = TimeSpan.FromSeconds(status->seeding_duration),
            Flags = (TorrentFlags)status->flags,
        };
    }
}
