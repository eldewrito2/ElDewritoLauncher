using System.Windows.Controls;

namespace EDLauncher.Installer.Screens
{
    public partial class SimpleStatusScreen : UserControl
    {
        public SimpleStatusScreen()
        {
            InitializeComponent();
        }

        public void SetStatus(string status, string secondaryStatus = "")
        {
            txtStatus.Text = status;
            txtStatus2.Text = secondaryStatus;
            txtStatus2.Visibility = string.IsNullOrEmpty(secondaryStatus) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }
    }
}
