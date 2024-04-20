namespace TorrentLib
{
    using static NativeApi;

    public class TorrentCreator
    {
        /// <summary>
        /// The size of each piece in bytes, must be pow2 >= 16KiB
        /// If a piece size of 0 is specified, it will be set automatically.
        /// </summary>
        public long PieceSize { get; set; } = 0;

        /// <summary>
        /// Full path to the file or directory to add to the torrent
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Optional comment to add to the torrent
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Optional creator of the torrent
        /// </summary>
        public string? Creator { get; set; }

        /// <summary>
        /// Optional creation date of the torrent. Defaults to the current time
        /// </summary>
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// List of http seeds to add to the torrent
        /// </summary>
        public List<string> HttpSeeds { get; set; } = new List<string>();

        /// <summary>
        /// List of url seeds to add to the torrent.
        /// </summary>
        public List<string> UrlSeeds { get; set; } = new List<string>();

        /// <summary>
        /// List of DHT nodes in the format hostnam:port
        /// </summary>
        public List<string> DHTNodes { get; set; } = new List<string>();

        /// <summary>
        /// List of tracker urls
        /// </summary>
        public List<string> Trackers { get; set; } = new List<string>();

        /// <summary>
        /// Sets whether this torrent is private, should not use DHT
        /// </summary>
        public bool Private { get; set; } = false;


        /// <summary>
        /// Additional creation flags
        /// </summary>
        public CreateFlags Flags { get; set; }


        [Flags]
        public enum CreateFlags
        {
            ModificationTime = 1 << 0,
            Symlinks = 1 << 1,
            V2_Only = 1 << 2,
            V1_Only = 1 << 3,
            CanonicalFiles = 1 << 4,
            NoAttributes = 1 << 5,
            CanonicalFilesNoTailPadding = 1 << 6,
        }


        /// <summary>
        /// Creates the torrent file.
        /// </summary>
        /// <returns>A torrent file dict. Use Bencode.Encode() to serialize</returns>
        public unsafe object Create()
        {
            var dict = new Dictionary<string, object?>();
            dict["piece_size"] = PieceSize;
            dict["full_path"] = FilePath;
            dict["comment"] = Comment;
            dict["creator"] = Creator;
            dict["creation_date"] = CreationDate.ToUnixTimeSeconds();
            if (HttpSeeds.Count > 0)
                dict["http_seeds"] = HttpSeeds;
            if (UrlSeeds.Count > 0)
                dict["url_seeds"] = UrlSeeds;
            if (DHTNodes.Count > 0)
                dict["nodes"] = DHTNodes;
            if (Trackers.Count > 0)
                dict["trackers"] = Trackers;
            dict["private"] = Private ? 1 : 0;

            dict["modification_time"] = Flags.HasFlag(CreateFlags.ModificationTime) ? 1 : 0;
            dict["symlinks"] = Flags.HasFlag(CreateFlags.Symlinks) ? 1 : 0;
            dict["v1_only"] = Flags.HasFlag(CreateFlags.V1_Only) ? 1 : 0;
            dict["v2_only"] = Flags.HasFlag(CreateFlags.V2_Only) ? 1 : 0;
            dict["no_attributes"] = Flags.HasFlag(CreateFlags.NoAttributes) ? 1 : 0;
            dict["canonical_files"] = Flags.HasFlag(CreateFlags.CanonicalFiles) ? 1 : 0;
            dict["canonical_files_no_tail_padding"] = Flags.HasFlag(CreateFlags.CanonicalFilesNoTailPadding) ? 1 : 0;

            byte[] input = Bencode.Encode(dict);
            object? output = null;
            int result = create_torrent(input, input.Length, (byte* buffer, int size) => output = Bencode.Decode(buffer, size));
            if (result != 0)
                throw new TorrentException(result);
            return output!;
        }
    }
}
