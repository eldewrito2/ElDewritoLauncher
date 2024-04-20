using EDLauncher.Core.Install;
using InstallerLib.Events;
using InstallerLib.Install;
using InstallerLib.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// TODO some of this stuff should be moved into EDLauncher.Core

namespace EDLauncher.Launcher.Services
{
    public record UpdateCheckResult(bool isAvailable, IPackage? package = null, long downloadSize = 0)
    {
        public static UpdateCheckResult NoUpdateAvailable() => new UpdateCheckResult(false);
        public static UpdateCheckResult UpdateAvailable(IPackage package, long downloadSize) => new UpdateCheckResult(true, package, downloadSize);
    }

    public interface IUpdateService
    {
        Task<UpdateCheckResult> CheckForUpdateAsync(string releaseChannel, string currentVersion, string directory, CancellationToken cancellationToken = default);
        Task DownloadUpdateAsync(string packageUri, string directory, Action<DownloadProgress> progress, CancellationToken cancellationToken = default);
        Task<List<Process>> CheckProgramsNeedClosing(IPackage package, string directory);
    }

    public class UpdateService : IUpdateService
    {
        public async Task DownloadUpdateAsync(string packageUri, string directory, Action<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            await using var scope = App.ServiceProvider.CreateAsyncScope();
            var releaseService = scope.ServiceProvider.GetRequiredService<IReleaseService>();
            var packageDownloader = scope.ServiceProvider.GetRequiredService<IPackageDownloader>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UpdateService>>();

            logger.LogInformation($"Downloading update...");

            InstallDirectory.Create(directory);
            string statePath = InstallDirectory.GetStatePath(directory);

            var options = InstallOptions.GetDefaultForUpdate(directory);
            var state = EDInstaller.Create(InstallOperation.Update, packageUri, options).Build();
            var engine = new InstallerEngine(scope.ServiceProvider, state);
            engine.TempDirectory = InstallDirectory.GetTempDirectory(directory);
            engine.EventRaised += (IInstallerEvent e) =>
            {
                switch (e)
                {
                    case DownloadPackageProgressEvent progressEvent:
                        progress(progressEvent.Progress);
                        break;
                }
            };
            await engine.ExecuteAsync(cancellationToken);
            InstallerState.Save(statePath, engine.ExportState());
        }

        public Task DownloadUpdateAsync(IPackage package, string directory, Action<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            return DownloadUpdateAsync(package.Uri, directory, progress, cancellationToken);
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(string releaseChannel, string currentVersion, string directory, CancellationToken cancellationToken)
        {
            await using var scope = App.ServiceProvider.CreateAsyncScope();
            var releaseService = scope.ServiceProvider.GetRequiredService<IReleaseService>();
            var packageDownloader = scope.ServiceProvider.GetRequiredService<IPackageDownloader>();
            var downloadSizeCalculcator = scope.ServiceProvider.GetRequiredService<IPackageDownloadSizeCalculator>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UpdateService>>();

            try
            {
                logger.LogInformation("Checking for updates...");

                // Fetch the latest release
                ReleaseInfo? releaseInfo = await releaseService.GetLatestAsync(releaseChannel, cancellationToken);

                // Check if we have release info
                if (releaseInfo != null)
                {
                    logger.LogInformation($"Found release {releaseInfo.Version}. Current version: {currentVersion}");

                    if (!SemanticVersion.TryParse(releaseInfo.Version, out SemanticVersion? releaseVersion))
                    {
                        logger.LogError("Failed to parse version in release info");
                        return UpdateCheckResult.NoUpdateAvailable();
                    }

                    // Check if it's newer than our version or if it is on a different channel
                    if (releaseVersion > SemanticVersion.Parse(currentVersion) || 
                        ReleaseInfo.GetChannel(releaseInfo.Version) != ReleaseInfo.GetChannel(currentVersion))
                    {
                        Dictionary<string, string> renamedFiles = InstallDirectory.GetRemappedFiles(directory);

                        // Fetch and estimate the package download size
                        logger.LogInformation("Calculating download size...");
                        IPackage? package = await packageDownloader.GetPackageAsync(releaseInfo.PackageUri);
                        long downloadSize = await downloadSizeCalculcator.CalculcateAsync(package, directory, renamedFiles);
                        return UpdateCheckResult.UpdateAvailable(package, downloadSize);
                    }
                    else
                    {
                        logger.LogInformation("No update available, current version is the latest");
                    }
                }
                else
                {
                    logger.LogError("Failed to get latest release info. DHT returned no results");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check for updates");
                throw;
            }

            return UpdateCheckResult.NoUpdateAvailable();
        }

        public async Task<List<Process>> CheckProgramsNeedClosing(IPackage package, string directory)
        {
            List<Process> processList = await Task.Run(() =>
            {
                try
                {
                    string[] files = package.Files
                        .Where(x => x.Path != InstallDirectory.LauncherFileName)
                        .Select(x => Path.Combine(directory, x.Path)).ToArray();

                    return FileLockUtility.WhoIsLocking(files);
                }
                catch (Win32Exception)
                {
                    return new();
                }
            });

            return processList;
        }
    }
}
