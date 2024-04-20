using System;
using System.IO;

namespace EDLauncher.Core.Install
{
    public class InstallOptions
    {
        public string InstallPath { get; set; } = "";

        public bool CreateDesktopShortcut { get; set; } = false;

        public bool AddFirewallRule { get; set; } = false;


        public static InstallOptions GetDefaultForUpdate(string path)
        {
            return new InstallOptions()
            {
                InstallPath = path,
            };
        }

        public static InstallOptions GetDefaultForNewInstall()
        {
            return new InstallOptions()
            {
                InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Constants.ApplicationName),
                CreateDesktopShortcut = true,
                AddFirewallRule = false
            };
        }
    }
}
