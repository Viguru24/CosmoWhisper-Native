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

        public void Personality_Click(string personality)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.AIPersonality = personality;
            PreferenceManager.Shared.Save();
            UpdatePersonalityUI();
        }

        public void UpdatePersonalityUI()
        {
            if (Window.BtnConcise == null || Window.BtnBalanced == null || Window.BtnDetailed == null) return;
            var p = PreferenceManager.Shared.Preferences;

            Window.BtnConcise.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.AIPersonality == "Concise" ? "#60A060" : "#10FFFFFF"));
            Window.BtnBalanced.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.AIPersonality == "Balanced" ? "#60A060" : "#10FFFFFF"));
            Window.BtnDetailed.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.AIPersonality == "Detailed" ? "#60A060" : "#10FFFFFF"));
        }

        public void ProviderChanged(string tag)
        {
            if (Window.PanelGroqKey != null) Window.PanelGroqKey.Visibility = tag == "Groq" ? Visibility.Visible : Visibility.Collapsed;
            if (Window.PanelOpenAIKey != null) Window.PanelOpenAIKey.Visibility = tag == "OpenAI" ? Visibility.Visible : Visibility.Collapsed;
            if (Window.PanelAnthropicKey != null) Window.PanelAnthropicKey.Visibility = tag == "Anthropic" ? Visibility.Visible : Visibility.Collapsed;

            UpdateGroqStatusUI();
        }

        public void UpdateGroqStatusUI()
        {
            if (Window.TxtGroqWarning == null || Window.TxtGroqSuccess == null) return;
            var p = PreferenceManager.Shared.Preferences;
            bool isUnlocked = p.IsAIUnlocked;

            Window.TxtGroqWarning.Visibility = isUnlocked ? Visibility.Collapsed : Visibility.Visible;
            Window.TxtGroqSuccess.Visibility = isUnlocked ? Visibility.Visible : Visibility.Collapsed;

            if (Window.UnlockPanel != null) Window.UnlockPanel.Visibility = isUnlocked ? Visibility.Collapsed : Visibility.Visible;

            if (Window.TxtGroqApiKey != null) Window.TxtGroqApiKey.IsEnabled = isUnlocked;
            if (Window.TxtOpenAIApiKey_Int != null) Window.TxtOpenAIApiKey_Int.IsEnabled = isUnlocked;
            if (Window.TxtAnthropicApiKey != null) Window.TxtAnthropicApiKey.IsEnabled = isUnlocked;

            if (Window.BtnToggleLock != null)
            {
                Window.BtnToggleLock.Content = isUnlocked ? "🔒 Lock" : "🔓 Unlock";
                Window.BtnToggleLock.Background = isUnlocked
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5A623"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"));
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
    }
}
