using EDLauncher.Launcher.Models;
using ElDewritoLauncher.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Windows.Threading;

namespace EDLauncher.Launcher.Services
{
    public class UpdateChecker
    {
        private readonly IUpdateService _updateService;
        private CancellationTokenSource? _cancellationTokenSource;
        private DispatcherTimer? _timer = null;

        public UpdateChecker(IUpdateService updateCheckService)
        {
            _updateService = updateCheckService;

            App.LauncherSettings.PropertyChanged += LauncherSettings_PropertyChanged;

            ScheduleTimer();
        }

        private void LauncherSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LauncherSettings.UpdateCheckInterval))
            {
                ScheduleTimer();
            }
        }

        public void CancelCheck()
        {
            _cancellationTokenSource?.Cancel();
            App.LauncherState.IsCheckingForUpdate = false;
        }

        public async void StartCheck(UpdateCheckInitiator initiator)
        {
            var logger = App.ServiceProvider.GetRequiredService<ILogger<UpdateChecker>>();
            logger.LogInformation($"Starting update check. Initiator: {initiator}");

            if (App.LauncherState.IsDownloadingUpdate)
            {
                return;
            }

            if (App.LauncherState.IsCheckingForUpdate)
            {
                logger.LogError("Already checking for updates");
                return;
            }

            App.LauncherState.UpdateCheckInitiator = initiator;
            App.LauncherState.LastUpdateCheck = DateTimeOffset.Now;
            App.LauncherState.IsCheckingForUpdate = true;
            App.LauncherState.UpdateCheckError = null;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));
        
            try
            {
                UpdateCheckResult updateCheckResult = await _updateService.CheckForUpdateAsync(
                    App.LauncherSettings.ReleaseChannel,
                    App.LauncherState.CurrentVersion,
                    Environment.CurrentDirectory,
                    _cancellationTokenSource.Token);

                if (updateCheckResult != null && updateCheckResult.isAvailable)
                {
                    if (initiator == UpdateCheckInitiator.User || App.LauncherState.UpdateInfo?.Package.Version != updateCheckResult.package!.Version)
                    {
                        OnUpdateAvailable(initiator, updateCheckResult);
                    }
                }
            }
            catch(Exception ex)
            {
                App.LauncherState.UpdateCheckError = ex;
            }
            finally
            {
                App.LauncherState.IsCheckingForUpdate = false;
                _cancellationTokenSource.Dispose();
            }
        }

        private void OnUpdateAvailable(UpdateCheckInitiator initiator, UpdateCheckResult updateCheckResult)
        {
            App.LauncherState.UpdateInfo = new UpdateInfo(updateCheckResult.package!, updateCheckResult.downloadSize);
            App.LauncherState.IsUpdateAvailable = true;
            App.LauncherState.RaisePropertyChanged(nameof(LauncherState.IsUpdateAvailable));

            if (App.LauncherSettings.EnableAutoUpdate)
            {
                if (initiator != UpdateCheckInitiator.User)
                {
                    App.ServiceProvider.GetRequiredService<AutoUpdateManager>().ScheduleUpdate();
                }
            }
            else
            {
                if (App.LauncherSettings.EnableDesktopNotifcations)
                {
                    App.Toasts.ShowToast(new UpdateAvailableToast());
                }
            }
        }

        private void ScheduleTimer()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick += (s, e) =>
                {
                    StartCheck(UpdateCheckInitiator.Automatic);
                };
            }

            _timer.Stop();

            if (App.LauncherSettings.UpdateCheckInterval != 0)
            {
                _timer.Interval = TimeSpan.FromMinutes(App.LauncherSettings.UpdateCheckInterval);
                _timer.Start();
            }
        }
    }
}