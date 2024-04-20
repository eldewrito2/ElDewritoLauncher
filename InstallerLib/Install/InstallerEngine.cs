using InstallerLib.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InstallerLib.Install
{
    public interface IInstallerEngineFactory
    {
        IInstallerEngine CreateEngine(InstallerState state);
    }

    public class InstallerEngineFactory : IInstallerEngineFactory
    {
        private readonly IServiceProvider serviceProvider;

        public InstallerEngineFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IInstallerEngine CreateEngine(InstallerState state)
        {
            return new InstallerEngine(serviceProvider, state);
        }
    }

    public partial class InstallerEngine : IInstallerEngine
    {
        private int _currentStepIndex;
        private List<IInstallStep> _steps;
        private SemanticVersion _installerVersion { get; set; }
        private int _failureCount = 0;

        public event Action<IInstallerEvent>? EventRaised;

        public InstallerEngine(IServiceProvider provider, InstallerState state)
        {
            ServiceProvider = provider;
            Logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<InstallerEngine>();

            Operation = state.Operation;
            InstallDirectory = state.InstallDirectory;
            _steps = state.Steps;
            _installerVersion = SemanticVersion.Parse(state.InstallerVersion);
            _currentStepIndex = state.CurrentStep;
            _failureCount = state.FailureCount;
        }

        public bool IsCompleted => _currentStepIndex == _steps.Count;

        public bool IsRelaunchRequested { get; private set; }

        public IServiceProvider ServiceProvider { get; }

        public ILogger Logger { get; }

        public InstallOperation Operation { get; }

        public CancellationToken CancellationToken { get; private set; }

        public string TempDirectory { get; set; }

        public string InstallDirectory { get; set; }

        /// <summary>
        /// Executie the installation steps
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;

            Logger.LogInformation("Engine started");

            if (_currentStepIndex > 0)
            {
                Logger.LogInformation($"Resuming installation from step {_currentStepIndex}");
            }

            while (_currentStepIndex < _steps.Count)
            {
                try
                {
                    // Execute the current Step
                    await _steps[_currentStepIndex].Execute(this);
                    _currentStepIndex++;
                }
                catch (Exception ex)
                {
                    _failureCount++;
                    Logger.LogError(ex, $"Error during installation step {_currentStepIndex}: {ex.Message}");
                    throw;
                }

                // Check if a relaunch was requested
                if (IsRelaunchRequested)
                {
                    Logger.LogWarning("Relaunch was requested by one of the steps");

                    // Return control back to the caller
                    break;
                }
            }

            // success, reset the failure count
            _failureCount = 0;

            Logger.LogInformation("Engine finished");
        }


        /// <summary>
        /// Export the current state
        /// </summary>
        public InstallerState ExportState()
        {
            return new InstallerState()
            {
                Operation = Operation,
                InstallDirectory = InstallDirectory,
                InstallerVersion = _installerVersion.ToNormalizedString(),
                Steps = _steps,
                CurrentStep = _currentStepIndex,
                FailureCount = _failureCount
            };
        }

        /// <summary>
        /// Request a relaunch
        /// </summary>
        /// <param name="path">The path of the program to execute</param>
        public void RequestRelaunch(string path)
        {
            IsRelaunchRequested = true;
        }

        /// <summary>
        /// Raise an event for the client
        /// </summary>
        /// <param name="eventObject"></param>
        public void RaiseEvent(IInstallerEvent eventObject)
        {
            EventRaised?.Invoke(eventObject);
        }
    }
}