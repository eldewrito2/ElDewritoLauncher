using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Launcher
{
    /// <summary>
    /// Interaction logic for NewsTab.xaml
    /// </summary>
    public partial class NewsTab : UserControl
    {
        private Task<string> _contentPromise;

        public NewsTab()
        {
            InitializeComponent();
            this.Loaded += NewsTab_Loaded;

            _contentPromise = LoadMarkdownAsync();
        }

        private async void NewsTab_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                newsViewer.Markdown = await _contentPromise;
            }
            catch (Exception ex)
            {
                App.ServiceProvider.GetRequiredService<ILogger<NewsTab>>().LogError(ex, "Failed to load news content");
                newsViewer.Markdown = "Failed to load content";
            }
        }

        async Task<string> LoadMarkdownAsync()
        {
            var resourceInfo = Application.GetResourceStream(App.GetResourceUri("Assets\\news.md"));
            using var sr = new StreamReader(resourceInfo.Stream);
            return await sr.ReadToEndAsync();
        }
    }
}
