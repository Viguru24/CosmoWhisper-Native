using System.Windows;
using System.Windows.Controls;

namespace CosmoWhisper.Controls
{
    public partial class IntelligenceView : System.Windows.Controls.UserControl
    {
        public IntelligenceView()
        {
            InitializeComponent();

            this.ComboPersonalityControl.SelectionChanged += (s, e) => GetParentWindow()?.ComboPersonality_SelectionChanged(s, e);
            this.ComboVerbosityControl.SelectionChanged += (s, e) => GetParentWindow()?.ComboVerbosity_SelectionChanged(s, e);
        }

        private void BtnSaveApiKeys_Click(object sender, RoutedEventArgs e)
        {
            GetParentWindow()?.BtnSaveApiKeys_Click(sender, e);
        }

        private DashboardWindow? GetParentWindow() => Window.GetWindow(this) as DashboardWindow;
    }
}
