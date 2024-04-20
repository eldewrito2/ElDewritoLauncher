using static TorrentLib.NativeApi;

namespace TorrentLib
{
    internal class ReadPieceQueue
    {
        public delegate void ReadPieceCallback(int err, IntPtr data, int size);

        private List<ReadPieceRequest> _requests;
        private Session _session;

        public ReadPieceQueue(Session session)
        {
            _session = session;
            _requests = new List<ReadPieceRequest>();
        }

        public unsafe void Enqueue(int torrentId, int pieceIndex, ReadPieceCallback callback)
        {
            int pending = 0;
            lock (_requests)
            {
                pending = _requests.Count;
                _requests.Add(new ReadPieceRequest(torrentId, pieceIndex, callback));
            }

            torrent_set_piece_priority(_session.Handle, torrentId, pieceIndex, (int)DownloadPriority.Top);
            torrent_set_piece_deadline(_session.Handle, torrentId, pieceIndex, pending * 100, alert_when_available: true);
            //torrent_read_piece(_session.Handle, torrentId, pieceIndex);
        }

        public unsafe void OnReadPieceCompleted(int err, int torrent_id, int pieceIndex, byte* buffer, int size)
        {
            List<ReadPieceRequest> requests;
            lock (_requests)
                requests = _requests.Where(req => req.TorrentId == torrent_id && req.PieceIndex == pieceIndex).ToList();

            foreach (var req in requests)
            {
                _requests.Remove(req);
                req.Callback(err, (IntPtr)buffer, size);
            }
        }

        class ReadPieceRequest
        {
            public readonly int TorrentId;
            public readonly int PieceIndex;
            public readonly ReadPieceCallback Callback;

            public ReadPieceRequest(int torrentId, int pieceIndex, ReadPieceCallback callback)
            {
                TorrentId = torrentId;
                PieceIndex = pieceIndex;
                Callback = callback;
            }
        }
    }
}
