using EDLauncher.Utility;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDLauncher.Launcher.Settings.Pages
{
    /// <summary>
    /// Interaction logic for SeedingInfoPage.xaml
    /// </summary>
    public partial class SeedingInfoPage : UserControl
    {
        public SeedingInfoPage()
        {
            InitializeComponent();
            this.Loaded += SeedingInfoPage_Loaded;
        }

        private async void SeedingInfoPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                newsViewer.Markdown = await LoadMarkdownAsync();
                
            }
            catch(Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<SeedingInfoPage>>().LogError(ex, "Failed to load news content");
                newsViewer.Markdown = "Failed to load content";
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<SeedingPage>(this)?.CloseInfo();
        }

        async Task<string> LoadMarkdownAsync()
        {
            var resourceInfo = Application.GetResourceStream(App.GetResourceUri("Assets\\seed_info.md"));
            using var sr = new StreamReader(resourceInfo.Stream);
            return await sr.ReadToEndAsync();
        }
    }
}
