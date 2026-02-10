using System;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using CosmoWhisper.Managers;
using CosmoWhisper;

namespace CosmoWhisper.Controllers
{
    public class IntelligenceController : BaseViewController
    {
        private const string UNLOCK_CODE = "10810";

        public IntelligenceController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            UpdateGroqStatusUI();
            UpdatePersonalityUI();
        }

        public void PersonalityChanged(string personality)
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.AIPersonality != personality)
            {
                p.AIPersonality = personality;
                PreferenceManager.Shared.Save();
                // UI update not strictly needed if triggered by UI change, but good for consistency
            }
        }

        public void VerbosityChanged(string verbosity)
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.AIVerbosity != verbosity)
            {
                p.AIVerbosity = verbosity;
                PreferenceManager.Shared.Save();
            }
        }

        public void UpdatePersonalityUI()
        {
            var p = PreferenceManager.Shared.Preferences;

            if (Window.ComboPersonality != null)
            {
                foreach (ComboBoxItem item in Window.ComboPersonality.Items)
                {
                    if (item.Tag?.ToString() == p.AIPersonality)
                    {
                        Window.ComboPersonality.SelectedItem = item;
                        break;
                    }
                }
            }

            if (Window.ComboVerbosity != null)
            {
                foreach (ComboBoxItem item in Window.ComboVerbosity.Items)
                {
                    if (item.Tag?.ToString() == p.AIVerbosity)
                    {
                        Window.ComboVerbosity.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // ProviderChanged Removed - Consolidated to Single API Key

        public void UpdateGroqStatusUI()
        {
            if (Window.TxtGroqWarning == null || Window.TxtGroqSuccess == null) return;
            var p = PreferenceManager.Shared.Preferences;
            bool isUnlocked = p.IsAIUnlocked;

            Window.TxtGroqWarning.Visibility = isUnlocked ? Visibility.Collapsed : Visibility.Visible;
            Window.TxtGroqSuccess.Visibility = isUnlocked ? Visibility.Visible : Visibility.Collapsed;

            if (Window.UnlockPanel != null) Window.UnlockPanel.Visibility = isUnlocked ? Visibility.Collapsed : Visibility.Visible;

            // Updated for new generic API Key field
            if (Window.TxtGroqApiKey != null) Window.TxtGroqApiKey.IsEnabled = isUnlocked;
            
            // Removed other provider keys
            
            if (Window.BtnToggleLock != null)
            {
                Window.BtnToggleLock.Content = isUnlocked ? "🔒 Lock" : "🔓 Unlock";
                Window.BtnToggleLock.Background = isUnlocked
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5A623"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"));
            }

            // Show/hide toggle panel when unlocked
            if (Window.ToggleAccessPanel != null)
            {
                Window.ToggleAccessPanel.Visibility = isUnlocked ? Visibility.Visible : Visibility.Collapsed;
            }

            // Update toggle switch position
            if (Window.TogglePremiumAccess != null)
            {
                var ellipse = Window.TogglePremiumAccess.Child as System.Windows.Shapes.Ellipse;
                if (ellipse != null)
                {
                    ellipse.HorizontalAlignment = isUnlocked ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
                }
                Window.TogglePremiumAccess.Background = isUnlocked
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));
            }
        }

        public void HandleUnlockCode(string code)
        {
            if (code == UNLOCK_CODE && !PreferenceManager.Shared.Preferences.IsAIUnlocked)
            {
                UnlockApiKey();
            }
        }

        public void ToggleLock()
        {
            if (PreferenceManager.Shared.Preferences.IsAIUnlocked)
            {
                LockApiKey();
            }
            else
            {
                if (Window.TxtUnlockCode.Text == UNLOCK_CODE)
                {
                    UnlockApiKey();
                }
                else
                {
                    _ = CosmoMessage.Show("Access Denied", "Incorrect unlock code. Please verify your credentials.", "🔒");
                }
            }
        }

        public void UnlockApiKey()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.IsAIUnlocked = true;
            PreferenceManager.Shared.Save();

            UpdateGroqStatusUI();
            _ = CosmoMessage.Show("Unlocked", "Premium Intelligence Features are now available.", "🔓");
        }

        public void LockApiKey()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.IsAIUnlocked = false;
            p.GroqApiKey = "";
            p.OpenAIApiKey = "";
            p.AnthropicApiKey = "";
            PreferenceManager.Shared.Save();

            if (Window.TxtGroqApiKey != null) Window.TxtGroqApiKey.Clear();
            if (Window.TxtOpenAIApiKey_Int != null) Window.TxtOpenAIApiKey_Int.Clear();
            if (Window.TxtAnthropicApiKey != null) Window.TxtAnthropicApiKey.Clear();

            UpdateGroqStatusUI();
            if (Window.TxtUnlockCode != null) Window.TxtUnlockCode.Clear();
        }

        public void TogglePremiumAccess()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.IsAIUnlocked = !p.IsAIUnlocked;
            PreferenceManager.Shared.Save();
            UpdateGroqStatusUI();
        }
    }
}
