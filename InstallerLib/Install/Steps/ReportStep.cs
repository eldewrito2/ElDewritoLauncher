using InstallerLib.Events;
using System.Threading.Tasks;

namespace InstallerLib.Install.Steps
{
    public class ReportStep : IInstallStep
    {
        public ReportStep(IInstallerEvent @event)
        {
            Event = @event;
        }

        public IInstallerEvent Event { get; set; }

        public Task Execute(IInstallerEngine engine)
        {
            engine.RaiseEvent(Event);
            return Task.CompletedTask;
        }
    }
}