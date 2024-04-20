using EDLauncher.Core.Torrents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstallerLib.Packages
{
    public interface IPackageDownloadSizeCalculator
    {
        Task<long> CalculcateAsync(IPackage package, string directory, Dictionary<string, string>? renamedFile);
    }

    public class PackageDownloadSizeCalculcator : IPackageDownloadSizeCalculator
    {
        public Task<long> CalculcateAsync(IPackage package, string directory, Dictionary<string, string>? renamedFile)
        {
            if (package is TorrentPackage torrentPackage)
            {
                return new TorrentDownloadSizeCalculcator(torrentPackage.Torrent).CalculcateAsync(directory, renamedFile);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
