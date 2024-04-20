using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace EDLauncher.Launcher.Services
{
    public class LauncherTrayIcon : IDisposable
    {
        private NotifyIcon? _notifyIcon;

        public void Show()
        {
            if (_notifyIcon == null)
            {
                _notifyIcon = new NotifyIcon();
                _notifyIcon.Text = "ElDewrito Launcher";
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
                _notifyIcon.MouseClick += OnMouseClick;
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip();

                var checkForUpdateItem = new ToolStripMenuItem("Check for updates");
                checkForUpdateItem.Click += CheckForUpdateItem_Click;

                var settingsItem = new ToolStripMenuItem("Settings");
                settingsItem.Click += SettingsItem_Click;

                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += ExitItem_Click; ;

                _notifyIcon.ContextMenuStrip.Items.Add(checkForUpdateItem);
                _notifyIcon.ContextMenuStrip.Items.Add(settingsItem);
                _notifyIcon.ContextMenuStrip.Items.Add("-");
                _notifyIcon.ContextMenuStrip.Items.Add(exitItem);
            }
            _notifyIcon.Visible = true;
        }

        private void ExitItem_Click(object? sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void SettingsItem_Click(object? sender, EventArgs e)
        {
            ActiveWindow();
            App.LauncherWindow.ShowSettings();
        }


        private void CheckForUpdateItem_Click(object? sender, EventArgs e)
        {
            App.UpdateChecker.StartCheck(Models.UpdateCheckInitiator.User);
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                ActiveWindow();
            }
        }

        private static void ActiveWindow()
        {
            App.LauncherState.IsMinimizedToTray = false;
            App.LauncherState.RaisePropertyChanged(nameof(App.LauncherState.IsMinimizedToTray));
        }

        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
