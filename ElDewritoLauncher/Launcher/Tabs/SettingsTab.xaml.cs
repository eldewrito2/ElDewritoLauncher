using EDLauncher.Launcher.Settings.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EDLauncher.Launcher
{
    public partial class SettingsTab : UserControl
    {
        private UserControl[] _pages;

        public SettingsTab()
        {
            InitializeComponent();

            _pages = new UserControl[]
            {
                new GeneralPage(),
                new UpdatesPage(),
                new LaunchOptionsPage(),
                new ModsPage(),
                new FilesPage(),
                new SeedingPage()
            };
            this.Loaded += SettingsTab_Loaded;
        }

        private void SettingsTab_Loaded(object sender, RoutedEventArgs e)
        {
            ShowPage(_pages[categoryList.SelectedIndex]);
        }

        private void categoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pages == null)
                return;

            if (categoryList.SelectedIndex >= _pages.Length)
            {
                stage.Content = null;
                return;
            }

            ShowPage(_pages[categoryList.SelectedIndex]);
        }

        private void btnCompactToggle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            double targetWidth = btnCompactToggle.IsChecked == true ? 160 : 62;
            if (grid1.ColumnDefinitions.Count == 0)
                return;

            var animation = new DoubleAnimation(targetWidth, TimeSpan.FromSeconds(0.2)) { EasingFunction = new CubicEase() };
            border1.BeginAnimation(Border.WidthProperty, animation);
        }

        private void ShowPage(UserControl page)
        {
            if (stage.Content == page)
            {
                return;
            }

            App.SaveLauncherSettings();

            if (page != null)
            {
                page.IsHitTestVisible = false;
                var transform = new TranslateTransform(0, 0);
                var slideAnim = new DoubleAnimation(30, 0, TimeSpan.FromSeconds(0.25)) { EasingFunction = new QuadraticEase() };
                var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4)) { EasingFunction = new QuadraticEase() };
                slideAnim.Completed += (s, e) => page.IsHitTestVisible = true;
                page.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty, slideAnim);
                page.BeginAnimation(OpacityProperty, fadeAnim);
            }
            stage.Content = page;
        }
    }
}
