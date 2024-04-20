using InstallerLib.Events;
using InstallerLib.Install;
using InstallerLib.Install.Steps;
using System.IO;

namespace EDLauncher.Core.Install
{
    public class EDInstaller
    {
        public static InstallerBuilder Create(InstallOperation operation, string packageId, InstallOptions options)
        {
            string directory = options.InstallPath;
            string newLauncherPath = InstallDirectory.GetStagedFilePath(directory, InstallDirectory.LauncherFileName);
            string newManifestPath = InstallDirectory.GetStagedFilePath(directory, InstallDirectory.ManifestFileName);
            string launcherPath = InstallDirectory.GetLauncherPath(directory);
            string manifestPath = InstallDirectory.GetManifestPath(directory);

            var installer = new InstallerBuilder();
            installer.Operation = operation;
            installer.InstallDirectory = directory;

            installer.ReportEvent(new DownloadStartedEvent());

            installer.AddStep(new DownloadPackageStep(
                packageId,
                directory,
                renamedFiles: InstallDirectory.GetRemappedFiles(directory)
            ));

            installer.AddStep(new MoveFileStep(newLauncherPath, launcherPath));

            //if (options.AddFirewallRule)
            //{
            //    installer.AddStep(new AddFirewallRuleStep("ElDewrito Launcher", "Allow ElDewrito Launcher through the firewall", launcherPath));
            //    installer.AddStep(new AddFirewallRuleStep("ElDewrito Game", "Allow ElDewrito Game through the firewall", Path.Combine(directory, "eldorado.exe")));
            //    installer.AddStep(new AddFirewallRuleStep("ElDewrito Web UI", "Allow ElDewrito Web UI the firewall", Path.Combine(directory, "custom_menu.exe")));
            //}

            installer.AddStep(new RelaunchStep(launcherPath));

            installer.ReportEvent(new InstallStartedEvent());


            installer.AddStep(new DeleteOldFilesStep(manifestPath, newManifestPath));


            if (options.CreateDesktopShortcut)
            {
                installer.AddStep(new CreateDesktopShortcutStep(
                    name: "ElDewrito",
                    description: "A Halo Online Mod",
                    targetPath: InstallDirectory.GetLauncherPath(directory),
                    arguments: "",
                    iconPath: InstallDirectory.GetLauncherPath(directory),
                    workingDirectory: directory
                ));
            }

            installer.AddStep(new MoveFileStep(newManifestPath, manifestPath));

            installer.ReportEvent(new InstallFinishedEvent());

            return installer;
        }
    }
}
