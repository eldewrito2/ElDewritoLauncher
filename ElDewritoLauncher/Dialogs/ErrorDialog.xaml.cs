using EDLauncher.Core;
using InstallerLib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace EDLauncher.Dialogs
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Controls.WindowBase
    {
        public ErrorDialog()
        {
            InitializeComponent();
        }

        public void SetErrorMessage(string message)
        {
            txtErrorMessage.Text = message;
        }

        private void btnRelaunch_Click(object sender, RoutedEventArgs e)
        {
            App.Relaunch(Environment.CurrentDirectory);
        }

        private void btnOpenLog_Click(object sender, RoutedEventArgs e)
        {
            SystemUtility.ExecuteProcess($"{System.IO.Path.Combine(Constants.GetLogDirectory(), "launcher.log")}");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
