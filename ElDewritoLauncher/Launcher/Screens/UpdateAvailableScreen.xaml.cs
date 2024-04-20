using EDLauncher.Utility;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Launcher.Screens
{
    public partial class UpdateAvailableScreen : UserControl
    {
        public UpdateAvailableScreen()
        {
            InitializeComponent();
            Loaded += UpdateAvailableScreen_Loaded;
            Unloaded += UpdateAvailableScreen_Unloaded;
            App.LauncherState.PropertyChanged += LauncherState_PropertyChanged;
        }


        private void LauncherState_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            btnUpdateNow.SetValue(AttachedProperties.IsBusyProperty, App.LauncherState.IsDownloadingUpdate);
        }

        private void UpdateAvailableScreen_Loaded(object sender, RoutedEventArgs e)
        {
            string version = App.LauncherState.UpdateInfo!.Package.Version.ToNormalizedString();
            long downloadSize = App.LauncherState.UpdateInfo!.DownloadSize;

            txtVersion.Text = $"Version: {version}";
            txtDownloadSize.Text = $"Download Size: {FormatUtils.FormatSize(downloadSize)}";
        }

        private void UpdateAvailableScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            btnUpdateNow.SetValue(AttachedProperties.IsBusyProperty, false);
        }

        private void btnUpdateNow_Clicked(object sender, RoutedEventArgs e)
        {
            // bit of a hack, but the file usage background check may take a second or two
            btnUpdateNow.SetValue(AttachedProperties.IsBusyProperty, true);

            App.UpdateDownloader.StartDownload();
        }
    }
}
