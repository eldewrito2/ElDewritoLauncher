using EDLauncher.Launcher.Screens;
using EDLauncher.Launcher.Services;
using EDLauncher.Utility;
using InstallerLib.Packages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EDLauncher.Launcher
{
    /// <summary>
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : UserControl
    {
        private UpdateAvailableScreen updateAvailableScreen;
        private DownloadScreen downloadScreen;
        private CloseProgramsScreen programsNeedClosingScreen;
        private ErrorScreen errorScreen;


        public UpdateDialog()
        {
            updateAvailableScreen = new UpdateAvailableScreen();
            downloadScreen = new DownloadScreen();
            errorScreen = new ErrorScreen();
            programsNeedClosingScreen = new CloseProgramsScreen();

            InitializeComponent();

            App.UpdateDownloader.DownloadStarted += UpdateDownloader_DownloadStarted;
            App.UpdateDownloader.ProgressChanged += UpdateDownloader_ProgressChanged;
            App.UpdateDownloader.DownloadError += UpdateDownloader_DownloadError;
            App.UpdateDownloader.ProgramsNeedClosing += UpdateDownloader_ProgramsNeedClosing;
        }

        private void UpdateDownloader_ProgramsNeedClosing(object? sender, ProgramsNeedClosingEventArgs e)
        {
            programsNeedClosingScreen.SetProcessList(e.ProcessList);
            ShowScreen(programsNeedClosingScreen);
        }

        private void UpdateDownloader_DownloadError(object? sender, UpdateDownloader.DownloadErrorEventArgs e)
        {
            errorScreen.SetError("The download has failed. Please try again.", e.Exception, retryAction: () => App.UpdateDownloader.StartDownload());
            ShowScreen(errorScreen);
        }

        private void UpdateDownloader_ProgressChanged(object? sender, DownloadProgress progress)
        {
            downloadScreen.UpdateProgress(progress);
        }

        private void UpdateDownloader_DownloadStarted(object? sender, EventArgs e)
        {
            ShowScreen(downloadScreen);
        }

        public void Hide()
        {
            body.Content = null;
        }

        public void ShowUpdateAvailable()
        {
            ShowScreen(updateAvailableScreen);
        }

        public void Show()
        {
            var transform = new TranslateTransform(0, 0);

            var slideAnim = new DoubleAnimation(15, 0, TimeSpan.FromSeconds(0.25)) { EasingFunction = new CubicEase() };
            inner.RenderTransform = transform;


            var fadeAnim = new DoubleAnimation();
            fadeAnim.Duration = TimeSpan.FromSeconds(0.2);
            fadeAnim.From = 0;
            fadeAnim.To = 1;

            transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            inner.BeginAnimation(OpacityProperty, fadeAnim);

        }

        private void ShowScreen(UserControl screen)
        {
            if (body.Content != null)
            {
                //screen.IsHitTestVisible = false;
                screen.Opacity = 0;

                var transform = new TranslateTransform(0, 0);
                var slideAnim = new DoubleAnimation(0, 0, TimeSpan.FromSeconds(0.4)) { EasingFunction = new CubicEase() };
                screen.RenderTransform = transform;
                var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));

                //slideAnim.Completed += (s, e) => screen.IsHitTestVisible = true;

                transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
                screen.BeginAnimation(OpacityProperty, fadeAnim);
            }
            else
            {
                screen.Opacity = 1;
            }

            body.Content = screen;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (App.LauncherState.IsDownloadingUpdate)
            {
                MessageBoxResult result = MessageBox.Show(
                     "Closing now could leave the game unplayable. Are you sure you want to close?",
                     "Warning",
                     MessageBoxButton.YesNo,
                     MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }

                App.UpdateDownloader.CancelDownload();
            }

            VisualTreeHelpers.FindAncestor<LauncherWindow>(this)!.HideUpdateDialog();
        }

        private void backdrop_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Allow moving the window
            if (e.Source == backdrop && e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                var window = VisualTreeHelpers.FindAncestor<LauncherWindow>(this);
                window?.DragMove();
            }
        }
    }
}
