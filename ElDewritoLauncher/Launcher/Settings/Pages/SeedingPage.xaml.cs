using EDLauncher.Utility;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EDLauncher.Launcher.Settings.Pages
{
    public partial class SeedingPage : UserControl
    {
        private SeedingStatusPage statusPage;
        private SeedingConfigPage configPage;

        public SeedingPage()
        {
            InitializeComponent();

            statusPage = new SeedingStatusPage();
            configPage = new SeedingConfigPage();
            this.Loaded += SeedingPage_Loaded;
        }

        private void SeedingPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowPage(statusPage);
        }

        private void btnConfigure_Click(object sender, System.Windows.RoutedEventArgs e)
        {

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

        internal void ShowConfig()
        {
            ShowPage(configPage);
        }

        internal void CloseConfig()
        {
            ShowPage(statusPage);
        }

        internal void ShowInfo()
        {
            var infoPage = new SeedingInfoPage();
            ShowPage(infoPage);
        }

        internal void CloseInfo()
        {
            ShowPage(statusPage);
        }
    }
}
