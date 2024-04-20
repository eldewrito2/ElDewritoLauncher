using InstallerLib.Packages;
using InstallerLib.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class DeleteOldFilesStep : IInstallStep
    {
        public string OldManifestPath { get; set; }
        public string NewManifestPath { get; set; }

        public DeleteOldFilesStep(string oldManifestPath, string newManifestPath)
        {
            OldManifestPath = oldManifestPath;
            NewManifestPath = newManifestPath;
        }

        public async Task Execute(IInstallerEngine engine)
        {
            if (!File.Exists(OldManifestPath))
            {
                engine.Logger.LogWarning($"Missing old manifest '{OldManifestPath}'");
                return;
            }

            engine.Logger.LogInformation($"Deleting old files... OldManifest: '{OldManifestPath}' NewManifest: '{NewManifestPath}'");

            Manifest oldManifest = await LoadManifest(OldManifestPath);
            Manifest newManifest = await LoadManifest(NewManifestPath);

            var filesToDelete = oldManifest.Files.Keys.Except(newManifest.Files.Keys).ToList();
            foreach (string filePath in filesToDelete)
            {
                string fullPath = Path.Combine(engine.InstallDirectory, filePath);
                if (File.Exists(fullPath))
                {
                    engine.Logger.LogTrace($"Deleting '{filePath}'");
                    await FileUtility.RetryOnFileAccessErrorAsync(() => File.Delete(fullPath)).ConfigureAwait(false);
                }
            }

            try
            {
                FileUtility.DeleteEmptyDirectories(engine.InstallDirectory);
            }
            catch(Exception ex)
            {
                engine.Logger.LogWarning(ex, "Failed to delete empty directories");
            }
        }

        private Task<Manifest> LoadManifest(string path)
        {
            return JsonFileUtility.LoadAsync<Manifest>(path)!;
        }
    }
}