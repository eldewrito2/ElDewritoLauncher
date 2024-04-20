using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EDLauncher.Controls
{
    /// <summary>
    /// Interaction logic for FontIcon.xaml
    /// </summary>
    public partial class FontIcon : UserControl
    {
        public static readonly DependencyProperty SpinProperty =
            DependencyProperty.Register("Spin", typeof(bool), typeof(FontIcon), new PropertyMetadata(false, SpinPropertyChanged));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(Duration), typeof(FontIcon), new PropertyMetadata(default(Duration), DurationPropertyChanged));

        public static readonly DependencyProperty IconProperty =
          DependencyProperty.Register("Icon", typeof(string), typeof(FontIcon), new PropertyMetadata(default(string)));

        private RotateTransform? _rotateTransform;

        public FontIcon()
        {
            InitializeComponent();
            Loaded += FontIcon_Loaded;
            Unloaded += FontIcon_Unloaded;
            IsVisibleChanged += FontIcon_IsVisibleChanged;
        }

        private void FontIcon_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRotation();
        }

        private void FontIcon_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAnimation();
        }

        private void FontIcon_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not FrameworkElement) return;

            bool isVisible = (bool)e.NewValue;
            if (!isVisible)
            {
                StopRotation();
            }
            else
            {
                UpdateAnimation();
            }
        }

        public bool Spin
        {
            get { return (bool)GetValue(SpinProperty); }
            set { SetValue(SpinProperty, value); }
        }

        public Duration Duration
        {
            get { return (Duration)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        private static void SpinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FontIcon fontIcon)
            {
                fontIcon.UpdateAnimation();
            }
        }

        private static void DurationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FontIcon fontIcon)
            {
                fontIcon.UpdateAnimation();
            }
        }

        private void UpdateAnimation()
        {
            if (Spin && Duration != default)
            {
                StartRotation();
            }
            else
            {
                StopRotation();
            }
        }
        
        private void StartRotation()
        {
            if (_rotateTransform != null)
                return;

            _rotateTransform = new RotateTransform();
            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = _rotateTransform;

            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void StopRotation()
        {
            if (_rotateTransform != null)
            {
                _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                RenderTransform = _rotateTransform = null;
            }
        }
    }
}
