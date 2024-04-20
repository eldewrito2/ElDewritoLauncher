namespace TorrentLib
{
    public class TorrentFile
    {
        public List<string> Trackers = new();
        public string? Comment { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public TorrentInfo? Info { get; set; }
        public List<string> WebSeeds { get; set; } = new();
        public Dictionary<string, object> UnknownExtra { get; set; } = new();

        public TorrentFile(byte[] buffer)
        {
            var decoded = Bencode.Decode(buffer);
            if (decoded == null)
                throw new FormatException("Invalid torrent file");
            var dict = (Dictionary<string, object>)decoded;
            ReadDict(dict);
        }

        public TorrentFile(Dictionary<string, object> dict)
        {
            ReadDict(dict);
        }

        public Dictionary<string, object> ToDict()
        {
            var dict = new Dictionary<string, object>();

            if (Trackers.Count > 0)
            {
                dict["announce"] = Trackers[0];

                if (Trackers.Count > 1)
                    dict["announce-list"] = Trackers.Skip(1).Select(entry => new List<string> { entry }).ToList();
            }

            if (Comment != null)
                dict["comment"] = Comment;

            if (CreatedBy != null)
                dict["created by"] = CreatedBy;

            if (CreationDate != null)
                dict["creation date"] = CreationDate.Value.ToUnixTimeSeconds();

            dict["info"] = Info!.ToDict();

            if (WebSeeds.Count > 0)
                dict["url-list"] = WebSeeds;

            foreach (var (k, v) in UnknownExtra)
                dict[k] = v;

            return dict;
        }

        private void ReadDict(Dictionary<string, object> dict)
        {
            foreach (var (k, v) in dict)
            {
                switch (k)
                {
                    case "announce":
                        Trackers.Add((string)v);
                        break;
                    case "announce-list":
                        ReadAnnounceList((List<object>)v);
                        break;
                    case "created by":
                        CreatedBy = (string)v;
                        break;
                    case "creation date":
                        CreationDate = DateTimeOffset.FromUnixTimeSeconds((long)v);
                        break;
                    case "comment":
                        Comment = (string)v;
                        break;
                    case "info":
                        Info = new TorrentInfo((Dictionary<string, object>)v);
                        break;
                    case "url-list":
                        ReadUrlList((List<object>)v);
                        break;
                    default:
                        UnknownExtra.Add(k, v);
                        break;
                }
            }
        }

        private void ReadAnnounceList(List<object> v)
        {
            foreach (List<object> tier in v)
                foreach (string entry in tier)
                    Trackers.Add(entry);
        }

        private void ReadUrlList(List<object> v)
        {
            foreach (string entry in v)
            {
                WebSeeds.Add(entry);
            }
        }
    }
}
