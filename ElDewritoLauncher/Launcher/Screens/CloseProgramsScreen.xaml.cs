using EDLauncher.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EDLauncher.Launcher.Screens
{
    public partial class CloseProgramsScreen : UserControl
    {
        private List<Process> _processList = new();
        private DispatcherTimer _checkTimer;

        public CloseProgramsScreen()
        {
            InitializeComponent();

            _checkTimer = new DispatcherTimer();
            _checkTimer.Interval = TimeSpan.FromMilliseconds(100);
            _checkTimer.Tick += _checkTimer_Tick;

            Loaded += FilesInuseScreen_Loaded;
            Unloaded += FilesInuseScreen_Unloaded;
        }


        private void FilesInuseScreen_Loaded(object sender, RoutedEventArgs e)
        {
            _checkTimer.Start();
        }

        private void FilesInuseScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            _checkTimer.Stop();
        }

        private void _checkTimer_Tick(object? sender, EventArgs e)
        {
            UpdateProcessList();
        }

        private void UpdateProcessList()
        {
            SetProcessList(_processList);
        }

        public void SetProcessList(List<Process> processList)
        {
            _processList = processList.Where(x => !x.HasExited).ToList();
            lbProcess.ItemsSource = new ObservableCollection<string>(_processList.Select(GetFriendlyProcessName));

            if (_processList.Count == 0)
            {
                DoContinue();
            }
        }


        private static string GetFriendlyProcessName(Process process)
        {
            try
            {
                return $"{AddEllipsis(process.MainWindowTitle, 70)} ({process.ProcessName})";
            }
            catch (SystemException)
            {

            }

            return "Unknown";
        }

        static string AddEllipsis(string input, int maxLength)
        {
            return input.Length <= maxLength ? input : (input.Substring(0, maxLength - 3) + "...");
        }

        private async void btnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            btnCloseAll.SetValue(AttachedProperties.IsBusyProperty, true);
            await Task.Run(() =>
            {
                foreach (var process in _processList)
                    process.Kill();
            });

            UpdateProcessList();
            btnCloseAll.SetValue(AttachedProperties.IsBusyProperty, false);
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            DoContinue();
        }

        private void DoContinue()
        {
            _checkTimer.Stop();
            App.UpdateDownloader.StartDownload();
        }
    }
}
