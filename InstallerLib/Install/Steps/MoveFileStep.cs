using InstallerLib.Utility;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class MoveFileStep : IInstallStep
    {
        public MoveFileStep(string sourcePath, string destPath)
        {
            SourcePath = sourcePath;
            DestPath = destPath;
        }

        public string SourcePath { get; }

        public string DestPath { get; }

        public async Task Execute(IInstallerEngine engine)
        {
            engine.Logger.LogInformation($"Move file '{SourcePath}' -> '{DestPath}'", this);

            if (FileUtility.IsFileLocked(DestPath))
            {
                engine.Logger.LogWarning($"Tried to move locked file. using alternative method... temp dir: '{engine.TempDirectory}");
                await FileUtility.ReplacePotentiallyInUseFile(DestPath, SourcePath, engine.TempDirectory);
            }
            else
            {
                await FileUtility.RetryOnFileAccessErrorAsync(() => File.Move(SourcePath, DestPath, true));
            }
        }
    }

}