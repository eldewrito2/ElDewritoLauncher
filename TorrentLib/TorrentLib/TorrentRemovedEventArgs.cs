namespace TorrentLib
{
    internal class TorrentRemovedEventArgs : EventArgs
    {
        public int ErrorCode { get; }

        public SHA1_Hash InfoHash { get; }

        /// <summary>
        /// true when deleteFiles was requested and the files have been deleted
        /// </summary>
        public bool Deleted { get; }

        public TorrentRemovedEventArgs(int errorCode, SHA1_Hash infoHash, bool deleted)
        {
            ErrorCode = errorCode;
            InfoHash = infoHash;
            Deleted = deleted;
        }
    }
}