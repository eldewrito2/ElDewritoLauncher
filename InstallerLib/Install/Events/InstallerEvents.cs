using InstallerLib.Packages;

namespace InstallerLib.Events
{
    public record DownloadStartedEvent() : IInstallerEvent;

    public record DownloadFinishedEvent() : IInstallerEvent;

    public record InstallStartedEvent() : IInstallerEvent;

    public record InstallFinishedEvent() : IInstallerEvent { }

    public record DownloadPackageProgressEvent(IPackage Package, DownloadProgress Progress) : IInstallerEvent;
}
