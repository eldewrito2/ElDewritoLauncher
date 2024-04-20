using EDLauncher.Core;
using InstallerLib.Utility;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Launcher.Screens
{
    public partial class ErrorScreen : UserControl
    {
        private WeakReference<Action?>? _weakRetryAction = null;
        private Exception? _exception;

        public ErrorScreen()
        {
            InitializeComponent();
        }

        public void SetError(string message, Exception? exception, Action? retryAction)
        {
            _exception = exception;
            _weakRetryAction = new WeakReference<Action?>(retryAction);
            txtMessage.Text = message;
            btnRetry.Visibility = retryAction != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            btnDetails.Visibility = exception != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void btnRetry_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_weakRetryAction != null && _weakRetryAction.TryGetTarget(out Action? retryAction))
            {
                retryAction?.Invoke();
            }
        }

        private void btnDetails_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Debug.Assert(_exception != null);
            SystemUtility.ExecuteProcess($"{System.IO.Path.Combine(Constants.GetLogDirectory(), "launcher.log")}");
        }
    }
}
