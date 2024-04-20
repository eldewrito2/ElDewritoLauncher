using EDLauncher.Core.Install;
using InstallerLib.Utility;
using System;
using System.IO;

namespace EDLauncher.Launcher.Services
{
    /// <summary>
    /// Provides temp storage for launcher state between relaunches
    /// </summary>
    public class TempLauncherState
    {
        class LauncherStatePersist
        {
            public bool IsMinimizedToTray { get; set; }
        }

        public static void SaveTempLauncherState()
        {
            if (!App.IsLauncher)
            {
                return;
            }

            var persist = new LauncherStatePersist();
            persist.IsMinimizedToTray = App.LauncherState.IsMinimizedToTray;
            JsonFileUtility.Store(GetFilePath(), persist);
        }

        public static void LoadTempLauncherState()
        {
            if (!App.IsLauncher)
            {
                return;
            }

            LauncherStatePersist? persist = JsonFileUtility.Load<LauncherStatePersist>(GetFilePath());
            if (persist != null)
            {
                App.LauncherState.IsMinimizedToTray = persist.IsMinimizedToTray;
            }
        }

        public static string GetFilePath()
        {
            return Path.Combine(InstallDirectory.GetTempDirectory(Environment.CurrentDirectory), "launcher_state.json");
        }
    }
}
