using System;
using System.Diagnostics;
using CosmoWhisper.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using CosmoWhisper.Managers;
using CosmoWhisper.Controllers;
using System.Collections.ObjectModel;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using Button = System.Windows.Controls.Button;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using ColorConverter = System.Windows.Media.ColorConverter;
using Colors = System.Windows.Media.Colors;
using Application = System.Windows.Application;

namespace CosmoWhisper
{
    public partial class DashboardWindow : Window
    {
        public static DashboardWindow? Instance { get; private set; }
        internal NavigationController _navigation;
        private VocabularyController _vocabulary;
        private PreferencesController _prefs;
        private IntelligenceController _intelligence;
        private NarrationController _narration;
        private MicrophoneController _mic;
        private ThemeController _theme;
        private DashboardController _dashboard;

        #region View Property Bridges
        // Bridge properties for Controllers to access views and deep controls inside UserControls
        public FrameworkElement DashboardView => DashboardViewControl;
        public FrameworkElement SmartCommandsView => SmartCommandsViewControl;
        public FrameworkElement MicrophoneView => MicrophoneViewControl;
        public FrameworkElement VocabularyView => VocabularyViewControl;
        public FrameworkElement NarrationView => NarrationViewControl;
        public FrameworkElement IntelligenceView => IntelligenceViewControl;
        public FrameworkElement PreferencesView => PreferencesViewControl;
        public FrameworkElement AccountView => AccountViewControl;
        public FrameworkElement LoginView => LoginViewControl;
        public FrameworkElement CloudLibraryView => CloudLibraryViewControl;

        // DashboardView Controls
        public Border StoreLiteBanner => DashboardViewControl.StoreLiteBannerControl;
        public Border ProBanner => DashboardViewControl.ProBannerControl;
        public TextBlock TxtTierStatus => DashboardViewControl.TxtTierStatusControl;
        public TextBlock TierIcon => DashboardViewControl.TierIconControl;
        public TextBlock TxtUsageLabel => DashboardViewControl.TxtUsageLabelControl;
        public TextBlock TxtUsageStats => DashboardViewControl.TxtUsageStatsControl;
        public Border UsageProgressBar => DashboardViewControl.UsageProgressBarControl;
        public TextBlock StatTotalWords => DashboardViewControl.StatTotalWordsControl;
        public TextBlock StatTimeSaved => DashboardViewControl.StatTimeSavedControl;
        public TextBlock StatTranscriptions => DashboardViewControl.StatTranscriptionsControl;

        // AccountView Controls
        public TextBlock AccountUserName => AccountViewControl.AccountUserNameControl;
        public TextBlock TxtAccountEmail => AccountViewControl.TxtAccountEmailControl;
        public TextBlock AccountTierStatus => AccountViewControl.AccountTierStatusControl;
        public TextBlock AccountTierIcon => AccountViewControl.AccountTierIconControl;
        public TextBlock AccountUsageLabel => AccountViewControl.AccountUsageLabelControl;
        public TextBlock AccountUsageStats => AccountViewControl.AccountUsageStatsControl;
        public Border AccountUsageProgressBar => AccountViewControl.AccountUsageProgressBarControl;
        public TextBox TxtLicense => AccountViewControl.TxtLicense;

        // VocabularyView Controls (Bridged if using UserControl)
        public System.Windows.Controls.ListBox? VocabList => VocabularyViewControl?.VocabListControl;
        public TextBox? TxtVocabHints => VocabularyViewControl?.TxtVocabHintsControl;
        public TextBlock? PlaceholderVocabHints => VocabularyViewControl?.PlaceholderVocabHintsControl;
        public ToggleButton? ToggleSignOff => VocabularyViewControl?.ToggleSignOffControl;
        public TextBox? TxtSignature => VocabularyViewControl?.TxtSignatureControl;
        public TextBlock? PlaceholderSignature => VocabularyViewControl?.PlaceholderSignatureControl;
        public TextBox? TxtNewKey => VocabularyViewControl?.TxtNewKeyControl;
        public TextBox? TxtNewValue => VocabularyViewControl?.TxtNewValueControl;
        public TextBlock? PlaceholderKey => VocabularyViewControl?.PlaceholderKeyControl;
        public TextBlock? PlaceholderValue => VocabularyViewControl?.PlaceholderValueControl;

        // IntelligenceView Controls

        // NarrationView Controls
        public ComboBox? ComboVoices => NarrationViewControl?.ComboVoicesControl;
        public ComboBox? ComboOutput => NarrationViewControl?.ComboOutputControl;
        
        // MicrophoneView Controls
        public ComboBox? ComboMics => MicrophoneViewControl?.ComboMics;
        public Slider? SldSensitivity => MicrophoneViewControl?.SldSensitivity;
        public TextBlock? TxtSensitivityValue => MicrophoneViewControl?.TxtSensitivityValue;
        public ToggleButton? ToggleInteractionSounds => MicrophoneViewControl?.ToggleInteractionSounds;
        public Button? BtnCalibrate => MicrophoneViewControl?.BtnCalibrate;
        
        // Extra missing controls
        public Button? BtnUpgradeAccount => AccountViewControl?.BtnUpgradeAccount;
        public ComboBox? ComboVoice => NarrationViewControl?.ComboVoiceControl;
        public ComboBox? ComboPersonality => IntelligenceViewControl?.ComboPersonalityControl;
        public ComboBox? ComboVerbosity => IntelligenceViewControl?.ComboVerbosityControl;
        public PasswordBox? TxtGroqApiKey => IntelligenceViewControl?.TxtGroqApiKeyControl;
        public PasswordBox? TxtOpenAIApiKey => IntelligenceViewControl?.TxtOpenAIApiKeyControl;
        public TextBox? TxtPlayground => NarrationViewControl?.TxtPlaygroundControl;
        public Button? BtnPlaySample => NarrationViewControl?.BtnPlaySampleControl;

        // PreferencesView Controls
        public ToggleButton? ToggleClipboard => PreferencesViewControl?.ToggleClipboardControl;
        public Border? ToggleAutoSubmit => DashboardViewControl?.ToggleAutoSubmitControl;
        public ToggleButton? ToggleAutoCopy => PreferencesViewControl?.ToggleAutoCopyControl;
        public ToggleButton? ToggleSpeechPolish => PreferencesViewControl?.ToggleSpeechPolishControl;
        public TextBlock? TxtBackupPath => PreferencesViewControl?.TxtBackupPathControl;
        public Button? BtnBackupNow => PreferencesViewControl?.BtnBackupNowControl;
        public TextBlock? TxtHotkey => PreferencesViewControl?.TxtHotkeyControl;
        public TextBlock? TxtListeningPrompt => PreferencesViewControl?.TxtListeningPromptControl;
        public Border? ToggleLocalWhisper => DashboardViewControl?.ToggleLocalWhisperControl;
        public ToggleButton? ToggleForceCloudProxy => PreferencesViewControl?.ToggleForceCloudProxyControl;
        public ToggleButton? ToggleForceDirectApi => PreferencesViewControl?.ToggleForceDirectApiControl;
        public Slider? SldWidgetOpacity => PreferencesViewControl?.SldWidgetOpacityControl;
        public TextBlock? TxtWidgetOpacityValue => PreferencesViewControl?.TxtWidgetOpacityValueControl;
        public Slider? SldUIScale => PreferencesViewControl?.SldUIScaleControl;
        public TextBlock? TxtUIScaleValue => PreferencesViewControl?.TxtUIScaleValueControl;
        public ToggleButton? ToggleRegionalSpelling => PreferencesViewControl?.ToggleRegionalSpellingControl;
        public ToggleButton? ToggleLaunchOnStartup => PreferencesViewControl?.ToggleLaunchOnStartupControl;
        public ToggleButton? TogglePasteMode => PreferencesViewControl?.TogglePasteModeControl;
        public ComboBox? ComboDateFormat => PreferencesViewControl?.ComboDateFormatControl;
        public ComboBox? ComboTimeFormat => PreferencesViewControl?.ComboTimeFormatControl;
        public ToggleButton? ToggleAlwaysOnTop => PreferencesViewControl?.ToggleAlwaysOnTopControl;
        public ComboBox? ComboLocalVoiceEngine => PreferencesViewControl?.ComboLocalVoiceEngineControl;
        public PasswordBox? TxtApiKey => PreferencesViewControl?.TxtApiKeyControl;
        public Border? ToggleLocalOnly => PreferencesViewControl?.ToggleLocalOnlyControl;
        public TextBlock? TxtLocalWhisperStatus => DashboardViewControl?.TxtLocalWhisperStatusControl;
        public TextBlock? TxtAutoSubmitStatus => DashboardViewControl?.TxtAutoSubmitStatusControl;
        public TextBlock? LocalWhisperIcon => DashboardViewControl?.LocalWhisperIconControl;
        public TextBlock? AutoSubmitIcon => DashboardViewControl?.AutoSubmitIconControl;
        public Ellipse? LocalWhisperGlow => DashboardViewControl?.LocalWhisperGlowControl;
        public Ellipse? AutoSubmitGlow => DashboardViewControl?.AutoSubmitGlowControl;
        public TextBlock? LocalModeStatusTitle => PreferencesViewControl?.LocalModeStatusTitleControl;
        public TextBlock? LocalModeStatusDesc => PreferencesViewControl?.LocalModeStatusDescControl;
        public TextBlock? LocalOverlayIcon => PreferencesViewControl?.LocalOverlayIconControl;

        #endregion

        public DashboardWindow()
        {
            Instance = this;
            _navigation = new NavigationController(this);
            _vocabulary = new VocabularyController(this);
            _prefs = new PreferencesController(this);
            _intelligence = new IntelligenceController(this);
            _narration = new NarrationController(this);
            _mic = new MicrophoneController(this);
            _theme = new ThemeController(this);
            _dashboard = new DashboardController(this);

            try
            {
                InitializeComponent();

                // --- BRANDING & VERSIONING ---
                var appVer = Assembly.GetExecutingAssembly().GetName().Version;
                if (TxtAppVersion != null && appVer != null)
                {
                    TxtAppVersion.Text = $"v{appVer.Major}.{appVer.Minor}.{appVer.Build}";
                }

                if (PreferenceManager.Shared.IsDevMode)
                {
                    // [ORANGE] Development Instance
                    Title = "[ORANGE DEV] CosmoWhisper";
                    if (SidebarGearIcon != null) SidebarGearIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 149, 0)); // Orange
                }
                else if (!PreferenceManager.Shared.Preferences.IsStoreVersion)
                {
                    // [GREEN] Personal Stable Instance (Louis Edition)
                    Title = "CosmoWhisper";
                    if (SidebarGearIcon != null) SidebarGearIcon.Foreground = new SolidColorBrush(Color.FromRgb(50, 215, 75)); // Lime Green
                }
                else
                {
                    // [GRAY] Microsoft Store Build
                    Title = "CosmoWhisper (Lite)";
                    if (SidebarGearIcon != null) SidebarGearIcon.Foreground = Brushes.Gray;
                }

                if (BtnPreferences != null) BtnPreferences.Visibility = Visibility.Visible;

                DataContext = PreferenceManager.Shared.Preferences;

                // Setup Global Exception Handlers for Crash Debugging
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    LogCrash($"Unhandled Domain Exception: {e.ExceptionObject}");
                };
                Dispatcher.UnhandledException += (s, e) =>
                {
                    LogCrash($"Unhandled Dispatcher Exception: {e.Exception.Message}\n{e.Exception.StackTrace}");
                    e.Handled = true; // Try to prevent app closing
                };

                // Subscribe to Audio Events (Do this BEFORE InitializeMicrophones)
                AudioRecorder.Shared.ErrorOccurred += (msg) =>
                {
                    System.Diagnostics.Debug.WriteLine($"AUDIO ERROR: {msg}");
                    Dispatcher.Invoke(() => _ = CosmoMessage.Show("Audio System", msg, "\uE783"));
                };


                try { _narration.Initialize(); } catch (Exception ex) { LogCrash($"InitVoices Error: {ex.Message}"); }
                try { _ = _mic.Initialize(); } catch (Exception ex) { LogCrash($"InitMics Error: {ex.Message}"); }

                // Init UI values
                if (SldSensitivity != null) SldSensitivity.Value = AudioRecorder.Shared.Sensitivity * 100;
                try { _mic.UpdateInteractionSoundsUI(); } catch (Exception ex) { LogCrash($"UpdateSoundsUI Error: {ex.Message}"); }
                
                // START HEARTBEAT / BACKGROUND SYNC
                try { _dashboard.Initialize(); } catch (Exception ex) { LogCrash($"InitDashboard Error: {ex.Message}"); }

                // Ensure default state is Productivity Dashboard
                if (!PreferenceManager.Shared.Preferences.HasCompletedOnboarding)
                {
                    ShowOnboarding();
                }
                else
                {
                    try { _navigation.ShowDashboard(); } catch (Exception ex) { LogCrash($"ShowDashboard Init Error: {ex.Message}"); }
                }

                this.Closing += (s, e) =>
                {
                    _savePositionCts?.Cancel();
                    _scaleSaveCts?.Cancel();
                    SavePosition();
                    if (_needsScaleFlush) Managers.PreferenceManager.Shared.Save();
                };
                this.Activated += (s, e) => AudioRecorder.Shared.ResetSpacingState();
                this.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.F12) LaunchDevConsole(); };
                this.Closed += (s, e) =>
                {
                    _savePositionCts?.Dispose();
                    _scaleSaveCts?.Dispose();
                    _dashboard?.Cleanup();
                };
                LoadPosition();
                this.LocationChanged += (s, e) => ScheduleSavePosition();

                // Respect Always on Top and Force Activation
                this.Activate();
                ApplyAlwaysOnTop();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Dashboard Init Failed: {ex.Message}\n{ex.StackTrace}", "Critical Error");
                LogCrash($"Dashboard Constructor Fatal: {ex.Message}");
            }
        }

        public static void LogCrash(string message) => CrashLogger.Log(message);



        private void LoadPosition()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.DashboardTop != -1)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = p.DashboardTop;
                this.Left = p.DashboardLeft;

                ValidatePosition();
            }
        }

        private void ValidatePosition()
        {
            // Ensure the window is visible on at least one screen
            bool onScreen = false;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var rect = screen.WorkingArea;
                // Check if the center of our window is within this screen
                double centerX = this.Left + (this.Width / 2);
                double centerY = this.Top + (this.Height / 2);

                if (centerX >= rect.Left && centerX <= rect.Right &&
                    centerY >= rect.Top && centerY <= rect.Bottom)
                {
                    onScreen = true;
                    break;
                }
            }

            if (!onScreen && System.Windows.Forms.Screen.PrimaryScreen != null)
            {
                // Move to primary screen center
                var primary = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                this.Left = primary.Left + (primary.Width - this.Width) / 2;
                this.Top = primary.Top + (primary.Height - this.Height) / 2;
                System.Diagnostics.Debug.WriteLine("CosmoWhisper: Window was off-screen. Resetting to Primary Monitor.");
            }
        }

        private void SavePosition()
        {
            if (this.WindowState == WindowState.Minimized) return;

            var p = PreferenceManager.Shared.Preferences;
            // Guard against invalid values during startup or minimization
            if (double.IsNaN(this.Top) || double.IsNaN(this.Left) || this.Top < -2000 || this.Left < -2000) return;

            p.DashboardTop = this.Top;
            p.DashboardLeft = this.Left;
            PreferenceManager.Shared.Save();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            bool isMax = this.WindowState == WindowState.Maximized;
            if (RootContentBorder != null)
            {
                RootContentBorder.Padding = isMax ? new Thickness(7) : new Thickness(0);
            }
            if (BtnMaximizeRestore != null)
            {
                BtnMaximizeRestore.Content = isMax ? "\uE923" : "\uE922";
                BtnMaximizeRestore.ToolTip = isMax ? "Restore" : "Maximize";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            AudioRecorder.Shared.StopMonitoring();
            System.Windows.Application.Current.Shutdown();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowEffectManager.ApplyEffect(this, BackdropType.Mica);
                InitializeAll();

            // Initialize UI Scale
            _needsScaleInit = true;
            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
               if (_needsScaleInit && SldUIScale != null)
               {
                   var savedScale = Managers.PreferenceManager.Shared.Preferences.UIScale * 100.0;
                   if (savedScale < 80) savedScale = 100;
                   SldUIScale.Value = savedScale;
                   ApplyGlobalScale(savedScale);
                   _needsScaleInit = false;
               }
            }));
                await _dashboard.CheckAuthStatus();
            }
            catch (Exception ex)
            {
                LogCrash($"InitPrefs Loaded Error: {ex.Message}");
            }
        }



        public void LoadExamples_Click(object sender, RoutedEventArgs e)
        {
             _vocabulary.LoadExamples();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowDashboard();
        }

        private void Preferences_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            _navigation.ShowPreferences();
#else
            _navigation.ShowDashboard();
#endif
        }

        private void SmartCommands_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowSmartCommands();
        }

        private void Vault_Click(object sender, RoutedEventArgs e)
        {
            _navigation.OpenVault();
        }

        public void ResetStats_Click(object sender, RoutedEventArgs e) => _dashboard.ResetStats();

        // --- Theme Management ---

        private void ThemeOcean_Click(object sender, RoutedEventArgs e) => _theme.ApplyOcean();
        private void ThemeSunset_Click(object sender, RoutedEventArgs e) => _theme.ApplySunset();
        private void ThemeForest_Click(object sender, RoutedEventArgs e) => _theme.ApplyForest();
        private void ThemePurple_Click(object sender, RoutedEventArgs e) => _theme.ApplyPurple();

        private void ApplyTheme(string accent, string glow, string secondary, string bgStart, string bgMid, string bgEnd, string surface) => _theme.ApplyTheme(accent, glow, secondary, bgStart, bgMid, bgEnd, surface);

        private void Microphone_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowMicrophone();
        }

        private void Narration_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowNarration();
        }

        private void Library_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowLibrary();
        }

        private void Account_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowAccount();
        }





        private void SetVisibility(UIElement? element, Visibility visibility) => _navigation.SetVisibility(element, visibility);

        // --- Audio Logic ---



        public void SldSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _mic?.SensitivityChanged(e.NewValue);
        public void ToggleInteractionSounds_Click(object sender, RoutedEventArgs e) => _mic.ToggleInteractionSounds();
        public void UpdateInteractionSoundsUI() => _mic.UpdateInteractionSoundsUI();

        public void ToggleAlwaysOnTop_Click(object sender, RoutedEventArgs e) => _prefs.ToggleAlwaysOnTop();
        public void ApplyAlwaysOnTop() => this.Topmost = PreferenceManager.Shared.Preferences.AlwaysOnTop;
        public void BtnSaveApiKeys_Click(object sender, RoutedEventArgs e) => _intelligence?.SaveApiKeys();
        public async void BtnCalibrate_Click(object sender, RoutedEventArgs e) => await _mic.Calibrate();
        public void ComboMics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem item)
            {
                _mic?.MicSelectionChanged(item);
            }
        }

        public void InitializeAll()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    _prefs?.Initialize();
                    _vocabulary?.Initialize();
                    _intelligence?.Initialize();
                    _narration?.Initialize();
                    _mic?.UpdateInteractionSoundsUI();
                }
                catch (Exception ex)
                {
                    LogCrash($"InitializeAll Error: {ex.Message}");
                }
            });
        }

        public void ComboOutput_SelectionChanged(object sender, SelectionChangedEventArgs e) => _narration?.OutputDeviceChanged();
        public async void BtnPlaySample_Click(object sender, RoutedEventArgs e) => await _narration.PlaySample();
        public void SldWidgetOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _prefs?.UpdateWidgetOpacity(e.NewValue);
        private bool _needsScaleInit = true;
        private bool _needsScaleFlush = false;
        private System.Threading.CancellationTokenSource? _savePositionCts;
        private System.Threading.CancellationTokenSource? _scaleSaveCts;

        private async void ScheduleSavePosition()
        {
            _savePositionCts?.Cancel();
            _savePositionCts?.Dispose();
            _savePositionCts = new System.Threading.CancellationTokenSource();
            var token = _savePositionCts.Token;
            try
            {
                await Task.Delay(400, token);
                SavePosition();
            }
            catch (System.OperationCanceledException) { }
        }

        public async void SldUIScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyGlobalScale(e.NewValue);

            var prefs = Managers.PreferenceManager.Shared.Preferences;
            if (prefs == null) return;

            double newScale = e.NewValue / 100.0;
            if (Math.Abs(prefs.UIScale - newScale) < 0.001) return;

            prefs.UIScale = newScale;
            _needsScaleFlush = true;

            _scaleSaveCts?.Cancel();
            _scaleSaveCts?.Dispose();
            _scaleSaveCts = new System.Threading.CancellationTokenSource();
            var token = _scaleSaveCts.Token;

            try
            {
                await Task.Delay(350, token);
                _needsScaleFlush = false;
                Managers.PreferenceManager.Shared.Save();
            }
            catch (System.OperationCanceledException) { }
        }

        private void ApplyGlobalScale(double sliderValue)
        {
            try
            {
                // Target the outermost visual container for best results
                if (RootContentBorder == null) return;

                double scale = sliderValue / 100.0;
                if (scale < 0.5) scale = 0.5;
                if (scale > 3.0) scale = 3.0;

                // Direct Transform Application
                var transform = new ScaleTransform(scale, scale);
                RootContentBorder.LayoutTransform = transform;

                // Update Text
                if (TxtUIScaleValue != null)
                {
                    TxtUIScaleValue.Text = $"{(int)sliderValue}%";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scale Error: {ex.Message}");
            }
        }


        private void Intelligence_Click(object sender, RoutedEventArgs e) => _navigation.ShowIntelligence();

        private void Vocabulary_Click(object sender, RoutedEventArgs e) => _navigation.ShowVocabulary();

        public void AddVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.AddVocabulary();
        public void BtnAddVocab_Click(object sender, RoutedEventArgs e) => _vocabulary.AddVocabulary();
        public void SecureMode_Click(object sender, RoutedEventArgs e) => _vocabulary.ToggleSecureMode(true);
        public void ConfirmSecureMode_Click(object sender, RoutedEventArgs e) => _vocabulary.ConfirmSecureMode();
        public void CancelSecureMode_Click(object sender, RoutedEventArgs e) => _vocabulary.ToggleSecureMode(false);
        public void TxtNewInput_TextChanged(object sender, TextChangedEventArgs e) => _vocabulary.UpdatePlaceholderVisibility((TextBox)sender);
        public void DeleteVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.DeleteVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        public void ConfirmDelete_Click(object sender, RoutedEventArgs e) => _vocabulary.ConfirmDelete();
        public void CancelDelete_Click(object sender, RoutedEventArgs e) => _vocabulary.CancelDelete();
        public void EditVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.EditVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        public void SaveVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.SaveVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        public void CancelVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.CancelVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        public void TxtSignature_TextChanged(object sender, TextChangedEventArgs e) => _vocabulary.UpdateSignature();
        public void ToggleSignOff_Click(object sender, RoutedEventArgs e) => _vocabulary.ToggleSignOff();

        private void Preferences_Click(object sender, RoutedEventArgs e) => _navigation.ShowPreferences();

        private void TabPrefGeneral_Click(object sender, RoutedEventArgs e) => _navigation.ShowPreferences();

        private void TabPrefMicrophone_Click(object sender, RoutedEventArgs e) => _navigation.ShowMicrophone();
        private void TabPrefVocabulary_Click(object sender, RoutedEventArgs e) => _navigation.ShowVocabulary();
        private void TabPrefNarration_Click(object sender, RoutedEventArgs e) => _navigation.ShowNarration();
        private void TabPrefIntelligence_Click(object sender, RoutedEventArgs e) => _navigation.ShowIntelligence();


        private void TxtVocabHints_TextChanged(object sender, TextChangedEventArgs e) { } // Handled manually in Controller, but needed for XAML compatibility



        
        public void BorderActivationTrigger_Click(object sender, MouseButtonEventArgs e) => _prefs.StartActivationCapture();
        public void BtnClearHotkey_Click(object sender, MouseButtonEventArgs e) => _prefs.ClearHotkey();

        public void BtnChangeBackup_Click(object sender, RoutedEventArgs e) => _prefs.ChangeBackupPath();
        public void BtnChangeBackupPath_Click(object sender, RoutedEventArgs e) => _prefs.ChangeBackupPath();
        public void BtnBackupNow_Click(object sender, RoutedEventArgs e) => _prefs.BackupNow(sender as System.Windows.Controls.Button);

        private TaskCompletionSource<(string? password, string? name)>? _vaultTask;
        public async Task<(string? password, string? name)> GetVaultPasswordAsync(bool isRestore = false)
        {
            // Reset state
            _vaultTask?.TrySetCanceled();
            _vaultTask = new TaskCompletionSource<(string? password, string? name)>();
            
            TxtVaultName.Text = $"Backup_{DateTime.Now:yyyyMMdd}";
            TxtVaultPassword.Password = "";
            
            // Dynamic UI
            if (TxtVaultTitle != null) TxtVaultTitle.Text = isRestore ? "Unlock Your Vault" : "Secure Your Vault";
            
            if (isRestore)
            {
                if (TxtVaultSubtitle != null) TxtVaultSubtitle.Text = "Enter the password to decrypt this backup.";
                if (LblVaultName != null) LblVaultName.Visibility = Visibility.Collapsed;
                if (TxtVaultName != null) TxtVaultName.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (TxtVaultSubtitle != null) TxtVaultSubtitle.Text = "Enter a password to encrypt your backup (Optional).";
                if (LblVaultName != null) LblVaultName.Visibility = Visibility.Visible;
                if (TxtVaultName != null) TxtVaultName.Visibility = Visibility.Visible;
            }
            
            // Clear any active animations and reset state
            VaultPasswordOverlay.BeginAnimation(OpacityProperty, null);
            if (VaultContent.RenderTransform is ScaleTransform stFix)
            {
                stFix.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                stFix.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }

            VaultPasswordOverlay.Opacity = 0;
            VaultPasswordOverlay.Visibility = Visibility.Visible;
            VaultPasswordOverlay.IsHitTestVisible = false; // Initially false to prevent race conditions

            // Animate In
            var vaultFade = new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)) { EasingFunction = new QuadraticEase() };
            var vaultScale = new DoubleAnimation(1, TimeSpan.FromMilliseconds(350)) { EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut } };
            
            VaultPasswordOverlay.BeginAnimation(OpacityProperty, vaultFade);
            if (VaultContent.RenderTransform is ScaleTransform st2)
            {
                st2.BeginAnimation(ScaleTransform.ScaleXProperty, vaultScale);
                st2.BeginAnimation(ScaleTransform.ScaleYProperty, vaultScale);
            }
            
            // Set focus and enable interaction
            await Task.Delay(200);
            VaultPasswordOverlay.IsHitTestVisible = true;
            
            if (isRestore && TxtVaultPassword != null)
            {
                 TxtVaultPassword.Focus();
            }
            else if (TxtVaultName != null)
            {
                TxtVaultName.Focus();
                TxtVaultName.SelectAll();
            }
            
            return await _vaultTask.Task;
        }

        private async Task HideVaultOverlayAsync()
        {
            VaultPasswordOverlay.IsHitTestVisible = false;
            var fadeAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase() };
            var scaleAnim = new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase() };

            VaultPasswordOverlay.BeginAnimation(OpacityProperty, fadeAnim);
            if (VaultContent.RenderTransform is ScaleTransform st)
            {
                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }

            await Task.Delay(210);
            VaultPasswordOverlay.Visibility = Visibility.Collapsed;
        }

        private async void ConfirmVault_Click(object sender, RoutedEventArgs e)
        {
            await HideVaultOverlayAsync();
            _vaultTask?.TrySetResult((TxtVaultPassword.Password, TxtVaultName.Text));
        }

        private async void CancelVault_Click(object sender, RoutedEventArgs e)
        {
            await HideVaultOverlayAsync();
            _vaultTask?.TrySetResult((null, null));
        }

        private TaskCompletionSource<bool>? _dialogTask;
        private TaskCompletionSource<string?>? _listDialogTask;
        private TaskCompletionSource<string?>? _inputDialogTask;

        public async Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "", string icon = "\uE70F")
        {
            _inputDialogTask = new TaskCompletionSource<string?>();
            TxtDialogTitle.Text = title;
            TxtDialogMessage.Text = message;
            TxtDialogIcon.Text = icon;
            
            BtnDialogCancel.Visibility = Visibility.Visible;
            System.Windows.Controls.Grid.SetColumnSpan(BtnDialogConfirm, 1);
            BtnDialogConfirm.Content = "OK";

            ListDialogOptions.Visibility = Visibility.Collapsed;
            TxtDialogInput.Visibility = Visibility.Visible;
            TxtDialogInput.Text = defaultValue;
            TxtDialogInput.Focus();
            TxtDialogInput.SelectAll();

            await AnimateDialogOpen();

            return await _inputDialogTask.Task;
        }

        public async Task<string?> ShowListDialogAsync(string title, string message, IEnumerable<string> options, string icon = "\uE8B7")
        {
            _listDialogTask = new TaskCompletionSource<string?>();
            TxtDialogTitle.Text = title;
            TxtDialogMessage.Text = message;
            TxtDialogIcon.Text = icon;
            
            BtnDialogCancel.Visibility = Visibility.Visible;
            System.Windows.Controls.Grid.SetColumnSpan(BtnDialogConfirm, 1);
            BtnDialogConfirm.Content = "Restore Selected";

            ListDialogOptions.ItemsSource = options;
            ListDialogOptions.Visibility = Visibility.Visible;
            ListDialogOptions.SelectedIndex = 0;

            _ = AnimateDialogOpen();

            return await _listDialogTask.Task;
        }

        private Task AnimateDialogOpen()
        {
            // Reset state
            UniversalDialog.BeginAnimation(OpacityProperty, null);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            
            UniversalDialog.Opacity = 0;
            DialogScale.ScaleX = 0.92;
            DialogScale.ScaleY = 0.92;
            UniversalDialog.Visibility = Visibility.Visible;
            UniversalDialog.IsHitTestVisible = true;
            
            // Animate in (GPU-composited opacity & scale only)
            var fadeAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var scaleXAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var scaleYAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            
            UniversalDialog.BeginAnimation(OpacityProperty, fadeAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);

            return Task.CompletedTask;
        }

        public async Task<bool> ShowDialogAsync(string title, string message, string icon = "\uE946", bool showCancel = false, string confirmButtonText = "Confirm")
        {
            _dialogTask = new TaskCompletionSource<bool>();
            if (TxtDialogTitle != null) TxtDialogTitle.Text = title;
            if (TxtDialogMessage != null) TxtDialogMessage.Text = message;
            if (TxtDialogIcon != null) TxtDialogIcon.Text = icon;
            if (BtnDialogCancel != null) BtnDialogCancel.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
            if (BtnDialogConfirm != null) BtnDialogConfirm.Content = confirmButtonText;
            if (ListDialogOptions != null) ListDialogOptions.Visibility = Visibility.Collapsed;
            
            if (showCancel)
            {
                if (BtnDialogConfirm != null) System.Windows.Controls.Grid.SetColumnSpan(BtnDialogConfirm, 1);
            }
            else
            {
                if (BtnDialogConfirm != null) System.Windows.Controls.Grid.SetColumnSpan(BtnDialogConfirm, 2);
            }

            await AnimateDialogOpen();

            return await _dialogTask.Task;
        }

        private async void DialogConfirm_Click(object sender, RoutedEventArgs e)
        {
            string? selected = ListDialogOptions.SelectedItem?.ToString();
            string? inputResult = TxtDialogInput.Text;
            await HideDialogAsync();
            
            _dialogTask?.TrySetResult(true);
            _listDialogTask?.TrySetResult(selected);
            _inputDialogTask?.TrySetResult(inputResult);
        }

        private async void DialogCancel_Click(object sender, RoutedEventArgs e)
        {
            await HideDialogAsync();
            _dialogTask?.TrySetResult(false);
            _listDialogTask?.TrySetResult(null);
            _inputDialogTask?.TrySetResult(null);
        }

        private async Task HideDialogAsync()
        {
            UniversalDialog.IsHitTestVisible = false;
            var fadeAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            var scaleXAnim = new DoubleAnimation(0.95, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            var scaleYAnim = new DoubleAnimation(0.95, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };

            UniversalDialog.BeginAnimation(OpacityProperty, fadeAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);

            await Task.Delay(180);
            UniversalDialog.Visibility = Visibility.Collapsed;
            TxtDialogInput.Visibility = Visibility.Collapsed;
            ListDialogOptions.Visibility = Visibility.Collapsed;
        }

        private void UniversalDialog_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent clicking through the backdrop
        }

        internal void ShowToast(string message, string icon = "\uE73E")
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



        public void ToggleRegionalSpelling_Click(object sender, RoutedEventArgs e) => _prefs.ToggleRegionalSpelling();

        public void ToggleLaunchOnStartup_Click(object sender, RoutedEventArgs e) => _prefs.ToggleLaunchOnStartup();



        public void ComboVoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboVoice == null || ComboVoice.SelectedItem == null) return;
            _narration.VoiceChanged(ComboVoice.SelectedItem);
        }

        public void ComboDateFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboDateFormat?.SelectedItem is ComboBoxItem item && item.Tag?.ToString() is string df)
                _prefs?.SetDateFormat(df);
        }

        public void ComboTimeFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboTimeFormat?.SelectedItem is ComboBoxItem item && item.Tag?.ToString() is string tf)
                _prefs?.SetTimeFormat(tf);
        }

        public void ToggleClipboard_Click(object sender, RoutedEventArgs e) => _prefs.ToggleClipboard();
        public void TogglePasteMode_Click(object sender, RoutedEventArgs e) => _prefs.TogglePasteMode();
        public void ToggleAutoSubmit_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleAutoSubmit();
        public void ToggleAutoCopy_Click(object sender, RoutedEventArgs e) => _prefs.ToggleAutoCopy();
        public void ToggleSpeechPolish_Click(object sender, RoutedEventArgs e) => _prefs.ToggleSpeechPolish();
        public void BtnRestore_Click(object sender, RoutedEventArgs e) => _prefs.RestoreBackup();
        public void ToggleLocalWhisper_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleLocalWhisper();
        public void ToggleForceCloudProxy_Click(object sender, RoutedEventArgs e) => _prefs.ToggleForceCloudProxy();
        public void ToggleForceDirectApi_Click(object sender, RoutedEventArgs e) => _prefs.ToggleForceDirectApi();
        public void ToggleLocalOnly_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleLocalOnly();
        public void ComboLocalVoiceEngine_SelectionChanged(object sender, SelectionChangedEventArgs e) => _prefs.LocalVoiceEngineChanged(sender, e);
        public void StartActivationCapture_Click(object sender, MouseButtonEventArgs e) => _prefs.StartActivationCapture();
        public void ResetHotkey_Click(object sender, MouseButtonEventArgs e) => _prefs.ClearHotkey();


        public void VisitDocs_Click(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://CosmoWhisper.com/faq",
                UseShellExecute = true
            });
        }

        private void VisitGitHub_Click(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Viguru24/CosmoWhisper-Native",
                UseShellExecute = true
            });
        }



        // --- Account & Login Logic ---

        // private async void PerformLogin_Click(object sender, RoutedEventArgs e) => await _dashboard.PerformLogin(TxtLoginEmail.Text, TxtLoginPassword.Password);
        public void ActivateLicense_Click(object sender, RoutedEventArgs e) => _dashboard.ActivateLicense(TxtLicense.Text);
        public void SignOut_Click(object sender, RoutedEventArgs e) => _dashboard.SignOut();
        public async void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            await _dashboard.CheckAuthStatus();
            ShowToast("Account Status Refreshed", "\uE72C");
        }

        public void UpgradeToPro_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://CosmoWhisper.com/pricing", UseShellExecute = true });
        public void SignUp_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://CosmoWhisper.com/register";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        
        public void VisitLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string path)
            {
                string targetUrl = path == "/discord" ? "https://discord.gg/mb5AUNCr" : "https://CosmoWhisper.com" + path;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = targetUrl, UseShellExecute = true });
            }
        }

        public void GetFullAccess_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
            { 
                FileName = "https://CosmoWhisper.com/pricing", 
                UseShellExecute = true 
            });
        }

        public void GoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            string baseUrl = PreferenceManager.Shared.Preferences.BackendUrl.TrimEnd('/');
            // Pass platform=windows-dev if in dev mode so the backend knows to use the -dev protocol deep link
            string platform = PreferenceManager.Shared.IsDevMode ? "windows-dev" : "windows";
            string authUrl = $"{baseUrl}/api/auth/google?platform={platform}";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
            { 
                FileName = authUrl, 
                UseShellExecute = true 
            });
        }

        public async void MagicLink_Click(object sender, RoutedEventArgs e)
        {
             await _dashboard.PerformMagicLogin("");
        }

        // Removed Provider logic
        
        public void UpdatePersonalityUI() => _intelligence?.UpdatePersonalityUI();
        
        public void ComboPersonality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboPersonality?.SelectedItem is ComboBoxItem item)
            {
                _intelligence?.PersonalityChanged(item.Tag?.ToString() ?? "None");
            }
        }

        public void ComboVerbosity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboVerbosity?.SelectedItem is ComboBoxItem item)
            {
                _intelligence?.VerbosityChanged(item.Tag?.ToString() ?? "Balanced");
            }
        }
        private void UpdateDashboardStats() => _dashboard?.UpdateDashboardStats();
        public async Task CheckAuthStatus() => await _dashboard.CheckAuthStatus();

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 1. Priority: Hotkey Capture Mode Override
            if (_prefs.IsCapturingHotkey)
            {
                // Escape cancels hotkey capture
                if (e.Key == Key.Escape)
                {
                    _prefs.StopActivationCapture();
                    e.Handled = true;
                }
                return; // Let HandleUniversalCapture deal with other keys via KeyDown
            }

            // 2. Handle Escape Key (Cancel/Close Actions)
            if (e.Key == Key.Escape)
            {
                if (OverlayConfirmDelete.Visibility == Visibility.Visible)
                {
                    CancelDelete_Click(null!, null!);
                    e.Handled = true;
                }
                else if (OverlaySecureMode.Visibility == Visibility.Visible)
                {
                    CancelSecureMode_Click(null!, null!);
                    e.Handled = true;
                }
                else if (VaultPasswordOverlay.Visibility == Visibility.Visible)
                {
                    CancelVault_Click(null!, null!);
                    e.Handled = true;
                }
                else if (UniversalDialog.Visibility == Visibility.Visible && UniversalDialog.IsHitTestVisible)
                {
                    DialogCancel_Click(null!, null!);
                    e.Handled = true;
                }
            }
            // 3. Handle Enter Key (Confirm/Submit Actions)
            else if (e.Key == Key.Enter)
            {
                if (OverlayConfirmDelete.Visibility == Visibility.Visible)
                {
                    ConfirmDelete_Click(null!, null!);
                    e.Handled = true;
                }
                else if (OverlaySecureMode.Visibility == Visibility.Visible)
                {
                    ConfirmSecureMode_Click(null!, null!);
                    e.Handled = true;
                }
                else if (OverlayOnboarding.Visibility == Visibility.Visible)
                {
                    if (_currentOnboardingStep < 3)
                        OnboardingNext_Click(null!, null!);
                    else
                        OnboardingComplete_Click(null!, null!);
                    e.Handled = true;
                }
                else if (VaultPasswordOverlay.Visibility == Visibility.Visible && VaultPasswordOverlay.IsHitTestVisible)
                {
                    ConfirmVault_Click(null!, null!);
                    e.Handled = true;
                }
                else if (UniversalDialog.Visibility == Visibility.Visible && UniversalDialog.IsHitTestVisible)
                {
                    DialogConfirm_Click(null!, null!);
                    e.Handled = true;
                }
                else if (LoginView.Visibility == Visibility.Visible)
                {
                    // Login view logic removed since auth is out-of-band via browser
                }
                else if (AccountView.Visibility == Visibility.Visible)
                {
                    if (TxtLicense.IsFocused)
                    {
                        ActivateLicense_Click(null!, null!);
                        e.Handled = true;
                    }
                }
                else if (VocabularyView.Visibility == Visibility.Visible)
                {
                    if (TxtNewKey?.IsFocused == true)
                    {
                        AddVocabulary_Click(null!, null!);
                        e.Handled = true;
                    }
                    else if (TxtNewValue?.IsFocused == true)
                    {
                        // Allow Enter for new lines in replacement
                        // Only submit on Enter if user didn't request multi-line or is holding Ctrl?
                        // For now, let's just let it be multi-line since that's what the user wants.
                    }
                }
            }
        }
        #region Onboarding Logic
        private int _currentOnboardingStep = 1;

        public async void ShowOnboarding()
        {
            OverlayOnboarding.Visibility = Visibility.Visible;
            OverlayOnboarding.Opacity = 0;

            var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));
            OverlayOnboarding.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Populate mics for onboarding
            await _mic.InitializeMicrophones();
            if (SldOnboardingSensitivity != null) SldOnboardingSensitivity.Value = AudioRecorder.Shared.Sensitivity * 100;
        }

        private void UpdateGroqConfigUI()
        {
            // Removed stale onboarding references
        }

        private async void OnboardingNext_Click(object sender, RoutedEventArgs e)
        {
            _currentOnboardingStep++;
            await SwitchOnboardingStep(_currentOnboardingStep);
        }

        private async Task SwitchOnboardingStep(int step)
        {
            // Hide all steps with a small fade out
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
            
            StepWelcome.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            StepMic.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            StepActivation.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            StepVocabulary.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            StepIntelligence.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            StepLibrary.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            await Task.Delay(200);

            StepWelcome.Visibility = Visibility.Collapsed;
            StepMic.Visibility = Visibility.Collapsed;
            StepActivation.Visibility = Visibility.Collapsed;
            StepVocabulary.Visibility = Visibility.Collapsed;
            StepIntelligence.Visibility = Visibility.Collapsed;
            StepLibrary.Visibility = Visibility.Collapsed;

            Grid target = step switch
            {
                1 => StepWelcome,
                2 => StepMic,
                3 => StepActivation,
                4 => StepVocabulary,
                5 => StepIntelligence,
                6 => StepLibrary,
                _ => StepWelcome
            };

            target.Visibility = Visibility.Visible;
            target.Opacity = 0;

            var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300));
            target.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        public void OnboardingGetGroq_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://console.groq.com/keys") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening Groq URL: {ex.Message}");
                _ = CosmoMessage.Show("Error", "Could not open browser. Please visit console.groq.com manually.", "\uE783");
            }
        }

        public void OpenLibraryWeb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://CosmoWhisper.com/commands") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening Library URL: {ex.Message}");
                _ = CosmoMessage.Show("Error", "Could not open browser. Please visit CosmoWhisper.com manually.", "\uE783");
            }
        }


        private void OnboardingSkip_Click(object sender, RoutedEventArgs e)
        {
            CompleteOnboarding();
        }

        private void OnboardingComplete_Click(object sender, RoutedEventArgs e)
        {
            // Save settings from onboarding
            if (ComboOnboardingMics.SelectedItem is ComboBoxItem mic)
                _mic.MicSelectionChanged(mic);

            _mic.SensitivityChanged(SldOnboardingSensitivity.Value);

            CompleteOnboarding();
        }

        private void CompleteOnboarding()
        {
            PreferenceManager.Shared.Preferences.HasCompletedOnboarding = true;
            PreferenceManager.Shared.Save();

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => OverlayOnboarding.Visibility = Visibility.Collapsed;
            OverlayOnboarding.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            _navigation.ShowDashboard();
            
            _ = CosmoMessage.Show("Setup Complete", "Welcome to CosmoWhisper! You can change these settings anytime in the sidebar.", "\uE73E");
        }
        public void RestartSetup_Click(object sender, RoutedEventArgs e)
        {
            ShowOnboarding();
        }

        public static void LaunchDevConsole()
        {
            try
            {
                string script = @"
$Host.UI.RawUI.WindowTitle = 'CosmoWhisper Live Dev Console'
Clear-Host
Write-Host '=======================================================' -ForegroundColor DarkCyan
Write-Host '       COSMOWHISPER LIVE DEV LOGS & DIAGNOSTICS         ' -ForegroundColor Cyan
Write-Host '=======================================================' -ForegroundColor DarkCyan
$appLog = ""$env:APPDATA\CosmoWhisper_Dev\logs\cosmo_debug.log""
if (-not (Test-Path $appLog)) {
    New-Item -ItemType File -Path $appLog -Force | Out-Null
}
Get-Content -Path $appLog -Wait -Tail 30 | ForEach-Object {
    if ($_ -match 'whisper_init_state|whisper_backend_init_gpu|kv self size|compute buffer') { return }
    if ($_ -match 'Error:|failed|Exception' -and $_ -notmatch 'Whisper.net') { Write-Host $_ -ForegroundColor Red }
    elseif ($_ -match 'TEST|ProcessCommand|Direct|groq|gpt|openai|Status: OK') { Write-Host $_ -ForegroundColor Yellow }
    elseif ($_ -match 'Transcription result|Original|Processed') { Write-Host $_ -ForegroundColor Green }
    else { Write-Host $_ -ForegroundColor Gray }
}";
                string tempScript = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CosmoDevConsole.ps1");
                System.IO.File.WriteAllText(tempScript, script);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{tempScript}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        #endregion

    }
}
