using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using CosmoWhisper.Interfaces;

namespace CosmoWhisper.Managers
{
    public sealed class DpapiStringConverter : JsonConverter<string>
    {
        private const string Prefix = "dpapi:";

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? raw = reader.GetString();
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            if (raw.StartsWith(Prefix, StringComparison.Ordinal))
            {
                try
                {
                    byte[] encryptedBytes = Convert.FromBase64String(raw.Substring(Prefix.Length));
                    byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                    return System.Text.Encoding.UTF8.GetString(plainBytes);
                }
                catch
                {
                    return string.Empty;
                }
            }

            // Backward compatibility: handle legacy unprefixed DPAPI base64 or plaintext strings
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(raw);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return raw;
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(value);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            writer.WriteStringValue(Prefix + Convert.ToBase64String(encryptedBytes));
        }
    }

    public class UserPreferences : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return encryptedText;
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // If decryption fails (e.g., old unencrypted data), return as-is for migration
                return encryptedText;
            }
        }

        private string _activationKey = "F9";
        public string ActivationKey { get => _activationKey; set { _activationKey = value; OnPropertyChanged(nameof(ActivationKey)); } }

        private uint _virtualKey = 0x78; // F9
        public uint VirtualKey { get => _virtualKey; set { _virtualKey = value; OnPropertyChanged(nameof(VirtualKey)); } }
        public string MouseButton { get; set; } = "None"; // "Left", "Right", "Middle", "XButton1", "XButton2"
        public bool RestoreClipboard { get; set; } = true;
        public bool AutoSubmit { get; set; } = false;
        public string BackupDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CosmoWhisper", "Backups");
        public string TranscriptionHints { get; set; } = "";

        // Voice Studio: Speed, Pitch & Selection
        public double VoiceSpeed { get; set; } = 1.0;
        public double VoicePitch { get; set; } = 1.0;
        public double VoiceVolume { get; set; } = 100.0;
        public string SelectedVoice { get; set; } = ""; // Empty means use default (George)

        // Intelligence: API Keys & AI Models
        private string _groqApiKey = "";
        [JsonConverter(typeof(DpapiStringConverter))]
        public string GroqApiKey { get => DecryptString(_groqApiKey); set { _groqApiKey = EncryptString(value); OnPropertyChanged(nameof(GroqApiKey)); } }
        private string _openAIApiKey = "";
        [JsonConverter(typeof(DpapiStringConverter))]
        public string OpenAIApiKey { get => DecryptString(_openAIApiKey); set { _openAIApiKey = EncryptString(value); OnPropertyChanged(nameof(OpenAIApiKey)); } }
        private string _anthropicApiKey = "";
        [JsonConverter(typeof(DpapiStringConverter))]
        public string AnthropicApiKey { get => DecryptString(_anthropicApiKey); set { _anthropicApiKey = EncryptString(value); OnPropertyChanged(nameof(AnthropicApiKey)); } }
        private string _xAiApiKey = "";
        [JsonConverter(typeof(DpapiStringConverter))]
        public string XAiApiKey { get => DecryptString(_xAiApiKey); set { _xAiApiKey = EncryptString(value); OnPropertyChanged(nameof(XAiApiKey)); } }
        public string AIModel { get; set; } = "whisper-large-v3";
        public bool EnableSpeechPolish { get; set; } = true; // AI Speech Polish (Removes "um/uh", stutters, false starts)
        public bool UseLocalWhisper { get; set; } = false; // Enable local Whisper fallback (Disabled by default)
        public bool UseLocalWhisperOnly { get; set; } = false; // Force local usage only
        public string LocalVoiceEngine { get; set; } = "ParakeetRedux"; // "ParakeetRedux" or "WhisperTiny"
        public bool ForceCloudProxy { get; set; } = false; // Force Cloud Proxy (VPN Mode)
        private bool _forceDirectApi = false;
        public bool ForceDirectApi { get => _forceDirectApi; set { _forceDirectApi = value; OnPropertyChanged(nameof(ForceDirectApi)); } }
        public int MaxOutputTokens { get; set; } = 4096; // Max tokens for AI response

        // Setup: Mic ID & Calibration
        public string MicDeviceName { get; set; } = "Default";
        public string MicDeviceId { get; set; } = "";
        public double MicSensitivity { get; set; } = 0.5;

        // Output Device Settings
        public string OutputDeviceName { get; set; } = "Default";
        public int OutputDeviceIndex { get; set; } = -1; // -1 = default

        // Agent Settings
        public string AIPersonality { get; set; } = "None";
        public string AIVerbosity { get; set; } = "Balanced";

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
        private string _licenseToken = "";
        [JsonConverter(typeof(DpapiStringConverter))]
        public string LicenseToken { get => DecryptString(_licenseToken); set { _licenseToken = EncryptString(value); OnPropertyChanged(nameof(LicenseToken)); } }
        private string _authToken = "";
        [JsonConverter(typeof(DpapiStringConverter))]
        public string AuthToken { get => DecryptString(_authToken); set { _authToken = EncryptString(value); OnPropertyChanged(nameof(AuthToken)); } }
        public string BackendUrl { get; set; } = "https://api.cosmowhisper.com"; // Production Only
        public string UserEmail { get; set; } = "";
        public string UserTier { get; set; } = "free";
        public double UsageMinutes { get; set; } = 0.0;
        public int UsageLimitMinutes { get; set; } = 60;
        public int LastLocalResetMonth { get; set; } = DateTime.Now.Month;

        private bool? _isStoreVersion;
        public bool IsStoreVersion
        {
            get
            {
                if (_isStoreVersion.HasValue) return _isStoreVersion.Value;
                try
                {
                    // If we can access the package ID, we are a packaged app
                    var package = Windows.ApplicationModel.Package.Current;
                    // The specific Store Package Name we used in our manifest
                    _isStoreVersion = package.Id.Name.Contains("MicroMeadow.CosmoWhisper", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    _isStoreVersion = false;
                }
                return _isStoreVersion.Value;
            }
        }

        // Cumulative Stats
        public long TotalWords { get; set; } = 0;
        public int TotalTranscriptions { get; set; } = 0;
        public double TotalTimeSavedMinutes { get; set; } = 0;

        // Startup Settings
        public bool LaunchOnStartup { get; set; } = false;

        public bool InteractionSoundsEnabled { get; set; } = true;

        // Vocabulary Settings
        public bool EnableSmartEmailCorrections { get; set; } = true; // Auto-format emails, domains, etc.

        private bool _hasCompletedOnboarding = false;
        public bool HasCompletedOnboarding { get => _hasCompletedOnboarding; set { _hasCompletedOnboarding = value; OnPropertyChanged(nameof(HasCompletedOnboarding)); } }

        // Sign-Off / Signature Settings
        public string SignOffSignature { get; set; } = "";
        public bool EnableSignOff { get; set; } = false;

        private bool _usePasteMode = true;
        public bool UsePasteMode { get => _usePasteMode; set { _usePasteMode = value; OnPropertyChanged(nameof(UsePasteMode)); } }

        private bool _alwaysOnTop = true;
        public bool AlwaysOnTop { get => _alwaysOnTop; set { _alwaysOnTop = value; OnPropertyChanged(nameof(AlwaysOnTop)); } }
    }

    public class PreferenceManager : IPreferenceManager, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public static PreferenceManager Shared => (PreferenceManager)ServiceLocator.GetService<IPreferenceManager>();
        
        public bool IsDevMode { get; private set; }
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
        public string AppDataFolder => _appDataFolder;
        private readonly string _appDataFolder;

        public event Action? PreferencesUpdated;

        public static bool IsDevelopmentMode()
        {
#if DEV_BUILD
            return true;
#else
            return false;
#endif
        }

        public PreferenceManager()
        {
            try
            {
                string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                bool isDev = Environment.GetCommandLineArgs().Contains("--dev", StringComparer.OrdinalIgnoreCase);
#if DEBUG
                isDev = true;
#endif
                IsDevMode = isDev;

                if (isDev)
                {
                    _appDataFolder = Path.Combine(appDataRoot, "CosmoWhisper_Dev");
                }
                else
                {
                    _appDataFolder = Path.Combine(appDataRoot, "CosmoWhisper_Personal");
                }

                if (!Directory.Exists(_appDataFolder))
                {
                    Directory.CreateDirectory(_appDataFolder);
                }
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

                    // If restored backup directory doesn't exist (e.g. from another machine), reset to default
                    if (string.IsNullOrEmpty(Preferences.BackupDirectory) || !Directory.Exists(Preferences.BackupDirectory))
                    {
                        Preferences.BackupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CosmoWhisper", "Backups");
                    }
                    
                    // Default Backend URLs
                    // If stuck on localhost in a non-dev scenario, reset to prod; keep localhost when IsDevMode is active
                    if (string.IsNullOrEmpty(Preferences.BackendUrl) || (!IsDevMode && Preferences.BackendUrl.Contains("localhost")))
                    {
                        Preferences.BackendUrl = "https://api.cosmowhisper.com";
                        Save();
                    }

                    // Upgrade legacy plaintext secrets on disk to DPAPI-protected values
                    bool hasSecrets = !string.IsNullOrEmpty(Preferences.AuthToken) ||
                                      !string.IsNullOrEmpty(Preferences.LicenseToken) ||
                                      !string.IsNullOrEmpty(Preferences.GroqApiKey) ||
                                      !string.IsNullOrEmpty(Preferences.OpenAIApiKey) ||
                                      !string.IsNullOrEmpty(Preferences.AnthropicApiKey) ||
                                      !string.IsNullOrEmpty(Preferences.XAiApiKey);
                    if (hasSecrets && !json.Contains("\"dpapi:", StringComparison.Ordinal))
                    {
                        Save();
                    }


                    // Migration: Ensure everyone is on the 60-minute limit
                    if (Preferences.UsageLimitMinutes < 60)
                    {
                        Preferences.UsageLimitMinutes = 60;
                        Save();
                    }

                    PreferencesUpdated?.Invoke();
                }
                catch { Preferences = new UserPreferences(); }
            }
            else
            {
                Preferences = new UserPreferences();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                string json = JsonSerializer.Serialize(Preferences, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsPath, json);
                PreferencesUpdated?.Invoke();
            }
            catch { } // logging?
        }

        /// <summary>
        /// Synchronously saves the current preferences to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Preferences, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                PreferencesUpdated?.Invoke();
            }
            catch { }
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


