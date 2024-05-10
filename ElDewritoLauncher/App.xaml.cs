using EDLauncher.Controls;
using EDLauncher.Core.Install;
using EDLauncher.Core.Torrents;
using EDLauncher.Dialogs;
using EDLauncher.Installer.Services;
using EDLauncher.Launcher;
using EDLauncher.Launcher.Models;
using EDLauncher.Launcher.Services;
using EDLauncher.Utility;
using ElDewritoLauncher.Toasts;
using InstallerLib.Install;
using InstallerLib.Packages;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Constants = EDLauncher.Core.Constants;

namespace EDLauncher
{
    public partial class App : Application
    {
        private static LoggingLevelSwitch _logLevelSwitch = new(LogEventLevel.Information);
        private static bool _settingsNeedSaving = false;

        public static bool IsLauncher { get; private set; }
        public static ILogger<App> Logger { get; private set; } = null!;
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static IUpdateService UpdateService { get; private set; } = null!;
        public static LauncherSettings LauncherSettings { get; private set; } = new LauncherSettings();
        public static LauncherState LauncherState { get; private set; } = new LauncherState();
        public static TaskbarItemInfo TaskBar { get; private set; } = null!;
        public static UpdateChecker UpdateChecker { get; private set; } = null!;
        public static UpdateDownloader UpdateDownloader { get; private set; } = null!;
        public static IToastService Toasts { get; private set; } = null!;
        public static SeedManager Seeder { get; private set; } = null!;
        public static SingleInstanceManager InstanceManager { get; set; } = null!;
        public static LauncherTrayIcon TrayIcon { get; private set; } = new LauncherTrayIcon();
        public static LauncherWindow LauncherWindow { get; private set; } = null!;
        public static InstallerState? LastInstallerState { get; private set; } = null;


        public App()
        {
            ServiceProvider = BuildServices();
            Logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            Toasts = ServiceProvider.GetRequiredService<IToastService>();
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Environment.GetCommandLineArgs().Contains("--debug"))
            {
                _logLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }

            // Handle unahndled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Create the main directories
            Directory.CreateDirectory(Constants.GetAppDirectory());
            Directory.CreateDirectory(Constants.GetLogDirectory());
            Directory.CreateDirectory(Constants.GetPackageDirectory());
            Directory.CreateDirectory(Constants.GetCacheDirectory());

            try
            {
                await StartupAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "App failed to start");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.LogInformation("App exiting...");

            if (IsLauncher)
            {
                SaveLauncherSettings();
            }

            ServiceProvider.GetRequiredService<IPeerCache>().SavePeerList();

            TrayIcon.Hide();
            TrayIcon.Dispose();

            base.OnExit(e);
        }

        private Task StartupAsync()
        {
            Logger.LogInformation($"App Starting... CurrentDirectory='{Environment.CurrentDirectory}'");

            // Used for showing progress on the taskbar
            TaskBar = new TaskbarItemInfo();

            // Detect the current version
            SemanticVersion? currentVersion = InstallerService.DetectExistingInstall();
            if (currentVersion != null)
            {
                Logger.LogInformation($"Detected install. Version: {currentVersion}");
            }
      
            LastInstallerState = InstallerService.GetInstallerState();

            // Determine whether to start the installer or launcher
            bool hasUnfinishedInstall = InstallerService.DetectUnfinishedInstall();
            if (currentVersion == null || hasUnfinishedInstall)
            {
                var lastRun = LastRunInfo.GetLastRunInfo();
                if(lastRun != null)
                {
                    MessageBoxResult decision = MessageBox.Show(
                        $"An existing install of ElDewrito was found in\r\n\r\n\"{lastRun.Path}\"\r\n\r\nDo you want to use this one instead?", 
                        "Existing Install Found",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if(decision == MessageBoxResult.Yes)
                    {
                        RelaunchUnelevated(lastRun.Path);
                        return Task.CompletedTask;
                    }
                }

                return StartInstallerAsync(hasUnfinishedInstall);
            }
            else
            {
                Toasts.Install();
                return StartLauncherAsync(currentVersion);
            }
        }

        private static Task StartInstallerAsync(bool hasUnfinishedInstall)
        {
            Logger.LogInformation($"Starting installer... HasUnfinishedInstall={hasUnfinishedInstall}");

            //if (SystemUtility.IsProcessElevated())
            //{
            //    FirewallHelper.AddAuthorizedApplication("ElDewrito Installer", "Allow ElDewrito Installer through the firewall", Environment.ProcessPath!);
            //}

            // Create the installer window
            var installerWindow = new InstallerWindow();
            installerWindow.TaskbarItemInfo = TaskBar;
            installerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            installerWindow.InitInstance(resuming: hasUnfinishedInstall);
            

            if (!Environment.GetCommandLineArgs().Contains("--silent") || !hasUnfinishedInstall)
            {
                installerWindow.Show();
            }

            return Task.CompletedTask;
        }

        private void OnToastActivated(string args)
        {
            if (!IsLauncher)
                return;

            App.LauncherState.IsMinimizedToTray = false;
            LauncherWindow.WindowState = WindowState.Normal;
            LauncherWindow.Show();
            LauncherWindow.Activate();

            if (LauncherState.IsUpdateAvailable)
            {
                LauncherWindow.ShowUpdateDialog(true);
            }
        }   

        private Task StartLauncherAsync(SemanticVersion currentVersion)
        {
            IsLauncher = true;

            Logger.LogInformation($"Starting launcher...");
      
            TempLauncherState.LoadTempLauncherState();
            CleanupTempDirectory();
            LoadLauncherSettings();

            LauncherState.CurrentVersion = currentVersion.ToNormalizedString();
            UpdateService = new UpdateService();
            UpdateDownloader = new UpdateDownloader(UpdateService);
            UpdateChecker = new UpdateChecker(UpdateService);
            Seeder = ServiceProvider.GetRequiredService<SeedManager>();

            if(Environment.GetCommandLineArgs().Contains("--tray"))
            {
                LauncherState.IsMinimizedToTray = true;
            }

            // Create the launcher window
            LauncherWindow = new LauncherWindow();
            LauncherWindow.TaskbarItemInfo = TaskBar;
            LauncherWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (!LauncherState.IsMinimizedToTray)
            {
                LauncherWindow.Show();
            }

            TrayIcon.Show();

            // If we auto updated successfully, show a notification
            if (LauncherSettings.EnableDesktopNotifcations &&
                LauncherState.IsMinimizedToTray &&
                LastInstallerState != null
                && LastInstallerState.Operation == InstallOperation.Update
                && LastInstallerState.IsCompleted)
            {
                Toasts.ShowToast(new UpdateDownloadedToast());
            }
   
            // Start an update check in the background
            UpdateChecker.StartCheck(UpdateCheckInitiator.Startup);

            if (LauncherSettings.EnableAutoSeed)
            {
                Seeder.StartSeeding();
            }

            // Finally update the last run info
            LastRunInfo.UpdateLastRunInfo(LauncherState.CurrentVersion, Environment.CurrentDirectory);

            return Task.CompletedTask;
        }

        private static void CleanupTempDirectory()
        {
            var tempDir = InstallDirectory.GetTempDirectory(Environment.CurrentDirectory);
            Logger.LogInformation($"Clearning temp directory: '{tempDir}'");

            try
            {
                if (Directory.Exists(tempDir))
                {
                    foreach (var filePath in Directory.GetFiles(tempDir))
                    {
                        try
                        {
                            File.Delete(filePath);
                            Logger.LogTrace($"Deleted temp file '{filePath}'");
                        }
                        catch (SystemException ex)
                        {
                            Logger.LogError(ex, $"Failed to delete temp file '{filePath}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to clean temp directory");
            }
        }

        private IServiceProvider BuildServices()
        {
            // Configure services used by the rest of the application
            var services = new ServiceCollection();

            // Logging
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.ControlledBy(_logLevelSwitch)
              .Enrich.FromLogContext()
              .WriteTo.File(Path.Combine(Constants.GetLogDirectory(), "launcher.log"),
               outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message}{NewLine}{Exception}")
              .CreateLogger();
            services.AddLogging(builder => builder.AddSerilog());

            // Torrents
            services.AddSingleton(_ =>
                    new TorrentPackageCacheOptions(
                        Directory: Constants.GetPackageDirectory(),
                        MaxItems: 5
                    ));
            services.AddSingleton<TorrentSessionFactory>();
            services.AddScoped(provider =>
                    provider.GetRequiredService<TorrentSessionFactory>().Create(
                        enableDebugLog: _logLevelSwitch.MinimumLevel == LogEventLevel.Verbose,
                        port: LauncherSettings.Port)
                    );

            services.AddSingleton<IPackageCache, TorrentPackageCache>();
            services.AddScoped(_ => new TorrentPackageDownloadOptions(
                Timeout: TimeSpan.FromMinutes(2),
                Trackers: new string[] {}));
            services.AddScoped<IPackageDownloader, TorrentPackageDownloader>();
            services.AddSingleton<IPackageDownloadSizeCalculator, PackageDownloadSizeCalculcator>();
            services.AddTransient(_ =>
                new DhtReleaseServiceOptions(
                    PublicKey: Environment.GetEnvironmentVariable("ED_PUBKEY") ?? Constants.PublicKey,
                    channelSalts: new() {
                        ["release"] = ""
                    },
                    DefaultTimeout: TimeSpan.FromSeconds(10)
                )
            );
            //services.AddScoped(_ =>
            //    new PeerCacheOptions(
            //        FilePath: Path.Combine(Constants.GetCacheDirectory(), "peer_cache.txt"),
            //        MaxItems: 50
            //    ));
            //services.AddScoped<IPeerCache, PeerCache>();
            services.AddScoped<IPeerCache, NullPeerCache>();
            services.AddTransient<PeerBootstrapper>();
            services.AddSingleton<DhtCache>();
            services.AddSingleton<DhtAannounceService>();
            string? releaseJsonPath = Environment.GetEnvironmentVariable("ED_RELEASE_JSON");
            if (releaseJsonPath == null)
            {
                services.AddScoped<IReleaseService, DhtReleaseService>();
            }
            else
            {
                services.AddSingleton(_ => new FakeReleaseServiceOptions(releaseJsonPath));
                services.AddScoped<IReleaseService, FakeReleaseService>();
            }

            // Misc services
            services.AddSingleton<AutoUpdateManager>();
            services.AddSingleton<SeedManager>();
            services.AddToasts(builder =>
            {
                builder
                .ConfigureApp(
                    appGuid: new Guid("B032089D-0E09-4514-B1FA-660FE6520117"),
                    appId: "ElDewrito.Launcher",
                    displayName: "ElDewrito Launcher",
                    iconUrl: InstallDirectory.GetIconPath(Environment.CurrentDirectory)
                 )
                .AddArgument("-ToastActivated")
                .SetActivatedCallback((string args) => Dispatcher.Invoke(() => OnToastActivated(args)))
                .RegisterToasts();
            });

            return services.BuildServiceProvider();
        }

        void ShowErrorDialog(object ex)
        {
            var ownerWindow = MainWindow as WindowBase;
            ownerWindow?.ShowModalBackdrop();
            var dlg = new ErrorDialog();
            dlg.WindowStartupLocation = ownerWindow == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            dlg.Owner = ownerWindow;
            dlg.SetErrorMessage(ex?.ToString() ?? "An unknown error has occured");
            dlg.ShowDialog();
            Shutdown();
        }

        public static Uri GetResourceUri(string resourceName)
        {
            string? assemblyName = typeof(App).Assembly.GetName().Name;
            string? resourcePath = $"{assemblyName};component/{resourceName}";
            return new Uri($"/{resourcePath}", UriKind.Relative);
        }

        public static void SaveLauncherSettings()
        {
            try
            {
                if(_settingsNeedSaving)
                {
                    _settingsNeedSaving = false;
                    LauncherSettings.Save(LauncherSettings);
                }
                
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Failed to save settings");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void LoadLauncherSettings()
        {
            try
            {
                LauncherSettings.PropertyChanged -= LauncherSettings_PropertyChanged;
                LauncherSettings = LauncherSettings.Load();
                LauncherSettings.PropertyChanged += LauncherSettings_PropertyChanged;
                UpdateMinimumLogLevel();
            }
            catch(Exception ex)
            {
                App.Logger.LogError(ex, "Failed load settings");
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void UpdateMinimumLogLevel()
        {
            _logLevelSwitch.MinimumLevel = LauncherSettings.EnableDebugLog ? LogEventLevel.Verbose : LogEventLevel.Information;
        }

        public static void RelaunchUnelevated(string directory, string args = "")
        {
            App.InstanceManager.SignalShutdown();
            SystemUtility.ExecuteProcessUnElevated(InstallDirectory.GetLauncherPath(directory), args, directory);
            App.Current.Shutdown();
        }

        public static void Relaunch(string directory, string args = "")
        {
            if (IsLauncher)
            {
                TempLauncherState.SaveTempLauncherState();
            }

            InstanceManager.SignalShutdown();
            SystemUtility.ExecuteProcess(InstallDirectory.GetLauncherPath(directory), args, directory);
            App.Current.Shutdown();
        }

        private static void LauncherSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LauncherSettings.EnableDebugLog))
            {
                UpdateMinimumLogLevel();
            }

            _settingsNeedSaving = true;
        }

        // Unhandled exception handlers

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                Logger.LogCritical(ex, "Unhandled exception was thrown");
            else
                Logger.LogCritical("Unhandled non-exception was thrown {ExceptionObject}", e.ExceptionObject);

            ShowErrorDialog(e.ExceptionObject);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogCritical(e.Exception, "Unhandled exception was thrown in the dispatcher");
            ShowErrorDialog(e.Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.LogError(e.Exception, "Unobserved task exception was thrown");
            ShowErrorDialog(e.Exception);
        }
    }
}
