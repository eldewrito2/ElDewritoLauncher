using InstallerLib.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InstallerLib.Install
{
    public interface IInstallerEngine
    {
        public event Action<IInstallerEvent> EventRaised;

        IServiceProvider ServiceProvider { get; }
        ILogger Logger { get; }
        InstallOperation Operation { get; }
        CancellationToken CancellationToken { get; }
        bool IsCompleted { get; }
        bool IsRelaunchRequested { get; }
        string TempDirectory { get; set; }
        string InstallDirectory { get; set; }

        Task ExecuteAsync(CancellationToken cancellationToken = default);
        void RaiseEvent(IInstallerEvent eventObject);
        void RequestRelaunch(string path);
        InstallerState ExportState();
    }
}