namespace TorrentLib
{
    public class TorrentFileErrorEventArgs : TorrentEventArgs
    {
        public int ErrorCode { get; }
        public string FileName { get; }
        public string Message { get; }

        public TorrentFileErrorEventArgs(int torrentId, int errorCode, string fileName, string message)
            : base(torrentId)
        {
            ErrorCode = errorCode;
            FileName = fileName;
            Message = message;
        }
    }
}