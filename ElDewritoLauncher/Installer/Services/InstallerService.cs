using EDLauncher.Core.Install;
using InstallerLib.Events;
using InstallerLib.Install;
using InstallerLib.Packages;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

// TODO some of this stuff should be moved into EDLauncher.Core

namespace EDLauncher.Installer.Services
{
    class InstallerService
    {
        public static async Task InstallAsync(InstallOperation operation, string packageId, InstallOptions options, Action<IInstallerEvent> eventCallback)
        {
            var logger = App.ServiceProvider.GetRequiredService<ILogger<InstallerService>>();

            logger.LogInformation("Starting new install...");

            try
            {
                string directory = options.InstallPath;
                string statePath = InstallDirectory.GetStatePath(directory);

                InstallDirectory.Create(directory);
                InstallerState installerState = EDInstaller.Create(operation, packageId, options).Build();

                var engine = new InstallerEngine(App.ServiceProvider, installerState);
                engine.TempDirectory = InstallDirectory.GetTempDirectory(directory);
                engine.EventRaised += eventCallback;

                await Task.WhenAll(engine.ExecuteAsync(), Task.Delay(2000));

                Debug.Assert(engine.IsRelaunchRequested);

                InstallerState.Save(statePath, engine.ExportState());
                App.RelaunchUnelevated(directory);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to install");
                throw;
            }
        }

        public static async Task ResumeAsync(Action<IInstallerEvent> eventCallback)
        {
            var logger = App.ServiceProvider.GetRequiredService<ILogger<InstallerService>>();
            logger.LogInformation("Resuming install...");

            string directory = Environment.CurrentDirectory;
            string statePath = InstallDirectory.GetStatePath(directory);

            var engine = new InstallerEngine(App.ServiceProvider, InstallerState.Load(statePath));
            engine.TempDirectory = InstallDirectory.GetTempDirectory(directory);
            engine.EventRaised += eventCallback;

            try
            {
                await Task.WhenAll(engine.ExecuteAsync(), Task.Delay(2000));
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to resume install");
                throw;
            }
            finally
            {
                InstallerState.Save(statePath, engine.ExportState());
            }

            App.Relaunch(directory);
        }

        public static SemanticVersion? DetectExistingInstall()
        {
            var logger = App.ServiceProvider.GetRequiredService<ILogger<InstallerService>>();

            string manifestPath = InstallDirectory.GetManifestPath(Environment.CurrentDirectory);
            if (!File.Exists(manifestPath))
            {
                logger.LogWarning("Missing manifest.json");
                return null;
            }

            try
            {
                Manifest? manifest = JsonSerializer.Deserialize<Manifest?>(File.ReadAllText(manifestPath));
                return SemanticVersion.Parse(manifest!.Version);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading manifest.json");
                return null;
            }
        }

        public static InstallerState? GetInstallerState()
        {
            string statePath = InstallDirectory.GetStatePath(Environment.CurrentDirectory);
            if (File.Exists(statePath))
            {
                return InstallerState.Load(statePath);
            }
            return null;
        }

        public static bool DetectUnfinishedInstall()
        {
            bool hasUnfinishedInstall = InstallDirectory.HasUnfinishedInstall(Environment.CurrentDirectory);
            if (hasUnfinishedInstall)
            {
                hasUnfinishedInstall = CheckInstallerState();
            }
            return hasUnfinishedInstall;
        }

        public static bool CheckInstallerState()
        {
            var logger = App.ServiceProvider.GetRequiredService<ILogger<InstallerService>>();

            string statePath = InstallDirectory.GetStatePath(Environment.CurrentDirectory);
            InstallerState state = InstallerState.Load(statePath);
            if (state.FailureCount > 0)
            {
                try
                {
                    logger.LogError("Multiple failures detected in installer state. Deleting");
                    File.Delete(statePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete state file");
                }

                return false;
            }

            if (state.CurrentStep == state.Steps.Count)
            {
                logger.LogInformation("Install Completed successfully");
                File.Delete(statePath);
                return false;
            }

            return true;
        }
    }
}
