using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace CosmoWhisper.Controls
{
    public partial class SmartCommandsView : UserControl
    {
        public SmartCommandsView()
        {
            InitializeComponent();
        }

        private void OpenLibraryWeb_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.OpenLibraryWeb_Click(sender, e);
        }
    }
}
