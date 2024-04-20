using System;
using System.Globalization;
using System.Windows.Data;

namespace EDLauncher.Utility
{
    public class DefaultLabelConverter : IValueConverter
    {
        public static DefaultLabelConverter Instance = new DefaultLabelConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If we have no value, use a fallback of 'Custom' which is useful for combo boxes
            if (value is string s && string.IsNullOrEmpty(s))
            {
                return "Custom";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
