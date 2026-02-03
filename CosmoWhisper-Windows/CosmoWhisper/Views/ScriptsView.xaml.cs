using System.Windows;
using System.Windows.Controls;

namespace CosmoWhisper.Views
{
    public partial class ScriptsView : System.Windows.Controls.UserControl
    {
        public ScriptsView()
        {
            InitializeComponent();
        }

        private void NewScript_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Script editor coming soon!", "CosmoWhisper");
        }
    }
}
