using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EDLauncher.Core.Torrents
{
    public record struct Peer(string ip, int port);

    public interface IPeerCache
    {
        void AddPeer(Peer peer);
        List<Peer> GetPeerList();
        void SavePeerList();
    }

    public record PeerCacheOptions(int MaxItems, string FilePath);

    public class NullPeerCache : IPeerCache
    {
        public void AddPeer(Peer peer)
        {
        }

        public List<Peer> GetPeerList()
        {
            return new List<Peer>();
        }

        public void SavePeerList()
        {
        }
    }

    public class PeerCache : IPeerCache
    {
        private readonly List<Peer> _peerList;
        private readonly int _maxItems;
        private readonly string _filePath;

        public PeerCache(PeerCacheOptions options)
        {
            _maxItems = options.MaxItems;
            _filePath = options.FilePath;
            _peerList = LoadPeerList();
        }

        public void AddPeer(Peer peer)
        {
            lock (_peerList)
            {
                _peerList.Remove(peer);
                _peerList.Insert(0, peer);
                while (_peerList.Count > _maxItems)
                    _peerList.RemoveAt(_peerList.Count - 1);
            }
        }

        public List<Peer> GetPeerList()
        {
            lock (_peerList)
            {
                return _peerList.ToList();
            }
        }

        public void SavePeerList()
        {
            using var writer = File.CreateText(_filePath);
            foreach (var peer in _peerList)
            {
                writer.WriteLine($"{peer.ip}:{peer.port}");
            }
        }

        private List<Peer> LoadPeerList()
        {
            var list = new List<Peer>();
            if (File.Exists(_filePath))
            {
                using var reader = File.OpenText(_filePath);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        list.Add(new Peer(parts[0], int.Parse(parts[1])));
                    }
                }
            }
            return list;
        }
    }
}
