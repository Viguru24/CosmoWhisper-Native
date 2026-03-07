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
        private Storyboard _soundWaveStoryboard;
        private Storyboard _rippleStoryboard;
        private Storyboard _idleBreathingStoryboard;
        private Storyboard _thinkingStoryboard;
        private Storyboard _jiggleStoryboard;
        private Storyboard _iridescentStoryboard;
        private SolidColorBrush _cachedWhiteBrush;
        private SolidColorBrush _cachedBlueBrush;

        public WidgetWindow()
        {
            InitializeComponent();

            // Apply Glassmorphism (ITEM 1)
            this.SourceInitialized += (s, e) =>
            {
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

            // Apply widget transparency from preferences
            ApplyWidgetTransparency();
            _pulseStoryboard = (Storyboard)FindResource("PulseAnimation");
            _soundWaveStoryboard = (Storyboard)FindResource("SoundWaveAnimation");
            _rippleStoryboard = (Storyboard)FindResource("RippleAnimation");
            _idleBreathingStoryboard = (Storyboard)FindResource("IdleBreathingAnimation");
            _thinkingStoryboard = (Storyboard)FindResource("ThinkingAnimation");
            _jiggleStoryboard = (Storyboard)FindResource("JiggleAnimation");
            _iridescentStoryboard = (Storyboard)FindResource("IridescentAnimation");

            // Start animations
            _idleBreathingStoryboard.Begin();
            _iridescentStoryboard.Begin();

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
                LogEvent("Hotkey Pressed: F9");
                if (!AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StartRecording();
                }
                Dispatcher.Invoke(() =>
                {
                });
            };
            _hotKeyManager.KeyReleased += () =>
            {
                LogEvent("Hotkey Released: F9");
                if (AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StopRecording();
                }
                Dispatcher.Invoke(() =>
                {
                });
            };
            _hotKeyManager.ErrorOccurred += (msg) =>
            {
                LogEvent($"Hotkey Error: {msg}");
                Dispatcher.Invoke(() =>
                {
                    string keyName = PreferenceManager.Shared.Preferences.ActivationKey;

                });
            };

            // Wire up Mouse Button for push-to-talk
            _mouseButtonManager.ButtonPressed += () =>
            {
                LogEvent("Mouse Button Pressed");
                if (!AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StartRecording();
                }
            };
            _mouseButtonManager.ButtonReleased += () =>
            {
                LogEvent("Mouse Button Released");
                if (AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StopRecording();
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
                LogEvent("Preferences Updated");
                Dispatcher.Invoke(() =>
                {
                    _hotKeyManager.Register(this, PreferenceManager.Shared.Preferences.VirtualKey);
                    _mouseButtonManager.Register(PreferenceManager.Shared.Preferences.MouseButton);
                    
                    var p = PreferenceManager.Shared.Preferences;
                    if (p.MouseButton != "None") TxtHotkeyHint.Text = $"Hold {p.MouseButton}";
                    else TxtHotkeyHint.Text = $"Hold {p.ActivationKey}";
                    
                    ApplyWidgetTransparency();
                });
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

#if DEBUG
            GearIcon.Fill = Brushes.Orange; // "Yellow" style
            LogEvent("Yellow Gear Active (DEBUG)");
#else
            GearIcon.Fill = Brushes.Gray; // Gray for Production
            LogEvent("Gray Gear Active (PRODUCTION)");
#endif
            // Ensure gear is visible 
            if (FindName("GearIcon") is FrameworkElement gear) gear.Visibility = Visibility.Visible;
            if (GearBg?.Parent is Grid gearGrid) gearGrid.Visibility = Visibility.Visible;
            // Init hotkey hint
            var pref = PreferenceManager.Shared.Preferences;
            if (pref.MouseButton != "None") TxtHotkeyHint.Text = $"Hold {pref.MouseButton}";
            else TxtHotkeyHint.Text = $"Hold {pref.ActivationKey}";

            // LOG STARTUP
            LogEvent("--- APP STARTING v2.2.20 ---");

            // WARM UP: Initialize audio graph immediately so the first request is instant
            // and the pre-roll buffer starts filling.
            AudioRecorder.Shared.StartMonitoring();
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

        private void LoadPosition()
        {
            var p = PreferenceManager.Shared.Preferences;
            this.Top = p.WidgetTop;
            this.Left = p.WidgetLeft;
        }

        private void SavePosition()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.WidgetTop = this.Top;
            p.WidgetLeft = this.Left;
            PreferenceManager.Shared.Save();
        }

        private void OnIsRecordingChanged(bool isRecording)
        {
            Dispatcher.Invoke(() =>
            {
                if (isRecording)
                {
                    // Show colorful sound wave, hide visualizer
                    VisualizerPanel.Visibility = Visibility.Collapsed;
                    TxtHotkeyHint.Visibility = Visibility.Collapsed;
                    SoundWavePanel.Visibility = Visibility.Visible;
                    _soundWaveStoryboard.Begin();
                    _jiggleStoryboard.Begin();
                    SetTheme(Colors.LimeGreen, "");
                }
                else
                {
                    // Hide sound wave, show visualizer
                    _soundWaveStoryboard.Stop();
                    _jiggleStoryboard.Begin();
                    SoundWavePanel.Visibility = Visibility.Collapsed;
                    VisualizerPanel.Visibility = Visibility.Visible;
                    TxtHotkeyHint.Visibility = Visibility.Visible;
                    SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), ""); // Back to Blue
                    ResetBars();
                }
            });
        }

        public void UpdateVolumeIndicator(float normalized)
        {
            Dispatcher.Invoke(() =>
            {
                // Scale Orb based on level
                float scale = 1.0f + (normalized * 0.5f);
                OrbScale.ScaleX = scale;
                OrbScale.ScaleY = scale;

                // Animate Bars
                foreach (var bar in _bars)
                {
                    double variation = _rnd.NextDouble() * 0.5 + 0.5;
                    bar.Height = Math.Max(4, normalized * 30 * variation);
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
            if (string.IsNullOrWhiteSpace(text)) return;

            Dispatcher.Invoke(async () =>
            {
                if (text.Contains("Thinking"))
                {
                    _thinkingStoryboard?.Begin();
                    ThinkingAura.Opacity = 1;
                    SetTheme(Colors.MediumPurple, "THINKING");
                }
                else
                {
                    _thinkingStoryboard?.Stop();
                    ThinkingAura.Opacity = 0;
                    SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), ""); // Back to Blue

                    // UI provides feedback via Orb
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
                // Show full error in status (don't use SetTheme as it overwrites the text)

                OrbMain.Fill = new SolidColorBrush(Colors.OrangeRed);
                OrbGlow.Fill = new SolidColorBrush(Colors.OrangeRed);

                System.Diagnostics.Debug.WriteLine($"Cosmo Error: {error}");

                // Log to file for debugging
                try
                {
                    string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                    System.IO.Directory.CreateDirectory(logDir);
                    string logPath = System.IO.Path.Combine(logDir, "cosmo_errors.log");
                    System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {error}\n");
                }
                catch { }
            });
        }

        private void SetTheme(System.Windows.Media.Color color, string statusText)
        {
            OrbMain.Fill = new SolidColorBrush(color);
            OrbGlow.Fill = new SolidColorBrush(color);
            if (OrbShadow != null) OrbShadow.Color = color;

            if (MainBorder != null)
            {
                MainBorder.BorderBrush = new SolidColorBrush(color) { Opacity = 0.5 };
            }

            if (statusText == "ERROR") OrbMain.Fill = Brushes.OrangeRed;
            if (statusText.Contains("THINKING")) OrbMain.Fill = new SolidColorBrush(Colors.MediumPurple);
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
