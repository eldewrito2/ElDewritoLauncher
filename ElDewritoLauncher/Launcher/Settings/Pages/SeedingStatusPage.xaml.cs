using EDLauncher.Launcher.Models;
using EDLauncher.Launcher.Services;
using EDLauncher.Utility;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TorrentLib;

namespace EDLauncher.Launcher.Settings.Pages
{
    public partial class SeedingStatusPage : UserControl
    {
        public SeedingStatusPage()
        {
            InitializeComponent();
            this.Loaded += SeedingStatusPage_Loaded;
            App.Seeder.StatusUpdated += Seeder_StatusUpdated;
            App.Seeder.ErrorOccured += Seeder_ErrorOccured;
            App.LauncherState.PropertyChanged += LauncherState_PropertyChanged;
        }

        private void Seeder_ErrorOccured(object? sender, SeedErrorEventArgs e)
        {
            txtStatus.Text = $"Error: {e.Error.Message}";
            txtStatus.Tag = "error";
        }

        private void Seeder_StatusUpdated(object? sender, TorrentStatusUpdatedEventArgs e)
        {
            txtStatus.Text = GetStatusText(e.Status.State);
            txtStatus.Tag = null;

            if (e.Status.State == TorrentState.CheckingFiles || e.Status.State == TorrentState.CheckingResumeData)
            {
                progress.Value = e.Status.Progress * 100;
                progress.IsIndeterminate = false;
                progress.Visibility = Visibility.Visible;
            }
            else
            {
                progress.Visibility = Visibility.Collapsed;
            }

            if (e.Status.State == TorrentState.Seeding)
            {
                txtStatus.Tag = "success";
            }

            int totalSeeds = e.Status.NumComplete != -1 ? e.Status.NumComplete : e.Status.ListSeeds;
            int totalPeers = e.Status.ListPeers;

            txtTimeActive.Text = e.Status.ActiveDuration.ToString("c");
            txtTotalUpload.Text = FormatUtils.FormatSize(e.Status.TotalUpload);
            txtUploadRate.Text = FormatUtils.FormatSpeed(e.Status.UploadRate);
            txtSeeds.Text = $"{totalSeeds}";
            txtPeers.Text = $"{e.Status.NumPeers}  ({totalPeers})";
        }

        private void btnConfigure_Click(object sender, RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<SeedingPage>(this)!.ShowConfig();
        }

        private string GetStatusText(TorrentState state)
        {
            switch (state)
            {
                case TorrentState.CheckingResumeData:
                case TorrentState.CheckingFiles:
                    return "Checking Files";
                case TorrentState.DownloadingMetadata:
                    return "Downloading Metadata";
                case TorrentState.Downloading:
                    return "Downloadin";
                case TorrentState.Finished:
                    return "Modified Files";
                case TorrentState.Seeding:
                    return "Seeding";
                case TorrentState.Allocating:
                    return "Allocating";
                default:
                    return "Unknown";
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (App.LauncherState.IsSeeding)
            {
                btnStart.SetValue(AttachedProperties.IsBusyProperty, true);
                await App.Seeder.StopSeedingAsycnc();
                btnStart.SetValue(AttachedProperties.IsBusyProperty, false);
            }
            else
            {
                App.Seeder.StartSeeding();
            }
        }

        private void SeedingStatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.LauncherState.PropertyChanged += LauncherState_PropertyChanged;
        }

        private void LauncherState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LauncherState.IsSeeding))
            {
                btnConfigure.IsEnabled = !App.LauncherState.IsSeeding;

                if (!App.LauncherState.IsSeeding)
                {
                    ResetDisplay();
                }
            }
        }

        private void ResetDisplay()
        {
            if ((string?)txtStatus.Tag != "error")
            {
                txtStatus.Text = "-";
                txtStatus.Tag = null;
            }
            txtTimeActive.Text = "-";
            txtUploadRate.Text = "-";
            txtTotalUpload.Text = "-";
            txtPeers.Text = "-";
            txtSeeds.Text = "-";
            progress.Visibility = Visibility.Collapsed;
        }

        private void linkLearnMore_Click(object sender, RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<SeedingPage>(this)!.ShowInfo();
        }

        private async void linkTorrentFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? torrentPath = await App.Seeder.GetTorrentFilePathAsync();
                if (torrentPath == null)
                {
                    MessageBox.Show("Could not find torrent file for current version. You may need to verify files", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string? directoryPath = Path.GetDirectoryName(torrentPath);
                Debug.Assert(directoryPath != null);

                SystemUtility.ExecuteProcess("explorer.exe", directoryPath);
            }
            catch(Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<SeedingStatusPage>>().LogError(ex, "Failed to open torrent file location");
                MessageBox.Show("Failed to open torrent file location: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private async void linkMagnet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? magnetLink = await App.Seeder.GetMagnetLinkAsync();
                if (magnetLink == null)
                {
                    MessageBox.Show("Could not find torrent file for current version. You may need to verify files", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Clipboard.SetDataObject(magnetLink);
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<SeedingStatusPage>>().LogError(ex, "Failed to open torrent file location");
                MessageBox.Show("Failed to copy magnet link: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
