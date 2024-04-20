using System;
using System.IO;
using System.Threading.Tasks;

namespace InstallerLib.Utility
{
    public class TempDirectory : IDisposable, IAsyncDisposable
    {
        private TempDirectory(string path)
        {
            FullPath = path;
            Directory.CreateDirectory(FullPath);
        }

        public string FullPath { get; }

        public static TempDirectory Create()
        {
            return new TempDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        }

        public void Dispose()
        {
            try { Directory.Delete(FullPath, true); } catch { }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(() => Dispose()).ConfigureAwait(false);
        }
    }
}
