using EDLauncher.Utility;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace EDLauncher.Launcher.Models
{
    public class LauncherSettings : ViewModelBase
    {
        const string SettingsFileName = "launcher_settings.json";

        private string _launchArguments = "";
        public string LaunchArguments
        {
            get => _launchArguments;
            set => SetAndNotify(ref _launchArguments, value);
        }

        private bool _doNotClose = true;
        public bool DoNotClose
        {
            get => _doNotClose;
            set => SetAndNotify(ref _doNotClose, value);
        }

        private bool _minimizeToTray = false;
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetAndNotify(ref _minimizeToTray, value);
        }

        private bool _enableDesktopNotifications = true;
        public bool EnableDesktopNotifcations
        {
            get => _enableDesktopNotifications;
            set => SetAndNotify(ref _enableDesktopNotifications, value);
        }

        private int _updateCheckInterval = 6*60;
        public int UpdateCheckInterval
        {
            get => _updateCheckInterval;
            set => SetAndNotify(ref _updateCheckInterval, value);
        }

        private bool _enableAutoUpdate = false;
        public bool EnableAutoUpdate
        {
            get => _enableAutoUpdate;
            set => SetAndNotify(ref _enableAutoUpdate, value);
        }

        private bool _enableDebugLog = false;
        public bool EnableDebugLog
        {
            get => _enableDebugLog;
            set => SetAndNotify(ref _enableDebugLog, value);
        }

        private bool _enableAutoSeed = false;
        public bool EnableAutoSeed
        {
            get => _enableAutoSeed;
            set => SetAndNotify(ref _enableAutoSeed, value);
        }

        private int _seedUploadRateLimit = 0;
        public int SeedUploadRateLimit
        {
            get => _seedUploadRateLimit;
            set => SetAndNotify(ref _seedUploadRateLimit, value);
        }

        private string _releaseChannel = "release";
        public string ReleaseChannel
        {
            get => _releaseChannel;
            set => SetAndNotify(ref _releaseChannel, value);
        }

        private int _port = 42305;
        public int Port
        {
            get => _port;
            set => SetAndNotify(ref _port, value);
        }

        public static void Save(LauncherSettings settings)
        {
            string serialized = JsonSerializer.Serialize(settings, new JsonSerializerOptions() { WriteIndented = true });

            // this shouldn't be possible, but it seems to be?
            if (string.IsNullOrWhiteSpace(serialized))
                throw new Exception("Settings serialized an empty string");

            File.WriteAllText(SettingsFileName, serialized);
        }

        public static LauncherSettings Load()
        {
            if (File.Exists(SettingsFileName))
            {
                string json = File.ReadAllText(SettingsFileName);
                LauncherSettings? settings = JsonSerializer.Deserialize<LauncherSettings>(json);
                if (settings != null)
                    return settings;
            }

            return new LauncherSettings();
        }
    }
}
