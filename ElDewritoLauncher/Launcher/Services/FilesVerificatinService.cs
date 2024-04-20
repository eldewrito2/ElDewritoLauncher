using EDLauncher.Core.Install;
using InstallerLib.Packages;
using InstallerLib.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TorrentLib;

namespace EDLauncher.Launcher.Services
{
    class FilesVerificatinService
    {
        public static async Task<FilesVerificationResult> VerifyFilesAsync(string directory, Action<float> progressCallback, CancellationToken cancellationToken = default)
        {
            var state = new VerifyFilesTaskState();

            // Start the verify task on a thread pool thread
            Task verifyTask = Task.Run(() => VerifyFilesCoreAsync(directory, state, cancellationToken), cancellationToken);

            // Monitor task progress
            int lastCurrent = 0;
            while (true)
            {
                // Report Progreess
                if (state.Current != lastCurrent)
                {
                    lastCurrent = state.Current;
                    float percent = state.Total == 0 ? 0 : (state.Current * 100.0f / state.Total);
                    progressCallback(percent);
                }

                // Check if the task is complete. we do this after the progress to ensure the
                // caller gets the last value before.
                if (verifyTask.IsCompleted)
                    break;

                await Task.Delay(120);
            }

            // Rethrow any exception that occurred
            await verifyTask;

            // Extract the list of invalid file paths
            var invalidFiles = state.InvalidFiles!
                 .Select((invalid, i) => invalid ? state.FilePaths[i] : null)
                 .Where(path => path != null)
                 .ToList();

            return new FilesVerificationResult(state.Total, invalidFiles!);
        }

        private static async Task VerifyFilesCoreAsync(string directory, VerifyFilesTaskState state, CancellationToken cancellationToken)
        {
            // Load the manifest
            Manifest? manifest = await JsonFileUtility.LoadAsync<Manifest?>(InstallDirectory.GetManifestPath(directory)).ConfigureAwait(false);
            if (manifest == null)
                throw new InvalidOperationException("Could not find manifest");

            string[] filePaths = manifest.Files.Keys.ToArray();
            string[] expectedHashes = manifest.Files.Values.ToArray();

            state.InvalidFiles = new bool[filePaths.Length];
            state.FilePaths = filePaths;

            // Implicit memory barrier ensures writes before this will be made visible to other threads
            Interlocked.Exchange(ref state.Total, filePaths.Length);

            // Hash the files
            await Task.WhenAll(filePaths.Select(async (filePath, i) =>
            {
                // Check cancellation before processing each file
                cancellationToken.ThrowIfCancellationRequested();

                // Get the full path of the file
                string fullPath = Path.GetFullPath(Path.Combine(directory, filePath));

                // Calculate the file hash asynchronously
                SHA1_Hash expectedHash = SHA1_Hash.Parse(expectedHashes[i]);
                SHA1_Hash calculatedHash = await CalculateFileHashAsync(fullPath).ConfigureAwait(false);

                // Record whether it matches or not
                state.InvalidFiles[i] = expectedHash != calculatedHash;

                // Increment the number of processed items for the caller to calculate a rough progress
                Interlocked.Increment(ref state.Current);

            })).ConfigureAwait(false);
        }

        private static async ValueTask<SHA1_Hash> CalculateFileHashAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return SHA1_Hash.Empty;

            using var hasher = SHA1.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan | FileOptions.Asynchronous);
            return new SHA1_Hash(await hasher.ComputeHashAsync(stream).ConfigureAwait(false));
        }

        private class VerifyFilesTaskState
        {
            public volatile int Current = 0;
            public volatile int Total = 0;
            public bool[] InvalidFiles = null!;
            public string[] FilePaths = null!;
        }
    }

    record FilesVerificationResult(int FileCount, List<string> InvalidFiles);
}
