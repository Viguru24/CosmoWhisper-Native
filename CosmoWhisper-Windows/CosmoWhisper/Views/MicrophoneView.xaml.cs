using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CosmoWhisper.Views
{
    public partial class MicrophoneView : System.Windows.Controls.UserControl
    {
        public MicrophoneView()
        {
            InitializeComponent();
            
            // Wire up internal events to delegate to parent
            this.ComboMics.SelectionChanged += (s, e) => GetParentWindow()?.ComboMics_SelectionChanged(s, e);
            this.SldSensitivity.ValueChanged += (s, e) => GetParentWindow()?.SldSensitivity_ValueChanged(s, e);
        }

        private DashboardWindow? GetParentWindow() => Window.GetWindow(this) as DashboardWindow;

        private void ToggleInteractionSounds_Click(object sender, MouseButtonEventArgs e) => GetParentWindow()?.ToggleInteractionSounds_Click(sender, e);
        private void BtnCalibrate_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.BtnCalibrate_Click(sender, e);
    }
}
