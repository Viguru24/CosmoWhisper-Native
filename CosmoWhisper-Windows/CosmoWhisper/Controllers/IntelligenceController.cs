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
        public IntelligenceController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            UpdatePersonalityUI();
            LoadApiKeysUI();
        }

        public void LoadApiKeysUI()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (Window.TxtGroqApiKey != null && !string.IsNullOrEmpty(p.GroqApiKey))
            {
                Window.TxtGroqApiKey.Password = p.GroqApiKey;
            }
            if (Window.TxtOpenAIApiKey != null && !string.IsNullOrEmpty(p.OpenAIApiKey))
            {
                Window.TxtOpenAIApiKey.Password = p.OpenAIApiKey;
            }
        }

        public void SaveApiKeys()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (Window.TxtGroqApiKey != null)
            {
                p.GroqApiKey = Window.TxtGroqApiKey.Password.Trim();
            }
            if (Window.TxtOpenAIApiKey != null)
            {
                p.OpenAIApiKey = Window.TxtOpenAIApiKey.Password.Trim();
            }

            PreferenceManager.Shared.Save();
            Window.ShowToast("API Keys Saved & Encrypted", "\uE72E");
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
                    if (string.Equals(item.Tag?.ToString(), p.AIPersonality, StringComparison.OrdinalIgnoreCase))
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
                    if (string.Equals(item.Tag?.ToString(), p.AIVerbosity, StringComparison.OrdinalIgnoreCase))
                    {
                        Window.ComboVerbosity.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // ProviderChanged Removed - Consolidated to Single API Key

    }
}


