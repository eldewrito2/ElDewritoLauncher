using EDLauncher.Controls;
using EDLauncher.Core.Install;
using EDLauncher.Installer.Screens;
using EDLauncher.Installer.Services;
using InstallerLib.Events;
using InstallerLib.Install;
using InstallerLib.Packages;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;

namespace EDLauncher
{
    public interface IWizard
    {
        InstallOptions Options { get; }
        void GotoNextStep();
        void Cancel();
    }

    public partial class InstallerWindow : WindowBase, IWizard
    {
        private WelcomeScreen welcomeScreen;
        private SimpleStatusScreen installingScreen;
        private SimpleStatusScreen fetchingPackageScreen;
        private InstallOptionsScreen installOptionsScreen;
        private DownloadScreen downloadScreen;
        private ErrorScreen errorScreen;
        private IPackage? _package;
        private long _downloadSize;

        public InstallerWindow()
        {
            welcomeScreen = new WelcomeScreen();
            installingScreen = new SimpleStatusScreen();
            fetchingPackageScreen = new SimpleStatusScreen();
            installOptionsScreen = new InstallOptionsScreen();
            downloadScreen = new DownloadScreen();
            errorScreen = new ErrorScreen();

            welcomeScreen.SetWizard(this);
            installOptionsScreen.SetWizard(this);

            InitializeComponent();
        }

        public InstallOptions Options { get; } = InstallOptions.GetDefaultForNewInstall();

        public void InitInstance(bool resuming)
        {
            // Wizard steps:
            // 1. welcome screen
            // 2. fetch latest package
            // 3. install options
            // 4. install

            if (resuming)
            {
                ResumeInstall();
            }
            else
            {
                if (!Environment.CommandLine.Contains("--skipwelcome"))
                    ShowWelcome();
                else
                {
                    // bit of a hack to avoid having to pass through the options
                    Options.AddFirewallRule = SystemUtility.IsProcessElevated();
                    FetchLatestPackage();
                }
            }
        }

        public void ShowWelcome()
        {
            ShowScreen(welcomeScreen);
        }

        private async void FetchLatestPackage()
        {
            var releaseService = App.ServiceProvider.GetRequiredService<IReleaseService>();
            var packageService = App.ServiceProvider.GetRequiredService<IPackageDownloader>();
            string status = "Getting latest release";

            try
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                fetchingPackageScreen.SetStatus(status, "Searching DHT...");
                ShowScreen(fetchingPackageScreen);

                // TODO: Update for release, force to debug for now
                ReleaseInfo? releaseInfo = await releaseService.GetLatestAsync("release"); 
                if (releaseInfo == null)
                {
                    throw new Exception("failed to get latest release info");
                }

                fetchingPackageScreen.SetStatus(status, "Downloading package...");

                _package = await packageService.GetPackageAsync(releaseInfo!.PackageUri);
                _downloadSize = _package.Files.Sum(x => x.FileSize);
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex, "Failed to find release");
                ShowError("Failed to find latest release", ex, retryCallback: FetchLatestPackage);
                return;
            }
            finally
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.None;
            }

            ShowInstallOptions();
        }

        private void ShowInstallOptions()
        {
            Debug.Assert(_package != null);
            installOptionsScreen.InitInstance(_package.Version.ToNormalizedString(), _downloadSize);
            ShowScreen(installOptionsScreen);
        }

        private async void Install()
        {
            Debug.Assert(_package != null);

            try
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                await InstallerService.InstallAsync(InstallOperation.Install, _package.Uri, Options, HandleInstallerEvent);
            }
            catch (Exception ex)
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.None;
                App.Logger.LogError(ex, "Failed to install");
                ShowError("Failed to install", ex, retryCallback: () => Install());
                return;
            }
        }

        private async void ResumeInstall()
        {
            installingScreen.SetStatus("Installing...");
            ShowScreen(installingScreen);

            try
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                await InstallerService.ResumeAsync(HandleInstallerEvent);
            }
            catch (Exception ex)
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.None;
                App.Logger.LogError(ex, "Failed to resume install");
                ShowError("Failed to resume install", ex, retryCallback: () => ResumeInstall());
                return;
            }
        }

        void HandleInstallerEvent(IInstallerEvent installerEvent)
        {
            switch (installerEvent)
            {
                case DownloadStartedEvent:
                    ShowScreen(downloadScreen);
                    break;
                case InstallStartedEvent:
                    App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                    ShowScreen(installingScreen);
                    break;
                case DownloadPackageProgressEvent progressEvent:
                    downloadScreen.UpdateProgress(progressEvent.Progress);
                    UpateTaskBarProgress(progressEvent.Progress);
                    break;
            }
        }

        private void UpateTaskBarProgress(DownloadProgress progress)
        {
            if (progress.Status == DownloadStatus.Downloading)
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Normal;
                App.TaskBar.ProgressValue = progress.Progress;
            }
        }

        void IWizard.GotoNextStep()
        {
            if (stage.Content == welcomeScreen)
            {
                if (Options.AddFirewallRule)
                {
                    string args = string.Join(" ", Environment.GetCommandLineArgs().Concat(new[] { "--skipwelcome" }));
                    SystemUtility.ExecuteProcessElevated(Environment.ProcessPath!, args, Environment.CurrentDirectory);
                    App.Current.Shutdown();
                    return;
                }
                else
                {
                    FetchLatestPackage();
                }
            }
            if (stage.Content == installOptionsScreen)
            {
                Install();
            }
        }

        void IWizard.Cancel()
        {
            Close();
        }

        public void ShowError(string message, Exception? exception, Action? retryCallback = null)
        {
            Show();
            errorScreen.SetError(message, exception, retryCallback);
            ShowScreen(errorScreen);
        }

        public void ShowLearnMore()
        {
            var learnMoreScreen = new LearnMoreScreen();
            ShowScreen(learnMoreScreen);
        }


        private void ShowScreen(UserControl screen)
        {
            var transform = new TranslateTransform(0, 0);
            var slideAnim = new DoubleAnimation(30, 0, TimeSpan.FromSeconds(0.25)) { EasingFunction = new QuadraticEase() };
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)) { EasingFunction = new QuadraticEase() };
            screen.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            screen.BeginAnimation(OpacityProperty, fadeAnim);
            stage.Content = screen;
        }
    }
}
