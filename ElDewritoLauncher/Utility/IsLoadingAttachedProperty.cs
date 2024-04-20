using System.Windows;

namespace EDLauncher.Utility
{
    public static class AttachedProperties
    {
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.RegisterAttached(
                "IsBusy",
                typeof(bool),
                typeof(AttachedProperties),
                new PropertyMetadata(false, OnIsBusyChanged)
            );

        public static bool GetIsBusy(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsBusyProperty);
        }

        public static void SetIsBusy(DependencyObject obj, bool value)
        {
            obj.SetValue(IsBusyProperty, value);
        }

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}
