using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InstallerLib.Utility
{
    public class FileUtility
    {
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;

        public static void SwallowAnyExceptions(Action action)
        {
            try { action(); } catch { }
        }

        public static async ValueTask RetryOnFileAccessErrorAsync(Action action, int maxRetries = 10, int initialRetryDelay = 10)
        {
            int nextWaitTime = initialRetryDelay;

            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (SystemException ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    if (--maxRetries == 0)
                    {
                        throw;
                    }
                    await Task.Delay(nextWaitTime);
                    nextWaitTime *= 2;
                }
            }
        }

        public static void CreateHiddenDirectory(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            directoryInfo.Create();
            directoryInfo.Attributes |= FileAttributes.Hidden;

        }

        public static async ValueTask ReplacePotentiallyInUseFile(string destPath, string sourcePath, string tempDir)
        {
            // Create a temp file to move the old file to
            string tempFilePath = Path.Combine(tempDir, Guid.NewGuid().ToString().Substring(0, 8) + ".tmp");
            if (File.Exists(destPath))
            {
                await RetryOnFileAccessErrorAsync(() => File.Move(destPath, tempFilePath, true));
            }
            await RetryOnFileAccessErrorAsync(() => File.Move(sourcePath, destPath, true));
        }

        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException ex)
            {
                int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public static void CreateDirectoryHidden(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            directoryInfo.Create();
            directoryInfo.Attributes |= FileAttributes.Hidden;
        }

        public static void DeleteEmptyDirectories(string dir)
        {
            if (!Directory.Exists(dir))
                return;
            foreach (var d in Directory.EnumerateDirectories(dir))
                DeleteEmptyDirectories(d);

            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }

        public static long GetAvailableDiskSpace(string path)
        {
            try
            {

                DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(path)!);
                if (driveInfo.IsReady)
                {
                    return driveInfo.AvailableFreeSpace;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public static bool IsFolderEmpty(string path)
        {
            return !Directory.Exists(path) || (Directory.GetFileSystemEntries(path).Length == 0);
        }

        public static void CheckDirectoryWritePermission(string path)
        {
            string tempFilePath = Path.Combine(path, Guid.NewGuid().ToString());
            using var fs = File.Create(tempFilePath, 4096, FileOptions.DeleteOnClose);
            fs.Write(new byte[1]);
        }
    }
}
