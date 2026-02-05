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
            _ = CosmoWhisper.CosmoMessage.Show("Coming Soon", "The Python Scripting Engine is currently in development.", "🐍");
        }
    }
}
