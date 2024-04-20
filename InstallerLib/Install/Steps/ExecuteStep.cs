using InstallerLib.Utility;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    class ExecuteProcessStep : IInstallStep
    {
        public ExecuteProcessStep(string fileName, string arguments = "", string workingDirectory = "", bool? runElevated = null)
        {
            FileName = fileName;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            RunElevated = runElevated;
        }

        public string FileName { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool UseShellExecute { get; set; }
        public bool? RunElevated { get; set; }

        public Task Execute(IInstallerEngine engine)
        {
            if (RunElevated == true)
            {
                SystemUtility.ExecuteProcessElevated(FileName, Arguments, WorkingDirectory);
            }
            else if (RunElevated == false)
            {
                SystemUtility.ExecuteProcessUnElevated(FileName, Arguments, WorkingDirectory);
            }
            else
            {
                SystemUtility.ExecuteProcess(FileName, Arguments, WorkingDirectory);
            }

            return Task.CompletedTask;
        }
    }
}
