namespace TorrentLib
{
    public class TorrentStatusUpdatedEventArgs : TorrentEventArgs
    {
        public readonly TorrentStatus Status;

        public unsafe TorrentStatusUpdatedEventArgs(int torrentId, TorrentStatus status) : base(torrentId)
        {
            Status = status;
        }
    }
}
