using static TorrentLib.NativeApi;

namespace TorrentLib
{
    internal class SessionLoop
    {
        private volatile bool _stopped = false;

        private Thread? _thread;
        private Session _session;
        private SessionSettings _settings;

        public bool IsStopped => _stopped;

        public SessionLoop(Session session)
        {
            _session = session;
            _settings = session.Settings;
        }

        public void Start()
        {
            _thread = new Thread(RunLoop);
            _thread.Name = "session";
            _thread.IsBackground = true;
            _thread.Start();
        }

        public async ValueTask StopAsync()
        {
            await Task.Run(() => Stop()).ConfigureAwait(false);
        }

        public void Stop()
        {
            _stopped = true;
            // wake up libtorrent
            _session.PostTorrentUpdates();
            _thread!.Join();
        }

        private void RunLoop()
        {
            DateTimeOffset lastPostTorrentUpdates = DateTimeOffset.MinValue;
            while (!_stopped)
            {
                _session.Poll(_settings.PollInterval);

                if ((DateTimeOffset.UtcNow - lastPostTorrentUpdates) >= _settings.TorrentUpdateInterval)
                {
                    lastPostTorrentUpdates = DateTimeOffset.UtcNow;
                    _session.PostTorrentUpdates();
                }
            }
        }
    }
}
