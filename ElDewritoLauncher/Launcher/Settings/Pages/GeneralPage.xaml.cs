using EDLauncher.Core;
using EDLauncher.Core.Install;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Launcher.Settings.Pages
{
    public partial class GeneralPage : UserControl
    {
        public GeneralPage()
        {
            InitializeComponent();
            this.Loaded += GeneralPage_Loaded;
        }

        private void GeneralPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStartupCheckBox();
        }

        private void btnOpenLog_Click(object sender, RoutedEventArgs e)
        {
            using var _ = Process.Start("explorer.exe", Constants.GetLogDirectory());
        }

        private void startWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                string directory = Environment.CurrentDirectory;
                string shortcutPath = GetStartupShortcutPath();

                if (startupCheckbox.IsChecked == true)
                {
                    ShortcutCreator.CreateShortcut(
                       path: shortcutPath,
                       description: "ElDewrito Launcher",
                       targetPath: Environment.ProcessPath!,
                       arguments: "--startup --tray",
                       iconPath: Environment.ProcessPath!,
                       workingDirectory: directory);
                }
                else
                {
                    File.Delete(shortcutPath);
                }
            }
            catch(Exception ex)
            {
              
                App.ServiceProvider.GetRequiredService<ILogger<GeneralPage>>().LogError(ex, "Failed to apply startup preference");
                MessageBox.Show($"Unable to apply startup preference: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateStartupCheckBox();
            }
        }

        private static string GetStartupShortcutPath()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolderPath, "ElDewrito Launcher.lnk");
        }

        private void UpdateStartupCheckBox()
        {
            startupCheckbox.IsChecked = File.Exists(GetStartupShortcutPath());
        }
    }
}
