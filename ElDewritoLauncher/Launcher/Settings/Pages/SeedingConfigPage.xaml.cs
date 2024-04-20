using EDLauncher.Utility;
using System;
using System.Collections.Generic;
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

namespace EDLauncher.Launcher.Settings.Pages
{
    /// <summary>
    /// Interaction logic for SeedingConfigPage.xaml
    /// </summary>
    public partial class SeedingConfigPage : UserControl
    {
        public SeedingConfigPage()
        {
            InitializeComponent();
            this.Loaded += SeedingConfigPage_Loaded;
            this.Unloaded += SeedingConfigPage_Unloaded;
        }

        private void SeedingConfigPage_Unloaded(object sender, RoutedEventArgs e)
        {
            App.SaveLauncherSettings();
        }

        private void SeedingConfigPage_Loaded(object sender, RoutedEventArgs e)
        {
            //tbSeedDirectory.Text = Environment.CurrentDirectory;
            UpdateDisplay();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<SeedingPage>(this)?.CloseConfig();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (txtPreviewUploadRateLimit != null)
            {
                if (int.TryParse(tbUpdateRateLimit.Text, out int updateRateLimit))
                {
                    if (updateRateLimit == 0)
                    {
                        txtPreviewUploadRateLimit.Text = "(Unlimited)";
                    }
                    else
                    {
                        txtPreviewUploadRateLimit.Text = $"({FormatUtils.FormatSpeed(updateRateLimit * 1024L)})";
                    }

                }
                else
                    txtPreviewUploadRateLimit.Text = "";
            }
        }

        private void tbUpdateRateLimit_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(tbUpdateRateLimit.Text, out int _))
            {
                tbUpdateRateLimit.Text = "0";
            }
        }
    }
}
