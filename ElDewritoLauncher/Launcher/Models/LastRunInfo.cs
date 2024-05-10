using EDLauncher.Core;
using EDLauncher.Core.Install;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace EDLauncher.Launcher.Models
{
    public record LastRunInfo(string Version, string Path)
    {
        public static string GetLastRunInfoPath()
        {
            return System.IO.Path.Combine(Constants.GetCacheDirectory(), "last_run.json");
        }

        public static LastRunInfo? GetLastRunInfo()
        {
            try
            {
                var runInfo = JsonFileUtility.Load<LastRunInfo>(GetLastRunInfoPath());
                if (runInfo != null)
                {
                    string launcherPath = InstallDirectory.GetLauncherPath(runInfo.Path);

                    // Check if the launcher exists there
                    if (File.Exists(launcherPath))
                    {
                        return runInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<LastRunInfo>>().LogError(ex, "Failed to check last run info");
            }

            return null;
        }

        public static void UpdateLastRunInfo(string version, string directory)
        {
            try
            {
                JsonFileUtility.Store(GetLastRunInfoPath(), new LastRunInfo(version, directory));
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<LastRunInfo>>().LogError(ex, "Failed to update last run info");
            }
        }
    }
}
