using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EDLauncher.Controls
{
    public class TabHeader : RadioButton
    {
        private readonly Storyboard _storyboard = new();

        public TabHeader()
        {
            Opacity = GetTargetOpacity();
        }

        private void UpdateAnimation(double duration)
        {
            DoubleAnimation doubleAnimation = new(GetTargetOpacity(), TimeSpan.FromSeconds(duration));
            doubleAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut };
            _storyboard.Children.Clear();
            _storyboard.Children.Add(doubleAnimation);
            Storyboard.SetTarget(doubleAnimation, this);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
            _storyboard.Begin();
        }

        private double GetTargetOpacity()
        {
            if (IsChecked == true)
                return 1.0f;
            else
                return IsMouseOver ? 1.0f : 0.3f;
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            UpdateAnimation(0.2);

        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            UpdateAnimation(0.2);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateAnimation(0.2);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            UpdateAnimation(0.2);
        }
    }
}
