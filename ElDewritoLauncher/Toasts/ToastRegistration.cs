namespace ElDewritoLauncher.Toasts
{
    public record UpdateAvailableToast();
    public record UpdateDownloadedToast();

    public static class ToastRegistration
    {
        public static ToastServiceOptionsBuilder RegisterToasts(this ToastServiceOptionsBuilder builder)
        {
            builder.AddToast((UpdateAvailableToast toast) =>
                 @"<toast scenario=""reminder"" activationType=""foreground"" duration=""Short"" launch=""updateNow"">
                    <visual>
                        <binding template=""ToastGeneric"">
                            <text>Update Available</text>
                            <text>A new version of ElDewrito is available!</text>
                        </binding>
                    </visual>
                    <audio src=""ms-winsoundevent:Notification.Default"" loop=""false"" silent=""false"" />
                </toast>"
            );

            builder.AddToast((UpdateDownloadedToast toast) =>
                @"<toast scenario=""reminder"" activationType=""foreground"" duration=""Short"">
                    <visual>
                        <binding template=""ToastGeneric"">
                            <text>Update Downloaded</text>
                            <text>An update for ElDewrito has been downloaded!</text>
                        </binding>
                    </visual>
                    <audio src=""ms-winsoundevent:Notification.Default"" loop=""false"" silent=""false"" />
                </toast>"
            );
            return builder;
        }
    }
}
