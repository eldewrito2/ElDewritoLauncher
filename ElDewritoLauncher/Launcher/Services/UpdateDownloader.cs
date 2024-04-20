using EDLauncher.Core.Install;
using EDLauncher.Installer.Services;
using EDLauncher.Launcher.Models;
using InstallerLib.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;

namespace EDLauncher.Launcher.Services
{
    public class UpdateDownloader
    {
        private readonly ILogger _logger;
        private readonly IUpdateService _updateService;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler? DownloadStarted;
        public event EventHandler<DownloadErrorEventArgs>? DownloadError;
        public event EventHandler<DownloadProgress>? ProgressChanged;
        public event EventHandler<ProgramsNeedClosingEventArgs>? ProgramsNeedClosing;

        public UpdateDownloader(IUpdateService updateService)
        {
            _logger = App.ServiceProvider.GetRequiredService<ILogger<UpdateDownloader>>();
            _updateService = updateService;
        }

        public void CancelDownload()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async void StartDownload()
        {
            await StopSeedingAsync();

            if (App.LauncherState.IsSeeding)
            {
                _logger.LogError("Cannot begin update while still seeding");
                return;
            }

            _logger.LogInformation("Starting download");

            if (App.LauncherState.IsDownloadingUpdate)
            {
                _logger.LogError("Cannot start the download until the previous download has finished");
                return;
            }

            // TODO: come up with something better
            ((LauncherWindow)App.Current.MainWindow).ShowUpdateDialog(false);

            string directory = Environment.CurrentDirectory;
            UpdateInfo? updateInfo = App.LauncherState.UpdateInfo;
            Debug.Assert(updateInfo != null);

            List<Process> blockingProcesses = await _updateService.CheckProgramsNeedClosing(updateInfo.Package, directory);
            if (blockingProcesses.Count > 0)
            {
                ProgramsNeedClosing?.Invoke(this, new ProgramsNeedClosingEventArgs(blockingProcesses));
                return;
            }

            DownloadStarted?.Invoke(this, EventArgs.Empty);
            App.LauncherState.IsDownloadingUpdate = true;
            DownloadStarted?.Invoke(this, EventArgs.Empty);
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                await _updateService.DownloadUpdateAsync(
                    updateInfo.Package.Uri,
                    directory,
                    (DownloadProgress progress) =>
                    {
                        // We are on the UI thread, however we don't want to catch exceptions from the callbacks here
                        InvokeLater(() =>
                        {
                            ProgressChanged?.Invoke(this, progress);
                            UpateTaskBarProgress(progress);
                        });
                    },
                    _cancellationTokenSource.Token);

                string args = "";
                if (App.LauncherState.IsMinimizedToTray)
                {
                    args += "--silent";
                }

                App.Relaunch(directory, args);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while downloading");
                // Queue the event with the dispatcher so that it can see the state we set below
                InvokeLater(() => DownloadError?.Invoke(this, new DownloadErrorEventArgs(ex)));
            }

            App.TaskBar.ProgressState = TaskbarItemProgressState.Normal;
            _cancellationTokenSource.Dispose();
            App.LauncherState.IsDownloadingUpdate = false;
        }

        private async Task StopSeedingAsync()
        {
            try
            {
                await App.Seeder.StopSeedingAsycnc();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop seeding");
            }
        }

        void InvokeLater(Action action)
        {
            var _ = Dispatcher.CurrentDispatcher.InvokeAsync(action);
        }

        private void UpateTaskBarProgress(DownloadProgress progress)
        {
            if (progress.Status == DownloadStatus.Downloading)
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Normal;
                App.TaskBar.ProgressValue = progress.Progress;
            }
        }

        public class DownloadErrorEventArgs : EventArgs
        {
            public DownloadErrorEventArgs(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }
        }
    }

    public class ProgramsNeedClosingEventArgs : EventArgs
    {
        public ProgramsNeedClosingEventArgs(List<Process> processList)
        {
            ProcessList = processList;
        }

        public List<Process> ProcessList { get; }
    }
}
