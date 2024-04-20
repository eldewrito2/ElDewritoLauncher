namespace TorrentLib
{
    public enum PeerActivityType
    {
        Connected = 23,
        Disconnected = 24,
        Banned = 19,
        Blocked = 54,
        Error = 22
    }

    public enum Direction
    {
        Unspecified,
        In,
        Out
    }

    public class PeerActivityEventArgs : EventArgs
    {
        public PeerActivityType Type { get; }
        public int TorrentId { get; }
        public string Ip { get; }
        public int Port { get; }
        public Direction Direction { get; }
        public int CloseReason { get; }
        public int Error { get; }

        public PeerActivityEventArgs(PeerActivityType type, int torrentId, string ip, int port, Direction direction, int closeReason, int error)
        {
            Type = type;
            TorrentId = torrentId;
            Ip = ip;
            Port = port;
            Direction = direction;
            CloseReason = closeReason;
            Error = error;
        }
    }
}