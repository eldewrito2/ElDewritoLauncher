using static TorrentLib.NativeApi;

namespace TorrentLib
{
    internal class ResumeDataQueue
    {
        public delegate void ResumeDataCallback(int err, IntPtr torrentParams);

        private List<ResumeDataRequest> _resumeDataRequests;
        private Session _session;

        public ResumeDataQueue(Session session)
        {
            _session = session;
            _resumeDataRequests = new List<ResumeDataRequest>();
        }

        public unsafe void Enqueue(int torrentId, ResumeDataCallback callback)
        {
            lock(_resumeDataRequests)
                _resumeDataRequests.Add(new ResumeDataRequest(torrentId, callback));
            torrent_save_resume_data(_session.Handle, torrentId);
        }

        public unsafe void OnTorrentSaveResumeDataCompleted(int err, int torrent_id, torrent_add_params* atp)
        {
            List<ResumeDataRequest> requests;
            lock(_resumeDataRequests)
                requests = _resumeDataRequests.Where(req => req.TorrentId == torrent_id).ToList();

            foreach (var req in requests)
            {
                _resumeDataRequests.Remove(req);
                req.Callback(err, (IntPtr)atp);
            }
        }

        class ResumeDataRequest
        {
            public readonly int TorrentId;
            public readonly ResumeDataCallback Callback;

            public ResumeDataRequest(int torrentId, ResumeDataCallback callback)
            {
                TorrentId = torrentId;
                Callback = callback;
            }
        }
    }
}
