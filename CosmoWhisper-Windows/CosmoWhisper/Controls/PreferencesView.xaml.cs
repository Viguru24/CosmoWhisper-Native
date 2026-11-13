using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CosmoWhisper.Controls
{
    public partial class PreferencesView : System.Windows.Controls.UserControl
    {
        public PreferencesView()
        {
            InitializeComponent();
        }

        private DashboardWindow? GetParentWindow() => Window.GetWindow(this) as DashboardWindow;

        // ACTIVATION
        private void Hotkey_Click(object sender, MouseButtonEventArgs e) => GetParentWindow()?.StartActivationCapture_Click(sender, e);
        private void ResetHotkey_Click(object sender, MouseButtonEventArgs e) => GetParentWindow()?.ResetHotkey_Click(sender, e);



        // FLOW & AUTOMATION
        private void ToggleClipboard_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleClipboard_Click(sender, e);
        private void TogglePasteMode_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.TogglePasteMode_Click(sender, e);
        private void ToggleAutoSubmit_Click(object sender, MouseButtonEventArgs e) => GetParentWindow()?.ToggleAutoSubmit_Click(sender, e);
        private void ToggleAutoCopy_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleAutoCopy_Click(sender, e);
        private void ToggleSpeechPolish_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleSpeechPolish_Click(sender, e);
        private void ToggleLocalWhisper_Click(object sender, MouseButtonEventArgs e) => GetParentWindow()?.ToggleLocalWhisper_Click(sender, e);
        private void ForceCloudProxy_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleForceCloudProxy_Click(sender, e);
        private void ForceDirectApi_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleForceDirectApi_Click(sender, e);
        private void LocalVoiceEngine_Changed(object sender, SelectionChangedEventArgs e) => GetParentWindow()?.ComboLocalVoiceEngine_SelectionChanged(sender, e);

        // DATA & RECOVERY
        private void ChangeBackupPath_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.BtnChangeBackupPath_Click(sender, e);
        private void BackupNow_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.BtnBackupNow_Click(sender, e);
        private void Restore_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.BtnRestore_Click(sender, e);

        // REGIONAL & WINDOWS
        private void ToggleRegionalSpelling_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleRegionalSpelling_Click(sender, e);
        private void ToggleLaunchOnStartup_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleLaunchOnStartup_Click(sender, e);

        // FORMATTING
        private void DateFormat_Changed(object sender, SelectionChangedEventArgs e) => GetParentWindow()?.ComboDateFormat_SelectionChanged(sender, e);
        private void TimeFormat_Changed(object sender, SelectionChangedEventArgs e) => GetParentWindow()?.ComboTimeFormat_SelectionChanged(sender, e);

        // VISUALS
        private void WidgetOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded) GetParentWindow()?.SldWidgetOpacity_ValueChanged(sender, e);
        }

        private void UIScale_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded) GetParentWindow()?.SldUIScale_ValueChanged(sender, e);
        }

        private void ToggleAlwaysOnTop_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.ToggleAlwaysOnTop_Click(sender, e);
    }
}
