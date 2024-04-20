using EDLauncher.Core;
using EDLauncher.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EDLauncher.Installer.Screens
{
    /// <summary>
    /// Interaction logic for LearnMoreScreen.xaml
    /// </summary>
    public partial class LearnMoreScreen : UserControl
    {
        public LearnMoreScreen()
        {
            InitializeComponent();
            this.Loaded += LearnMoreScreen_Loaded;
            
        }

        private async void LearnMoreScreen_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                md.Markdown = await LoadMarkdownAsync();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Failed to load content");
            }
        }

        async Task<string> LoadMarkdownAsync()
        {
            var resourceInfo = Application.GetResourceStream(App.GetResourceUri("Assets\\installer_info.md"));
            using var sr = new StreamReader(resourceInfo.Stream);
            return await sr.ReadToEndAsync();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<InstallerWindow>(this)?.ShowWelcome();
        }

        private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
        {
            OpenUri((string)e.Parameter);
        }

        private void ClickOnImage(object sender, ExecutedRoutedEventArgs e)
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
            if (uri == "discord")
                uri = Constants.DiscordUrl;
            if (uri == "reddit")
                uri = Constants.RedditUrl;

            if (!IsValidUri(uri))
                return false;
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return true;
        }
    }
}
