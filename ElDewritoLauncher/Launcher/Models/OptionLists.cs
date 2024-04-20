using System.Collections.ObjectModel;

namespace EDLauncher.Launcher.Models
{
    public static class OptionLists
    {
        public static ObservableCollection<GenericOption<int>> UpdateCheckIntervals { get; } = new()
        {
            new ("Never", 0 ),
            new ("15 Minutes", 15 ),
            new ("30 Minutes", 30 ),
            new ("1 Hour", 60 ),
            new ("6 Hours", 60*6 ),
            new ("12 Hours", 60*12 ),
            new ("24 Hours", 60*24 ),
        };

        public static ObservableCollection<GenericOption<string>> ReleaseChannels { get; } = new()
        {
            new ("Debug", "debug" ),
            new ("Release", "release" ),
        };
    }

    public record GenericOption<T>(string Label, T Value);
}
