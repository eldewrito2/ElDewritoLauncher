using EDLauncher.Core.Utility;
using EDLauncher.Launcher.Models;
using EDLauncher.Utility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Launcher.Settings.Pages
{
    public partial class UpdatesPage : UserControl
    {
        public UpdatesPage()
        {
            InitializeComponent();
            Loaded += UpdatesPage_Loaded;
            App.LauncherState.PropertyChanged += LauncherState_PropertyChanged; ;
        }

        private void LauncherState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdatesPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            LauncherState state = App.LauncherState;

            txtCurrentVersion.Text = state.CurrentVersion;

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(typeof(UpdatesPage).Assembly.Location);
            txtLauncherVersion.Text = fileVersionInfo.ProductVersion!.Truncate(12, ellipsis: true);
            txtLauncherVersion.ToolTip = fileVersionInfo.ProductVersion;

            txtLastUpdateCheck.Visibility = Visibility.Collapsed;
            txtUpdateCheckFailed.Visibility = Visibility.Collapsed;
            if (state.UpdateCheckError != null)
            {
                txtUpdateCheckFailed.Visibility = Visibility.Visible;
            }
            else if(state.LastUpdateCheck != DateTimeOffset.MinValue)
            {
                txtLastUpdateCheck.Visibility = Visibility.Visible;
            }

            txtLastUpdateCheck.Text = $"Last checked: {state.LastUpdateCheck:G}";

            btnCheckUpdate.SetValue(AttachedProperties.IsBusyProperty, state.IsCheckingForUpdate);
        }

        private void btnCheckUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.UpdateChecker.StartCheck(UpdateCheckInitiator.User);
        }

        //private void cbReleaseChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    // hack to make sure it as user-initiated
        //    if (!cbReleaseChannel.IsLoaded)
        //        return;

        //    if (App.LauncherState.IsCheckingForUpdate)
        //    {
        //        App.UpdateChecker.CancelCheck();
        //    }

        //    App.UpdateChecker.StartCheck(UpdateCheckInitiator.User);
        //}

        private void updateCheckErrorDetailsLink_Click(object sender, RoutedEventArgs e)
        {
            if (App.LauncherState.UpdateCheckError == null)
                return;

            MessageBox.Show(App.LauncherState.UpdateCheckError.ToString(), "Error Details", MessageBoxButton.OK);
        }
    }
}
