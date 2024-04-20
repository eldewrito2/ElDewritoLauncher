using System;
using System.Windows;
using System.Windows.Media;

namespace EDLauncher.Utility
{
    internal class VisualTreeHelpers
    {
        public static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            if (current == null)
                return null;

            var parent = VisualTreeHelper.GetParent(current);

            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        internal static object FindAncestor<T>()
        {
            throw new NotImplementedException();
        }
    }
}
