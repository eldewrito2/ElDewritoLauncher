using EDLauncher.Controls;
using EDLauncher.Launcher.Models;
using EDLauncher.Launcher.Screens;
using EDLauncher.Launcher.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace EDLauncher.Launcher
{
    /// <summary>
    /// Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : WindowBase
    {
        private PlayTab playTab;
        private NewsTab newsTab;
        private SettingsTab settingsTab;

        public LauncherWindow()
        {
            playTab = new PlayTab();
            newsTab = new NewsTab();
            settingsTab = new SettingsTab();

            InitializeComponent();
            Loaded += LauncherWindow_Loaded;
            Closing += LauncherWindow_Closing;
            Closed += LauncherWindow_Closed;

            App.LauncherState.PropertyChanged += LauncherState_PropertyChanged;
        }

        private void LauncherWindow_Loaded(object sender, RoutedEventArgs e)
        {
            App.LauncherState.CurrentTab = Tabs.Play;
            UpdateCurrentTab();
        }

        private void LauncherWindow_Closed(object? sender, EventArgs e)
        {
        }

        private void LauncherWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.LauncherSettings.MinimizeToTray)
            {
                e.Cancel = true;
                App.LauncherState.IsMinimizedToTray = true;
                Hide();
            }
        }

        private void LauncherState_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LauncherState.IsUpdateAvailable))
            {
                if (App.LauncherState.IsUpdateAvailable)
                {
                    if (App.LauncherState.UpdateCheckInitiator == UpdateCheckInitiator.User)
                    {
                        ShowUpdateDialog(true);
                    }
                }
            }

            if(e.PropertyName == nameof(LauncherState.CurrentTab))
            {
                UpdateCurrentTab();
            }

            if (e.PropertyName == nameof(LauncherState.IsMinimizedToTray))
            {
                if (App.LauncherState.IsMinimizedToTray)
                {
                    Hide();
                }
                else
                {
                    WindowState = WindowState.Normal;
                    Show();
                    Activate();
                }

            }
        }

        private void UpdateCurrentTab()
        {
            switch (App.LauncherState.CurrentTab)
            {
                case Tabs.Play:
                    playTabHeader.IsChecked = true;
                    ShowTab(playTab);
                    break;
                case Tabs.News:
                    newsTabHeader.IsChecked = true;
                    ShowTab(newsTab);
                    break;
                case Tabs.Settings:
                    settingsTabHeader.IsChecked = true;
                    ShowTab(settingsTab);
                    break;
            }
        }

        private void playTabHeader_Checked(object sender, RoutedEventArgs e)
        {
            if (App.LauncherState.CurrentTab == Tabs.Settings)
            {
                App.SaveLauncherSettings();
            }

            if (sender == playTabHeader)
            {
                App.LauncherState.CurrentTab = Tabs.Play;
            }
            else if (sender == newsTabHeader)
            {
                App.LauncherState.CurrentTab = Tabs.News;
            }
            else if (sender == settingsTabHeader)
            {
                App.LauncherState.CurrentTab = Tabs.Settings;
            }
        }

        public void ShowUpdateDialog(bool showAvailable)
        {
            if (updateDlg.Visibility == Visibility.Collapsed)
            {
                if (showAvailable)
                {
                    updateDlg.ShowUpdateAvailable();
                }
                updateDlg.Visibility = Visibility.Visible;
                updateDlg.Show();
            }
        }

        public void HideUpdateDialog()
        {
            updateDlg.Visibility = Visibility.Collapsed;
            updateDlg.Hide();
        }

        public void ShowSettings()
        {
            App.LauncherState.CurrentTab = Tabs.Settings;
        }

        private void ShowTab(UserControl screen)
        {
            if(tabContent.Content == screen)
            {
                return;
            }

            screen.IsHitTestVisible = false;
            var transform = new TranslateTransform(0, 0);
            var slideAnim = new DoubleAnimation(100, 0, TimeSpan.FromSeconds(0.25)) { EasingFunction = new QuadraticEase() };
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5)) { EasingFunction = new QuadraticEase() };
            slideAnim.Completed += (s, e) => screen.IsHitTestVisible = true;
            screen.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, slideAnim);
            screen.BeginAnimation(OpacityProperty, fadeAnim);
            tabContent.Content = screen;
        }
    }
}
