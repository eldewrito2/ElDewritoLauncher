using InstallerLib.Packages;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    public record TorrentPackageCacheOptions(string Directory, int MaxItems);

    public class TorrentPackageCache : IPackageCache
    {
        private readonly string _directory;
        private int _maxItems;
        private object _mutex = new object();
        private DateTimeOffset _lastPruned = DateTimeOffset.MinValue;
        private ILogger _logger;

        public TorrentPackageCache(ILogger<TorrentPackageCache> logger, TorrentPackageCacheOptions options)
        {
            _logger = logger;
            _directory = options.Directory;
            _maxItems = options.MaxItems;
            Directory.CreateDirectory(_directory);
        }

        public async Task AddPackageAsync(IPackage package)
        {
            var filePath = Path.Combine(_directory, package.Name, package.Version.ToNormalizedString(), $"{package.Id}.torrent");
            if (File.Exists(filePath))
            {
                _logger.LogInformation($"Package file already in cache, overwriting '{filePath}'");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var torrentPackage = (TorrentPackage)package;
            var contents = Bencode.Encode(torrentPackage.Torrent.ToDict());
            await File.WriteAllBytesAsync(filePath, contents).ConfigureAwait(false);

            _logger.LogInformation($"Added package to cache '{filePath}'");

            MaintainCacheCapacity(package.Name);
        }

        public Task DeletePackageAsync(string id)
        {
            string? filePath = FindPackageFileById(id);
            if (filePath != null && File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"Package deleted from cache {filePath}");
            }
            return Task.CompletedTask;
        }

        public async Task<IPackage?> GetPackageAsync(string id)
        {
            string? path = FindPackageFileById(id);
            if (path == null)
            {
                return null;
            }
            return await ReadPackageAsync(path).ConfigureAwait(false);
        }

        public Task<string?> FindPackageForVersionAsync(string name, string version)
        {
            return Task.FromResult(FindPackageForVersion(name, version));
        }

        private async Task<IPackage?> ReadPackageAsync(string path)
        {
            byte[] data = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            string infohash = Path.GetFileNameWithoutExtension(path);
            return new TorrentPackage(infohash, new TorrentFile(data));
        }

        private string? FindPackageForVersion(string name, string version)
        {
            string packageDirectory = Path.Combine(_directory, name, version);
            if (!Directory.Exists(packageDirectory))
            {
                return null;
            }

            string? path = Directory.GetFiles(packageDirectory, "*.torrent").FirstOrDefault();
            if (path == null)
            {
                return null;
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        private string? FindPackageFileById(string id)
        {
            return Directory.GetFiles(Path.Combine(_directory), $"{id}.torrent", SearchOption.AllDirectories).FirstOrDefault();
        }

        public Task<string?> GetPackageFilePath(string id)
        {
            return Task.FromResult(FindPackageFileById(id));
        }

        private void MaintainCacheCapacity(string packageName)
        {
            lock (_mutex)
            {
                if (DateTimeOffset.UtcNow - _lastPruned < TimeSpan.FromSeconds(60))
                    return;
                _lastPruned = DateTimeOffset.UtcNow;
            }

            _logger.LogTrace($"Maintaining package cache...");

            try
            {
                var directoryInfo = new DirectoryInfo(Path.Combine(_directory, packageName));
                if (!directoryInfo.Exists)
                    return;

                var versionFolders = directoryInfo
                     .GetDirectories()
                     .OrderBy(x => x.CreationTimeUtc)
                     .ToList();

                foreach (var dir in versionFolders.Take(Math.Max(0, versionFolders.Count - _maxItems)))
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch (SystemException ex)
                    {
                        _logger.LogError(ex, $"Failed to delete package cache version directory '${dir.FullName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occured while maintaining package cache");
            }
        }
    }
}
