namespace TorrentLib
{
    public record SessionSettings
    {
        /// <summary>
        /// A SynchronizationContext to post all events and callbacks.
        /// This is useful for example to ensure all callbacks and events get raised on the UI thread
        /// Without the need for Control.Invoke, Dispatcher.Invoke, etc..
        /// </summary>
        public SynchronizationContext? SynchronizationContext;

        /// <summary>
        /// Controls the polling rate for internal events
        /// </summary>
        public TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Controls how frequently TorrentStatusUpdated events are raised when state changes.
        /// </summary>
        public TimeSpan TorrentUpdateInterval = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// A dictonary of settings to populate the lt::settings_pack pack with
        /// </summary>
        public Dictionary<string, object> LTSettings { get; set; } = new();

        /// <summary>
        /// Requires peers be authenticated in order to download from us
        /// </summary>
        public bool UseAuth { get; set; }

        /// <summary>
        /// Enables logging to the given file path
        /// </summary>
        public bool UseLogging { get; set; }
    }
}
