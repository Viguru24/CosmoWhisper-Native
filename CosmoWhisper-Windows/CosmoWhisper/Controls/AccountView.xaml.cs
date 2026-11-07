using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Button = System.Windows.Controls.Button;

namespace CosmoWhisper.Controls
{
    public partial class AccountView : UserControl
    {
        public AccountView()
        {
            InitializeComponent();
        }

        // Exposing internal elements for the legacy controller
        public TextBlock AccountUserNameControl => AccountUserName;
        public TextBlock TxtAccountEmailControl => TxtAccountEmail;
        public TextBlock AccountTierStatusControl => AccountTierStatus;
        public TextBlock AccountTierIconControl => AccountTierIcon;
        public TextBlock AccountUsageLabelControl => AccountUsageLabel;
        public TextBlock AccountUsageStatsControl => AccountUsageStats;
        public Border AccountUsageProgressBarControl => AccountUsageProgressBar;
        public TextBox TxtLicenseControl => TxtLicense;

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.SignOut_Click(sender, e);
        }
        private void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.RefreshStatus_Click(sender, e);
        }

        private void UpgradeToPro_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.UpgradeToPro_Click(sender, e);
        }

        private void ActivateLicense_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.ActivateLicense_Click(sender, e);
        }

        private void RestartSetup_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.RestartSetup_Click(sender, e);
        }
    }
}
