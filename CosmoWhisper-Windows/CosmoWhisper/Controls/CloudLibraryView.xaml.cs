using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace CosmoWhisper.Controls
{
    public partial class CloudLibraryView : UserControl
    {
        public CloudLibraryView()
        {
            InitializeComponent();
        }

        private void VisitLibrary_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.VisitLibrary_Click(sender, e);
        }
    }
}
