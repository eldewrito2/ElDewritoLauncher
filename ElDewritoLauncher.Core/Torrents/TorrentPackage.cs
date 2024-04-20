using InstallerLib.Packages;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    public class TorrentPackage : IPackage
    {
        public TorrentFile Torrent { get; }

        public TorrentPackage(string infohash, TorrentFile torrent)
        {
            Uri = BuildMagnetUri(infohash, torrent);
            Id = infohash;
            Torrent = torrent;
            Name = torrent.Info.Name;
            Version = SemanticVersion.Parse((string)torrent.Info.UnknownExtra["ed_version"]);
            Files = new List<PackageFileEntry>();
            foreach (var torrentFileEntry in torrent.Info.Files)
            {
                Files.Add(new PackageFileEntry(torrentFileEntry.Path, torrentFileEntry.Length));
            }
        }

        public TorrentPackage(Uri uri, TorrentFile torrent)
        {
            Uri = uri.AbsoluteUri;
            Id = ExtractInfoHashFromMagnetUri(Uri);
            Torrent = torrent;
            Name = torrent.Info.Name;
            Version = SemanticVersion.Parse((string)torrent.Info.UnknownExtra["ed_version"]);
            Files = new List<PackageFileEntry>();
            foreach (var torrentFileEntry in torrent.Info.Files)
            {
                Files.Add(new PackageFileEntry(torrentFileEntry.Path, torrentFileEntry.Length));
            }
        }

        public string Id { get; }
        public string Uri { get; }
        public string Name { get; }
        public SemanticVersion Version { get; }
        public List<PackageFileEntry> Files { get; }

        private static string BuildMagnetUri(string infohash, TorrentFile torrent)
        {
            var trackers = new List<string>();

            foreach (var url in torrent.Trackers)
                trackers.Add(url);

            if (torrent.Info!.UnknownExtra.ContainsKey("trackers"))
            {
                foreach (var tracker in ((List<object>)torrent.Info.UnknownExtra["trackers"]).Cast<string>())
                    trackers.Add(tracker);
            }

            return BuildMagnetUri(infohash, torrent.Info.Name!, trackers.Distinct().ToArray());
        }

        private static string BuildMagnetUri(string infoHash, string name, string[] trackers)
        {
            StringBuilder magnetUriBuilder = new StringBuilder("magnet:?xt=urn:btih:");
            magnetUriBuilder.Append(infoHash);
            magnetUriBuilder.Append("&dn=");
            magnetUriBuilder.Append(name);
            foreach (string tracker in trackers)
            {
                magnetUriBuilder.Append("&tr=");
                magnetUriBuilder.Append(System.Uri.EscapeDataString(tracker));
            }
            return magnetUriBuilder.ToString();
        }

        public static string ExtractInfoHashFromMagnetUri(string magnetUri)
        {
            var match = Regex.Match(magnetUri, @"xt=urn:btih:([a-fA-F0-9]{40})");
            return match.Groups[1].Value;
        }
    }
}