using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    public class FileTruncator
    {
        public static async Task TruncateFilesAsync(string savePath, List<FileEntry> files, Dictionary<int, string>? remappedFiles = default, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileEntry fileEntry = files[i];
                string filePath = fileEntry.Path!;
                if (remappedFiles != null && remappedFiles.TryGetValue(i, out var remappedPath))
                {
                    filePath = remappedPath;
                }

                filePath = Path.Combine(savePath, filePath);

                int attempts = 0;
                do
                {
                    try
                    {
                        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
                        if (fileStream.Length > fileEntry.Length)
                            fileStream.SetLength(fileEntry.Length);
                        break;
                    }
                    catch (SystemException ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        // retry a couple times with a backoff
                        if (++attempts == 7)
                            throw;

                        int backoff = Math.Min((1 << attempts) * 100, 10000);
                        await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                    }
                } while (attempts > 0);
            }
        }
    }
}
