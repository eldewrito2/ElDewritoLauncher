using EDLauncher.Utility;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace EDLauncher
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains("-ToastActivated"))
            {
                // If activated by a toast notification, we need a working directory, use the process path
                Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory;
            }

            LoadEnvironmentVariables();

            App.InstanceManager = new SingleInstanceManager();
            App.InstanceManager.RunApp(() =>
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            });
        }

        private static void LoadEnvironmentVariables()
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, ".env");

            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath))
            {
                string[] parts = line.Split('=');
                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}
