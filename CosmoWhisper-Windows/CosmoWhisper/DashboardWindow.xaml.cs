using System;
using System.Diagnostics;
using CosmoWhisper.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.IO;
using System.Linq;
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

        public DashboardWindow()
        {
            Instance = this;
            try
            {
                InitializeComponent();
                _navigation = new NavigationController(this);
                _vocabulary = new VocabularyController(this);
                _prefs = new PreferencesController(this);
                _intelligence = new IntelligenceController(this);
                _narration = new NarrationController(this);
                _mic = new MicrophoneController(this);
                _theme = new ThemeController(this);
                _dashboard = new DashboardController(this);

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
                };

                try { _narration.Initialize(); } catch (Exception ex) { LogCrash($"InitVoices Error: {ex.Message}"); }
                try { _mic.Initialize(); } catch (Exception ex) { LogCrash($"InitMics Error: {ex.Message}"); }

                // Init UI values
                if (SldSensitivity != null) SldSensitivity.Value = AudioRecorder.Shared.Sensitivity * 100;
                try { _mic.UpdateInteractionSoundsUI(); } catch (Exception ex) { LogCrash($"UpdateSoundsUI Error: {ex.Message}"); }

                // Ensure default state is Productivity Dashboard
                try { _navigation.ShowDashboard(); } catch (Exception ex) { LogCrash($"ShowDashboard Init Error: {ex.Message}"); }

                this.LocationChanged += (s, e) => SavePosition();
                this.Closing += (s, e) => SavePosition();
                this.Closed += (s, e) =>
                {
                    _dashboard?.Cleanup();
                };
                LoadPosition();

                // FORCE TO FRONT ON RESTART
                this.Topmost = true;
                this.Activate();
                Task.Delay(500).ContinueWith(_ => Dispatcher.Invoke(() => this.Topmost = false));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Dashboard Init Failed: {ex.Message}\n{ex.StackTrace}", "Critical Error");
                LogCrash($"Dashboard Constructor Fatal: {ex.Message}");
            }
        }

        public static void LogCrash(string message)
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
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                BlurManager.ApplyMica(this);
                InitializeAll();

            // Initialize UI Scale
            _needsScaleInit = true;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
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
                AudioRecorder.Shared.StartMonitoring();

                // Initialize API key field as locked
                if (TxtGroqApiKey != null) TxtGroqApiKey.IsEnabled = false;
            }
            catch (Exception ex)
            {
                LogCrash($"InitPrefs Loaded Error: {ex.Message}");
            }
        }



        private void LoadExamples_Click(object sender, RoutedEventArgs e)
        {
             _vocabulary.LoadExamples();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowDashboard();
        }

        private void SmartCommands_Click(object sender, RoutedEventArgs e)
        {
            _navigation.ShowSmartCommands();
        }

        private void Vault_Click(object sender, RoutedEventArgs e)
        {
            _navigation.OpenVault();
        }

        private void ResetStats_Click(object sender, RoutedEventArgs e) => _dashboard.ResetStats();

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



        private void SldSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _mic?.SensitivityChanged(e.NewValue);
        private void ToggleInteractionSounds_Click(object sender, MouseButtonEventArgs e) => _mic.ToggleInteractionSounds();
        private void UpdateInteractionSoundsUI() => _mic.UpdateInteractionSoundsUI();
        private async void BtnCalibrate_Click(object sender, RoutedEventArgs e) => await _mic.Calibrate();
        private void ComboMics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem item)
            {
                _mic?.MicSelectionChanged(item);
            }
        }

        public void InitializeAll()
        {
            try
            {
                _prefs?.Initialize();
                _vocabulary?.Initialize();
                _intelligence?.Initialize();
                _narration?.Initialize();
                // We don't re-init mic here as it might disrupt recording
            }
            catch (Exception ex)
            {
                LogCrash($"InitializeAll Error: {ex.Message}");
            }
        }

        private void SldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _narration?.SpeedChanged(e.NewValue);
        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _narration?.VolumeChanged(e.NewValue);
        private void SldPitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _narration?.PitchChanged(e.NewValue);
        private async void BtnPlaySample_Click(object sender, RoutedEventArgs e) => await _narration.PlaySample();
        private void SldWidgetOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _prefs?.UpdateWidgetOpacity(e.NewValue);
        private bool _needsScaleInit = true;

        private void SldUIScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
             ApplyGlobalScale(e.NewValue);
             
             // Save preference (throttle if needed, but simple save is fine for now)
             var prefs = Managers.PreferenceManager.Shared.Preferences;
             if (prefs != null)
             {
                 prefs.UIScale = e.NewValue / 100.0;
                 Managers.PreferenceManager.Shared.Save();
             }
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

        private void AddVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.AddVocabulary();
        private void SecureMode_Click(object sender, RoutedEventArgs e) => _vocabulary.ToggleSecureMode(true);
        private void ConfirmSecureMode_Click(object sender, RoutedEventArgs e) => _vocabulary.ConfirmSecureMode();
        private void CancelSecureMode_Click(object sender, RoutedEventArgs e) => _vocabulary.ToggleSecureMode(false);
        private void TxtNewInput_TextChanged(object sender, TextChangedEventArgs e) => _vocabulary.UpdatePlaceholderVisibility((TextBox)sender);
        private void DeleteVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.DeleteVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        private void ConfirmDelete_Click(object sender, RoutedEventArgs e) => _vocabulary.ConfirmDelete();
        private void CancelDelete_Click(object sender, RoutedEventArgs e) => _vocabulary.CancelDelete();
        private void EditVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.EditVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        private void SaveVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.SaveVocabulary(((System.Windows.Controls.Button)sender).DataContext);
        private void CancelVocabulary_Click(object sender, RoutedEventArgs e) => _vocabulary.CancelVocabulary(((System.Windows.Controls.Button)sender).DataContext);

        private void Preferences_Click(object sender, RoutedEventArgs e) => _navigation.ShowPreferences();

        private void Language_Click(object sender, RoutedEventArgs e) => _navigation.ShowLanguage();
        private void TxtVocabHints_TextChanged(object sender, TextChangedEventArgs e) { } // Handled manually in Controller, but needed for XAML compatibility



        
        private void BorderActivationTrigger_Click(object sender, MouseButtonEventArgs e) => _prefs.StartActivationCapture();
        private void BtnClearHotkey_Click(object sender, MouseButtonEventArgs e) => _prefs.ClearHotkey();

        private void BtnChangeBackup_Click(object sender, RoutedEventArgs e) => _prefs.ChangeBackupPath();
        private void BtnBackupNow_Click(object sender, RoutedEventArgs e) => _prefs.BackupNow(sender as System.Windows.Controls.Button);

        private TaskCompletionSource<(string? password, string? name)>? _vaultTask;
        public async Task<(string? password, string? name)> GetVaultPasswordAsync(bool isRestore = false)
        {
            _vaultTask = new TaskCompletionSource<(string? password, string? name)>();
            TxtVaultName.Text = "";
            TxtVaultPassword.Password = "";
            
            // Dynamic UI
            TxtVaultTitle.Text = isRestore ? "ðŸ”“ Unlock Your Vault" : "ðŸ” Secure Your Vault";
            LblVaultName.Visibility = isRestore ? Visibility.Collapsed : Visibility.Visible;
            TxtVaultName.Visibility = isRestore ? Visibility.Collapsed : Visibility.Visible;
            
            VaultPasswordOverlay.Visibility = Visibility.Visible;
            
            if (isRestore) TxtVaultPassword.Focus();
            else TxtVaultName.Focus();
            
            return await _vaultTask.Task;
        }

        private void ConfirmVault_Click(object sender, RoutedEventArgs e)
        {
            VaultPasswordOverlay.Visibility = Visibility.Collapsed;
            _vaultTask?.SetResult((TxtVaultPassword.Password, TxtVaultName.Text));
        }

        private void CancelVault_Click(object sender, RoutedEventArgs e)
        {
            VaultPasswordOverlay.Visibility = Visibility.Collapsed;
            _vaultTask?.SetResult((null, null));
        }

        private TaskCompletionSource<bool>? _dialogTask;
        private TaskCompletionSource<string?>? _listDialogTask;

        public async Task<string?> ShowListDialogAsync(string title, string message, IEnumerable<string> options, string icon = "ðŸ“‚")
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

            await AnimateDialogOpen();

            return await _listDialogTask.Task;
        }

        private async Task AnimateDialogOpen()
        {
            // Reset transform and opacity
            UniversalDialog.Opacity = 0;
            DialogScale.ScaleX = 0.8;
            DialogScale.ScaleY = 0.8;
            UniversalDialog.Visibility = Visibility.Visible;

            // Animate in
            var fadeAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var scaleXAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } };
            var scaleYAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } };
            
            // Add background blur
            var blur = new BlurEffect { Radius = 0 };
            RootContentBorder.Effect = blur;
            var blurAnim = new DoubleAnimation(10, TimeSpan.FromMilliseconds(500)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            blur.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);

            UniversalDialog.BeginAnimation(OpacityProperty, fadeAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        public async Task<bool> ShowDialogAsync(string title, string message, string icon = "âœ¨", bool showCancel = false)
        {
            _dialogTask = new TaskCompletionSource<bool>();
            TxtDialogTitle.Text = title;
            TxtDialogMessage.Text = message;
            TxtDialogIcon.Text = icon;
            BtnDialogCancel.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
            BtnDialogConfirm.Content = "Confirm";
            ListDialogOptions.Visibility = Visibility.Collapsed;
            
            if (showCancel)
            {
                System.Windows.Controls.Grid.SetColumnSpan(BtnDialogConfirm, 1);
            }
            else
            {
                System.Windows.Controls.Grid.SetColumnSpan(BtnDialogConfirm, 2);
            }

            await AnimateDialogOpen();

            return await _dialogTask.Task;
        }

        private async void DialogConfirm_Click(object sender, RoutedEventArgs e)
        {
            string? selected = ListDialogOptions.SelectedItem?.ToString();
            await HideDialogAsync();
            
            _dialogTask?.SetResult(true);
            _listDialogTask?.SetResult(selected);
        }

        private async void DialogCancel_Click(object sender, RoutedEventArgs e)
        {
            await HideDialogAsync();
            _dialogTask?.SetResult(false);
            _listDialogTask?.SetResult(null);
        }

        private async Task HideDialogAsync()
        {
            var fadeAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            var scaleXAnim = new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            var scaleYAnim = new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };

            if (RootContentBorder.Effect is BlurEffect blur)
            {
                var blurAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                blur.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);
            }

            UniversalDialog.BeginAnimation(OpacityProperty, fadeAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);

            await Task.Delay(300);
            UniversalDialog.Visibility = Visibility.Collapsed;
            RootContentBorder.Effect = null;
        }

        private void UniversalDialog_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent clicking through the backdrop
        }

        internal void ShowToast(string message, string icon = "âœ¨")
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



        private void ToggleRegionalSpelling_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleRegionalSpelling();

        private void ToggleLaunchOnStartup_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleLaunchOnStartup();

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboLanguage?.SelectedItem is ComboBoxItem item) _prefs?.SetLanguage(item.Tag?.ToString());
        }

        private void ComboVoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboVoice == null || ComboVoice.SelectedItem == null) return;
            var item = ComboVoice.SelectedItem as ComboBoxItem;
            var content = item?.Content?.ToString() ?? "";
            _narration.VoiceChanged(content);
        }

        private void ComboDateTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboDateTime?.SelectedItem is ComboBoxItem item) _prefs?.SetDateFormat(item.Tag?.ToString());
        }

        private void ToggleClipboard_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleClipboard();
        private void ToggleAutoSubmit_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleAutoSubmit();
        private void ToggleAutoCopy_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleAutoCopy();
        private void FastPaste_Click(object sender, MouseButtonEventArgs e) => _prefs.SetInsertionMode(InsertionMethod.FastPaste);
        private void DirectType_Click(object sender, MouseButtonEventArgs e) => _prefs.SetInsertionMode(InsertionMethod.DirectTyping);
        private void BtnRestore_Click(object sender, RoutedEventArgs e) => _prefs.RestoreBackup();
        private void ToggleManusAgent_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleManusAgent();
        private void ToggleManusNarration_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleManusNarration();

        private void OpenLibraryWeb_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://cosmowhisper.com/library",
                UseShellExecute = true
            });
        }

        private void VisitDocs_Click(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://cosmowhisper.com/faq",
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

        private async void PerformLogin_Click(object sender, RoutedEventArgs e) => await _dashboard.PerformLogin(TxtLoginEmail.Text, TxtLoginPassword.Password);
        private void ActivateLicense_Click(object sender, RoutedEventArgs e) => _dashboard.ActivateLicense(TxtLicense.Text);
        private void SignOut_Click(object sender, RoutedEventArgs e) => _dashboard.SignOut();

        private void SignUp_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://cosmowhisper.com/signup", UseShellExecute = true });

        private void ComboAIProvider_SelectionChanged(object sender, SelectionChangedEventArgs e) => _intelligence?.ProviderChanged(((ComboBoxItem)ComboAIProvider.SelectedItem)?.Tag?.ToString());
        private void TxtUnlockCode_TextChanged(object sender, TextChangedEventArgs e) => _intelligence?.HandleUnlockCode(TxtUnlockCode.Text);
        private void BtnToggleLock_Click(object sender, RoutedEventArgs e) => _intelligence?.ToggleLock();
        private void UpdateGroqStatusUI() => _intelligence?.UpdateGroqStatusUI();
        private void UpdatePersonalityUI() => _intelligence?.UpdatePersonalityUI();
        private void Personality_Click(object sender, RoutedEventArgs e) => _intelligence?.Personality_Click(((System.Windows.Controls.Button)sender).Tag?.ToString());
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
                    CancelDelete_Click(null, null);
                    e.Handled = true;
                }
                else if (OverlaySecureMode.Visibility == Visibility.Visible)
                {
                    CancelSecureMode_Click(null, null);
                    e.Handled = true;
                }
                else if (VaultPasswordOverlay.Visibility == Visibility.Visible)
                {
                    CancelVault_Click(null, null);
                    e.Handled = true;
                }
                else if (UniversalDialog.Visibility == Visibility.Visible)
                {
                    DialogCancel_Click(null, null);
                    e.Handled = true;
                }
            }
            // 3. Handle Enter Key (Confirm/Submit Actions)
            else if (e.Key == Key.Enter)
            {
                if (OverlayConfirmDelete.Visibility == Visibility.Visible)
                {
                    ConfirmDelete_Click(null, null);
                    e.Handled = true;
                }
                else if (OverlaySecureMode.Visibility == Visibility.Visible)
                {
                    ConfirmSecureMode_Click(null, null);
                    e.Handled = true;
                }
                else if (VaultPasswordOverlay.Visibility == Visibility.Visible)
                {
                    ConfirmVault_Click(null, null);
                    e.Handled = true;
                }
                else if (UniversalDialog.Visibility == Visibility.Visible)
                {
                    DialogConfirm_Click(null, null);
                    e.Handled = true;
                }
                else if (LoginView.Visibility == Visibility.Visible)
                {
                    if (TxtLoginEmail.IsFocused || TxtLoginPassword.IsFocused)
                    {
                        PerformLogin_Click(null, null);
                        e.Handled = true;
                    }
                }
                else if (AccountView.Visibility == Visibility.Visible)
                {
                    if (TxtLicense.IsFocused)
                    {
                        ActivateLicense_Click(null, null);
                        e.Handled = true;
                    }
                }
                else if (VocabularyView.Visibility == Visibility.Visible)
                {
                    if (TxtNewKey.IsFocused || TxtNewValue.IsFocused)
                    {
                        AddVocabulary_Click(null, null);
                        e.Handled = true;
                    }
                }
            }
        }
    }

    public static class CosmoMessage
    {
        public static async Task<bool> Show(string title, string message, string icon = "âœ¨", bool showCancel = false)
        {
            if (DashboardWindow.Instance != null)
            {
                return await DashboardWindow.Instance.ShowDialogAsync(title, message, icon, showCancel);
            }
            
            // Fallback for cases where main window isn't loaded
            var res = MessageBox.Show(message, title, showCancel ? MessageBoxButton.YesNo : MessageBoxButton.OK);
            return res == MessageBoxResult.Yes || res == MessageBoxResult.OK;
        }
    }
}
