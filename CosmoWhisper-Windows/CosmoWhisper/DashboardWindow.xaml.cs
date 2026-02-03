using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using CosmoWhisper.Managers;

using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace CosmoWhisper
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            
            // Setup Global Exception Handlers for Crash Debugging
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                LogCrash($"Unhandled Domain Exception: {e.ExceptionObject}");
                System.Windows.MessageBox.Show($"CRITICAL ERROR: {e.ExceptionObject}", "Crash Detected");
            };
            Dispatcher.UnhandledException += (s, e) => {
                LogCrash($"Unhandled Dispatcher Exception: {e.Exception.Message}\n{e.Exception.StackTrace}");
                System.Windows.MessageBox.Show($"UI THREAD CRASH: {e.Exception.Message}", "Crash Detected");
                e.Handled = true; // Try to prevent app closing
            };

            try { InitializeVoices(); } catch (Exception ex) { LogCrash($"InitVoices Error: {ex.Message}"); }
            try { InitializeMicrophones(); } catch (Exception ex) { LogCrash($"InitMics Error: {ex.Message}"); }
            try { InitializeVoices(); } catch (Exception ex) { LogCrash($"InitVoices Error: {ex.Message}"); }
            try { InitializeMicrophones(); } catch (Exception ex) { LogCrash($"InitMics Error: {ex.Message}"); }
            // Moved to Window_Loaded to ensure UI is ready
            // try { InitializePreferences(); } catch (Exception ex) { LogCrash($"InitPrefs Error: {ex.Message}"); }
            
            // Subscribe to Audio Events
            AudioRecorder.Shared.AudioLevelChanged += OnAudioLevelChanged;

            // Init UI values
            if (SldSensitivity != null) SldSensitivity.Value = AudioRecorder.Shared.Sensitivity * 100;
            try { UpdateInteractionSoundsUI(); } catch (Exception ex) { LogCrash($"UpdateSoundsUI Error: {ex.Message}"); }
            
            // Ensure default state is Productivity Dashboard
            try { ShowDashboard(); } catch (Exception ex) { LogCrash($"ShowDashboard Init Error: {ex.Message}"); }

            this.LocationChanged += (s, e) => SavePosition();
            this.Closing += (s, e) => SavePosition();
            LoadPosition();
        }

        private void LogCrash(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CosmoWhisper_CrashLog.txt");
                File.AppendAllText(path, $"{DateTime.Now}: {message}\n----------------\n");
            }
            catch { }
        }

        private void LoadPosition()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.DashboardTop != -1)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = p.DashboardTop;
                this.Left = p.DashboardLeft;
            }
        }

        private void SavePosition()
        {
            if (this.WindowState == WindowState.Minimized) return;

            var p = PreferenceManager.Shared.Preferences;
            p.DashboardTop = this.Top;
            p.DashboardLeft = this.Left;
            PreferenceManager.Shared.Save();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AudioRecorder.Shared.StopMonitoring();
            this.Hide();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
             AudioRecorder.Shared.StopMonitoring();
             System.Windows.Application.Current.Shutdown();
             AudioRecorder.Shared.StopMonitoring();
             System.Windows.Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
             try 
             { 
                BlurManager.ApplyMica(this);
                InitializePreferences(); 
                CheckAuthStatus();
                AudioRecorder.Shared.StartMonitoring();
                
                // Initialize API key field as locked
                if (TxtGroqApiKey != null) TxtGroqApiKey.IsEnabled = false;
             } 
             catch (Exception ex) 
             { 
                LogCrash($"InitPrefs Loaded Error: {ex.Message}"); 
             }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void SmartCommands_Click(object sender, RoutedEventArgs e)
        {
            ShowSmartCommands();
        }

        private void Vault_Click(object sender, RoutedEventArgs e)
        {
            OpenVault();
        }

        private void ResetStats_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to reset all performance stats? This cannot be undone.", "Reset Stats", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var p = PreferenceManager.Shared.Preferences;
                p.TotalWords = 0;
                p.TotalTranscriptions = 0;
                p.TotalTimeSavedMinutes = 0;
                PreferenceManager.Shared.Save();
                UpdateDashboardStats();
            }
        }
    
        private void OpenVault()
        {
            // Try to open the local dev server first, or a fallback URL
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:8338",
                UseShellExecute = true
            });
        }

        private void Microphone_Click(object sender, RoutedEventArgs e)
        {
            ShowMicrophone();
        }

        private void Narration_Click(object sender, RoutedEventArgs e)
        {
            ShowNarration();
        }

        private void Library_Click(object sender, RoutedEventArgs e)
        {
            ShowLibrary();
        }

        private void Account_Click(object sender, RoutedEventArgs e)
        {
            ShowAccount();
        }



        // --- View Switching Logic ---

        private void ShowDashboard()
        {
            AudioRecorder.Shared.StartMonitoring();
            SwitchToView(DashboardHeader, DashboardView, BtnDashboard);
        }

        private void ShowSmartCommands()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(SmartCommandsHeader, SmartCommandsView, BtnSmartCommands);
        }

        private void ShowMicrophone()
        {
            AudioRecorder.Shared.StartMonitoring();
            SwitchToView(MicrophoneHeader, MicrophoneView, BtnMicrophone);
        }

        private void ShowNarration()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(NarrationHeader, NarrationView, BtnNarration);
        }

        private void ShowLibrary()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(null, LibraryView, BtnLibrary); // Library has no header text stack
        }

        private void ShowAccount()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(AccountHeader, AccountView, BtnAccount);
        }

        private void ShowLogin()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(LoginHeader, LoginView, null);
        }

        private async void CheckAuthStatus()
        {
             UpdateDashboardStats();
             if (!string.IsNullOrEmpty(PreferenceManager.Shared.Preferences.AuthToken))
             {
                 await BackendService.Shared.SyncStatus();
                 UpdateDashboardStats();
             }
        }

        private void UpdateDashboardStats()
        {
            Dispatcher.Invoke(() => {
                var p = PreferenceManager.Shared.Preferences;
                bool isPro = p.UserTier.Equals("pro", StringComparison.OrdinalIgnoreCase);

                if (TxtTierStatus != null) TxtTierStatus.Text = isPro ? "Pro Member" : "Free Tier";
                if (TierIcon != null) TierIcon.Text = isPro ? "💎" : "🔒";
                if (TxtUsageLabel != null) TxtUsageLabel.Text = isPro ? "Unlimited Access" : $"{p.UsageLimitMinutes - p.UsageMinutes:F1} min remaining";

                if (TxtUsageStats != null)
                {
                    TxtUsageStats.Text = isPro ? $"{p.UsageMinutes:F1} / ∞" : $"{p.UsageMinutes:F1} / {p.UsageLimitMinutes}";
                }

                if (UsageProgressBar != null)
                {
                    double percentage = isPro ? 0.1 : (p.UsageMinutes / p.UsageLimitMinutes);
                    if (percentage > 1) percentage = 1;

                    var animation = new DoubleAnimation(percentage * 300, TimeSpan.FromMilliseconds(800)) // Width 300 in dashboard
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    UsageProgressBar.BeginAnimation(WidthProperty, animation);
                    
                    UsageProgressBar.Background = (p.UsageMinutes >= p.UsageLimitMinutes && !isPro) 
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")) 
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"));
                }

                // Update Performance Snapshot Stats
                if (StatTotalWords != null) StatTotalWords.Text = p.TotalWords.ToString("N0");
                if (StatTranscriptions != null) StatTranscriptions.Text = p.TotalTranscriptions.ToString("N0");
                if (StatTimeSaved != null)
                {
                    double hours = p.TotalTimeSavedMinutes / 60.0;
                    StatTimeSaved.Text = hours >= 1 ? $"{hours:F1}h" : $"{p.TotalTimeSavedMinutes:F0}m";
                }

                // Update Account View Controls
                if (AccountTierStatus != null) AccountTierStatus.Text = isPro ? "Pro Member" : "Free Tier";
                if (AccountTierIcon != null) AccountTierIcon.Text = isPro ? "💎" : "🔒";
                if (AccountUsageLabel != null) AccountUsageLabel.Text = isPro ? "Unlimited Access" : $"{p.UsageLimitMinutes - p.UsageMinutes:F1} min remaining";
                if (AccountUsageStats != null) AccountUsageStats.Text = isPro ? $"{p.UsageMinutes:F1} / ∞" : $"{p.UsageMinutes:F1} / {p.UsageLimitMinutes}";
                
                if (AccountUsageProgressBar != null)
                {
                    double percentage = isPro ? 0.1 : (p.UsageMinutes / p.UsageLimitMinutes);
                    if (percentage > 1) percentage = 1;
                    
                    // Account view width is flexible, but let's assume ~400 for animation or just set width if parent is known.
                    // Actually, let's just animate to a reasonable width or percentage if possible.
                    // For now, let's assume the parent container width is handled by layout, but we are animating Width property.
                    // We'll use a fixed max width assumption or just skip animation for now to avoid complexity without ActualWidth.
                    // Let's use a safe fallback width animation of 300px for now.
                    var animation = new DoubleAnimation(percentage * 300, TimeSpan.FromMilliseconds(800))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    AccountUsageProgressBar.BeginAnimation(WidthProperty, animation);
                }
            });
        }

        private void SwitchToView(UIElement header, UIElement view, System.Windows.Controls.Button activeBtn)
        {
            if (view == null) return;
            if (view.Visibility == Visibility.Visible) return;

            try
            {
                // Collapse everything first (with null checks)
                if (DashboardHeader != null) DashboardHeader.Visibility = Visibility.Collapsed;
                if (DashboardView != null) DashboardView.Visibility = Visibility.Collapsed;
                if (SmartCommandsHeader != null) SmartCommandsHeader.Visibility = Visibility.Collapsed;
                if (SmartCommandsView != null) SmartCommandsView.Visibility = Visibility.Collapsed;
                if (MicrophoneHeader != null) MicrophoneHeader.Visibility = Visibility.Collapsed;
                if (MicrophoneView != null) MicrophoneView.Visibility = Visibility.Collapsed;
                if (NarrationHeader != null) NarrationHeader.Visibility = Visibility.Collapsed;
                if (NarrationView != null) NarrationView.Visibility = Visibility.Collapsed;
                if (IntelligenceHeader != null) IntelligenceHeader.Visibility = Visibility.Collapsed;
                if (IntelligenceView != null) IntelligenceView.Visibility = Visibility.Collapsed;
                if (VocabularyHeader != null) VocabularyHeader.Visibility = Visibility.Collapsed;
                if (VocabularyView != null) VocabularyView.Visibility = Visibility.Collapsed;
                if (PreferencesHeader != null) PreferencesHeader.Visibility = Visibility.Collapsed;
                if (PreferencesView != null) PreferencesView.Visibility = Visibility.Collapsed;
                if (LanguageHeader != null) LanguageHeader.Visibility = Visibility.Collapsed;
                if (LanguageView != null) LanguageView.Visibility = Visibility.Collapsed;
                if (AccountHeader != null) AccountHeader.Visibility = Visibility.Collapsed;
                if (AccountView != null) AccountView.Visibility = Visibility.Collapsed;
                if (LoginHeader != null) LoginHeader.Visibility = Visibility.Collapsed;
                if (LoginView != null) LoginView.Visibility = Visibility.Collapsed;

                // Inactivate all buttons
                SetButtonInactive(BtnDashboard);
                SetButtonInactive(BtnSmartCommands);
                SetButtonInactive(BtnMicrophone);
                SetButtonInactive(BtnVocabulary);
                SetButtonInactive(BtnNarration);
                SetButtonInactive(BtnIntelligence);
                SetButtonInactive(BtnPreferences);
                SetButtonInactive(BtnPreferences);
                SetButtonInactive(BtnLanguage);
                SetButtonInactive(BtnAccount);

                // Show selected
                if (header != null) header.Visibility = Visibility.Visible;
                
                view.Visibility = Visibility.Visible;
                view.Opacity = 0;
                
                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400));
                var slideUp = new ThicknessAnimation(new Thickness(0, 20, 0, 0), new Thickness(0, 0, 0, 0), TimeSpan.FromMilliseconds(400)) 
                { 
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } 
                };
                view.BeginAnimation(OpacityProperty, fadeIn);
                view.BeginAnimation(MarginProperty, slideUp);

                if (activeBtn != null) SetButtonActive(activeBtn);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SwitchToView Error: {ex.Message}");
                // Last resort fallback
                if (view != null)
                {
                    view.Visibility = Visibility.Visible;
                    view.Opacity = 1;
                }
            }
        }

        private void SetVisibility(UIElement element, Visibility visibility)
        {
            if (element != null) element.Visibility = visibility;
        }

        private void SetButtonActive(System.Windows.Controls.Button btn)
        {
            if (btn != null)
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FFFFFF"));
        }

        private void SetButtonInactive(System.Windows.Controls.Button btn)
        {
            if (btn != null)
                btn.Background = Brushes.Transparent;
        }

        // --- Audio Logic ---

        private void OnAudioLevelChanged(float db)
        {
            Dispatcher.Invoke(() =>
            {
                float minDb = -60;
                float normalized = (db - minDb) / (0 - minDb);
                if (normalized < 0) normalized = 0;
                if (normalized > 1) normalized = 1;

                if (LiveMonitorBar != null)
                {
                    var animation = new DoubleAnimation(normalized * 350, TimeSpan.FromMilliseconds(50));
                    LiveMonitorBar.BeginAnimation(WidthProperty, animation);
                }

                if (LiveMonitorBarDashboard != null)
                {
                    var dashAnim = new DoubleAnimation(normalized * 200, TimeSpan.FromMilliseconds(50));
                    LiveMonitorBarDashboard.BeginAnimation(WidthProperty, dashAnim);
                }

                if (TxtMicLevel != null) TxtMicLevel.Text = $"{(int)(normalized * 100)}%";
                if (TxtMicLevelDashboard != null) TxtMicLevelDashboard.Text = $"{(int)(normalized * 100)}%";

                // Also update widget if recording
                var widget = System.Windows.Application.Current.Windows.OfType<WidgetWindow>().FirstOrDefault();
                widget?.UpdateVolumeIndicator(normalized);
            });
        }

        private void SldSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AudioRecorder.Shared != null)
                AudioRecorder.Shared.Sensitivity = e.NewValue / 100.0;
        }

        private void ToggleInteractionSounds_Click(object sender, MouseButtonEventArgs e)
        {
            AudioRecorder.Shared.PlayInteractionSounds = !AudioRecorder.Shared.PlayInteractionSounds;
            UpdateInteractionSoundsUI();
        }

        private void UpdateInteractionSoundsUI()
        {
            if (ToggleInteractionSounds == null) return;
            
            bool isActive = AudioRecorder.Shared.PlayInteractionSounds;
            ToggleInteractionSounds.Background = isActive 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF"));
                
            var ellipse = ToggleInteractionSounds.Child as System.Windows.Shapes.Shape;
            if (ellipse != null)
            {
                ellipse.HorizontalAlignment = isActive ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
            }
        }

        private async void BtnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            if (BtnCalibrate == null) return;
            BtnCalibrate.Content = "Listening...";
            BtnCalibrate.IsEnabled = false;

            await Task.Delay(2000);

            AudioRecorder.Shared.Sensitivity = 0.65; 
            if (SldSensitivity != null) SldSensitivity.Value = 65;

            BtnCalibrate.Content = "✓ Optimized";
            await Task.Delay(1000);
            BtnCalibrate.Content = "⚡ Calibrate";
            BtnCalibrate.IsEnabled = true;
        }

        // --- Narration Logic ---

        private void SldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtSpeedValue != null) TxtSpeedValue.Text = e.NewValue.ToString("0.0");
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtVolumeValue != null) TxtVolumeValue.Text = $"{(int)e.NewValue}%";
        }

        private void SldPitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtPitchValue != null) TxtPitchValue.Text = e.NewValue.ToString("0.0");
        }

        private async void BtnPlaySample_Click(object sender, RoutedEventArgs e)
        {
            if (BtnPlaySample == null) return;
            string originalContent = "▷ Play Sample";

            // Determine Voice Type
            string voice = "alloy";
            bool isLocal = false;
            
            if (ComboVoice.SelectedItem is ComboBoxItem item)
            {
                var content = item.Content.ToString() ?? "";
                voice = content.Length > 3 ? content.Substring(3) : content; // Strip icon (e.g. "🇬🇧 ")
                isLocal = true;
            }

            // API Key for optional Cloud voices (Legacy support)
            string apiKey = TxtApiKey.Password;

            try 
            {
                BtnPlaySample.Content = "Generating...";
                BtnPlaySample.IsEnabled = false;

                string text = !string.IsNullOrWhiteSpace(TxtPlayground.Text) 
                    ? TxtPlayground.Text 
                    : "Hello, I am CosmoWhisper, your advanced AI assistant.";

                string audioFile = "";

                if (isLocal)
                {
                     // Local Generation
                     using (var synth = new SpeechSynthesizer())
                     {
                         // Apply Speed
                         synth.Options.SpeakingRate = SldSpeed.Value;
                         
                         // Generate to Stream
                         var voices = SpeechSynthesizer.AllVoices;
                         var selectedVoice = voices.FirstOrDefault(v => v.DisplayName == voice);
                         if (selectedVoice != null) synth.Voice = selectedVoice;

                         var stream = await synth.SynthesizeTextToStreamAsync(text);
                         
                         // Save to Temp File
                         audioFile = Path.Combine(Path.GetTempPath(), $"local_{Guid.NewGuid()}.mp3");
                         using (var dataReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)))
                         {
                             await dataReader.LoadAsync((uint)stream.Size);
                             byte[] buffer = new byte[(int)stream.Size];
                             dataReader.ReadBytes(buffer);
                             await File.WriteAllBytesAsync(audioFile, buffer);
                         }
                     }
                }
                else
                {
                    // OpenAI Generation
                    audioFile = await CosmoWhisper.Services.AIService.Shared.GenerateSpeech(text, voice, SldSpeed.Value, apiKey);
                }

                if (audioFile.StartsWith("Error:"))
                {
                    BtnPlaySample.Content = "❌ Failed";
                    System.Windows.MessageBox.Show(audioFile);
                    await Task.Delay(2000);
                }
                else 
                {
                    BtnPlaySample.Content = "🔊 Playing...";
                    await CosmoWhisper.Managers.AudioRecorder.Shared.PlayAudio(audioFile);
                    await Task.Delay(3000); // Visual feedback
                }
            }
            catch (Exception ex)
            {
                 System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                BtnPlaySample.Content = originalContent;
                BtnPlaySample.IsEnabled = true;
            }
        }

        private void Intelligence_Click(object sender, RoutedEventArgs e)
        {
            ShowIntelligence();
        }

        private void ShowIntelligence()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(IntelligenceHeader, IntelligenceView, BtnIntelligence);
        }

        private void Personality_Click(object sender, RoutedEventArgs e)
        {
             if (sender is System.Windows.Controls.Button btn)
             {
                 var p = PreferenceManager.Shared.Preferences;
                 p.AIPersonality = btn.Tag?.ToString() ?? "Balanced";
                 PreferenceManager.Shared.Save();
                 UpdatePersonalityUI();
             }
        }

        private void UpdatePersonalityUI()
        {
            if (BtnConcise == null || BtnBalanced == null || BtnDetailed == null) return;
            var p = PreferenceManager.Shared.Preferences;

            BtnConcise.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.AIPersonality == "Concise" ? "#60A060" : "#10FFFFFF"));
            BtnBalanced.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.AIPersonality == "Balanced" ? "#60A060" : "#10FFFFFF"));
            BtnDetailed.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.AIPersonality == "Detailed" ? "#60A060" : "#10FFFFFF"));
        }

        private void Vocabulary_Click(object sender, RoutedEventArgs e)
        {
            ShowVocabulary();
        }

        private void ShowVocabulary()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(VocabularyHeader, VocabularyView, BtnVocabulary);
        }
        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                try
                {
                    ShowPreferences();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Critical error in Preferences_Click: {ex.Message}\n\nStack: {ex.StackTrace}", "App Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        private void ShowPreferences()
        {
            try 
            {
                if (AudioRecorder.Shared != null) 
                {
                    try { AudioRecorder.Shared.StopMonitoring(); } catch { }
                }
                
                if (PreferencesView == null)
                {
                    System.Windows.MessageBox.Show("PreferencesView element is null. This usually means a XAML naming conflict or parsing error.", "Debug Info");
                    return;
                }
                
                if (PreferencesHeader == null)
                {
                    System.Windows.MessageBox.Show("PreferencesHeader element is null.", "Debug Info");
                    return;
                }

                SwitchToView(PreferencesHeader, PreferencesView, BtnPreferences);
            }
            catch (Exception ex)
            {
                 System.Windows.MessageBox.Show($"Error in ShowPreferences: {ex.Message}", "Debug Info");
            }
        }

        private void Language_Click(object sender, RoutedEventArgs e)
        {
            ShowLanguage();
        }

        private void ShowLanguage()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(LanguageHeader, LanguageView, BtnLanguage);
        }


        private void InitializeVoices()
        {
            if (ComboVoice == null) return;
            ComboVoice.Items.Clear();

            // Define the 10 most popular languages in priority order
            var languagePriority = new Dictionary<string, int>
            {
                { "en-GB", 1 },      // UK English
                { "en-US", 2 },      // US English
                { "zh", 3 },         // Chinese (Mandarin)
                { "es", 4 },         // Spanish
                { "hi", 5 },         // Hindi
                { "ar", 6 },         // Arabic
                { "pt", 7 },         // Portuguese
                { "fr", 8 },         // French
                { "de", 9 },         // German
                { "ja", 10 }         // Japanese
            };

            // Language icons
            var languageIcons = new Dictionary<string, string>
            {
                { "en-GB", "🇬🇧" },
                { "en-US", "🇺🇸" },
                { "zh", "🇨🇳" },
                { "es", "🇪🇸" },
                { "hi", "🇮🇳" },
                { "ar", "🇸🇦" },
                { "pt", "🇵🇹" },
                { "fr", "🇫🇷" },
                { "de", "🇩🇪" },
                { "ja", "🇯🇵" }
            };

            var voices = SpeechSynthesizer.AllVoices
                .Select(v => new
                {
                    Voice = v,
                    Priority = languagePriority.ContainsKey(v.Language) ? languagePriority[v.Language] :
                               languagePriority.Any(kv => v.Language.StartsWith(kv.Key + "-")) ? 
                               languagePriority.First(kv => v.Language.StartsWith(kv.Key + "-")).Value : 999
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Voice.DisplayName);

            foreach (var item in voices)
            {
                var v = item.Voice;
                string icon = "🌐";
                
                // Find matching icon
                foreach (var lang in languageIcons)
                {
                    if (v.Language.StartsWith(lang.Key))
                    {
                        icon = lang.Value;
                        break;
                    }
                }
                
                ComboVoice.Items.Add(new ComboBoxItem { Content = $"{icon} {v.DisplayName}" });
            }

            if (ComboVoice.Items.Count > 0) ComboVoice.SelectedIndex = 0;
        }

        private void ComboVoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboVoice.SelectedItem is ComboBoxItem item)
            {
                var content = item.Content.ToString() ?? "";
                // Strip the flag icon (first 2-3 characters) to get the voice name
                string voiceName = content.Length > 3 ? content.Substring(3).Trim() : content;
                
                // Save to preferences
                var p = PreferenceManager.Shared.Preferences;
                p.SelectedVoice = voiceName;
                PreferenceManager.Shared.Save();
            }
        }

        private async void InitializeMicrophones()
        {
            if (ComboMics == null) return;
            ComboMics.Items.Clear();

            try
            {
                var devices = await AudioRecorder.Shared.EnumerateInputDevices();
                foreach (var d in devices)
                {
                    ComboMics.Items.Add(new ComboBoxItem { Content = d.Name, Tag = d.Id });
                }

                if (ComboMics.Items.Count > 0)
                {
                    var p = PreferenceManager.Shared.Preferences;
                    var selected = ComboMics.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Tag.ToString() == p.MicDeviceId);
                    if (selected != null) ComboMics.SelectedItem = selected;
                    else ComboMics.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mic Enum Error: {ex.Message}");
            }
        }

        private void ComboMics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboMics.SelectedItem is ComboBoxItem item)
            {
                var p = PreferenceManager.Shared.Preferences;
                p.MicDeviceName = item.Content.ToString();
                p.MicDeviceId = item.Tag.ToString();
                PreferenceManager.Shared.Save();

                // Update recorder and restart monitoring if it was active
                AudioRecorder.Shared.SelectedDeviceId = p.MicDeviceId;
                if (MicrophoneView.Visibility == Visibility.Visible)
                {
                    AudioRecorder.Shared.StopMonitoring();
                    AudioRecorder.Shared.StartMonitoring();
                }
            }
        }

        private void InitializePreferences()
        {
            LoadPosition();
            var p = PreferenceManager.Shared.Preferences;
            
            if (ToggleClipboard != null) UpdateToggle(ToggleClipboard, p.RestoreClipboard);
            if (ToggleAutoSubmit != null) UpdateToggle(ToggleAutoSubmit, p.AutoSubmit);
            if (ToggleAutoCopy != null) UpdateToggle(ToggleAutoCopy, p.AutoCopy);
            if (ToggleMouseButton != null) UpdateToggle(ToggleMouseButton, p.MouseButton != "None");
            if (TxtBackupPath != null) TxtBackupPath.Text = p.BackupDirectory;
            if (TxtHotkey != null) TxtHotkey.Text = p.ActivationKey;
            
            // Intelligence View Manus Agent
            if (ToggleManusAgent != null) UpdateToggle(ToggleManusAgent, p.EnableManusAgent);
            if (ToggleManusNarration != null) UpdateToggle(ToggleManusNarration, p.ManusNarrationEnabled);

            // In InitializeMicrophones we handle Mic selection
            
            UpdateInsertionUI();
            UpdatePersonalityUI();
            
            // Widget Opacity
            if (SldWidgetOpacity != null)
            {
                SldWidgetOpacity.Value = p.WidgetOpacity * 100;
                if (TxtWidgetOpacityValue != null) TxtWidgetOpacityValue.Text = $"{(int)(p.WidgetOpacity * 100)}%";
            }
            
            // UI Scale
            if (SldUIScale != null)
            {
                // Set value from preferences
                SldUIScale.Value = p.UIScale * 100;
                if (TxtUIScaleValue != null) TxtUIScaleValue.Text = $"{(int)(p.UIScale * 100)}%";
                
                // Attach handler NOW (it was removed from XAML to prevent overwrite on init)
                SldUIScale.ValueChanged += SldUIScale_ValueChanged;
                
                // Apply the saved scale
                ApplyUIScale(p.UIScale);
            }
            
            // Mouse Button Configuration
            UpdateMouseConfigUI();
            
            // Language Settings
            if (ToggleRegionalSpelling != null) UpdateToggle(ToggleRegionalSpelling, p.EnableRegionalSpelling);
            if (ComboLanguage != null)
            {
                foreach (ComboBoxItem item in ComboLanguage.Items)
                {
                    if (item.Tag?.ToString() == p.InterfaceLanguage)
                    {
                        ComboLanguage.SelectedItem = item;
                        break;
                    }
                }
            }

            // DateTime Formatting
            if (ComboDateTime != null)
            {
                // Default fallback if not set
                if (string.IsNullOrEmpty(p.SelectedDateFormat)) p.SelectedDateFormat = "dd/MM/yyyy";
                
                foreach (ComboBoxItem item in ComboDateTime.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedDateFormat)
                    {
                        ComboDateTime.SelectedItem = item;
                        break;
                    }
                }
            }
            
            if (TxtApiKey != null)
            {
                TxtApiKey.Password = p.OpenAIApiKey;
                TxtApiKey.PasswordChanged += (s, e) => {
                    var prefs = PreferenceManager.Shared.Preferences;
                    prefs.OpenAIApiKey = TxtApiKey.Password;
                    PreferenceManager.Shared.Save();
                };
            }

            if (TxtGroqApiKey != null)
            {
                TxtGroqApiKey.Password = p.GroqApiKey;
                TxtGroqApiKey.PasswordChanged += (s, e) => {
                    if (TxtGroqApiKey.Password == "10810")
                    {
                        p.IsAIUnlocked = true;
                        p.GroqApiKey = ""; // Clear the code
                        TxtGroqApiKey.Password = "";
                        System.Windows.MessageBox.Show("Premium Intelligence Unlocked!");
                        UpdateGroqStatusUI();
                    }
                    else if (p.IsAIUnlocked)
                    {
                        p.GroqApiKey = TxtGroqApiKey.Password;
                    }
                    PreferenceManager.Shared.Save();
                };
            }
            UpdateGroqStatusUI();



            UpdateDashboardStats();
            PreferenceManager.Shared.PreferencesUpdated += () => UpdateDashboardStats();

            // Periodic sync if token exists
            if (!string.IsNullOrEmpty(p.LicenseToken))
            {
                Task.Run(async () => {
                    while (true)
                    {
                        await LicenseManager.Shared.SyncStatusAsync();
                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }
                });
            }
        }

        private void UpdateGroqStatusUI()
        {
            if (TxtGroqWarning == null || TxtGroqSuccess == null) return;
            var p = PreferenceManager.Shared.Preferences;
            
            TxtGroqWarning.Visibility = p.IsAIUnlocked ? Visibility.Collapsed : Visibility.Visible;
            TxtGroqSuccess.Visibility = p.IsAIUnlocked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SldUIScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SldUIScale == null) return;
            var scale = SldUIScale.Value / 100.0;
            var p = PreferenceManager.Shared.Preferences;
            p.UIScale = scale;
            if (TxtUIScaleValue != null) TxtUIScaleValue.Text = $"{(int)SldUIScale.Value}%";
            PreferenceManager.Shared.Save();
            
            ApplyUIScale(scale);
        }

        private void ApplyUIScale(double scale)
        {
            try
            {
                // Ensure scale is reasonable to avoid UI disappearing
                if (scale < 0.5 || scale > 3.0) scale = 1.0;

                // Scale content
                if (this.Content is FrameworkElement content)
                {
                   content.LayoutTransform = new ScaleTransform(scale, scale);
                }
                
                // Update text
                if (TxtUIScaleValue != null) 
                    TxtUIScaleValue.Text = $"{(int)(scale * 100)}%";

                // REMOVED SizeToContent calls which caused jumpiness.
                // The user can resize the window manually if needed.
            }
            catch (Exception ex) 
            {
               LogCrash($"ApplyUIScale Error: {ex.Message}");
            }
        }

        private void SldWidgetOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtWidgetOpacityValue != null) TxtWidgetOpacityValue.Text = $"{(int)e.NewValue}%";
            
            var p = PreferenceManager.Shared.Preferences;
            p.WidgetOpacity = e.NewValue / 100.0;
            PreferenceManager.Shared.Save();
            
            // Apply to widget immediately
            var widget = System.Windows.Application.Current.Windows.OfType<WidgetWindow>().FirstOrDefault();
            widget?.ApplyWidgetTransparency();
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;

                if (!Directory.Exists(destDir))
                {
                    System.Windows.MessageBox.Show("No backup directory found.");
                    return;
                }

                // Find latest backup folder
                var latestBackup = Directory.GetDirectories(destDir, "CosmoVault_*")
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                if (latestBackup == null)
                {
                    System.Windows.MessageBox.Show("No backups (CosmoVault_*) found in the folder.");
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"Found latest backup: {Path.GetFileName(latestBackup)}\n\nRestore this snapshot? (This will reload all settings)", 
                    "Confirm Restore", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    PreferenceManager.Shared.Restore(latestBackup);
                    InitializePreferences();
                    System.Windows.MessageBox.Show("Restore successful! Environment reloaded.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Restore failed: {ex.Message}");
            }
        }

        private bool _isCapturingHotkey = false;
        private void BtnEditHotkey_Click(object sender, MouseButtonEventArgs e)
        {
            _isCapturingHotkey = true;
            if (TxtHotkey != null) TxtHotkey.Text = "PRESS ANY KEY...";
            this.KeyDown += DashboardWindow_CaptureKeyDown;
        }

        private void DashboardWindow_CaptureKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isCapturingHotkey) return;

            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            uint vk = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);

            var p = PreferenceManager.Shared.Preferences;
            p.ActivationKey = key.ToString();
            p.VirtualKey = vk;
            PreferenceManager.Shared.Save();

            if (TxtHotkey != null) TxtHotkey.Text = p.ActivationKey;

            _isCapturingHotkey = false;
            this.KeyDown -= DashboardWindow_CaptureKeyDown;
            e.Handled = true;
        }

        private void BtnClearHotkey_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.ActivationKey = "NONE";
            p.VirtualKey = 0;
            PreferenceManager.Shared.Save();
            if (TxtHotkey != null) TxtHotkey.Text = "NONE";
        }

        private void BtnChangeBackup_Click(object sender, RoutedEventArgs e)
        {
             // In a real app, use Win32 FolderBrowserDialog.
             // For now, let's just simulate or show a message.
             using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
             {
                 dialog.Description = "Select a folder to store your CosmoWhisper backups";
                 dialog.UseDescriptionForTitle = true;
                 dialog.ShowNewFolderButton = true;
                 
                 if (Directory.Exists(PreferenceManager.Shared.Preferences.BackupDirectory))
                 {
                     dialog.SelectedPath = PreferenceManager.Shared.Preferences.BackupDirectory;
                 }

                 if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                 {
                     var p = PreferenceManager.Shared.Preferences;
                     p.BackupDirectory = dialog.SelectedPath;
                     PreferenceManager.Shared.Save();
                     
                     if (TxtBackupPath != null) TxtBackupPath.Text = p.BackupDirectory;
                 }
             }
        }

        private async void BtnBackupNow_Click(object sender, RoutedEventArgs e)
        {
            var btn = (System.Windows.Controls.Button)sender;
            string originalContent = btn.Content.ToString();
            
            try
            {
                btn.Content = "Backing up...";
                btn.IsEnabled = false;

                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;
                Directory.CreateDirectory(destDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFolder = Path.Combine(destDir, $"CosmoVault_{timestamp}");
                Directory.CreateDirectory(backupFolder);

                // Source Folder
                string sourceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
                
                if (Directory.Exists(sourceFolder))
                {
                    foreach (string file in Directory.GetFiles(sourceFolder, "*.json"))
                    {
                        string fileName = Path.GetFileName(file);
                        File.Copy(file, Path.Combine(backupFolder, fileName), true);
                    }
                }

                // Calculate size
                long sizeInBytes = 0;
                foreach (string file in Directory.GetFiles(backupFolder))
                {
                    sizeInBytes += new FileInfo(file).Length;
                }
                double sizeInMb = sizeInBytes / (1024.0 * 1024.0);

                await Task.Delay(1500); // Simulate processing

                btn.Content = "✅ Success";
                System.Windows.MessageBox.Show($"Universal Snapshot 'CosmoVault_{timestamp}' created successfully!\n\nLocation: {destDir}\nSize: {sizeInMb:F2} MB", "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Backup failed: {ex.Message}");
                btn.Content = "❌ Failed";
            }
            finally
            {
                await Task.Delay(2000);
                btn.Content = originalContent;
                btn.IsEnabled = true;
            }
        }

        private void UpdateToggle(Border toggle, bool isOn)
        {
            if (toggle == null) return;
            toggle.Background = isOn 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF")) 
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF"));

            if (toggle.Child is System.Windows.Shapes.Ellipse ellipse)
            {
                 ellipse.HorizontalAlignment = isOn ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
            }
        }

        private void ToggleClipboard_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.RestoreClipboard = !p.RestoreClipboard;
            PreferenceManager.Shared.Save();
            UpdateToggle(ToggleClipboard, p.RestoreClipboard);
        }

        private void ToggleAutoSubmit_Click(object sender, MouseButtonEventArgs e)
        {
             var p = PreferenceManager.Shared.Preferences;
             p.AutoSubmit = !p.AutoSubmit;
             PreferenceManager.Shared.Save();
             UpdateToggle(ToggleAutoSubmit, p.AutoSubmit);
             ShowToast(p.AutoSubmit ? "Auto-Submit Enabled" : "Auto-Submit Disabled", "⚡");
        }

        private void ToggleAutoCopy_Click(object sender, MouseButtonEventArgs e)
        {
             var p = PreferenceManager.Shared.Preferences;
             p.AutoCopy = !p.AutoCopy;
             PreferenceManager.Shared.Save();
             UpdateToggle(ToggleAutoCopy, p.AutoCopy);
             ShowToast(p.AutoCopy ? "Auto-Copy Enabled" : "Auto-Copy Disabled", "📋");
        }

        private void ShowToast(string message, string icon = "✨")
        {
            if (ToastContainer == null || TxtToastMessage == null || TxtToastIcon == null) return;

            TxtToastMessage.Text = message;
            TxtToastIcon.Text = icon;
            ToastContainer.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300));
            var slideIn = new ThicknessAnimation(new Thickness(0, 30, 0, 0), new Thickness(0, 50, 0, 0), TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase() };

            ToastContainer.BeginAnimation(OpacityProperty, fadeIn);
            ToastContainer.BeginAnimation(MarginProperty, slideIn);

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (s, ev) =>
            {
                var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
                fadeOut.Completed += (s2, ev2) => ToastContainer.Visibility = Visibility.Collapsed;
                ToastContainer.BeginAnimation(OpacityProperty, fadeOut);
                timer.Stop();
            };
            timer.Start();
        }

        private void ToggleMouseButton_Click(object sender, MouseButtonEventArgs e)
        {
             var p = PreferenceManager.Shared.Preferences;
             if (p.MouseButton == "None") p.MouseButton = "Middle";
             else p.MouseButton = "None";
             PreferenceManager.Shared.Save();
             UpdateToggle(ToggleMouseButton, p.MouseButton != "None");
        }

        private void FastPaste_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.InsertionMode = InsertionMethod.FastPaste;
            PreferenceManager.Shared.Save();
            UpdateInsertionUI();
        }

        private void DirectType_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.InsertionMode = InsertionMethod.DirectTyping;
            PreferenceManager.Shared.Save();
            UpdateInsertionUI();
        }

        private void UpdateInsertionUI()
        {
            if (BtnFastPaste == null || BtnDirectType == null) return;
            var p = PreferenceManager.Shared.Preferences;
            bool isFast = p.InsertionMode == InsertionMethod.FastPaste;
            
            BtnFastPaste.Opacity = isFast ? 1.0 : 0.6;
            BtnFastPaste.BorderBrush = isFast ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF")) : Brushes.Transparent;
            
            BtnDirectType.Opacity = !isFast ? 1.0 : 0.6;
            BtnDirectType.BorderBrush = !isFast ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF")) : Brushes.Transparent;
        }

        private void ToggleManusAgent_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.EnableManusAgent = !p.EnableManusAgent;
            PreferenceManager.Shared.Save();
            UpdateToggle(ToggleManusAgent, p.EnableManusAgent);
        }

        private void ToggleManusNarration_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.ManusNarrationEnabled = !p.ManusNarrationEnabled;
            PreferenceManager.Shared.Save();
            UpdateToggle(ToggleManusNarration, p.ManusNarrationEnabled);
        }

        private void MouseConfigBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (TxtMouseConfig == null) return;
            
            TxtMouseConfig.Text = "Press any mouse button...";
            TxtMouseConfig.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF453A"));
            
            // Capture the next mouse button press
            this.PreviewMouseDown += CaptureMouseButton;
        }

        private void CaptureMouseButton(object sender, MouseButtonEventArgs e)
        {
            // Remove the event handler so we only capture once
            this.PreviewMouseDown -= CaptureMouseButton;
            
            // Don't capture left click (that's for UI interaction)
            if (e.ChangedButton == MouseButton.Left)
            {
                TxtMouseConfig.Text = PreferenceManager.Shared.Preferences.MouseButton;
                TxtMouseConfig.Foreground = Brushes.White;
                return;
            }
            
            // Get the button name
            string buttonName = e.ChangedButton.ToString();
            
            // Save the new mouse button
            var p = PreferenceManager.Shared.Preferences;
            p.MouseButton = buttonName;
            PreferenceManager.Shared.Save();
            
            // Update UI
            UpdateMouseConfigUI();
            
            e.Handled = true;
        }

        private void BtnClearMouse_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent triggering the parent click
            
            var p = PreferenceManager.Shared.Preferences;
            p.MouseButton = "None";
            PreferenceManager.Shared.Save();
            
            UpdateMouseConfigUI();
            
            // Also turn off the toggle
            UpdateToggle(ToggleMouseButton, false);
        }

        private void UpdateMouseConfigUI()
        {
            if (TxtMouseConfig == null) return;
            
            var p = PreferenceManager.Shared.Preferences;
            if (string.IsNullOrEmpty(p.MouseButton) || p.MouseButton == "None")
            {
                TxtMouseConfig.Text = "Click to configure...";
                TxtMouseConfig.Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.6 };
            }
            else
            {
                TxtMouseConfig.Text = $"{p.MouseButton} Button Configured";
                TxtMouseConfig.Foreground = Brushes.White;
            }
        }

        private void ToggleRegionalSpelling_Click(object sender, MouseButtonEventArgs e)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.EnableRegionalSpelling = !p.EnableRegionalSpelling;
            PreferenceManager.Shared.Save();
            UpdateToggle(ToggleRegionalSpelling, p.EnableRegionalSpelling);
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboLanguage == null || ComboLanguage.SelectedItem == null) return;
            
            var item = ComboLanguage.SelectedItem as ComboBoxItem;
            var langCode = item?.Tag?.ToString();
            
            if (!string.IsNullOrEmpty(langCode))
            {
                var p = PreferenceManager.Shared.Preferences;
                if (p.InterfaceLanguage != langCode)
                {
                    p.InterfaceLanguage = langCode;
                    PreferenceManager.Shared.Save();
                }
            }
        }

        private void ComboDateTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboDateTime == null || ComboDateTime.SelectedItem == null) return;

            var item = ComboDateTime.SelectedItem as ComboBoxItem;
            var format = item?.Tag?.ToString();

            if (!string.IsNullOrEmpty(format))
            {
                var p = PreferenceManager.Shared.Preferences;
                if (p.SelectedDateFormat != format)
                {
                    p.SelectedDateFormat = format;
                    PreferenceManager.Shared.Save();
                }
            }
        }
        private void OpenLibraryWeb_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://cosmowhisper.com/features",
                UseShellExecute = true
            });
        }

        private void VisitDocs_Click(object sender, MouseButtonEventArgs e)
        {
             Process.Start(new ProcessStartInfo
            {
                FileName = "https://cosmowhisper.com/faq",
                UseShellExecute = true
            });
        }

        private void VisitGitHub_Click(object sender, MouseButtonEventArgs e)
        {
             Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Viguru24/CosmoWhisper-Native",
                UseShellExecute = true
            });
        }


        // --- Account & Login Logic ---

        private async void PerformLogin_Click(object sender, RoutedEventArgs e)
        {
             string email = TxtLoginEmail.Text;
             string password = TxtLoginPassword.Password;
             
             if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
             {
                 System.Windows.MessageBox.Show("Please enter both email and password.", "Validation Error");
                 return;
             }
             
             var (success, message) = await BackendService.Shared.Login(email, password);
             
             if (success)
             {
                 UpdateDashboardStats();
                 ShowAccount();
                 await BackendService.Shared.SyncStatus();
                 UpdateDashboardStats(); 
             }
             else
             {
                 System.Windows.MessageBox.Show(message, "Login Failed");
             }
        }

        private void ActivateLicense_Click(object sender, RoutedEventArgs e)
        {
             string key = TxtLicense.Text;
             if (string.IsNullOrWhiteSpace(key)) return;

             // TODO: Call Backend License Verify
             if (key == "COSMO-PRO-TEST")
             {
                 var p = PreferenceManager.Shared.Preferences;
                 p.LicenseToken = key;
                 p.UserTier = "pro";
                 PreferenceManager.Shared.Save();
                 
                 System.Windows.MessageBox.Show("License Activated Successfully!", "Success");
                 UpdateDashboardStats();
             }
             else
             {
                 System.Windows.MessageBox.Show("Invalid License Key", "Error");
             }
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
             var p = PreferenceManager.Shared.Preferences;
             p.LicenseToken = "";
             p.AuthToken = "";
             p.UserTier = "free";
             PreferenceManager.Shared.Save();
             
             UpdateDashboardStats();
             ShowLogin();
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
             Process.Start(new ProcessStartInfo
             {
                 FileName = "https://cosmowhisper.com/signup", // Placeholder URL
                 UseShellExecute = true
             });
        }

        // Lock/Unlock API Key Field
        private bool isApiKeyUnlocked = false;
        private const string UNLOCK_CODE = "10810";

        private void TxtUnlockCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-check if code is correct
            if (TxtUnlockCode.Text == UNLOCK_CODE && !isApiKeyUnlocked)
            {
                UnlockApiKey();
            }
        }

        private void BtnToggleLock_Click(object sender, RoutedEventArgs e)
        {
            if (isApiKeyUnlocked)
            {
                // Lock it
                LockApiKey();
            }
            else
            {
                // Try to unlock
                if (TxtUnlockCode.Text == UNLOCK_CODE)
                {
                    UnlockApiKey();
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorrect unlock code. Please enter 10810.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void UnlockApiKey()
        {
            isApiKeyUnlocked = true;
            TxtGroqApiKey.IsEnabled = true;
            BtnToggleLock.Content = "🔒 Lock"; // Lock emoji
            BtnToggleLock.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5A623"));
            TxtGroqWarning.Visibility = Visibility.Collapsed;
            TxtGroqSuccess.Visibility = Visibility.Visible;
            UnlockPanel.Visibility = Visibility.Collapsed;
        }

        private void LockApiKey()
        {
            isApiKeyUnlocked = false;
            TxtGroqApiKey.IsEnabled = false;
            TxtGroqApiKey.Clear();
            BtnToggleLock.Content = "🔓 Unlock"; // Unlock emoji
            BtnToggleLock.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007AFF"));
            TxtGroqWarning.Visibility = Visibility.Visible;
            TxtGroqSuccess.Visibility = Visibility.Collapsed;
            UnlockPanel.Visibility = Visibility.Visible;
            TxtUnlockCode.Clear();
        }
    }
}
