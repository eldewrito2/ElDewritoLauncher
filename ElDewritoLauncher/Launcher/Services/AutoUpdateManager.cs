using EDLauncher.Launcher.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EDLauncher.Launcher.Services
{
    public class AutoUpdateManager
    {
        private readonly TimeSpan AttemptInterval = TimeSpan.FromMinutes(10);

        private bool _scheduled = false;
        private ILogger _logger;
        private CancellationTokenSource? _cancelTokenSource;

        public AutoUpdateManager(ILogger<AutoUpdateManager> logger)
        {
            _logger = logger;
        }

        public void ScheduleUpdate()
        {
            if (_scheduled)
            {
                return;
            }

            _scheduled = true;
            _cancelTokenSource?.Dispose();
            _cancelTokenSource = new CancellationTokenSource();
            RunUpdateLoop(_cancelTokenSource.Token);
        }

        public void Cancel()
        {
            if (_cancelTokenSource != null)
            {
                _cancelTokenSource?.Cancel();
                _cancelTokenSource = null;
                App.LauncherState.IsUpdateAvailable = false;
            }
        }

        private async void RunUpdateLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await AttemptUpdate())
                    break;

                await Task.Delay(AttemptInterval);
            }
            _scheduled = false;
            _cancelTokenSource = null;
        }

        private async Task<bool> AttemptUpdate()
        {
            try
            {
                return await AttempUpdateCore();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while auto updating");
            }
            return false;
        }

        private async Task<bool> AttempUpdateCore()
        {

            string directory = Environment.CurrentDirectory;

            if (!App.LauncherState.IsUpdateAvailable)
            {
                _logger.LogError("Tried to auto update, but no update was available.");
                return true;
            }

            if (App.LauncherState.IsSeeding)
            {
                _logger.LogInformation("Stopping seeding to update...");
                await App.Seeder.StopSeedingAsycnc();
            }

            UpdateInfo updateInfo = App.LauncherState.UpdateInfo!;

            List<Process> blockingProcesses = await App.UpdateService.CheckProgramsNeedClosing(updateInfo.Package, directory);
            if (blockingProcesses.Count > 0)
            {
                _logger.LogError($"Auto update failed, programs need closing ");
                return false;
            }

            _logger.LogInformation("Starting auto update...");
            App.UpdateDownloader.StartDownload();
            return true;
        }
    }
}
