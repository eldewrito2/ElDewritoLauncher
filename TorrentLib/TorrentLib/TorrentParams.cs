using System.Net;

namespace TorrentLib
{
    public class TorrentParams
    {
        /// <summary>
        /// In case there's no other name in this torrent, this name will be used.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The path where the torrent is or will be stored.
        /// </summary>
        public string? SavePath { get; set; }

        /// <summary>
        /// A mapping of file index to new filenames.
        /// </summary>
        public Dictionary<int, string> RenamedFiles { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// A mapping of file index to download priority.
        /// </summary>
        public Dictionary<int, DownloadPriority> FilePriorties { get; set; } = new Dictionary<int, DownloadPriority>();

        /// <summary>
        /// List of DHT nodes in the format: hostname:port
        /// </summary>
        public List<string> DHTNodes { get; set; } = new List<string>();

        /// <summary>
        /// Peers to add to the torrent
        /// </summary>
        public List<IPEndPoint> Peers { get; set; } = new();

        /// <summary>
        /// List of HTTP seeds add to the torrent.
        /// </summary>
        public List<string> HttpSeeds { get; set; } = new List<string>();

        /// <summary>
        /// List of url seeds to add to the torrent.
        /// </summary>
        public List<string> UrlSeeds { get; set; } = new List<string>();

        /// <summary>
        /// List of trackers to add to the torrent
        /// </summary>
        public List<string> Trackers { get; set; } = new List<string>();

        /// <summary>
        /// Initial flags to set on the torrent.
        /// </summary>
        public TorrentFlags Flags { get; set; } = TorrentFlags.Default;

        /// <summary>
        /// the download limit for this torrent, specified in bytes per second. -1 means unlimited.
        /// </summary>
        public int DownloadLimit { get; set; } = -1;

        /// <summary>
        /// the upload limit for this torrent, specified in bytes per second. -1 means unlimited.
        /// </summary>
        public int UploadLimit { get; set; } = -1;


        internal byte[] Encode()
        {
            var dict = new Dictionary<string, object?>();
            dict["name"] = Name;
            dict["save_path"] = SavePath;
            dict["renamed_files"] = RenamedFiles.ToDictionary(k => k.Key.ToString(), v => v.Value);
            dict["file_priorities"] = FilePriorties.ToDictionary(k => k.Key.ToString(), v => (int)v.Value);
            dict["nodes"] = DHTNodes;
            dict["peers"] = Peers.Select(x => x.ToString()).ToList();
            dict["http_seeds"] = HttpSeeds;
            dict["url_seeds"] = UrlSeeds;
            dict["trackers"] = Trackers;
            dict["flags"] = (int)Flags;
            dict["download_limit"] = DownloadLimit;
            dict["upload_limit"] = UploadLimit;
            return Bencode.Encode(dict);
        }
    }
}
