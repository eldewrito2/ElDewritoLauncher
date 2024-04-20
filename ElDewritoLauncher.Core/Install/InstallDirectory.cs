using InstallerLib.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace EDLauncher.Core.Install
{
    public static class InstallDirectory
    {
        public const string LauncherDataFolderName = ".launcher";
        public const string LauncherFileName = "launcher.exe";
        public const string ManifestFileName = "manifest.json";
        public const string InstallerStateFileName = "installer_state.json";
        public const string StagingFolderName = "staging";
        public const string IconFileName = "eldorado.ico";
        public const string ModsFolderName = "mods";

        public static void Create(string baseDirectory)
        {
            Directory.CreateDirectory(baseDirectory);
            FileUtility.CreateDirectoryHidden(GetLauncherDataDirectory(baseDirectory));
            Directory.CreateDirectory(GetStagingDirectory(baseDirectory));
            Directory.CreateDirectory(GetTempDirectory(baseDirectory));
        }

        public static bool HasUnfinishedInstall(string baseDirecotry)
        {
            return File.Exists(GetStatePath(baseDirecotry));
        }

        public static string GetLauncherDataDirectory(string baseDirectory)
        {
            return Path.Combine(baseDirectory, LauncherDataFolderName);
        }

        public static string GetLauncherPath(string baseDirectory)
        {
            return Path.Combine(baseDirectory, LauncherFileName);
        }

        public static string GetStatePath(string baseDirectory)
        {
            return Path.Combine(GetLauncherDataDirectory(baseDirectory), InstallerStateFileName);
        }

        public static string GetStagingDirectory(string baseDirectory)
        {
            return Path.Combine(GetLauncherDataDirectory(baseDirectory), StagingFolderName);
        }

        public static string GetManifestPath(string baseDirectory)
        {
            return Path.Combine(baseDirectory, ManifestFileName);
        }

        public static string GetStagedFilePath(string baseDirectory, string relativePath)
        {
            return Path.Combine(GetStagingDirectory(baseDirectory), relativePath);
        }

        public static Dictionary<string, string> GetRemappedFiles(string baseDirectory)
        {
            return new Dictionary<string, string>()
            {
                { LauncherFileName, GetStagedFilePath(baseDirectory, LauncherFileName) },
                { ManifestFileName, GetStagedFilePath(baseDirectory, ManifestFileName) },
            };
        }

        public static string GetIconPath(string baseDirectory)
        {
            return Path.Combine(baseDirectory, IconFileName);
        }

        public static string GetTempDirectory(string directory)
        {
            return Path.Combine(GetLauncherDataDirectory(directory), "temp");
        }
    }
}
