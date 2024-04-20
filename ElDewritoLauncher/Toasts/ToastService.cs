using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElDewritoLauncher.Toasts
{
    public interface IToastService : IDisposable, IAsyncDisposable
    {
        void Install();
        void Uninstall();
        void ShowToast<T>(T toast);
    }

    internal class ToastService : IToastService, ToastManager.INotificationActivationCallback
    {
        private readonly ILogger _logger;
        private ToastManager? _toastManager;
        private ToastServiceOptions _options;
        private Thread? _thread;
        private BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private Dictionary<Type, Func<object, string>> _contentFactories = new();

        public ToastService(ToastServiceOptions options, ILogger<ToastService> logger)
        {
            _logger = logger;
            _options = options;
            StartToastThread();
            InitNativeManager(options);
        }

        public void Install()
        {
            ToastManager.Install(
               activatorPath: Environment.ProcessPath!,
               args: _options.Arguments,
               appGuid: _options.AppGuid,
               appId: _options.AppId,
               displayName: _options.DisplayName,
               iconUrl: _options.IconUrl);
        }

        public void Uninstall()
        {
            ToastManager.Uninstall(_options.AppGuid, _options.AppId);
        }

        public void RegisterToast<T>(Func<T, string> contentFactory)
        {
            _contentFactories.Add(typeof(T), (x) => contentFactory((T)x));
        }

        public void ShowToast<T>(T toast)
        {
            if (_toastManager == null)
                return;

            if (!_options.contentFactories.TryGetValue(typeof(T), out var factory))
            {
                throw new InvalidOperationException($"Toast not registered: '{typeof(T).FullName}'");
            }
            QueueAction(() => _toastManager!.ShowToast(factory(toast!)));
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _thread?.Join();
            _toastManager?.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose(); // no need for async
            return ValueTask.CompletedTask;
        }

        private void InitNativeManager(ToastServiceOptions options)
        {
            try
            {
                QueueAction(() => _toastManager = CreateNativeToastManager(options));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToastService could not start.");
            }
        }

        private ToastManager CreateNativeToastManager(ToastServiceOptions options)
        {
            return new ToastManager(options.AppGuid, options.AppId, () => this);
        }

        private void StartToastThread()
        {
            _thread = new Thread(RunThreadLoop);
            _thread.Name = "Toast";
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void RunThreadLoop()
        {
            try
            {
                foreach (var action in _queue.GetConsumingEnumerable())
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void QueueAction(Action action)
        {
            var tcs = new TaskCompletionSource();
            _queue.Add(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            tcs.Task.GetAwaiter().GetResult();
        }

        void ToastManager.INotificationActivationCallback.Activate(
            string appUserModelId, string invokedArgs, ToastManager.NOTIFICATION_USER_INPUT_DATA[] data, int count)
        {
            _options.ActivatedCallback?.Invoke(invokedArgs);
        }
    }

    public record ToastServiceOptions(
        Guid AppGuid,
        string AppId,
        string DisplayName,
        string IconUrl,
        string Arguments,
        Action<string> ActivatedCallback,
        Dictionary<Type, Func<object, string>> contentFactories
    );

    public class ToastServiceOptionsBuilder
    {
        private Dictionary<Type, Func<object, string>> _contentFactories = new Dictionary<Type, Func<object, string>>();
        private Guid _appGuid;
        private string? _appId = null!;
        private string? _displayName = null!;
        private string? _iconUrl = null!;
        private List<string> _arguments = new();
        private Action<string>? _activatedCallback;

        public ToastServiceOptionsBuilder AddToast<T>(Func<T, string> contentFactory)
        {
            _contentFactories.Add(typeof(T), x => contentFactory((T)x));
            return this;
        }

        public ToastServiceOptionsBuilder ConfigureApp(Guid appGuid, string appId, string displayName, string iconUrl)
        {
            _appGuid = appGuid;
            _appId = appId;
            _displayName = displayName;
            _iconUrl = iconUrl;
            return this;
        }

        public ToastServiceOptionsBuilder AddArgument(string arg)
        {
            if (arg.Contains(" "))
                arg = $"\"{arg}\"";
            _arguments.Add(arg);
            return this;
        }

        public ToastServiceOptionsBuilder SetActivatedCallback(Action<string> callback)
        {
            _activatedCallback = callback;
            return this;
        }

        public ToastServiceOptions Build()
        {
            return new ToastServiceOptions(
                _appGuid,
                _appId!,
                _displayName!,
                _iconUrl!,
                string.Join(" ", _arguments)!,
                _activatedCallback!,
                _contentFactories
               );
        }
    }

    static class ToastServiceExtensions
    {
        public static IServiceCollection AddToasts(this IServiceCollection services, Action<ToastServiceOptionsBuilder> configure)
        {
            var builder = new ToastServiceOptionsBuilder();
            configure(builder);
            services.AddTransient(_ => builder.Build());
            services.AddSingleton<IToastService, ToastService>();
            return services;
        }
    }
}