using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InstallerLib.Packages
{
    public class PackageDownloadParameters
    {
        public Dictionary<string, string>? RenamedFiles;
        public BitArray? FilesToDownload;
    }

    public enum DownloadStatus
    {
        Unknown,
        Preparing,
        Downloading,
        Checking,
        Finished,
        Error
    }

    public record struct DownloadProgress(
        DownloadStatus Status,
        double Progress = 0, // [0-1]
        long Current = 0,
        long Total = 0,
        int DownloadRate = 0,
        TimeSpan ETA = default)
    { }


    public interface IPackageDownloader
    {
        Task<IPackage> GetPackageAsync(string packageUri, CancellationToken cancellationToken = default);

        Task DownloadPackageContentsAsync(
            IPackage package,
            string downloadDirectory,
            PackageDownloadParameters parameters,
            Action<DownloadProgress>? progress = default,
            CancellationToken cancellationToken = default);
    }
}
