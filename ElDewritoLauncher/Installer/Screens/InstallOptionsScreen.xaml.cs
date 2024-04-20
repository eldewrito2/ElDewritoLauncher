using EDLauncher.Core;
using EDLauncher.Utility;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Installer.Screens
{
    public partial class InstallOptionsScreen : UserControl
    {
        private IWizard _wizard = null!;
        private DebounceHelper _locationChangedDebounce;

        public InstallOptionsScreen()
        {
            InitializeComponent();
            _locationChangedDebounce = new DebounceHelper(TimeSpan.FromMilliseconds(500), () =>
            {
                OnInstallLocationChanged();
            });
        }

        public void SetWizard(IWizard wizard)
        {
            _wizard = wizard;
            txtInstallLocation.Text = _wizard.Options.InstallPath;
            checkAddDesktopShortcut.IsChecked = _wizard.Options.CreateDesktopShortcut;
        }

        public void InitInstance(string version, long downloadSize)
        {
            txtDownloadInfo.Text = $"Required space: {FormatUtils.FormatSize(downloadSize)}";
        }

        private void OnInstallLocationChanged()
        {
            CheckFolderNotEmpty();
            DisplayAvailableDiskSpace();
        }

        private void CheckFolderNotEmpty()
        {
            try
            {
                if (!FileUtility.IsFolderEmpty(txtInstallLocation.Text))
                {
                    txtEmptuFolderWarning.Visibility = Visibility.Visible;
                }
                else
                {
                    txtEmptuFolderWarning.Visibility = Visibility.Collapsed;
                }
            }
            catch(Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<InstallOptionsScreen>>().LogError(ex, "Failed to check if the folder was empty");
                txtEmptuFolderWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayAvailableDiskSpace()
        {
            try
            {
                long diskSpace = FileUtility.GetAvailableDiskSpace(txtInstallLocation.Text);
                txtAvailableDiskSpace.Text = $"Available space: {FormatUtils.FormatSize(diskSpace)}";
            }
            catch(Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<InstallOptionsScreen>>().LogError(ex, "Failed to get available disk space");
                txtAvailableDiskSpace.Text = $"Available space: Unknown";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _wizard.Cancel();
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (!AttemptCreateInstallDirectory())
                return;

            _wizard.Options.InstallPath = txtInstallLocation.Text;
            _wizard.Options.CreateDesktopShortcut = checkAddDesktopShortcut.IsChecked == true;
            _wizard.GotoNextStep();
        }

        private bool AttemptCreateInstallDirectory()
        {
            try
            {
                Directory.CreateDirectory(txtInstallLocation.Text);
                FileUtility.CheckDirectoryWritePermission(txtInstallLocation.Text);
                return true;
            }
            catch(UnauthorizedAccessException ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<InstallOptionsScreen>>().LogError(ex, "Failed to create install directory");
                MessageBox.Show(
                    "Your User Account does not have permission to install to the chosen folder. Please choose a different folder.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch(Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<InstallOptionsScreen>>().LogError(ex, "Failed to create install directory");
                MessageBox.Show($"Could not create the install directory: {ex.Message}.\nPlease try again or choose a different folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        private void btnBrowseLocation_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = txtInstallLocation.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtInstallLocation.Text = Path.Combine(dialog.SelectedPath, Constants.ApplicationName);
                OnInstallLocationChanged();
            }
        }

        private void txtInstallLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            _locationChangedDebounce.Debounce();
        }
    }
}
