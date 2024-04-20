using EDLauncher.Utility;
using InstallerLib.Packages;
using System;

namespace EDLauncher.Launcher.Models
{
    public class LauncherState : ViewModelBase
    {
        private bool _isDownloadingUpdate;
        public bool IsDownloadingUpdate
        {
            get => _isDownloadingUpdate;
            set => SetAndNotify(ref _isDownloadingUpdate, value);
        }

        private string _currentVersion = "";
        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetAndNotify(ref _currentVersion, value);
        }

        private DateTimeOffset _lastUpdateCheck;
        public DateTimeOffset LastUpdateCheck
        {
            get => _lastUpdateCheck;
            set => SetAndNotify(ref _lastUpdateCheck, value);
        }

        private bool _isCheckingforUpdate;
        public bool IsCheckingForUpdate
        {
            get => _isCheckingforUpdate;
            set => SetAndNotify(ref _isCheckingforUpdate, value);
        }

        private bool _isUpdateAvailable;
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetAndNotify(ref _isUpdateAvailable, value);
        }

        private Exception? _updateCheckError;
        public Exception? UpdateCheckError
        {
            get => _updateCheckError;
            set => SetAndNotify(ref _updateCheckError, value);
        }

        private bool _isAutoUpdating;
        public bool IsAutoUpdating
        {
            get => _isAutoUpdating;
            set => SetAndNotify(ref _isAutoUpdating, value);
        }

        private UpdateInfo? _updateInfo;
        public UpdateInfo? UpdateInfo
        {
            get => _updateInfo;
            set => SetAndNotify(ref _updateInfo, value);
        }

        private UpdateCheckInitiator _updateCheckIniterator;
        public UpdateCheckInitiator UpdateCheckInitiator
        {
            get => _updateCheckIniterator;
            set => SetAndNotify(ref _updateCheckIniterator, value);
        }

        private Tabs _currentTab;
        public Tabs CurrentTab
        {
            get => _currentTab;
            set => SetAndNotify(ref _currentTab, value);
        }

        private bool _isSeeding;
        public bool IsSeeding
        {
            get => _isSeeding;
            set => SetAndNotify(ref _isSeeding, value);
        }

        private bool _isMinimizedToTray;
        public bool IsMinimizedToTray
        {
            get => _isMinimizedToTray;
            set => SetAndNotify(ref _isMinimizedToTray, value);
        }
    }

    public enum UpdateCheckInitiator
    {
        Startup,
        Automatic,
        User,
    }

    public enum Tabs
    {
        Play,
        News,
        Settings
    }

    public record UpdateInfo(IPackage Package, long DownloadSize);
}
