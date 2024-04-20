using System;

namespace EDLauncher.Utility
{
    static class FormatUtils
    {
        const long KB = 1024;
        const long MB = 1024 * 1024;
        const long GB = 1024 * 1024 * 1024;

        public static string FormatSize(long size)
        {
            double convertedSize;
            string units;

            if (size < KB)
            {
                convertedSize = size;
                units = "B";
            }
            else if (size < MB)
            {
                convertedSize = size / (double)KB;
                units = "KB";
            }
            else if (size < GB)
            {
                convertedSize = size / (double)MB;
                units = "MB";
            }
            else
            {
                convertedSize = size / (double)GB;
                units = "GB";
            }

            return $"{convertedSize:0.##} {units}";
        }

        public static string FormatSpeed(long size)
        {
            return $"{FormatSize(size)}/s";
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
                return "Less than a minute";

            string formatted = string.Format("{0}{1}{2}{3}",
            duration.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", duration.Days, duration.Days == 1 ? string.Empty : "s") : string.Empty,
            duration.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", duration.Hours, duration.Hours == 1 ? string.Empty : "s") : string.Empty,
            duration.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", duration.Minutes, duration.Minutes == 1 ? string.Empty : "s") : string.Empty,
            duration.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", duration.Seconds, duration.Seconds == 1 ? string.Empty : "s") : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);
            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            return formatted;
        }

        public static string FormatDuration2(TimeSpan duration)
        {
            if (duration.TotalMinutes <= 1)
                return "Less than a minute";

            int minutes = (int)Math.Ceiling(duration.Minutes + duration.Seconds / 60.0);

            string formatted = string.Format("{0}{1}{2}",
            duration.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", duration.Days, duration.Days == 1 ? string.Empty : "s") : string.Empty,
            duration.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", duration.Hours, duration.Hours == 1 ? string.Empty : "s") : string.Empty,
            duration.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", minutes, minutes == 1 ? string.Empty : "s") : string.Empty);
            //duration.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", duration.Seconds, duration.Seconds == 1 ? string.Empty : "s") : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);
            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            return formatted;
        }
    }
}
