using EDLauncher.Utility;
using InstallerLib.Packages;
using System;
using System.Windows.Controls;

namespace EDLauncher.Installer.Screens
{
    public partial class DownloadScreen : UserControl
    {
        public DownloadScreen()
        {
            InitializeComponent();
        }

        internal void UpdateProgress(DownloadProgress progress)
        {
            Console.WriteLine($"Downloading {progress.Progress * 100:0.0}%");

            this.progress.Value = progress.Progress * 100;
            this.progress.IsIndeterminate = false;

            txtSpeed.Text = FormatUtils.FormatSpeed(progress.DownloadRate);


            if (progress.ETA == default)
                txtETA.Text = $"Time Remaining: Calculating...";
            else
                txtETA.Text = $"Time Remaining: {FormatUtils.FormatDuration2(progress.ETA)}";

            if (progress.Status == DownloadStatus.Downloading)
            {
                txtStatus.Text = "Downloading...";
                txtETA.Visibility = System.Windows.Visibility.Visible;
                txtSpeed.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                txtStatus.Text = "Checking files...";
                txtETA.Visibility = System.Windows.Visibility.Hidden;
                txtSpeed.Visibility = System.Windows.Visibility.Hidden;
            }
        }
    }
}
