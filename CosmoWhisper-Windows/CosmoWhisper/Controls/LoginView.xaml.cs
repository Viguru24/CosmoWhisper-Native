using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace CosmoWhisper.Controls
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void GoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.GoogleLogin_Click(sender, e);
        }

        private void MagicLink_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.MagicLink_Click(sender, e);
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.SignUp_Click(sender, e);
        }
    }
}
