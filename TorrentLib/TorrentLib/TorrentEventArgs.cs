namespace TorrentLib
{
    public class TorrentEventArgs : EventArgs
    {
        public readonly int TorrentId;

        public TorrentEventArgs(int torrentId)
        {
            TorrentId = torrentId;
        }
    }
}