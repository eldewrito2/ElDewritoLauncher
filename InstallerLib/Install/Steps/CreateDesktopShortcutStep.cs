using InstallerLib.Utility;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class CreateDesktopShortcutStep : IInstallStep
    {
        public CreateDesktopShortcutStep(string name, string description, string targetPath, string arguments, string iconPath, string workingDirectory)
        {
            Name = name;
            Description = description;
            TargetPath = targetPath;
            Arguments = arguments;
            IconPath = iconPath;
            WorkingDirectory = workingDirectory;
        }

        public string Name { get; }

        public string Description { get; }

        public string TargetPath { get; }

        public string Arguments { get; }

        public string IconPath { get; }

        public string WorkingDirectory { get; }


        public Task Execute(IInstallerEngine engine)
        {
            engine.Logger.LogInformation($"Creating desktop shortcut Name: '{Name}' TargetPath: '{TargetPath}'");
            ShortcutCreator.CreateDesktopShortcut(Name, Description, TargetPath, Arguments, IconPath, WorkingDirectory);

            return Task.CompletedTask;
        }
    }
}