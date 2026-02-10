using System;
using System.IO;
using System.Text.Json;

namespace CosmoWhisper.Managers
{
    public enum InsertionMethod { FastPaste, DirectTyping }

    public class UserPreferences : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private string _activationKey = "F9";
        public string ActivationKey { get => _activationKey; set { _activationKey = value; OnPropertyChanged(nameof(ActivationKey)); } }

        private uint _virtualKey = 0x78; // F9
        public uint VirtualKey { get => _virtualKey; set { _virtualKey = value; OnPropertyChanged(nameof(VirtualKey)); } }
        public string MouseButton { get; set; } = "None"; // "Left", "Right", "Middle", "XButton1", "XButton2"
        public InsertionMethod InsertionMode { get; set; } = InsertionMethod.FastPaste;
        public bool RestoreClipboard { get; set; } = true;
        public bool AutoSubmit { get; set; } = true;
        public string BackupDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CosmoWhisper", "Backups");
        public string TranscriptionHints { get; set; } = "";

        // Voice Studio: Speed, Pitch & Selection
        public double VoiceSpeed { get; set; } = 1.0;
        public double VoicePitch { get; set; } = 1.0;
        public string SelectedVoice { get; set; } = "alloy";

        // Intelligence: API Keys & AI Models
        public string GroqApiKey { get; set; } = "";
        public string OpenAIApiKey { get; set; } = "";
        public string AnthropicApiKey { get; set; } = "";
        public string XAiApiKey { get; set; } = "";
        public string AIModel { get; set; } = "whisper-large-v3";

        // Setup: Mic ID & Calibration
        public string MicDeviceName { get; set; } = "Default";
        public string MicDeviceId { get; set; } = "";
        public double MicSensitivity { get; set; } = 0.5;

        // Agent Settings
        private bool _enableManusAgent = true;
        public bool EnableManusAgent { get => _enableManusAgent; set { _enableManusAgent = value; OnPropertyChanged(nameof(EnableManusAgent)); } }
        private bool _manusNarrationEnabled = true;
        public bool ManusNarrationEnabled { get => _manusNarrationEnabled; set { _manusNarrationEnabled = value; OnPropertyChanged(nameof(ManusNarrationEnabled)); } }
        public string AIPersonality { get; set; } = "Balanced";

        // Window States
        public double WidgetTop { get; set; } = 100;
        public double WidgetLeft { get; set; } = 100;
        public double DashboardTop { get; set; } = -1; // -1 means center screen
        public double DashboardLeft { get; set; } = -1;
        public bool IsAIUnlocked { get; set; } = false;
        public double WidgetOpacity { get; set; } = 0.95; // Widget transparency (0.0 to 1.0)

        // Language Settings
        public string InterfaceLanguage { get; set; } = "en-GB"; // Default to UK English
        public string SelectedDateFormat { get; set; } = "dd/MM/yyyy";
        public string SelectedTimeFormat { get; set; } = "HH:mm";
        public bool EnableRegionalSpelling { get; set; } = false; // Auto-convert US/UK spelling
        private double _uiScale = 1.0;
        public double UIScale { get => _uiScale; set { _uiScale = value; OnPropertyChanged(nameof(UIScale)); } }

        private bool _autoCopy = false;
        public bool AutoCopy { get => _autoCopy; set { _autoCopy = value; OnPropertyChanged(nameof(AutoCopy)); } }

        // License & Web Control
        public string LicenseToken { get; set; } = "";
        public string AuthToken { get; set; } = "";
        public string BackendUrl { get; set; } = "http://localhost:5000"; // Default dev URL from the old app
        public string UserTier { get; set; } = "free";
        public double UsageMinutes { get; set; } = 0.0;
        public int UsageLimitMinutes { get; set; } = 20; // Corrected 20 minute monthly limit

        // Cumulative Stats
        public long TotalWords { get; set; } = 0;
        public int TotalTranscriptions { get; set; } = 0;
        public double TotalTimeSavedMinutes { get; set; } = 0;

        // Startup Settings
        public bool LaunchOnStartup { get; set; } = false;

        public bool InteractionSoundsEnabled { get; set; } = true;

        // Vocabulary Settings
        public bool EnableSmartEmailCorrections { get; set; } = true; // Auto-format emails, domains, etc.
    }

    public class PreferenceManager : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public static PreferenceManager Shared { get; } = new PreferenceManager();
        
        private UserPreferences _preferences;
        public UserPreferences Preferences 
        { 
            get => _preferences; 
            private set 
            {
                _preferences = value;
                OnPropertyChanged(nameof(Preferences));
            }
        }
        private readonly string _settingsPath;
        private readonly string _appDataFolder;

        public event Action? PreferencesUpdated;

        public PreferenceManager()
        {
            try
            {
                _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
                Directory.CreateDirectory(_appDataFolder);
                _settingsPath = Path.Combine(_appDataFolder, "settings.json");
                Load();
            }
            catch
            {
                Preferences = new UserPreferences();
            }
        }

        public void Load()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsPath);
                    Preferences = JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
                    PreferencesUpdated?.Invoke();
                }
                catch { Preferences = new UserPreferences(); }
            }
            else
            {
                Preferences = new UserPreferences();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Preferences, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                PreferencesUpdated?.Invoke();
            }
            catch { } // logging?
        }

        public void Restore(string backupFolderPath)
        {
            if (!Directory.Exists(backupFolderPath)) return;

            foreach (string file in Directory.GetFiles(backupFolderPath, "*.json"))
            {
                string destFile = Path.Combine(_appDataFolder, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Reload all
            Load();
            VocabularyManager.Shared.Load();
        }
    }
}
