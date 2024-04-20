using System;
using System.Globalization;
using System.Windows.Data;

namespace EDLauncher.Utility
{
    public class FileSizeToStringConverter : IValueConverter
    {
        public static FileSizeToStringConverter Instance = new FileSizeToStringConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not long size)
            {
                throw new NotImplementedException();
            }

            return FormatUtils.FormatSize(size);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
