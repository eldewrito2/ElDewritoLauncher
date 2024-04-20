using InstallerLib.Events;
using InstallerLib.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class DownloadPackageStep : IInstallStep
    {
        public DownloadPackageStep(string packageUri, string directory, Dictionary<string, string>? renamedFiles = default)
        {
            PackageUri = packageUri;
            Directory = directory;
            RenamedFiles = renamedFiles;
        }

        public string PackageUri { get; }
        public string Directory { get; }
        public Dictionary<string, string>? RenamedFiles { get; }

        public async Task Execute(IInstallerEngine engine)
        {
            engine.Logger.LogInformation($"Downloading package '{PackageUri}'");

            IPackageDownloader packageDownloader = engine.ServiceProvider.GetRequiredService<IPackageDownloader>();
            IPackage package = await packageDownloader.GetPackageAsync(PackageUri, engine.CancellationToken);

            engine.Logger.LogInformation($"Package found {package.Name}-{package.Version}");

            engine.Logger.LogInformation($"Downloading package contents to '{Directory}'...");

            if (engine.Operation == InstallOperation.Install)
            {
                if (!new DirectoryInfo(Directory).GetFiles("*", SearchOption.AllDirectories).Any())
                {
                    engine.Logger.LogWarning("Directory is not empty");
                }
            }
            var downloadParams = new PackageDownloadParameters()
            {
                RenamedFiles = RenamedFiles,
            };
            await packageDownloader.DownloadPackageContentsAsync(package, Directory, downloadParams, (progress) =>
            {
                engine.RaiseEvent(new DownloadPackageProgressEvent(package, progress));

            }, engine.CancellationToken);

            engine.Logger.LogInformation($"Download finished");
        }
    }

}