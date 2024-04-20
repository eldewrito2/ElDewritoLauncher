using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class RelaunchStep : IInstallStep
    {
        public RelaunchStep(string programPath)
        {
            ProgramPath = programPath;
        }

        public string ProgramPath { get; set; }


        public Task Execute(IInstallerEngine engine)
        {
            engine.RequestRelaunch("");
            return Task.CompletedTask;
        }
    }
}
