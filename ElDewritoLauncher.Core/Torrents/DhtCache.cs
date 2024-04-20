using EDLauncher.Core.Install;
using NuGet.Versioning;
using static TorrentLib.DHT;

namespace EDLauncher.Core.Torrents
{
    /// <summary>
    /// A runtime cache of last know dhtitem seen
    /// warning: the programmer was not sober writing this
    /// </summary>
    public class DhtCache
    {
        private DHTItem? _currentDht;
        private object _mutex = new();

        /// <summary>
        /// Replace our cached dht value if its sequence is higher than the current (or current is not set)
        /// if the sequence value is larger than last know
        /// </summary>
        /// <param name="dhtInput"></param>
        /// <returns>True if replaced cached dht item</returns>
        public bool ReplaceIfNewer(DHTItem dhtInput)
        {
            lock (_mutex)
            {
                if (IsNewerVersion(dhtInput))
                {
                    _currentDht = dhtInput;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns the last seen dht if there is one
        /// </summary>
        /// <returns></returns>
        public DHTItem? GetCurrentDHT()
        {
            lock (_mutex)
            {
                return _currentDht;
            }
        }

        private bool IsNewerVersion(DHTItem dhtInput)
        {
            if (_currentDht == null)
                return true;

            // We need to check the version incase the sequence gets reset
            ReleaseInfo newRelease = ReleaseInfo.Decode(dhtInput.Value!);
            ReleaseInfo oldRelease = ReleaseInfo.Decode(dhtInput.Value!);
            SemanticVersion newVersion = SemanticVersion.Parse(newRelease.Version);
            SemanticVersion oldVersion = SemanticVersion.Parse(newRelease.Version);

            return ReleaseInfo.GetChannel(newRelease.Version)  != ReleaseInfo.GetChannel(oldRelease.Version) 
                || newVersion > oldVersion;
        }
    }
}
