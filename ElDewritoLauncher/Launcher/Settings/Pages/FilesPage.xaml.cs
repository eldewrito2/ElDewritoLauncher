using EDLauncher.Core;
using EDLauncher.Core.Install;
using EDLauncher.Launcher.Services;
using EDLauncher.Utility;
using InstallerLib.Packages;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Launcher.Settings.Pages
{
    public partial class FilesPage : UserControl
    {
        private readonly string _directory;

        public FilesPage()
        {
            _directory = Environment.CurrentDirectory;

            InitializeComponent();
            Loaded += FilesPage_Loaded;
        }

        private void FilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private async void Refresh()
        {
            try
            {
                txtLocation.Text = _directory;
                UsageInfo info = await Task.Run(() => GatherUsageInfo(_directory));
                txtTotalUsage.Text = FormatUtils.FormatSize(info.TotalUsage);

                double mapsFraction = 0, modsFraction = 0, cacheFraction = 0, otherFraction = 0;
                if (info.TotalUsage != 0.0)
                {
                    modsFraction = (info.ModsUsage) / (double)info.TotalUsage;
                    mapsFraction = (info.MapsUsage) / (double)info.TotalUsage;
                    cacheFraction = (info.CacheUsage) / (double)info.TotalUsage;
                    otherFraction = (info.OtherUsage) / (double)info.TotalUsage;
                }

                double[] fractions = new double[] { modsFraction, mapsFraction, cacheFraction, otherFraction };
                double[] widths = new double[fractions.Length];
                CalculateUsageSliceWidths(widths, fractions, usageBar.ActualWidth, 5);

                modsUsageSlice.Width = widths[0];
                mapsUsageSlice.Width = widths[1];
                cacheUsageSlice.Width = widths[2];
                otherUsageSlice.Width = widths[3];

                txtModsUsage.Text = FormatUtils.FormatSize(info.ModsUsage);
                txtMapsUsage.Text = FormatUtils.FormatSize(info.MapsUsage);
                txtOtherUsage.Text = FormatUtils.FormatSize(info.OtherUsage);
                txtCacheUsage.Text = FormatUtils.FormatSize(info.CacheUsage);
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<FilesPage>>().LogError(ex, "Failed to get file usage");
                MessageBox.Show(App.Current.MainWindow, "Failed to gather usage info", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateUsageSliceWidths(double[] widths, double[] fractions, double totalWidth, double minWidth)
        {
            double adjustedWidth = 0;
            for (int i = 0; i < fractions.Length; i++)
            {
                widths[i] = fractions[i] == 0.0 ? 0 : Math.Max(minWidth, fractions[i] * totalWidth);
                adjustedWidth += widths[i];
            }
            double adjustment = totalWidth - adjustedWidth;
            if (adjustedWidth > 0)
            {
                double ratio = adjustment / adjustedWidth;
                for (int i = 0; i < fractions.Length; i++)
                {
                    widths[i] += widths[i] * ratio;
                }
            }
        }

        class UsageInfo
        {
            public long TotalUsage { get; set; }
            public long ModsUsage { get; set; }
            public long CacheUsage { get; set; }
            public long MapsUsage { get; set; }
            public long OtherUsage { get; set; }
        }

        private static UsageInfo GatherUsageInfo(string directory)
        {
            var info = new UsageInfo();

            string mapsDirectory = Path.GetFullPath(Path.Combine(directory, "maps"));
            string modsDirectory = Path.GetFullPath(Path.Combine(directory, "mods"));
            string cacheDirectory = Path.GetFullPath(Path.Combine(directory, "data\\cache"));

            foreach (FileInfo fileInfo in new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (fileInfo.DirectoryName?.StartsWith(modsDirectory) == true)
                {
                    info.ModsUsage += fileInfo.Length;
                }
                else if (fileInfo.DirectoryName?.StartsWith(mapsDirectory) == true)
                {
                    info.MapsUsage += fileInfo.Length;
                }
                else if (fileInfo.DirectoryName?.StartsWith(cacheDirectory) == true)
                {
                    info.CacheUsage += fileInfo.Length;
                }
                else
                {
                    info.OtherUsage += fileInfo.Length;
                }

                info.TotalUsage += fileInfo.Length;
            }
            return info;
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _directory);
        }

        private async void btnDeleteCache_Click(object sender, RoutedEventArgs e)
        {
            AttachedProperties.SetIsBusy(btnDeleteCache, true);
            try
            {
                await Task.Run(async () =>
                {
                    string directory = Path.Combine(_directory, "data", "cache");
                    if (Directory.Exists(directory))
                    {
                        await FileUtility.RetryOnFileAccessErrorAsync(() => Directory.Delete(directory, true), 3, 50);
                    }
                });
                Refresh();
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<FilesPage>>().LogError(ex, "Failed to delete cache");
                MessageBox.Show($"Failed to delete cache: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AttachedProperties.SetIsBusy(btnDeleteCache, false);
            }
        }

        private async void btnVerifyFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                txtVerifyResult.Visibility = Visibility.Collapsed;
                btnVerifyFiles.SetValue(AttachedProperties.IsBusyProperty, true);

                FilesVerificationResult result = await FilesVerificatinService.VerifyFilesAsync(Environment.CurrentDirectory, (progress) =>
                {
                    btnVerifyFiles.Content = $"Verifying {progress:0.0}%";
                    App.TaskBar.ProgressValue = progress / 100.0;
                });

                if (result.InvalidFiles.Count > 0)
                {
                    MessageBoxResult decision = MessageBox.Show(
                        $"Found {result.InvalidFiles.Count} invalid {(result.InvalidFiles.Count == 1 ? "file" : "files")}. Repair now?",
                        "Invalid files found",
                        MessageBoxButton.YesNo, MessageBoxImage.Error);

                    if (decision == MessageBoxResult.Yes)
                    {
                        await RepairFilesAsync();
                    }
                }
                else
                {
                    txtVerifyResult.Text = $"Successfully verified {result.FileCount} files";
                    txtVerifyResult.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<FilesPage>>().LogError(ex, "Failed to verify files");
                MessageBox.Show($"Failed to verify files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                btnVerifyFiles.SetValue(AttachedProperties.IsBusyProperty, false);
                btnVerifyFiles.Content = "Verify Files";
            }
        }

        private async Task RepairFilesAsync()
        {
            // Stop Seeding
            if (App.LauncherState.IsSeeding)
            {
                try
                {
                    btnVerifyFiles.Content = $"Stopping seeding";
                    await App.Seeder.StopSeedingAsycnc();
                }
                catch
                {
                    // swallow the exception}
                }
            }

            // Cancel any auto updates
            if (App.LauncherState.IsAutoUpdating)
            {
                var autoUpdater = App.ServiceProvider.GetRequiredService<AutoUpdateManager>();
                autoUpdater.Cancel();
            }

            // Ideally we would run the installer in repair mode, but for now I'm just going to do it here
            try
            {
                App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                btnVerifyFiles.SetValue(AttachedProperties.IsBusyProperty, true);

                using IServiceScope scope = App.ServiceProvider.CreateScope();
                var releaseService = scope.ServiceProvider.GetRequiredService<IReleaseService>();

                string packageUri = null!;
                try
                {
                    btnVerifyFiles.Content = "Getting latest";
                    ReleaseInfo? releaseInfo = await releaseService.GetLatestAsync(App.LauncherSettings.ReleaseChannel);
                    if (releaseInfo == null)
                    {
                        throw new InvalidOperationException("Failed to find release info");
                    }

                    packageUri = releaseInfo.PackageUri;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to get latest release: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Check we haven't already started updating
                    if(App.LauncherState.IsDownloadingUpdate)
                    {
                        throw new InvalidOperationException("Cannot repair files an update is being downloaded");
                    }

                    App.LauncherState.IsDownloadingUpdate = true;
                    await App.UpdateService.DownloadUpdateAsync(packageUri, Environment.CurrentDirectory, (progress) =>
                    {
                        if (progress.Status == DownloadStatus.Checking)
                        {
                            btnVerifyFiles.Content = $"Checking files {(progress.Progress * 100):0.0}%";
                        }
                        else if (progress.Status == DownloadStatus.Downloading)
                        {
                            App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                            App.TaskBar.ProgressValue = progress.Progress;
                            btnVerifyFiles.Content = $"Downloading files {(progress.Progress * 100):0.0}%";
                        }
                    });

                    btnVerifyFiles.Content = $"Repairing files";
                    await Task.Delay(2000);
                    App.Relaunch(Environment.CurrentDirectory);
                }
                catch (Exception ex)
                {
                    App.ServiceProvider.GetRequiredService<ILogger<FilesPage>>().LogError(ex, "Failed to repair files");
                    MessageBox.Show($"Failed to repair files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    App.LauncherState.IsDownloadingUpdate = false;
                }
            }
            finally
            {
                App.TaskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                btnVerifyFiles.SetValue(AttachedProperties.IsBusyProperty, false);
                btnVerifyFiles.Content = "Verify Files";
            }
        }
    }
}
