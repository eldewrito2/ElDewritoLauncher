using EDLauncher.Core.Install;
using EDLauncher.Utility;
using InstallerLib.Utility;
using System.Windows;
using System.Windows.Controls;

namespace EDLauncher.Installer.Screens
{
    public partial class WelcomeScreen : UserControl
    {
        private IWizard _wizard = null!;

        public WelcomeScreen()
        {
            InitializeComponent();
        }

        public void SetWizard(IWizard wizard)
        {
            _wizard = wizard;
            //chkAddFirewallRules.IsChecked = _wizard.Options.AddFirewallRule;
        }

        private void btnNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //_wizard.Options.AddFirewallRule = chkAddFirewallRules.IsChecked == true;
            _wizard.GotoNextStep();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _wizard.Cancel();
        }

        private void learnMore_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            VisualTreeHelpers.FindAncestor<InstallerWindow>(this)?.ShowLearnMore();
        }
    }
}
