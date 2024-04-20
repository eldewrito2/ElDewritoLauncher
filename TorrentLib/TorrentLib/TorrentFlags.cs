namespace TorrentLib
{
    [Flags]
    public enum TorrentFlags : uint
    {
        /// <summary>
        /// No flags set. Not recommended to use this. Use <see cref="Default"/> instead.
        /// </summary>
        None = 0,

        /// <summary>
        /// If ``seed_mode`` is set, libtorrent will assume that all files
        /// are present for this torrent and that they all match the hashes in
        /// the torrent file. Each time a peer requests to download a block,
        /// the piece is verified against the hash, unless it has been verified
        /// already. If a hash fails, the torrent will automatically leave the
        /// seed mode and recheck all the files. The use case for this mode is
        /// if a torrent is created and seeded, or if the user already know
        /// that the files are complete, this is a way to avoid the initial
        /// file checks, and significantly reduce the startup time.
        ///
        /// Setting ``seed_mode`` on a torrent without metadata (a
        /// .torrent file) is a no-op and will be ignored.
        ///
        /// It is not possible to *set* the ``seed_mode`` flag on a torrent after it has
        /// been added to a session. It is possible to *clear* it though.
        /// </summary
        SeedMode = 1 << 0,

        /// <summary>
        /// If ``upload_mode`` is set, the torrent will be initialized in
        /// upload-mode, which means it will not make any piece requests. This
        /// state is typically entered on disk I/O errors, and if the torrent
        /// is also auto managed, it will be taken out of this state
        /// periodically (see ``settings_pack::optimistic_disk_retry``).
        ///
        /// This mode can be used to avoid race conditions when
        /// adjusting priorities of pieces before allowing the torrent to start
        /// downloading.
        ///
        /// If the torrent is auto-managed (``auto_managed``), the torrent
        /// will eventually be taken out of upload-mode, regardless of how it
        /// got there. If it's important to manually control when the torrent
        /// leaves upload mode, don't make it auto managed.
        /// </summary>
        UploadMode = 1 << 1,

        /// <summary>
        /// determines if the torrent should be added in *share mode* or not.
        /// Share mode indicates that we are not interested in downloading the
        /// torrent, but merely want to improve our share ratio (i.e. increase
        /// it). A torrent started in share mode will do its best to never
        /// download more than it uploads to the swarm. If the swarm does not
        /// have enough demand for upload capacity, the torrent will not
        /// download anything. This mode is intended to be safe to add any
        /// number of torrents to, without manual screening, without the risk
        /// of downloading more than is uploaded.
        ///
        /// A torrent in share mode sets the priority to all pieces to 0,
        /// except for the pieces that are downloaded, when pieces are decided
        /// to be downloaded. This affects the progress bar, which might be set
        /// to "100% finished" most of the time. Do not change file or piece
        /// priorities for torrents in share mode, it will make it not work.
        ///
        /// The share mode has one setting, the share ratio target, see
        /// ``settings_pack::share_mode_target`` for more info.
        /// <summary>
        ShareMode = 1 << 2,

        /// <summary>
        /// determines if the IP filter should apply to this torrent or not. By
        /// default all torrents are subject to filtering by the IP filter
        /// (i.e. this flag is set by default). This is useful if certain
        /// torrents needs to be exempt for some reason, being an auto-update
        /// torrent for instance.
        /// <summary>
        ApplyIPFilter = 1 << 3,

        /// <summary>
        /// specifies whether or not the torrent is paused. i.e. it won't connect to the tracker or any of the peers
        /// until it's resumed. Note that a paused torrent that also has the
        /// auto_managed flag set can be started at any time by libtorrent's queuing
        /// logic. See queuing_.
        /// <summary>
        Paused = 1 << 4,

        /// <summary>
        /// If the torrent is auto-managed (``auto_managed``), the torrent
        /// may be resumed at any point, regardless of how it paused. If it's
        /// important to manually control when the torrent is paused and
        /// resumed, don't make it auto managed.
        ///
        /// If ``auto_managed`` is set, the torrent will be queued,
        /// started and seeded automatically by libtorrent. When this is set,
        /// the torrent should also be started as paused. The default queue
        /// order is the order the torrents were added. They are all downloaded
        /// in that order. For more details, see queuing_.
        /// </summary>
        AutoManaged = 1 << 5,

        /// <summary>
        /// used in add_torrent_params to indicate that it's an error to attempt
        /// to add a torrent that's already in the session. If it's not considered an
        /// error, a handle to the existing torrent is returned.
        /// This flag is not saved by write_resume_data(), since it is only meant for
        /// adding torrents.
        /// </summary>
        DuplicateIsError = 1 << 6,

        /// <summary>
        /// on by default and means that this torrent will be part of state
        /// updates when calling post_torrent_updates().
        /// This flag is not saved by write_resume_data().
        /// </summary>
        UpdateSubscribe = 1 << 7,

        /// <summary>
        /// sets the torrent into super seeding/initial seeding mode. If the torrent
        /// is not a seed, this flag has no effect.
        SuperSeeding = 1 << 8,

        /// <summary>
        /// sets the sequential download state for the torrent. In this mode the
        /// piece picker will pick pieces with low index numbers before pieces with
        /// high indices. The actual pieces that are picked depend on other factors
        /// still, such as which pieces a peer has and whether it is in parole mode
        /// or "prefer whole pieces"-mode. Sequential mode is not ideal for streaming
        /// media. For that, see set_piece_deadline() instead.
        SequentialDownload = 1 << 9,

        /// <summary>
        /// When this flag is set, the torrent will *force stop* whenever it
        /// transitions from a non-data-transferring state into a data-transferring
        /// state (referred to as being ready to download or seed). This is useful
        /// for torrents that should not start downloading or seeding yet, but want
        /// to be made ready to do so. A torrent may need to have its files checked
        /// for instance, so it needs to be started and possibly queued for checking
        /// (auto-managed and started) but as soon as it's done, it should be
        /// stopped.
        ///
        /// *Force stopped* means auto-managed is set to false and it's paused. As
        /// if the auto_manages flag is cleared and the paused flag is set on the torrent.
        ///
        /// Note that the torrent may transition into a downloading state while
        /// setting this flag, and since the logic is edge triggered you may
        /// miss the edge. To avoid this race, if the torrent already is in a
        /// downloading state when this call is made, it will trigger the
        /// stop-when-ready immediately.
        ///
        /// When the stop-when-ready logic fires, the flag is cleared. Any
        /// subsequent transitions between downloading and non-downloading states
        /// will not be affected, until this flag is set again.
        ///
        /// The behavior is more robust when setting this flag as part of adding
        /// the torrent. See add_torrent_params.
        ///
        /// The stop-when-ready flag fixes the inherent race condition of waiting
        /// for the state_changed_alert and then call pause(). The download/seeding
        /// will most likely start in between posting the alert and receiving the
        /// call to pause.
        ///
        /// A downloading state is one where peers are being connected. Which means
        /// just downloading the metadata via the ``ut_metadata`` extension counts
        /// as a downloading state. In order to stop a torrent once the metadata
        /// has been downloaded, instead set all file priorities to dont_download
        /// </summary>
        StopWhenReady = 1 << 10,

        /// <summary>
        /// when this flag is set, the tracker list in the add_torrent_params
        /// object override any trackers from the torrent file. If the flag is
        /// not set, the trackers from the add_torrent_params object will be
        /// added to the list of trackers used by the torrent.
        /// This flag is set by read_resume_data() if there are trackers present in
        /// the resume data file. This effectively makes the trackers saved in the
        /// resume data take precedence over the original trackers. This includes if
        /// there's an empty list of trackers, to support the case where they were
        /// explicitly removed in the previous session.
        /// This flag is not saved by write_resume_data()
        /// </summary>
        OverrideTrackers = 1 << 11,

        /// <summary>
        /// If this flag is set, the web seeds from the add_torrent_params
        /// object will override any web seeds in the torrent file. If it's not
        /// set, web seeds in the add_torrent_params object will be added to the
        /// list of web seeds used by the torrent.
        /// This flag is set by read_resume_data() if there are web seeds present in
        /// the resume data file. This effectively makes the web seeds saved in the
        /// resume data take precedence over the original ones. This includes if
        /// there's an empty list of web seeds, to support the case where they were
        /// explicitly removed in the previous session.
        /// This flag is not saved by write_resume_data()
        /// </summary>
        OverrideWebSeeds = 1 << 12,

        /// <summary>
        /// if this flag is set (which it is by default) the torrent will be
        /// considered needing to save its resume data immediately as it's
        /// added. New torrents that don't have any resume data should do that.
        /// This flag is cleared by a successful call to save_resume_data()
        /// This flag is not saved by write_resume_data(), since it represents an
        /// ephemeral state of a running torrent.
        /// </summary>
        NeedSaveResume = 1 << 13,

        /// <summary>
        /// set this flag to disable DHT for this torrent. This lets you have the DHT
        /// enabled for the whole client, and still have specific torrents not
        /// participating in it. i.e. not announcing to the DHT nor picking up peers
        /// from it.
        /// </summary>
        DisableDHT = 1 << 19,

        /// <summary>
        /// set this flag to disable local service discovery for this torrent.
        /// </summary>
        DisableLSD = 1 << 20,

        /// <summary>
        /// set this flag to disable peer exchange for this torrent.
        /// </summary>
        DisablePEX = 1 << 21,

        /// <summary>
        /// if this flag is set, the resume data will be assumed to be correct
        /// without validating it against any files on disk. This may be used when
        /// restoring a session by loading resume data from disk. It will save time
        /// and also delay any hard disk errors until files are actually needed. If
        /// the resume data cannot be trusted, or if a torrent is added for the first
        /// time to some save path that may already have some of the files, this flag
        /// should not be set.
        /// </summary>
        NoVerifyFiles = 1 << 22,

        /// <summary>
        /// default all file priorities to dont_download. This is useful for adding
        /// magnet links where the number of files is unknown, but the
        /// file_priorities is still set for some files. Any file not covered by
        /// the file_priorities list will be set to normal download priority,
        /// unless this flag is set, in which case they will be set to 0
        /// (dont_download).
        /// </summary>
        DefaultDontDownload = 1 << 23,

        /// <summary>
        /// The default flags
        /// </summary>
        Default = UpdateSubscribe | AutoManaged | Paused | ApplyIPFilter | NeedSaveResume,
        All = (1 << 24) - 1,
    }
}