using EDLauncher.Core;
using EDLauncher.Launcher.Models;
using EDLauncher.Utility;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace EDLauncher.Launcher
{
    /// <summary>
    /// Interaction logic for PlayTab.xaml
    /// </summary>
    public partial class PlayTab : UserControl
    {
        public PlayTab()
        {
            InitializeComponent();

            Loaded += PlayTab_Loaded;
            App.LauncherState.PropertyChanged += LauncherState_PropertyChanged;
        }

        private void LauncherState_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            LauncherState state = App.LauncherState;

            UpdateDisplay();

            if (e.PropertyName == nameof(LauncherState.IsUpdateAvailable) && state.IsUpdateAvailable)
            {
                Storyboard storyboard = (Storyboard)FindResource("updateAvailableStoryBoard");
                storyboard.Begin();
            }

            if (e.PropertyName == nameof(LauncherState.IsCheckingForUpdate))
            {
                // show/Hide the spinner on the play button
                bool forceEnable = btnPlay.IsEnabled;  // hack to override the attached property styles
                btnPlay.SetValue(AttachedProperties.IsBusyProperty, state.IsCheckingForUpdate);
                if(forceEnable)
                {
                    btnPlay.IsEnabled = true;
                }
            }
        }

        public void UpdateDisplay()
        {
            LauncherState state = App.LauncherState;
            txtVersion.Text = $"Version: {state.CurrentVersion}";

            updateCheckStatus.Visibility = Visibility.Collapsed;
            updateCheckFailed.Visibility = Visibility.Collapsed;

            if (state.IsCheckingForUpdate)
            {
                txtStatus1.Text = "Checking for updates...";
                updateCheckStatus.Visibility = Visibility.Visible;
            }
            else if(state.UpdateCheckError != null)
            {
                updateCheckFailed.Visibility = Visibility.Visible;
            }

            if (state.IsUpdateAvailable)
            {
                btnPlay.Style = (Style)FindResource("L_SecondaryButtonStyle");
                btnUpdate.Visibility = Visibility.Visible;
            }
            else
            {
                btnPlay.Style = (Style)FindResource("L_PrimaryButtonStyle");
                btnUpdate.Visibility = Visibility.Collapsed;
            }
        }

        public void SetUpdateCheckStatus(string status)
        {
            txtStatus1.Text = status;
        }

        public void ShowUpdateCheck(bool show)
        {
            updateCheckStatus.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PlayTab_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplay();
        }

        private void btnUpdate_Clicked(object sender, RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<LauncherWindow>(this)!.ShowUpdateDialog(true);
        }

        private async void btnPlay_Clicked(object sender, RoutedEventArgs e)
        {
            btnPlay.SetValue(AttachedProperties.IsBusyProperty, true);

            await Task.Run(() =>
            {
                SystemUtility.ExecuteProcess("eldorado.exe", App.LauncherSettings.LaunchArguments);
            });

            try
            {
                if (!App.LauncherSettings.DoNotClose)
                {
                    await Dispatcher.InvokeAsync(() => App.Current.Shutdown());
                }
                else
                {
                    await Task.Delay(1000);
                    btnPlay.SetValue(AttachedProperties.IsBusyProperty, false);
                }
            }
            catch(Exception ex)
            {
                // being overally cautious at this point
                App.ServiceProvider.GetRequiredService<ILogger<PlayTab>>().LogError(ex, "Failed to shutdown launcher after launching the game");
            }
        }


        private void SocialButton_Clicked(object sender, RoutedEventArgs e)
        {
            switch ((string)((Button)sender).Tag)
            {
                case "discord":
                    SystemUtility.ExecuteProcess(Constants.DiscordUrl);
                    break;
                case "reddit":
                    SystemUtility.ExecuteProcess(Constants.RedditUrl);
                    break;
            }
        }

        private void updateCheckErrorDetailsLink_Click(object sender, RoutedEventArgs e)
        {
            if (App.LauncherState.UpdateCheckError == null)
                return;

            MessageBox.Show(App.LauncherState.UpdateCheckError.ToString(), "Error Details", MessageBoxButton.OK);
        }
    }
}
