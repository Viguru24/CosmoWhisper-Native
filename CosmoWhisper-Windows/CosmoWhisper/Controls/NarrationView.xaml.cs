using System.Windows;
using System.Windows.Controls;

namespace CosmoWhisper.Controls
{
    public partial class NarrationView : System.Windows.Controls.UserControl
    {
        public NarrationView()
        {
            InitializeComponent();
        }

        private DashboardWindow? GetParentWindow() => Window.GetWindow(this) as DashboardWindow;

        private void BtnPlaySample_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.BtnPlaySample_Click(sender, e);
    }
}
