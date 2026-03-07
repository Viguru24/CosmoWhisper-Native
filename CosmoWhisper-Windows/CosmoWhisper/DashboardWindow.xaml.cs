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
        private TaskCompletionSource<bool>? _dialogTask;
        private TaskCompletionSource<string?>? _listDialogTask;
        private TaskCompletionSource<string?>? _inputDialogTask;

        public DashboardWindow()
        {
            Instance = this;
            try
            {
                InitializeComponent();

#if DEBUG
                if (SidebarGearIcon != null) SidebarGearIcon.Foreground = Brushes.Gold; // Yellow
#else
                if (SidebarGearIcon != null) SidebarGearIcon.Foreground = Brushes.Gray; // Gray
#endif
                if (BtnPreferences != null) BtnPreferences.Visibility = Visibility.Visible;

                DataContext = PreferenceManager.Shared.Preferences;
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
                    Dispatcher.Invoke(() => _ = CosmoMessage.Show("Audio System", msg, "??"));
                };


                try { _narration.Initialize(); } catch (Exception ex) { LogCrash($"InitVoices Error: {ex.Message}"); }
                try { _mic.Initialize(); } catch (Exception ex) { LogCrash($"InitMics Error: {ex.Message}"); }

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

                // --- UI Event Wireup ---
                UIEvents.Shared.RequestMessageBox += async (title, msg, icon, showCancel, cancelText) => 
                {
                    return await Dispatcher.InvokeAsync(() => ShowDialogAsync(title, msg, icon, showCancel, "OK")).Result;
                };

                UIEvents.Shared.RequestOpenUrl += (url) => 
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
                };

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
                string logDir = System.IO.Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = System.IO.Path.Combine(logDir, "crash.log");
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
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
                WindowEffectManager.ApplyEffect(this, BackdropType.Mica);
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

        private void ResetStats_Click(object sender, RoutedEventArgs e) => _dashboard.ResetStats();

        // --- Theme Management ---

        private void ThemeOcean_Click(object sender, RoutedEventArgs e) => _theme.ApplyOcean();
        private void ThemeSunset_Click(object sender, RoutedEventArgs e) => _theme.ApplySunset();
        private void ThemeForest_Click(object sender, RoutedEventArgs e) => _theme.ApplyForest();
        private void ThemePurple_Click(object sender, RoutedEventArgs e) => _theme.ApplyPurple();

        private void ApplyTheme(string accent, string glow, string secondary, string bgStart, string bgMid, string bgEnd, string surface) => _theme.ApplyTheme(accent, glow, secondary, bgStart, bgMid, bgEnd, surface);
        
        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://discord.gg/mb5AUNCr",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogCrash($"Discord link error: {ex.Message}");
            }
        }

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

        private void ComboOutput_SelectionChanged(object sender, SelectionChangedEventArgs e) => _narration?.OutputDeviceChanged();
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


        private void TxtVocabHints_TextChanged(object sender, TextChangedEventArgs e) { } // Handled manually in Controller, but needed for XAML compatibility



        
        private void BorderActivationTrigger_Click(object sender, MouseButtonEventArgs e) => _prefs.StartActivationCapture();
        private void BtnClearHotkey_Click(object sender, MouseButtonEventArgs e) => _prefs.ClearHotkey();

        private void BtnChangeBackup_Click(object sender, RoutedEventArgs e) => _prefs.ChangeBackupPath();
        private void BtnBackupNow_Click(object sender, RoutedEventArgs e) => _prefs.BackupNow(sender as System.Windows.Controls.Button);

        private TaskCompletionSource<(string? password, string? name)>? _vaultTask;
        public async Task<(string? password, string? name)> GetVaultPasswordAsync(bool isRestore = false)
        {
            // Reset state
            _vaultTask?.TrySetCanceled();
            _vaultTask = new TaskCompletionSource<(string? password, string? name)>();
            
            TxtVaultName.Text = $"Backup_{DateTime.Now:yyyyMMdd}";
            TxtVaultPassword.Password = "";
            
            // Dynamic UI
            if (TxtVaultTitle != null) TxtVaultTitle.Text = isRestore ? "\uD83D\uDD13 Unlock Your Vault" : "\uD83D\uDD12 Secure Your Vault";
            
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

        public async Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "", string icon = "\uD83D\uDCDD")
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

        public async Task<string?> ShowListDialogAsync(string title, string message, IEnumerable<string> options, string icon = "\uD83D\uDCC2")
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
            // Reset state
            UniversalDialog.BeginAnimation(OpacityProperty, null);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            UniversalDialog.Opacity = 0;
            DialogScale.ScaleX = 0.8;
            DialogScale.ScaleY = 0.8;
            UniversalDialog.Visibility = Visibility.Visible;
            UniversalDialog.IsHitTestVisible = true;

            // Animate in
            var fadeAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var scaleXAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } };
            var scaleYAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } };
            
            // Add background blur
            var blur = new BlurEffect { Radius = 0 };
            RootContentBorder.Effect = blur;
            var blurAnim = new DoubleAnimation(10, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            blur.BeginAnimation(BlurEffect.RadiusProperty, blurAnim);

            UniversalDialog.BeginAnimation(OpacityProperty, fadeAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            DialogScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        public async Task<bool> ShowDialogAsync(string title, string message, string icon = "\u2728", bool showCancel = false, string confirmButtonText = "Confirm")
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
            TxtDialogInput.Visibility = Visibility.Collapsed;
            ListDialogOptions.Visibility = Visibility.Collapsed;
            RootContentBorder.Effect = null;
        }

        private void UniversalDialog_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent clicking through the backdrop
        }

        internal void ShowToast(string message, string icon = "\u2728")
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



        private void ComboVoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboVoice == null || ComboVoice.SelectedItem == null) return;
            _narration.VoiceChanged(ComboVoice.SelectedItem);
        }

        private void ComboDateFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboDateFormat?.SelectedItem is ComboBoxItem item) _prefs?.SetDateFormat(item.Tag?.ToString());
        }

        private void ComboTimeFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboTimeFormat?.SelectedItem is ComboBoxItem item) _prefs?.SetTimeFormat(item.Tag?.ToString());
        }

        private void ToggleClipboard_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleClipboard();
        private void ToggleAutoSubmit_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleAutoSubmit();
        private void ToggleAutoCopy_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleAutoCopy();
        private void FastPaste_Click(object sender, MouseButtonEventArgs e) => _prefs.SetInsertionMode(InsertionMethod.FastPaste);
        private void DirectType_Click(object sender, MouseButtonEventArgs e) => _prefs.SetInsertionMode(InsertionMethod.DirectTyping);
        private void BtnRestore_Click(object sender, RoutedEventArgs e) => _prefs.RestoreBackup();
        private void ToggleManusAgent_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleManusAgent();
        private void ToggleManusNarration_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleManusNarration();
        private void ToggleLocalWhisper_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleLocalWhisper();
        private void ToggleForceCloudProxy_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleForceCloudProxy();
        private void ToggleLocalOnly_Click(object sender, MouseButtonEventArgs e) => _prefs.ToggleLocalOnly();

        private void OpenLibraryWeb_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://CosmoWhisper.com/features",
                UseShellExecute = true
            });
        }

        private void VisitDocs_Click(object sender, MouseButtonEventArgs e)
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

        private void SaveApiKey_Click(object sender, RoutedEventArgs e) => _prefs.SaveApiKey();
        private void DeleteApiKey_Click(object sender, RoutedEventArgs e) => _prefs.DeleteApiKey();
        private void GetOpenAIKey_Click(object sender, RoutedEventArgs e) => _prefs.OpenOpenAIKeyLink();


        // --- Account & Login Logic ---

        private void ActivateLicense_Click(object sender, RoutedEventArgs e) => _dashboard.ActivateLicense(TxtLicense.Text);
        private void SignOut_Click(object sender, RoutedEventArgs e) => _dashboard.SignOut();

        private void UpgradeToPro_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://CosmoWhisper.com/pricing", UseShellExecute = true });
        private void SignUp_Click(object sender, RoutedEventArgs e) 
        {
            string url = "https://CosmoWhisper.com/register";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        
        private void VisitLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string path)
            {
                string url = path.StartsWith("http") ? path : (path == "/discord" ? "https://discord.gg/mb5AUNCr" : "https://CosmoWhisper.com" + path);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }

        private void GetFullAccess_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
            { 
                FileName = "https://CosmoWhisper.com/pricing", 
                UseShellExecute = true 
            });
        }

        private void GoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            string baseUrl = PreferenceManager.Shared.Preferences.BackendUrl.TrimEnd('/');
            string authUrl = $"{baseUrl}/api/auth/google";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
            { 
                FileName = authUrl, 
                UseShellExecute = true 
            });
        }


        // Removed Provider logic
        
        private void TxtUnlockCode_TextChanged(object sender, TextChangedEventArgs e) => _intelligence?.HandleUnlockCode(TxtUnlockCode.Text);
        private void BtnToggleLock_Click(object sender, RoutedEventArgs e) => _intelligence?.ToggleLock();
        private void UpdateGroqStatusUI() => _intelligence?.UpdateGroqStatusUI();
        private void UpdatePersonalityUI() => _intelligence?.UpdatePersonalityUI();
        
        private void ComboPersonality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboPersonality?.SelectedItem is ComboBoxItem item)
            {
                _intelligence?.PersonalityChanged(item.Tag?.ToString() ?? "None");
            }
        }

        private void ComboVerbosity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboVerbosity?.SelectedItem is ComboBoxItem item)
            {
                _intelligence?.VerbosityChanged(item.Tag?.ToString() ?? "Balanced");
            }
        }
        private void TogglePremiumAccess_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => _intelligence?.TogglePremiumAccess();
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
                else if (UniversalDialog.Visibility == Visibility.Visible && UniversalDialog.IsHitTestVisible)
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
                else if (OverlayOnboarding.Visibility == Visibility.Visible)
                {
                    if (_currentOnboardingStep < 3)
                        OnboardingNext_Click(null, null);
                    else
                        OnboardingComplete_Click(null, null);
                    e.Handled = true;
                }
                else if (VaultPasswordOverlay.Visibility == Visibility.Visible && VaultPasswordOverlay.IsHitTestVisible)
                {
                    ConfirmVault_Click(null, null);
                    e.Handled = true;
                }
                else if (UniversalDialog.Visibility == Visibility.Visible && UniversalDialog.IsHitTestVisible)
                {
                    DialogConfirm_Click(null, null);
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
                        ActivateLicense_Click(null, null);
                        e.Handled = true;
                    }
                }
                else if (VocabularyView.Visibility == Visibility.Visible)
                {
                    if (TxtNewKey.IsFocused)
                    {
                        AddVocabulary_Click(null, null);
                        e.Handled = true;
                    }
                    else if (TxtNewValue.IsFocused && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        AddVocabulary_Click(null, null);
                        e.Handled = true;
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

        private void OnboardingGetGroq_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://console.groq.com/keys") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening Groq URL: {ex.Message}");
                _ = CosmoMessage.Show("Error", "Could not open browser. Please visit console.groq.com manually.", "❌");
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

        private async void CompleteOnboarding()
        {
            PreferenceManager.Shared.Preferences.HasCompletedOnboarding = true;
            PreferenceManager.Shared.Save();

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => OverlayOnboarding.Visibility = Visibility.Collapsed;
            OverlayOnboarding.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            _navigation.ShowDashboard();
            
            _ = CosmoMessage.Show("Setup Complete", "Welcome to CosmoWhisper! You can change these settings anytime in the sidebar.", "✨");
        }
        private void RestartSetup_Click(object sender, RoutedEventArgs e)
        {
            ShowOnboarding();
        }

        #endregion

    }

    public static class CosmoMessage
    {
        public static async Task<bool> Show(string title, string message, string icon = "✨", bool showCancel = false, string confirmButtonText = "Confirm")
        {
            if (DashboardWindow.Instance != null)
            {
                return await DashboardWindow.Instance.ShowDialogAsync(title, message, icon, showCancel, confirmButtonText);
            }
            
            // Fallback for cases where main window isn't loaded
            var res = MessageBox.Show(message, title, showCancel ? MessageBoxButton.YesNo : MessageBoxButton.OK);
            return res == MessageBoxResult.Yes || res == MessageBoxResult.OK;
        }
    }
}

