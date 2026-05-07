using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using CosmoWhisper.Managers;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;

namespace CosmoWhisper
{
    public partial class WidgetWindow : Window
    {
        private Storyboard _pulseStoryboard;
        private List<System.Windows.Shapes.Rectangle> _bars = new List<System.Windows.Shapes.Rectangle>();
        private Random _rnd = new Random();
        private HotKeyManager _hotKeyManager = new HotKeyManager();
        private MouseButtonManager _mouseButtonManager = new MouseButtonManager();
        public static DashboardWindow _dashboard;
        private Storyboard _thinkingDotsStoryboard;
        private Storyboard _rippleStoryboard;
        private Storyboard _idleBreathingStoryboard;
        private Storyboard _thinkingStoryboard;
        private Storyboard _jiggleStoryboard;
        private Storyboard _iridescentStoryboard;
        private Storyboard _satelliteStoryboard;
        private SolidColorBrush _cachedWhiteBrush;
        private SolidColorBrush _cachedBlueBrush;
        private uint _lastRegisteredVk = 0;
        private string _lastRegisteredMouseButton = "";
        private double _sinePhase = 0.0;  // Tracks wave phase for smooth animation
        private double _currentAmplitude = 2.0; // Current wave amplitude (audio-driven)
        private System.Threading.Timer _sineAnimTimer; // Timer for animating wave when quiet

        public WidgetWindow()
        {
            InitializeComponent();

            // Apply Glassmorphism (ITEM 1)
            this.SourceInitialized += (s, e) =>
            {
                // HARD WALL BOUNDARY: Compile-time colors
#if DEV_BUILD
                if (GearIcon != null) GearIcon.Fill = new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange
#else
                if (GearIcon != null) GearIcon.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 100)); // Bright Green
#endif

                // WindowEffectManager.ApplyEffect(this, BackdropType.Mica); 
                _hotKeyManager.Register(this, PreferenceManager.Shared.Preferences.VirtualKey);
                _mouseButtonManager.Register(PreferenceManager.Shared.Preferences.MouseButton);
                UpdateTopmostState();
            };

            // Ensure we stay on top when other windows are focused
            this.Deactivated += (s, e) =>
            {
                UpdateTopmostState();
            };

            this.LocationChanged += (s, e) => SavePosition();
            this.Loaded += (s, e) => LoadPosition();

            // Apply widget transparency from preferences
            ApplyWidgetTransparency();
            _pulseStoryboard = (Storyboard)FindResource("PulseAnimation");
            _thinkingDotsStoryboard = (Storyboard)FindResource("ThinkingDotsAnimation");
            _rippleStoryboard = (Storyboard)FindResource("RippleAnimation");
            _idleBreathingStoryboard = (Storyboard)FindResource("IdleBreathingAnimation");
            _thinkingStoryboard = (Storyboard)FindResource("ThinkingAnimation");
            _jiggleStoryboard = (Storyboard)FindResource("JiggleAnimation");
            _iridescentStoryboard = (Storyboard)FindResource("IridescentAnimation");
            _satelliteStoryboard = (Storyboard)FindResource("SatelliteAnimation");

            // Start animations
            _idleBreathingStoryboard.Begin();
            _iridescentStoryboard.Begin();
            _satelliteStoryboard.Begin();

            // Capture the bars we created in XAML
            foreach (var child in VisualizerPanel.Children)
            {
                if (child is System.Windows.Shapes.Rectangle rect) _bars.Add(rect);
            }

            // Cache brushes
            _cachedWhiteBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
            if (_cachedWhiteBrush.CanFreeze) _cachedWhiteBrush.Freeze();

            _cachedBlueBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)) { Opacity = 0.8 }; // Original logic used White 0.8 too? No, it used different color.
            // Wait, line 185 was: new SolidColorBrush(isRecordingActive ? Colors.White : System.Windows.Media.Color.FromRgb(255, 255, 255)) { Opacity = 0.8 };
            // Both are White? "System.Windows.Media.Color.FromRgb(255, 255, 255)" IS White.
            // So we only need one brush.

            // Wire up Audio Events
            AudioRecorder.Shared.IsRecordingChanged += OnIsRecordingChanged;
            AudioRecorder.Shared.AudioLevelChanged += OnAudioLevelChanged;
            AudioRecorder.Shared.TranscriptionReceived += OnTranscriptionReceived;
            AudioRecorder.Shared.TranscriptionSourceReceived += OnTranscriptionSourceReceived;
            AudioRecorder.Shared.ErrorOccurred += OnErrorOccurred;

            // Command feedback
            CommandController.Shared.CommandExecuted += OnCommandExecuted;

            // Wire up HotKey for push-to-talk
            _hotKeyManager.KeyPressed += () =>
            {
                Task.Run(() => LogEvent("Hotkey (Key Down) Triggered"));
                if (!AudioRecorder.Shared.IsRecording)
                {
                    Task.Run(() => AudioRecorder.Shared.StartRecording());
                }
            };
            _hotKeyManager.KeyReleased += () =>
            {
                Task.Run(() => LogEvent("Hotkey (Key Up) Released"));
                if (AudioRecorder.Shared.IsRecording)
                {
                    Task.Run(() => AudioRecorder.Shared.StopRecording());
                }
            };
            _hotKeyManager.ErrorOccurred += (msg) =>
            {
                Task.Run(() => LogEvent($"Hotkey Error: {msg}"));
            };

            // Wire up Mouse Button for push-to-talk
            _mouseButtonManager.ButtonPressed += () =>
            {
                Task.Run(() => LogEvent("Mouse Button Pressed"));
                if (!AudioRecorder.Shared.IsRecording)
                {
                    Task.Run(() => AudioRecorder.Shared.StartRecording());
                }
            };
            _mouseButtonManager.ButtonReleased += () =>
            {
                Task.Run(() => LogEvent("Mouse Button Released"));
                if (AudioRecorder.Shared.IsRecording)
                {
                    Task.Run(() => AudioRecorder.Shared.StopRecording());
                }
            };
            _mouseButtonManager.ErrorOccurred += (msg) =>
            {
                LogEvent($"Mouse Button Error: {msg}");
                Dispatcher.Invoke(() =>
                {
                    // Handle mouse button errors silently
                });
            };

            PreferenceManager.Shared.PreferencesUpdated += () =>
            {
                var p = PreferenceManager.Shared.Preferences;
                
                // Only re-register if the actual key/button changed
                if (p.VirtualKey != _lastRegisteredVk || p.MouseButton != _lastRegisteredMouseButton)
                {
                    LogEvent($"Preferences Updated (IO Change: {p.ActivationKey}/{p.MouseButton})");
                    Dispatcher.Invoke(() =>
                    {
                        _hotKeyManager.Register(this, p.VirtualKey);
                        _mouseButtonManager.Register(p.MouseButton);
                        
                        _lastRegisteredVk = p.VirtualKey;
                        _lastRegisteredMouseButton = p.MouseButton;

                        if (p.MouseButton != "None") TxtHotkeyHint.Text = $"Hold {p.MouseButton}";
                        else TxtHotkeyHint.Text = $"Hold {p.ActivationKey}";
                    });
                }
                
                Dispatcher.Invoke(ApplyWidgetTransparency);
            };

            this.Closed += (s, e) =>
            {
                LogEvent("App Shutting Down");
                SavePosition();
                _hotKeyManager.Dispose();
                _mouseButtonManager.Dispose();
            };

            this.LocationChanged += (s, e) => SavePosition();

            LoadPosition();

            // --- GEAR COLOR LOGIC ---
            if (PreferenceManager.Shared.IsDevMode)
            {
                GearIcon.Fill = Brushes.Orange; 
                LogEvent("Orange Gear Active (DEV MODE)");
            }
            else if (!PreferenceManager.Shared.Preferences.IsStoreVersion)
            {
                // Personal / Custom Build (Louis Edition)
                GearIcon.Fill = new SolidColorBrush(Color.FromRgb(50, 215, 75)); // Lime Green
                LogEvent("Lime Green Gear Active (PERSONAL)");
            }
            else
            {
                // Official Microsoft Store Build
                GearIcon.Fill = Brushes.Gray; 
                LogEvent("Gray Gear Active (STORE)");
            }
            // Ensure gear is visible 
            if (FindName("GearIcon") is FrameworkElement gear) gear.Visibility = Visibility.Visible;
            if (GearBg?.Parent is Grid gearGrid) gearGrid.Visibility = Visibility.Visible;
            // Init hotkey hint
            var pref = PreferenceManager.Shared.Preferences;
            if (pref.MouseButton != "None") TxtHotkeyHint.Text = $"Hold {pref.MouseButton}";
            else TxtHotkeyHint.Text = $"Hold {pref.ActivationKey}";

            // LOG STARTUP
            LogEvent("--- APP STARTING v2.2.90.0 ---");

            // WARM UP: Removed StartMonitoring() to save resources. 
            // Hardware will initialize on-demand when recording starts.
        }

        private void LogEvent(string msg)
        {
            try
            {
                string logDir = System.IO.Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = System.IO.Path.Combine(logDir, "cosmo_debug.log");
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}\n");
            }
            catch { }
        }


        private void UpdateVisualizer(float level)
        {
            // Performance Guard: Skip UI updates for background noise
            if (level < -60 && !isRecordingActive) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (TxtStatus.Visibility == Visibility.Visible && level > -50)
                        TxtStatus.Visibility = Visibility.Collapsed;

                    // Update Sine Wave
                    UpdateSineWave(level);

                    // Update the old bar visualizer (fallback)
                    if (VisualizerPanel.Visibility == Visibility.Visible)
                    {
                        foreach (System.Windows.Shapes.Rectangle rect in VisualizerPanel.Children)
                        {
                            double targetHeight = 4 + (level + 60) * 0.8;
                            if (targetHeight < 4) targetHeight = 4;
                            if (targetHeight > 40) targetHeight = 40;
                            rect.Height = targetHeight;
                        }
                    }
                }
                catch { }
            }));
        }

        private void UpdateSineWave(float level)
        {
            // Normalize -60dB -> 0 to 0dB -> 1
            float minDb = -60;
            float normalized = (level - minDb) / (0 - minDb);
            if (normalized < 0) normalized = 0;
            if (normalized > 1) normalized = 1;

            // Drive the amplitude of the drawing loop
            _currentAmplitude = normalized;
        }

        private void OnIsRecordingChanged(bool isRecording)
        {
            Dispatcher.Invoke(() =>
            {
                if (isRecording)
                {
                    // Show Neural Sine Wave, hide idle elements
                    VisualizerPanel.Visibility = Visibility.Collapsed;
                    TxtHotkeyHint.Visibility = Visibility.Collapsed;
                    ThinkingDotsPanel.Visibility = Visibility.Collapsed;
                    SineWaveCanvas.Visibility = Visibility.Visible;
                    _currentAmplitude = 2.0;

                    // Start the sine wave animation timer (runs at ~60fps to phase-shift the wave)
                    _sineAnimTimer?.Dispose();
                    _sineAnimTimer = new System.Threading.Timer(_ =>
                    {
                        _sinePhase += 0.18; // advance phase each tick
                        Dispatcher.BeginInvoke(() => DrawSineWave(_currentAmplitude));
                    }, null, 0, 16); // ~60fps

                    _jiggleStoryboard.Begin();

                    // Orb goes white when listening
                    SetTheme(Colors.White, "");
                    _satelliteStoryboard.SetSpeedRatio(2.0); // Boost speed
                }
                else
                {
                    // Stop sine wave, reset to idle
                    _sineAnimTimer?.Dispose();
                    _sineAnimTimer = null;
                    _thinkingDotsStoryboard.Stop();
                    _jiggleStoryboard.Begin();
                    SineWaveCanvas.Visibility = Visibility.Collapsed;
                    ThinkingDotsPanel.Visibility = Visibility.Collapsed;
                    VisualizerPanel.Visibility = Visibility.Visible;
                    TxtHotkeyHint.Visibility = Visibility.Visible;
                    SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), ""); // Back to Blue (now Moon)
                    _satelliteStoryboard.SetSpeedRatio(1.0); // Normal speed
                    ResetBars();
                }
            });
        }

        /// <summary>
        /// Draws a smooth sine wave across the SineWaveCanvas.
        /// The amplitude is driven by real-time audio level (0-1 normalised).
        /// </summary>
        private void DrawSineWave(double amplitudeNorm)
        {
            const double canvasWidth = 120;
            const double centerY = 22;
            const double minAmp = 2.0;
            const double maxAmp = 18.0;
            double amplitude = minAmp + (amplitudeNorm * (maxAmp - minAmp));
            const int points = 80;

            var figure = new PathFigure { IsClosed = false, IsFilled = false };
            for (int i = 0; i <= points; i++)
            {
                double x = (canvasWidth / points) * i;
                double y = centerY + amplitude * Math.Sin(_sinePhase + (i / (double)points) * Math.PI * 4);
                if (i == 0) figure.StartPoint = new System.Windows.Point(x, y);
                else figure.Segments.Add(new LineSegment(new System.Windows.Point(x, y), true));
            }

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            SineWavePath.Data = geometry;
            SineWaveGlow.Data = geometry; // glow uses same path
        }

        public void UpdateVolumeIndicator(float normalized)
        {
            // Store amplitude for the sine wave timer to pick up
            _currentAmplitude = normalized;

            Dispatcher.Invoke(() =>
            {
                // Scale orb based on level
                float scale = 1.0f + (normalized * 0.4f);
                OrbScale.ScaleX = scale;
                OrbScale.ScaleY = scale;
                // Old bars are still used for idle visualiser (kept minimal)
                foreach (var bar in _bars)
                {
                    bar.Height = Math.Max(4, normalized * 30 * (_rnd.NextDouble() * 0.5 + 0.5));
                    bar.Fill = _cachedWhiteBrush;
                }
            });
        }

        private void OnAudioLevelChanged(float db)
        {
            // Use same logic as Dashboard for consistency
            float minDb = -80;
            float sensitivityFactor = (float)AudioRecorder.Shared.Sensitivity * 2.0f;

            float normalized = (db - minDb) / (0 - minDb);
            normalized *= sensitivityFactor;

            if (normalized < 0) normalized = 0;
            if (normalized > 1) normalized = 1;

            UpdateVolumeIndicator(normalized);
        }

        private bool isRecordingActive => AudioRecorder.Shared.IsRecording;

        private void OnTranscriptionReceived(string text)
        {
            Dispatcher.Invoke(async () =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    // Reset to idle if we get an empty string (e.g. filtered garbage)
                    ResetOrbToIdle();
                    return;
                }

                if (text.Contains("Thinking"))
                {
                    // Switch from sine wave → thinking dots
                    SineWaveCanvas.Visibility = Visibility.Collapsed;
                    _sineAnimTimer?.Dispose();
                    _sineAnimTimer = null;

                    ThinkingDotsPanel.Visibility = Visibility.Visible;
                    _thinkingDotsStoryboard.Begin();
                    _thinkingStoryboard?.Begin();
                    ThinkingAura.Opacity = 1;
                    SetTheme(Colors.MediumPurple, "THINKING");
                }
                else
                {
                    _thinkingDotsStoryboard?.Stop();
                    ThinkingDotsPanel.Visibility = Visibility.Collapsed;
                    _thinkingStoryboard?.Stop();
                    ThinkingAura.Opacity = 0;
                    SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), "");

                    if (!isRecordingActive)
                    {
                        var colorAnim = new ColorAnimation(Colors.Cyan, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                        OrbMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                    }
                }

                await Task.Delay(2000);
            });
        }
        
        private void OnTranscriptionSourceReceived(string source, long timeMs)
        {
            Dispatcher.Invoke(async () =>
            {
                // Update hotkey hint briefly with source stats
                string icon = source.Contains("Local") ? "🏠" : "☁️";
                string originalHint = TxtHotkeyHint.Text;
                
                TxtHotkeyHint.Text = $"{icon} {source}: {timeMs}ms";
                TxtHotkeyHint.Opacity = 1.0;
                TxtHotkeyHint.Foreground = source.Contains("Local") ? Brushes.Cyan : Brushes.White;

                // Brief visual splash
                if (source.Contains("Local"))
                {
                    var colorAnim = new ColorAnimation(Colors.Cyan, TimeSpan.FromMilliseconds(300)) { AutoReverse = true };
                    OrbMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                }

                await Task.Delay(3000);
                
                // Restore original state
                TxtHotkeyHint.Text = originalHint;
                TxtHotkeyHint.Opacity = 0.3;
                TxtHotkeyHint.Foreground = Brushes.White;
            });
        }

        private void OnCommandExecuted()
        {
            Dispatcher.Invoke(() =>
            {
                _rippleStoryboard?.Begin();

                // Extra flash
                var colorAnim = new ColorAnimation(Colors.Gold, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                OrbGlow.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            });
        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() =>
            {
                // Show full error in status
                OrbMain.Fill = new SolidColorBrush(Colors.OrangeRed);
                OrbGlow.Fill = new SolidColorBrush(Colors.OrangeRed);

                System.Diagnostics.Debug.WriteLine($"Cosmo Error: {error}");

                if (TxtStatus != null)
                {
                    TxtStatus.Text = "Error: " + error;
                    TxtStatus.Visibility = Visibility.Visible;
                }

                // Log to file for debugging
                try
                {
                    string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                    System.IO.Directory.CreateDirectory(logDir);
                    string logPath = System.IO.Path.Combine(logDir, "cosmo_errors.log");
                    System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {error}\n");
                }
                catch { }

                // Auto-reset after a few seconds to prevent "lock up" feel
                Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() => ResetOrbToIdle()));
            });
        }

        private void ResetOrbToIdle()
        {
            // Restores original colors and idle state (Blue)
            SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), ""); 
            if (TxtStatus != null)
            {
                TxtStatus.Text = "Ready";
                TxtStatus.Visibility = Visibility.Collapsed;
            }
        }



        private void SetTheme(System.Windows.Media.Color color, string statusText)
        {
            // NEW: Use White/Silver Moon as default, only change if specified (like OrangeRed for error)
            System.Windows.Media.Color themeColor = color;
            if (color == System.Windows.Media.Color.FromRgb(0, 122, 255)) themeColor = Colors.White; // Override Blue with Moon

            OrbMain.Fill = new SolidColorBrush(themeColor);
            OrbGlow.Fill = new SolidColorBrush(themeColor);
            if (OrbShadow != null) OrbShadow.Color = themeColor;

            if (MainBorder != null)
            {
                MainBorder.BorderBrush = new SolidColorBrush(themeColor) { Opacity = 0.5 };
            }

            if (statusText == "ERROR") 
            {
                OrbMain.Fill = Brushes.OrangeRed;
                _satelliteStoryboard.SetSpeedRatio(0.5); // slow down on error
            }
            else if (statusText.Contains("THINKING")) 
            {
                OrbMain.Fill = new SolidColorBrush(Colors.MediumPurple);
                _satelliteStoryboard.SetSpeedRatio(2.5); // spin faster when thinking
            }
            else
            {
                _satelliteStoryboard.SetSpeedRatio(1.0); // normal speed
            }
        }

        private void LoadPosition()
        {
            try
            {
                var p = PreferenceManager.Shared.Preferences;
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = p.WidgetTop;
                this.Left = p.WidgetLeft;
                
                EnsureVisible();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading widget position: {ex.Message}");
            }
        }

        private void SavePosition()
        {
            try
            {
                if (this.WindowState != WindowState.Normal) return;
                
                var p = PreferenceManager.Shared.Preferences;
                p.WidgetTop = this.Top;
                p.WidgetLeft = this.Left;
                PreferenceManager.Shared.Save();
            }
            catch { }
        }

        private void EnsureVisible()
        {
            try
            {
                bool isVisible = false;
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    // Check if window center is within any screen
                    double centerX = this.Left + (this.Width / 2);
                    double centerY = this.Top + (this.Height / 2);

                    if (centerX >= screen.WorkingArea.Left && centerX <= screen.WorkingArea.Right &&
                        centerY >= screen.WorkingArea.Top && centerY <= screen.WorkingArea.Bottom)
                    {
                        isVisible = true;
                        break;
                    }
                }

                if (!isVisible)
                {
                    // Snap to primary screen center-ish
                    var primary = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    this.Left = primary.Right - this.Width - 100;
                    this.Top = primary.Bottom - this.Height - 100;
                    SavePosition();
                }
            }
            catch { }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Unless the application is shutting down, we just hide.
            // This prevents "vanishing" if user accidentally hits Alt+F4
            if (System.Windows.Application.Current != null && !App.IsShuttingDown) 
            {
                e.Cancel = true;
                this.Visibility = Visibility.Collapsed;
            }
            base.OnClosing(e);
        }

        public void ApplyWidgetTransparency()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (MainBorder != null && MainBorder.Background != null)
            {
                MainBorder.Background.Opacity = p.WidgetOpacity;
            }
        }

        private void ResetBars()
        {
            foreach (var bar in _bars) bar.Height = 4;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void MicControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AudioRecorder.Shared.ToggleRecording();
        }

        public void ToggleDashboard()
        {
            try
            {
                // Ensure dashboard is valid
                if (_dashboard == null || !_dashboard.IsLoaded)
                {
                    _dashboard = new DashboardWindow();
                    _dashboard.IsVisibleChanged += (s, e) =>
                    {
                        UpdateTopmostState();
                    };
                }

                if (_dashboard.IsVisible && _dashboard.WindowState != WindowState.Minimized)
                {
                    _dashboard.Hide();
                }
                else
                {
                    _dashboard.Show();

                    if (_dashboard.WindowState == WindowState.Minimized)
                    {
                        _dashboard.WindowState = WindowState.Normal;
                    }

                    _dashboard.Activate();
                    _dashboard.Topmost = true;
                    _dashboard.Topmost = false;
                    _dashboard.Focus();

                    // Force layout update
                    _dashboard.InvalidateVisual();

                    // If coordinates are weird (offscreen), force center
                    if (_dashboard.Left < -10000 || _dashboard.Top < -10000)
                    {
                        _dashboard.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        _dashboard.Left = (SystemParameters.PrimaryScreenWidth - _dashboard.Width) / 2;
                        _dashboard.Top = (SystemParameters.PrimaryScreenHeight - _dashboard.Height) / 2;
                    }
                }
                UpdateTopmostState();
            }
            catch (Exception ex)
            {
                _ = CosmoMessage.Show("Widget Error", $"Failed to toggle Dashboard: {ex.Message}", "⚠️");
            }
        }

        private void UpdateTopmostState()
        {
            try
            {
                // Widget should ALWAYS be topmost to ensure it's accessible as a mini-controller
                if (!this.Topmost)
                {
                    this.Topmost = true;
                }

                // Periodic "kick" ensuring we stay above other topmost windows
                this.Topmost = false;
                this.Topmost = true;

                // FINAL SAFETY: If we are visible, ensure we are actually on screen
                if (this.Visibility == Visibility.Visible) EnsureVisible();
            }
            catch { }
        }

        private void Gear_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToggleDashboard();
        }

        private void Gear_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right click logic: Open Confirm Exit Dialog
            e.Handled = true; 

            var exitWindow = new Views.ConfirmExitWindow();
            exitWindow.Owner = this; // Optional: keeps it related to widget
            exitWindow.ShowDialog();

            if (exitWindow.ConfirmedExit)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}

