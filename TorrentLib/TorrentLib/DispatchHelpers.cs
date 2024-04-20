namespace TorrentLib
{
    static class DispatchHelpers
    {
        // Set of helpers extension methods to post callbacks and events to a given SynchronizationContext

        public static void Invoke(this Action action, SynchronizationContext syncContext)
        {
            if (action == null)
                return;

            if (syncContext != null)
                syncContext.Post((state) => action!.Invoke(), null);
            else

                action!.Invoke();
        }

        public static void Invoke(this EventHandler handler, object sender, SynchronizationContext? syncContext)
        {
            if (handler == null)
                return;

            if (syncContext != null)
                syncContext.Post((state) => handler!.Invoke(sender, EventArgs.Empty), null);
            else
                handler.Invoke(sender, EventArgs.Empty);
        }

        public static void Invoke<T>(this EventHandler<T> handler, object sender, T args, SynchronizationContext? syncContext) where T : EventArgs
        {
            if (handler == null)
                return;

            if (syncContext != null)
                syncContext.Post((state) => handler!.Invoke(sender, args), null);
            else
                handler!.Invoke(sender, args);
        }
    }
}
