using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EDLauncher.Utility
{
    public class NumericTextBoxBehavior
    {
        public static bool GetIsNumericOnly(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNumericOnlyProperty);
        }

        public static void SetIsNumericOnly(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNumericOnlyProperty, value);
        }

        public static readonly DependencyProperty IsNumericOnlyProperty =
            DependencyProperty.RegisterAttached("IsNumericOnly", typeof(bool), typeof(NumericTextBoxBehavior), new PropertyMetadata(false, OnIsNumericOnlyChanged));

        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                    textBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;
                }
                else
                {
                    textBox.PreviewTextInput -= NumericTextBox_PreviewTextInput;
                    textBox.PreviewKeyDown -= NumericTextBox_PreviewKeyDown;
                }
            }
        }

        private static void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private static void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsAllowedKey(e))
            {
                e.Handled = true;
            }
        }

        private static bool IsAllowedKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A when Keyboard.Modifiers == ModifierKeys.Control:
                case Key.X when Keyboard.Modifiers == ModifierKeys.Control:
                case Key.C when Keyboard.Modifiers == ModifierKeys.Control:
                case Key.V when Keyboard.Modifiers == ModifierKeys.Control:
                case Key.Tab:
                case Key.Back:
                    return true;
            }

            if (IsNavigationKey(e.Key))
            {
                return true;
            }

            return IsNumericKey(e.Key);
        }

        private static bool IsNumericKey(Key key)
        {
            return (key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9);
        }

        private static bool IsNavigationKey(Key key)
        {
            return key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down ||
                   key == Key.Home || key == Key.End || key == Key.PageUp || key == Key.PageDown;
        }
    }
}
