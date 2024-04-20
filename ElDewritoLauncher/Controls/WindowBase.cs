using EDLauncher.Core;
using InstallerLib.Utility;
using Markdig.Wpf;
using System.Diagnostics;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace EDLauncher.Controls
{
    [TemplatePart(Name = "PART_MinimizeButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_MaximizeButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_WindowTitle", Type = typeof(Border))]
    public class WindowBase : Window
    {
        public WindowBase() : base()
        {
            App.InstanceManager.Activated += InstanceManager_Activated;

            Loaded += (sender, evnt) =>
            {
                var minimizeButton = (Button)Template.FindName("PART_MinimizeButton", this);
                var closeButton = (Button)Template.FindName("PART_CloseButton", this);
                var windowTitle = (Border)Template.FindName("PART_WindowTitle", this);
                minimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
                closeButton.Click += (s, e) => Close();
                windowTitle.MouseDown += WindowTitle_MouseDown;

                // Add command bindings for markdown viewer (hyperlinks etc..)
                CommandBindings.Add(new System.Windows.Input.CommandBinding(Markdig.Wpf.Commands.Hyperlink, OpenHyperlink));
                CommandBindings.Add(new System.Windows.Input.CommandBinding(Markdig.Wpf.Commands.Image, ClickOnImage));

                // Handle other hyper links
                AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(Window_RequestNavigate));
            };
        }

        public object? BackgroundLayer
        {
            get { return (object?)GetValue(BackgroundLayerProperty); }
            set { SetValue(BackgroundLayerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundLayer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundLayerProperty =
            DependencyProperty.Register("BackgroundLayer", typeof(object), typeof(WindowBase), new PropertyMetadata(null));


        private void WindowTitle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_RequestNavigate(object sender, RoutedEventArgs e)
        {
            // TODO: merge with OpenUri
            var args = (RequestNavigateEventArgs)e;
            switch (args.Uri.ToString())
            {
                case "#discord":
                    SystemUtility.ExecuteProcess(Constants.DiscordUrl);
                    break;
                case "#reddit":
                    SystemUtility.ExecuteProcess(Constants.RedditUrl);
                    break;
            }
        }

        private void InstanceManager_Activated()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        public void ShowModalBackdrop()
        {
            var backdrop = (Border)Template.FindName("PART_ModalBackdrop", this);
            backdrop.Visibility = Visibility.Visible;
        }


        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            OpenUri((string)e.Parameter);
        }

        private void ClickOnImage(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {

        }

        public static bool IsValidUri(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? tmp))
                return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

        public static bool OpenUri(string uri)
        {
            switch (uri)
            {
                case "discord":
                    uri = Constants.DiscordUrl;
                    break;
                case "reddit":
                    uri = Constants.RedditUrl;
                    break;
            }

            if (!IsValidUri(uri))
                return false;

            SystemUtility.ExecuteProcess(uri);
            return true;
        }
    }
}
