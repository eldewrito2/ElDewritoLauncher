using System.Text;

namespace TorrentLib
{
    public class TorrentInfo
    {
        public List<string> Collections = new();
        public List<FileEntry> Files { get; set; } = new();
        public List<string> HttpSeeds = new();
        public long Length { get; set; }
        public string? Name { get; set; }
        public long PieceLength { get; set; }
        public List<SHA1_Hash> PieceHashes { get; set; } = new();
        public bool? Private { get; set; }
        public List<(string host, int port)> Nodes = new();
        public SHA1_Hash RootHash { get; set; } = new();
        public List<SHA1_Hash> Similiar = new();
        public string? Source { get; set; }
        public Dictionary<string, object> UnknownExtra { get; private set; } = new();

        public TorrentInfo(byte[] buffer)
        {
            var decoded = Bencode.Decode(buffer);
            if (decoded == null)
                throw new FormatException("Invalid torrent file");

            var dict = (Dictionary<string, object>)decoded;
            ReadDict(dict);
        }

        public TorrentInfo(Dictionary<string, object> dict)
        {
            ReadDict(dict);
        }

        private void ReadDict(Dictionary<string, object> dict)
        {
            foreach (var (k, v) in dict)
            {
                switch (k)
                {
                    case "collections":
                        ReadCollections((List<object>)v);
                        break;
                    case "files":
                        ReadFiles((List<object>)v);
                        break;
                    case "httpseeds":
                        ReadHttpSeeds((List<object>)v);
                        break;
                    case "length":
                        Length = (long)v;
                        break;
                    case "name":
                        Name = (string)v;
                        break;
                    case "name.utf-8":
                        Name = Encoding.UTF8.GetString(Bencode.Encoding.GetBytes((string)v));
                        break;
                    case "nodes":
                        ReadNodes((List<object>)v);
                        break;
                    case "piece length":
                        PieceLength = (int)(long)v;
                        break;
                    case "pieces":
                        ReadPeiceHashes((string)v);
                        break;
                    case "private":
                        Private = (long)v == 1;
                        break;
                    case "root hash":
                        Private = (long)v == 1;
                        break;
                    case "similiar":
                        ReadSimiliar((List<object>)v);
                        break;
                    case "source":
                        Source = (string)v;
                        break;
                    default:
                        UnknownExtra.Add(k, v);
                        break;
                }
            }
        }

        public Dictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>();

            if (Collections.Count > 0)
                dict["collections"] = Collections;

            if (Files.Count > 0)
            {
                var fileListEntry = new List<Dictionary<string, object>>();
                foreach (var file in Files)
                    fileListEntry.Add(file.ToDict());
                dict["files"] = fileListEntry;
            }

            if (HttpSeeds.Count > 0)
                dict["httpseeds"] = HttpSeeds;

            if (Length != 0)
                dict["length"] = Length;

            bool utf8Name = !Name!.Any(x => char.IsAscii(x));

            if (Name != null && !utf8Name)
                dict["name"] = Name;

            if (Name != null && utf8Name)
                dict["name.utf8"] = Bencode.Encoding.GetString(Encoding.UTF8.GetBytes(Name));

            dict["piece length"] = PieceLength;

            if (PieceHashes.Count > 0)
            {
                var buff = new StringBuilder();
                foreach (var hash in PieceHashes)
                    buff.Append(Bencode.Encoding.GetString(hash.ToArray()));
                dict["pieces"] = buff.ToString();
            }

            if (Private != null)
                dict["private"] = Private == true ? 1 : 0;

            if (Nodes.Count > 0)
            {
                var nodesListEntry = new List<object>();
                foreach (var node in Nodes)
                {
                    var nodeEntry = new List<object> { node.host, node.port };
                    nodesListEntry.Add(nodeEntry);
                }
                dict["nodes"] = nodesListEntry;
            }

            if (RootHash != SHA1_Hash.Empty)
                dict["root hash"] = Bencode.Encoding.GetString(RootHash.ToArray());

            if (Similiar.Count > 0)
                dict["similiar"] = Similiar;

            if (Source != null)
                dict["source"] = Source;

            foreach (var (k, v) in UnknownExtra)
                dict[k] = v;

            return dict;
        }

        private void ReadCollections(List<object> v)
        {
            foreach (string entry in v)
                Collections.Add(entry);
        }

        private void ReadFiles(List<object> v)
        {
            foreach (Dictionary<string, object> entry in v)
                Files.Add(new FileEntry(entry));
        }


        private void ReadHttpSeeds(List<object> v)
        {
            foreach (string entry in v)
                HttpSeeds.Add(entry);
        }

        private void ReadNodes(List<object> v)
        {
            foreach (List<object> entry in v)
            {
                string host = (string)entry[0];
                int port = (int)(long)entry[1];
                Nodes.Add((host, port));
            }
        }

        private void ReadPeiceHashes(string v)
        {
            PieceHashes = new List<SHA1_Hash>();
            var bytes = Bencode.Encoding.GetBytes(v);
            if (bytes.Length % SHA1_Hash.Length != 0)
                throw new FormatException("Piece hashes are invalid");

            int offset = 0;
            while (offset < bytes.Length)
            {
                PieceHashes.Add(new SHA1_Hash(bytes.Skip(offset).Take(SHA1_Hash.Length).ToArray()));
                offset += SHA1_Hash.Length;
            }
        }

        private void ReadSimiliar(List<object> v)
        {
            foreach (string entry in v)
                Similiar.Add(new SHA1_Hash(Bencode.Encoding.GetBytes(entry)));
        }
    }
}
