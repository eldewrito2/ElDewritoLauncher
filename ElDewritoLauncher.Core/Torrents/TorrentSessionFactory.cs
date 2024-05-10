using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    /// <summary>
    /// A class to enforce that only one session exists at any one time
    /// </summary>
    public class TorrentSessionFactory
    {
        private readonly ILogger<TorrentSessionFactory> _logger;
        private IServiceProvider _serviceProvider;
        private TrackedSession? _session;
        private object _mutex = new object();
        private int _nextId = 0;

        public TorrentSessionFactory(ILogger<TorrentSessionFactory> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Session Create(bool enableDebugLog, int port)
        {
            lock (_mutex)
            {
                if (_session == null)
                {

                    _session = CreateSession(_nextId, enableDebugLog, port);
                    _logger.LogDebug($"Created session. id={_session.SessionId}, tid={Thread.CurrentThread.ManagedThreadId}");
                }
                else
                {
                    _session.AddRef();
                    _logger.LogDebug($"Reusing session id={_session.SessionId}, ref_count={_session.RefCount}, tid={Thread.CurrentThread.ManagedThreadId}");
                }
                return _session;
            }
        }

        private void OnSessionDiposed()
        {
            _logger.LogDebug($"Disposed session id={_session!.SessionId}, tid={Thread.CurrentThread.ManagedThreadId}");
            _session = null;
        }

        private void OnSessionReleased(TrackedSession trackedSession)
        {
            Debug.Assert(ReferenceEquals(_session, trackedSession));
            _logger.LogDebug($"Released session id={_session!.SessionId}, ref_count={_session.RefCount}, tid={Thread.CurrentThread.ManagedThreadId}");
        }

        private TrackedSession CreateSession(int nextId, bool enableDebugLog, int port)
        {
            var settings = new SessionSettings()
            {
                PollInterval = TimeSpan.FromMilliseconds(33),
                SynchronizationContext = SynchronizationContext.Current,
                LTSettings =
                {
                    ["listen_interfaces"] = $"0.0.0.0:{port},[::]:{port}",
                    ["dht_bootstrap_nodes"] = "dht.libtorrent.org:25401,router.bittorrent.com:6881,dht.transmissionbt.com:6881,dht.aelitis.com:6881,192.168.0.155:42306",
                    ["enable_dht"] = true
                },
#if PRODUCTION
                UseAuth = false,
#else
                UseAuth = true
#endif
            };

            settings.UseLogging = enableDebugLog;

            var session = new TrackedSession(this, nextId, settings);

            var peerCache = _serviceProvider.GetRequiredService<IPeerCache>();
            var logger = _serviceProvider.GetRequiredService<ILogger<Session>>();

            // this is kind of hacky, but it'll work for now
            session.PeerActivity += (s, e) =>
            {
                if (e.Type == PeerActivityType.Connected)
                {
                    peerCache.AddPeer(new Peer(e.Ip, e.Port));
                }
            };

            session.Logged += (s, e) =>
            {
                logger.LogTrace(e.Message);
            };

            return session;
        }

        class TrackedSession : Session
        {
            private TorrentSessionFactory _factory;
            private volatile int _refCount = 1;

            public TrackedSession(TorrentSessionFactory factory, int sessionId, SessionSettings settings) : base(settings)
            {
                _factory = factory;
                SessionId = sessionId;
            }

            public int SessionId { get; }

            public int RefCount => _refCount;

            internal void AddRef()
            {
                lock (_factory._mutex)
                {
                    _refCount++;
                }
            }

            public override void Dispose()
            {
                lock (_factory._mutex)
                {
                    int newRefcount = _refCount--;

                    _factory.OnSessionReleased(this);

                    if (newRefcount == 1)
                    {
                        base.Dispose();
                        _factory.OnSessionDiposed();
                    }
                }
            }
        }
    }
}
