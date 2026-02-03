using System.Windows.Controls;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Views
{
    public partial class MicrophoneView : System.Windows.Controls.UserControl
    {
        public MicrophoneView()
        {
            InitializeComponent();
            LoadDevices();
            
            SldSensitivity.ValueChanged += (s, e) => {
                TxtSensitivityValue.Text = $"{(int)e.NewValue}%";
                PreferenceManager.Shared.Preferences.MicSensitivity = e.NewValue / 100.0;
                PreferenceManager.Shared.Save();
            };
        }

        private async void LoadDevices()
        {
            var devices = await AudioRecorder.Shared.EnumerateInputDevices();
            ComboMics.ItemsSource = devices;
            ComboMics.DisplayMemberPath = "Name";
            
            // Set selected device from preferences
            // (Simplified for now)
        }
    }
}
