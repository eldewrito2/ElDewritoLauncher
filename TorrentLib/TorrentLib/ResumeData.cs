using System.Net;
using System.Net.Sockets;

namespace TorrentLib
{
    public class ResumeData
    {
        public ResumeData()
        {
        }

        public ResumeData(byte[] data) : this((Dictionary<string, object>)Bencode.Decode(data)!)
        {
        }

        public ResumeData(Dictionary<string, object> dict)
        {
            FromDict(dict);
        }

        public enum AllocationMode
        {
            Allocate,
            Sparse
        }

        /// <summary>
        /// The number of seconds since epoch when the torrent was added
        /// </summary>
        public DateTimeOffset AddedTime { get; set; }

        /// <summary>
        /// The number of seconds since epoch when the torrent completed
        /// </summary>
        public DateTimeOffset CompletedTime { get; set; }

        /// <summary>
        /// The number of seconds since epoch when the torrent finished
        /// </summary>
        public DateTimeOffset FinishedTime { get; set; }

        /// <summary>
        /// If provided, the number of seconds since epoch when the torrent was created
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// If provided, the creator of the torrent
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// The last time a peer was seen complete
        /// </summary>
        public DateTimeOffset LastSeenComplete { get; set; }

        /// <summary>
        /// The torrent's comment if it has one
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// The LibTorrent version the resume data was created from/for
        /// </summary>
        public string? LibTorrentVersion { get; set; }

        /// <summary>
        /// The torrent name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The number of peers in the swarm that are seeds.
        /// </summary>
        public int NumComplete { get; set; }

        /// <summary>
        /// is the number of times the torrent has been downloaded (not initiated, but the number of times a download has completed).
        /// </summary>
        public int NumDownloaded { get; set; }

        /// <summary>
        /// The number of peers in the swarm that do not have every piece.
        /// </summary>
        public int NumIncomplete { get; set; }

        /// <summary>
        /// string, the info hash of the torrent this data is saved for. 
        /// This is a 20 byte SHA-1 hash of the info section of the torrent if this is a v1 or v1+v2-hybrid torrent.
        /// </summary>
        public SHA1_Hash InfoHash { get; set; }
        /// <summary>
        /// string, the v2 info hash of the torrent this data is saved. for, in case it is a v2 or v1+v2-hybrid torrent.
        /// This is a 32 byte SHA-256 hash of the info section of the torrent.
        /// </summary>
        public SHA256_Hash InfoHashV2 { get; set; }

        /// <summary>
        /// A string with piece flags, one character per piece. Bit 1 means we have that piece. 
        /// Bit 2 means we have verified that this piece is correct. 
        /// This only applies when the torrent is in seed_mode.
        /// </summary>
        public List<byte>? Pieces { get; set; }

        /// <summary>
        /// The number of bytes that have been uploaded in total for this torrent.
        /// </summary>
        public long TotalUploaded { get; set; }
        /// <summary>
        /// The number of bytes that have been downloaded in total for this torrent.
        /// </summary>
        public long TotalDownloaded { get; set; }

        /// <summary>
        /// The number of seconds this torrent has been active. i.e. not paused.
        /// </summary>
        public TimeSpan ActiveTime { get; set; }

        /// <summary>
        /// The number of seconds this torrent has been active and seeding.
        /// </summary>
        public TimeSpan SeedingTime { get; set; }

        /// <summary>
        /// The number of seconds since epoch when we last uploaded payload to a peer on this torrent.
        /// </summary>
        public DateTimeOffset LastUpload;

        /// <summary>
        /// The number of seconds since epoch when we last downloaded payload from a peer on this torrent.
        /// </summary>
        public DateTimeOffset LastDownload { get; set; }

        /// <summary>
        /// In case this torrent has a per-torrent upload rate limit, this is that limit. In bytes per second.
        /// </summary>
        public int UploadRateLimit { get; set; }

        /// <summary>
        /// The download rate limit for this torrent in case one is set, in bytes per second.
        /// </summary>
        public int DownloadRateLimit { get; set; }

        /// <summary>
        /// The max number of peer connections this torrent may have, if a limit is set.
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// The max number of unchoked peers this torrent may have, if a limit is set.
        /// </summary>
        public int MaxUploads { get; set; }

        /// <summary>
        /// One entry per file in the torrent. Each entry is the priority of the file with the same index.
        /// </summary>
        public List<int>? FilePriority { get; set; }

        /// <summary>
        /// Each byte is interpreted as an integer and is the priority of that piece.
        /// </summary>
        public List<byte>? PiecePriority { get; set; }

        /// <summary>
        /// if the torrent is in seed mode
        /// </summary>
        public bool SeedMode { get; set; }

        /// <summary>
        /// if the torrent_flags::upload_mode is set.
        /// </summary>
        public bool UploadMode { get; set; }

        /// <summary>
        /// if the torrent_flags::share_mode  is set.
        /// </summary>
        public bool ShareMode { get; set; }

        /// <summary>
        /// if the torrent_flags::apply_ip_filter is set.
        /// </summary>
        public bool ApplyIPFilter { get; set; }

        /// <summary>
        /// If the torrent is paused.
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// If the torrent is auto managed.
        /// </summary>
        public bool AutoManaged { get; set; }

        /// <summary>
        /// if the torrent_flags::super_seeding is set.
        /// </summary>
        public bool SuperSeeding { get; set; }

        /// <summary>
        /// if the torrent is in sequential download mode
        /// </summary>
        public bool SequentialDownload { get; set; }

        /// <summary>
        /// if the torrent_flags::stop_when_ready is set.
        /// </summary>
        public bool StopWhenReady { get; set; }

        /// <summary>
        /// if the torrent_flags::disable_dht is set.
        /// </summary>
        public bool DisableDHT { get; set; }

        /// <summary>
        /// if the torrent_flags::disable_lsd is set.
        /// </summary>
        public bool DisableLSD { get; set; }

        /// <summary>
        /// if the torrent_flags::disable_pex is set.
        /// </summary>
        public bool DisablePex { get; set; }

        /// <summary>
        /// The top level list lists all tracker tiers. Each tier is a comma separated list of tracker urls
        /// </summary>
        public List<string>? Trackers { get; set; }

        /// <summary>
        /// ist of strings. If any file in the torrent has been renamed, this entry contains a list of all the filenames.
        /// In the same order as in the torrent file.
        /// </summary>
        public List<string>? MappedFiles { get; set; }

        /// <summary>
        /// List of url-seed URLs used by this torrent. The URLs are expected to be properly encoded and not contain any illegal url characters.
        /// </summary>
        public List<string>? UrlSeeds { get; set; }

        /// <summary>
        /// List of HTTP seed URLs used by this torrent. The URLs are expected to be properly encoded and not contain any illegal url characters.
        /// </summary>
        public List<string>? HttpSeeds { get; set; }

        /// <summary>
        /// In case this is a v2 (or v1+v2-hybrid) torrent, this is an optional list containing the merkle tree nodes we know of so far, for all files.
        /// </summary>
        public List<MerkleTreeNode>? Trees { get; set; }

        /// <summary>
        /// The save path where this torrent was saved. This is especially useful when moving torrents with move_storage() since this will be updated.
        /// </summary>
        public string? SavePath { get; set; }

        /// <summary>
        /// A list of IPv4 and port pairs of peers we were connected to last session. Format: ipv4:port
        /// </summary>
        public List<IPEndPoint>? Peers { get; set; }

        /// <summary>
        /// A list of IPv4 and port pairs of peers that have bveen banned. Format ipv4:port
        /// </summary>
        public List<IPEndPoint>? BannedPeers { get; set; }

        /// <summary>
        /// The torrent info dictionary
        /// </summary>
        public TorrentInfo? Info { get; set; }

        /// <summary>
        /// Each entry represents a piece
        /// </summary>
        public List<PieceProgress>? Unfinished { get; set; }

        /// <summary>
        /// The allocation mode for the storage.
        /// </summary>
        public AllocationMode Allocation { get; set; }

        public Dictionary<string, object> UnknownExtra = new Dictionary<string, object>();

        private void FromDict(Dictionary<string, object> dict)
        {
            foreach (var (key, value) in dict)
            {
                switch (key)
                {
                    case "active_time":
                        {
                            ActiveTime = TimeSpan.FromSeconds((long)value);
                        }
                        break;
                    case "added_time":
                        {
                            AddedTime = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "allocation":
                        {
                            Allocation = Enum.Parse<AllocationMode>((string)value, true);
                        }
                        break;
                    case "apply_ip_filter":
                        {
                            ApplyIPFilter = (long)value == 1;
                        }
                        break;
                    case "auto_managed":
                        {
                            AutoManaged = (long)value == 1;
                        }
                        break;
                    case "banned_peers":
                        {
                            if (BannedPeers == null) BannedPeers = new();
                            BannedPeers.AddRange(Utils.ReadEndpoints((string)value, 4));
                        }
                        break;
                    case "banned_peers6":
                        {
                            if (BannedPeers == null) BannedPeers = new();
                            BannedPeers.AddRange(Utils.ReadEndpoints((string)value, 16));
                        }
                        break;
                    case "comment":
                        {
                            Comment = (string)value;
                        }
                        break;
                    case "completed_time":
                        {
                            CompletedTime = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "creation date":
                        {
                            CreationDate = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "created by":
                        {
                            CreatedBy = (string)value;
                        }
                        break;
                    case "disable_dht":
                        {
                            DisableDHT = (long)value == 1;
                        }
                        break;
                    case "disable_lsd":
                        {
                            DisableDHT = (long)value == 1;
                        }
                        break;
                    case "disable_pex":
                        {
                            DisableDHT = (long)value == 1;
                        }
                        break;
                    case "download_rate_limit":
                        {
                            DownloadRateLimit = (int)(long)value;
                        }
                        break;
                    case "file-format":
                        {
                            if ((string)value != "libtorrent resume file")
                                throw new FormatException("Unknown file format");
                        }
                        break;
                    case "file_priority":
                        {
                            FilePriority = new(((List<object>)value).OfType<long>().Select(x => (int)x));
                        }
                        break;
                    case "file-version":
                        {
                            if ((int)(long)value != 1)
                                throw new FormatException("Unknown file version");
                        }
                        break;
                    case "finished_time":
                        {
                            FinishedTime = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "httpseeds":
                        {
                            HttpSeeds = new(((List<object>)value).OfType<string>());
                        }
                        break;
                    case "info":
                        {
                            Info = new TorrentInfo((Dictionary<string, object>)value);
                        }
                        break;
                    case "info-hash":
                        {
                            InfoHash = new SHA1_Hash(Bencode.Encoding.GetBytes((string)value));
                        }
                        break;
                    case "info-hash2":
                        {
                            InfoHashV2 = new SHA256_Hash(Bencode.Encoding.GetBytes((string)value));
                        }
                        break;
                    case "last_download":
                        {
                            LastDownload = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "last_seen_complete":
                        {
                            LastSeenComplete = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "last_upload":
                        {
                            LastUpload = DateTimeOffset.FromUnixTimeSeconds((long)value);
                        }
                        break;
                    case "libtorrent-version":
                        {
                            LibTorrentVersion = (string)value;
                        }
                        break;
                    case "mapped_files":
                        {
                            MappedFiles = new(((List<object>)value).OfType<string>());
                        }
                        break;
                    case "max_connections":
                        {
                            MaxConnections = (int)(long)value;
                        }
                        break;
                    case "max_uploads":
                        {
                            MaxUploads = (int)(long)value;
                        }
                        break;
                    case "num_complete":
                        {
                            NumComplete = (int)(long)value;
                        }
                        break;
                    case "num_downloaded":
                        {
                            NumDownloaded = (int)(long)value;
                        }
                        break;
                    case "num_incomplete":
                        {
                            NumIncomplete = (int)(long)value;
                        }
                        break;
                    case "name":
                        {
                            Name = (string)value;
                        }
                        break;
                    case "paused":
                        {
                            Paused = (long)value == 1;
                        }
                        break;
                    case "peers":
                        {
                            if (Peers == null) Peers = new();
                            Peers.AddRange(Utils.ReadEndpoints((string)value, 4));
                        }
                        break;
                    case "peers6":
                        {
                            if (Peers == null) Peers = new();
                            Peers.AddRange(Utils.ReadEndpoints((string)value, 16));
                        }
                        break;
                    case "pieces":
                        {
                            Pieces = new List<byte>(Bencode.Encoding.GetBytes((string)value));
                        }
                        break;
                    case "piece_priority":
                        {
                            PiecePriority = new(Bencode.Encoding.GetBytes((string)value));
                        }
                        break;
                    case "save_path":
                        {
                            SavePath = (string)value;
                        }
                        break;
                    case "seed_mode":
                        {
                            SeedMode = (long)value == 1;
                        }
                        break;
                    case "seeding_time":
                        {
                            SeedingTime = TimeSpan.FromSeconds((long)value);
                        }
                        break;
                    case "sequential_download":
                        {
                            SequentialDownload = (long)value == 1;
                        }
                        break;
                    case "share_mode":
                        {
                            ShareMode = (long)value == 1;
                        }
                        break;
                    case "super_seeding":
                        {
                            SuperSeeding = (long)value == 1;
                        }
                        break;
                    case "stop_when_ready":
                        {
                            StopWhenReady = (long)value == 1;
                        }
                        break;
                    case "total_downloaded":
                        {
                            TotalDownloaded = (long)value;
                        }
                        break;
                    case "total_uploaded":
                        {
                            TotalUploaded = (long)value;
                        }
                        break;
                    case "trackers":
                        {
                            Trackers = new();
                            var trackerTierLists = ((List<object>)value).OfType<List<object>>();
                            foreach (var tierList in trackerTierLists)
                                Trackers.Add(string.Join(",", tierList));
                        }
                        break;
                    case "trees":
                        {
                            Trees = new();
                            var nodeDicts = ((List<object>)value).OfType<Dictionary<string, object>>();
                            foreach (var nodeDict in nodeDicts)
                                Trees.Add(new MerkleTreeNode(nodeDict));
                        }
                        break;
                    case "unfinished":
                        {
                            Unfinished = new();
                            var pieceDicts = ((List<object>)value).OfType<Dictionary<string, object>>();
                            foreach (var pieceDict in pieceDicts)
                                Unfinished.Add(new PieceProgress(pieceDict));
                        }
                        break;
                    case "upload_mode":
                        {
                            UploadMode = (long)value == 1;
                        }
                        break;
                    case "upload_rate_limit":
                        {
                            UploadRateLimit = (int)(long)value;
                        }
                        break;
                    case "url-list":
                        {
                            UrlSeeds = new(((List<object>)value).OfType<string>());
                        }
                        break;
                    default:
                        UnknownExtra.Add(key, value);
                        break;
                }
            }
        }

        public Dictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>();

            dict["active_time"] = ActiveTime.Ticks / TimeSpan.TicksPerSecond;
            dict["added_time"] = AddedTime.ToUnixTimeSeconds();
            dict["allocation"] = Allocation.ToString().ToLower();
            dict["apply_ip_filter"] = ApplyIPFilter;
            dict["auto_managed"] = AutoManaged;

            if (BannedPeers != null && BannedPeers.Count > 0)
            {
                var v4BannedPeers = BannedPeers?.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList() ?? new();
                var v6BannedPeers = BannedPeers?.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).ToList() ?? new();
                dict["banned_peers"] = Utils.WriteEndpoints(v4BannedPeers, 4);
                dict["banned_peers6"] = Utils.WriteEndpoints(v6BannedPeers, 16);
            }

            if (Comment != null && Comment.Length > 0)
            {
                dict["comment"] = Comment;
            }

            dict["completed_time"] = CompletedTime.ToUnixTimeSeconds();
            dict["creation date"] = CreationDate.ToUnixTimeSeconds();

            if (CreatedBy != null && CreatedBy.Length > 0)
            {
                dict["created by"] = CreatedBy;
            }

            dict["disable_dht"] = DisableDHT;
            dict["disable_lsd"] = DisableLSD;
            dict["disable_pex"] = DisablePex;
            dict["download_rate_limit"] = DownloadRateLimit;
            dict["file-format"] = "libtorrent resume file";

            if (FilePriority != null && FilePriority.Count > 0)
            {
                dict["file_priority"] = FilePriority.ToList();
            }

            dict["file-version"] = 1;
            dict["finished_time"] = FinishedTime.ToUnixTimeSeconds();

            if (HttpSeeds != null && HttpSeeds.Count > 0)
            {
                dict["httpseeds"] = HttpSeeds.ToList();
            }
            else
            {
                dict["httpseeds"] = new List<string>();
            }

            if (Info != null)
            {
                dict["info"] = Info.ToDict();
            }

            dict["info-hash"] = Bencode.Encoding.GetString(InfoHash.ToArray());
            dict["info-hash2"] = Bencode.Encoding.GetString(InfoHashV2.ToArray());




            dict["last_download"] = LastDownload.ToUnixTimeSeconds();
            dict["last_seen_complete"] = LastSeenComplete.ToUnixTimeSeconds();
            dict["last_upload"] = LastUpload.ToUnixTimeSeconds();

            if (LibTorrentVersion != null && LibTorrentVersion.Length > 0)
            {
                dict["libtorrent-version"] = LibTorrentVersion;
            }


            if (MappedFiles != null && MappedFiles.Count > 0)
            {
                dict["mapped_files"] = MappedFiles.ToList();
            }

            dict["max_connections"] = MaxConnections;
            dict["max_uploads"] = MaxUploads;

            if (Name != null && Name.Length > 0)
            {
                dict["name"] = Name;
            }

            dict["num_complete"] = NumComplete;
            dict["num_downloaded"] = NumDownloaded;
            dict["num_incomplete"] = NumIncomplete;
            dict["paused"] = Paused;

            if (Peers != null && Peers.Count > 0)
            {
                var v4Peers = Peers?.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList() ?? new();
                var v6Peers = Peers?.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).ToList() ?? new();
                dict["peers"] = Utils.WriteEndpoints(v4Peers, 4);
                dict["peers6"] = Utils.WriteEndpoints(v6Peers, 16);
            }

            if (Pieces != null && Pieces.Count > 0)
            {
                dict["pieces"] = Bencode.Encoding.GetString(Pieces.ToArray());
            }
            else
            {
                dict["pieces"] = "";
            }

            if (PiecePriority != null && PiecePriority.Count > 0)
            {
                dict["piece_priority"] = PiecePriority.ToList();
            }

            dict["save_path"] = SavePath ?? "";
            dict["seed_mode"] = SeedMode;
            dict["seeding_time"] = SeedingTime.Ticks / TimeSpan.TicksPerSecond;
            dict["sequential_download"] = SequentialDownload;
            dict["share_mode"] = ShareMode;
            dict["stop_when_ready"] = StopWhenReady;
            dict["super_seeding"] = SuperSeeding;
            dict["total_downloaded"] = TotalUploaded;
            dict["total_uploaded"] = TotalUploaded;

            if (Trackers != null && Trackers.Count > 0)
            {
                var tierLists = new List<List<string>>();
                foreach (var trackerTier in Trackers)
                    tierLists.Add(trackerTier.Split(',').ToList());
                dict["trackers"] = tierLists;
            }
            else
            {
                dict["trackers"] = new List<object>();
            }

            if (Trees != null && Trees.Count > 0)
            {
                dict["trees"] = Trees.Select(node => node.ToDict()).ToList();
            }

            if (Unfinished != null && Unfinished.Count > 0)
            {
                var pieceDicts = new List<object>();
                foreach (var piece in Unfinished)
                    pieceDicts.Add(piece.ToDict());
                dict["unfinished"] = pieceDicts;
            }

            dict["upload_mode"] = UploadMode;
            dict["upload_rate_limit"] = UploadRateLimit;

            if (UrlSeeds != null && UrlSeeds.Count > 0)
            {
                dict["url-list"] = UrlSeeds.ToList();
            }
            else
            {
                dict["url-list"] = new List<string>();
            }

            foreach (var (key, value) in UnknownExtra)
                dict[key] = value;

            return dict;
        }

        public class MerkleTreeNode
        {
            public List<SHA256_Hash>? Hashes { get; set; }
            public List<bool>? Mask { get; set; }
            public List<bool>? Verified { get; set; }

            public MerkleTreeNode(Dictionary<string, object> dict)
            {
                FromDict(dict);
            }

            private void FromDict(Dictionary<string, object> dict)
            {
                foreach (var (key, value) in dict)
                {
                    switch (key)
                    {
                        case "hashes":
                            {
                                Hashes = Utils.ReadHashesSHA256((string)value);
                            }
                            break;
                        case "mask":
                            {
                                Mask = Utils.ReadBits((string)value);
                            }
                            break;
                        case "verified":
                            {
                                Verified = Utils.ReadBits((string)value);
                            }
                            break;
                    }
                }
            }

            public Dictionary<string, object> ToDict()
            {
                var dict = new Dictionary<string, object>();

                if (Hashes != null && Hashes.Count > 0)
                {
                    dict["hashes"] = Utils.WriteHashesSHA256(Hashes);
                }
                else
                {
                    dict["hashes"] = "";
                }

                if (Mask != null && Mask.Count > 0)
                {
                    dict["mask"] = Utils.WriteBits(Mask);
                }

                if (Verified != null && Verified.Count > 0)
                {
                    dict["verified"] = Utils.WriteBits(Verified);
                }

                return dict;
            }
        }

        public class PieceProgress
        {
            public int PieceIndex;
            public List<byte>? Bitmask;
            public uint Adler32;

            public PieceProgress(Dictionary<string, object> dict)
            {
                foreach (var (key, value) in dict)
                {
                    switch (key)
                    {
                        case "piece":
                            {
                                PieceIndex = (int)(long)value;
                            }
                            break;
                        case "mask":
                            {
                                Bitmask = Utils.ReadBitmask((string)value)!;
                            }
                            break;
                        case "adler32":
                            {
                                Adler32 = (uint)(long)value;
                            }
                            break;
                    }
                }
            }

            public Dictionary<string, object> ToDict()
            {
                var dict = new Dictionary<string, object>();

                dict["piece"] = PieceIndex;

                if (Bitmask != null && Bitmask.Count > 0)
                {
                    dict["bitmask"] = Utils.WriteBitmask(Bitmask);
                }

                dict["adler32"] = (long)Adler32;

                return dict;
            }
        }

        class Utils
        {
            public static List<SHA1_Hash> ReadHashesSHA1(string value)
            {
                var bytes = Bencode.Encoding.GetBytes(value);
                if (bytes.Length % SHA1_Hash.Length != 0)
                    throw new FormatException("Invalid SHA1 hash list");

                var result = new List<SHA1_Hash>(value.Length / SHA1_Hash.Length);
                for (int i = 0; i < bytes.Length; i += SHA1_Hash.Length)
                    result.Add(new SHA1_Hash(bytes.AsSpan(i, SHA1_Hash.Length)));
                return result;
            }

            public static string WriteHashesSHA1(List<SHA1_Hash> value)
            {
                var hashList = new MemoryStream();
                foreach (var hash in value)
                    hashList.Write(hash.ToArray());
                return Bencode.Encoding.GetString(hashList.ToArray());
            }

            public static string WriteHashesSHA256(List<SHA256_Hash> value)
            {
                var hashList = new MemoryStream();
                foreach (var hash in value)
                    hashList.Write(hash.ToArray());
                return Bencode.Encoding.GetString(hashList.ToArray());
            }

            public static List<SHA256_Hash> ReadHashesSHA256(string value)
            {
                var bytes = Bencode.Encoding.GetBytes(value);
                if (bytes.Length % SHA256_Hash.Length != 0)
                    throw new FormatException("Invalid SHA256 hash list");

                var result = new List<SHA256_Hash>(value.Length / SHA256_Hash.Length);
                for (int i = 0; i < bytes.Length; i += SHA256_Hash.Length)
                    result.Add(new SHA256_Hash(bytes.AsSpan(i, SHA256_Hash.Length)));
                return result;
            }

            public static List<IPEndPoint> ReadEndpoints(string value, int addressSize)
            {
                var result = new List<IPEndPoint>();
                var ms = new MemoryStream(Bencode.Encoding.GetBytes(value));
                using var br = new BinaryReader(ms);
                while (ms.Position < ms.Length)
                {
                    var address = new IPAddress(br.ReadBytes(addressSize));
                    var port = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
                    result.Add(new IPEndPoint(address, port));
                }
                return result;
            }

            public static string WriteEndpoints(List<IPEndPoint> endpoints, int addressSize)
            {
                var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms, Bencode.Encoding, true);
                foreach (var endpoint in endpoints)
                {
                    var addressBytes = endpoint.Address.GetAddressBytes();
                    if (endpoint.Port > ushort.MaxValue)
                        throw new FormatException("Invalid endpoint");
                    if (addressBytes.Length != addressSize)
                        throw new FormatException("Invalid endpoint");

                    bw.Write(addressBytes);
                    bw.Write(IPAddress.HostToNetworkOrder((short)endpoint.Port));
                }
                bw.Flush();
                return Bencode.Encoding.GetString(ms.ToArray());
            }

            public static List<byte>? ReadBitmask(string value)
            {
                return Bencode.Encoding.GetBytes(value).ToList();
            }

            public static string WriteBitmask(List<byte> value)
            {
                var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                foreach (var mask in value)
                    bw.Write(mask);
                return Bencode.Encoding.GetString(ms.ToArray());
            }

            public static List<bool> ReadBits(string value)
            {
                return value.Select(x => x == '1').ToList();
            }

            public static string WriteBits(List<bool> value)
            {
                return string.Join("", value.Select(x => x ? "1" : "0"));
            }
        }
    }
}
