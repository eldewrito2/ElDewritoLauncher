using System;
using System.Collections.Generic;
using System.Text;

namespace EDLauncher.Core.Utility
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int length, bool ellipsis)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= length)
                return value;

            if (ellipsis)
                return value.Substring(0, length) + "...";
            else
                return value.Substring(0, length);
        }
    }
}
