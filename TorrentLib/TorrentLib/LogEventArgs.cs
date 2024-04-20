namespace TorrentLib
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(int type, string message)
        {
            Type = type;
            Message = message;
        }

        public int Type { get; }
        public string Message { get; }
    }
}