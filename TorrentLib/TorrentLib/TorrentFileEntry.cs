using System.Text;

namespace TorrentLib
{
    public record FileEntry
    {
        public string? Path { get; set; }
        public long Length { get; set; }
        public string? Attr { get; set; }
        public SHA1_Hash SHA1 { get; set; } = new();
        public DateTimeOffset? ModifiedTime { get; set; }
        public string? SymlinkPath { get; set; }

        public FileEntry()
        {
            
        }

        public FileEntry(Dictionary<string, object> dict)
        {
            LoadDict(dict);
        }

        private void LoadDict(Dictionary<string, object> dict)
        {
            foreach (var (k, v) in dict)
            {
                switch (k)
                {
                    case "path":
                        Path = ReadPath((List<object>)v);
                        break;
                    case "path.utf-8":
                        Path = ReadPathUTF8((List<object>)v);
                        break;
                    case "length":
                        Length = (long)v;
                        break;
                    case "attr":
                        Attr = (string)v;
                        break;
                    case "mtime" when v is long:
                        ModifiedTime = DateTimeOffset.FromUnixTimeSeconds((long)v);
                        break;
                    case "symlink path":
                        SymlinkPath = (string)v;
                        break;
                    case "sha1":
                        SHA1 = SHA1_Hash.Parse((string)v);
                        break;
                }
            }
        }

        public Dictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>();
            bool isUtf8 = !Path!.Any(x => char.IsAscii(x));

            if (Path != null)
            {
                if (isUtf8)
                {
                    dict["path.utf-8"] = Path!.Split(System.IO.Path.DirectorySeparatorChar)
                        .Select(s => Bencode.Encoding.GetString(Encoding.UTF8.GetBytes(s)))
                        .ToList();
                }
                else
                {
                    dict["path"] = Path!.Split(System.IO.Path.DirectorySeparatorChar);
                }
            }

            dict["length"] = Length;

            if (Attr != null)
                dict["attr"] = Attr;

            if (ModifiedTime != null)
                dict["mtime"] = ModifiedTime.Value.ToUnixTimeSeconds();

            if (SymlinkPath != null)
                dict["symlink path"] = SymlinkPath;

            if (SHA1 != SHA1_Hash.Empty)
                dict["sha1"] = SHA1.ToString();

            return dict;
        }


        private string ReadPath(List<object> v)
        {
            return string.Join(System.IO.Path.DirectorySeparatorChar, v.OfType<string>().ToArray());
        }

        private string ReadPathUTF8(List<object> v)
        {
            return string.Join(System.IO.Path.DirectorySeparatorChar, v.OfType<string>()
                .Select(s => Encoding.UTF8.GetString(Bencode.Encoding.GetBytes(s)))
                .ToArray());
        }
    }
}
