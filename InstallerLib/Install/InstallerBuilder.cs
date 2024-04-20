using InstallerLib.Events;
using InstallerLib.Install.Steps;
using System.Collections.Generic;

namespace InstallerLib.Install
{
    public class InstallerBuilder
    {
        const string InstallerVersion = "1.0.0";

        private List<IInstallStep> _installSteps = new List<IInstallStep>();

        public InstallOperation Operation { get; internal set; }
        public string InstallDirectory { get; set; } = "";

        public void AddStep(IInstallStep step)
        {
            _installSteps.Add(step);
        }

        public void ReportEvent(IInstallerEvent eventObject)
        {
            _installSteps.Add(new ReportStep(eventObject));
        }

        public InstallerState Build()
        {
            return new InstallerState()
            {
                Operation = Operation,
                InstallerVersion = InstallerVersion,
                InstallDirectory = InstallDirectory,
                Steps = _installSteps,
                CurrentStep = 0,
            };
        }
    }
}