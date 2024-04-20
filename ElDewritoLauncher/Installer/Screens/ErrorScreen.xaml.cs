using EDLauncher.Core;
using InstallerLib.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Installer.Screens
{
    public partial class ErrorScreen : UserControl
    {
        private Action? _retryAction = null;
        private Exception? _exception;

        public ErrorScreen()
        {
            InitializeComponent();
            this.Unloaded += ErrorScreen_Unloaded;
        }

        private void ErrorScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
        }

        public void SetError(string message, Exception? exception, Action? retryAction)
        {
            App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
            _exception = exception;
            _retryAction = retryAction;
            txtMessage.Text = message;
            btnRetry.Visibility = retryAction != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            btnDetails.Visibility = exception != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void btnRetry_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _retryAction?.Invoke();
        }

        private void btnDetails_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Debug.Assert(_exception != null);
            SystemUtility.ExecuteProcess($"{Path.Combine(Constants.GetLogDirectory(), "launcher.log")}");
        }

        private void discordLink_Click(object sender, RoutedEventArgs e)
        {
            SystemUtility.ExecuteProcess(Constants.DiscordUrl);
        }
    }
}
