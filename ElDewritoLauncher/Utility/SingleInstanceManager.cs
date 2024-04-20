using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace EDLauncher.Utility
{
    public class SingleInstanceManager
    {
        const string mutexName = "{1D410651-A46A-42B1-9D75-7ABEB25DED1C}";
        const string semaphoreName = "{B5A1D8F7-2932-4525-B399-D305CC6E99A3}";

        private CancellationTokenSource _cancellationTokenSource = new();
        private Semaphore? _semaphore;
        private Thread? _activatorThread;

        public event Action? Activated;

        public void RunApp(Action app)
        {
            // Try to acquire the mutex
            using var mutex = new Mutex(true, mutexName, out bool createdNew);
            if (!createdNew)
            {
                // Another instance exists, try to open its activation semaphore
                if (Semaphore.TryOpenExisting(semaphoreName, out Semaphore? semaphore))
                {
                    // Activate the existing instance and exit
                    using (semaphore)
                    {
                        semaphore.Release();
                        return;
                    }
                }
                else
                {
                    // If the semaphore can't be opened, that likely means the previous
                    // instance is shutting down  wait for it give up the mutex
                    if (!WaitForPreviousInstanceToExit(mutex))
                        return;
                }
            }

            InitActivator();
            app();
            SignalShutdown();
        }

        public void SignalShutdown()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            Debug.Assert(_activatorThread != null);
            Debug.Assert(_semaphore != null);

            // Signal the activator thread and wait for it to exit
            _cancellationTokenSource.Cancel();
            _semaphore.Release();
            _activatorThread.Join();
            _activatorThread = null;

            // Dipose of the semaphore, signaling to any other instance that it should wait on the mutex
            _semaphore.Dispose();
            _semaphore = null;
        }

        private bool WaitForPreviousInstanceToExit(Mutex mutex)
        {
            try
            {
                return mutex.WaitOne(TimeSpan.FromSeconds(20));
            }
            catch (AbandonedMutexException)
            {
                // Other process exited, we own the mutex now
            }
            return true;
        }

        private void InitActivator()
        {
            CancellationToken cancelToken = _cancellationTokenSource.Token;
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            _semaphore = new Semaphore(0, int.MaxValue, semaphoreName);

            _activatorThread = new Thread(() => RunActivatorLoop(dispatcher, cancelToken));
            _activatorThread.Start();
        }

        private void RunActivatorLoop(Dispatcher dispatcher, CancellationToken cancelToken)
        {
            while (true)
            {
                if (_semaphore!.WaitOne())
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    dispatcher.InvokeAsync(() => Activated?.Invoke());
                }
            }
        }
    }
}
