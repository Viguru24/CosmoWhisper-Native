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
        private SolidColorBrush _cachedWhiteBrush;
        private SolidColorBrush _cachedBlueBrush;

        public WidgetWindow()
        {
            InitializeComponent();

            // Apply blur
            this.SourceInitialized += (s, e) =>
            {
                // BlurManager.ApplyMica(this); // Disabled to fix black corners on transparent widget
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

            // Start animations
            _idleBreathingStoryboard.Begin();

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
            AudioRecorder.Shared.ErrorOccurred += OnErrorOccurred;

            // Command feedback
            CommandController.Shared.CommandExecuted += OnCommandExecuted;

            // Wire up HotKey for push-to-talk
            _hotKeyManager.KeyPressed += () =>
            {
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
                Dispatcher.Invoke(() =>
                {
                    string keyName = PreferenceManager.Shared.Preferences.ActivationKey;

                });
            };

            // Wire up Mouse Button for push-to-talk
            _mouseButtonManager.ButtonPressed += () =>
            {
                if (!AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StartRecording();
                }
            };
            _mouseButtonManager.ButtonReleased += () =>
            {
                if (AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StopRecording();
                }
            };
            _mouseButtonManager.ErrorOccurred += (msg) =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Handle mouse button errors silently
                });
            };

            PreferenceManager.Shared.PreferencesUpdated += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    _hotKeyManager.Register(this, PreferenceManager.Shared.Preferences.VirtualKey);
                    _mouseButtonManager.Register(PreferenceManager.Shared.Preferences.MouseButton);
                    ApplyWidgetTransparency();
                });
            };

            this.Closed += (s, e) =>
            {
                SavePosition();
                _hotKeyManager.Dispose();
                _mouseButtonManager.Dispose();
            };

            this.LocationChanged += (s, e) => SavePosition();

            LoadPosition();
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
                    SoundWavePanel.Visibility = Visibility.Visible;
                    _soundWaveStoryboard.Begin();
                    SetTheme(Colors.LimeGreen, "");
                }
                else
                {
                    // Hide sound wave, show visualizer
                    _soundWaveStoryboard.Stop();
                    SoundWavePanel.Visibility = Visibility.Collapsed;
                    VisualizerPanel.Visibility = Visibility.Visible;
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
                // Returns to neutral state if not recording
                if (!isRecordingActive)
                {
                    // UI provides feedback via Orb
                    var colorAnim = new ColorAnimation(Colors.Cyan, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                    OrbMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                }

                await Task.Delay(2000);
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
                    System.IO.File.AppendAllText("cosmo_errors.log",
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {error}\n");
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
            if (statusText.Contains("THINKING")) OrbMain.Fill = Brushes.Gold;
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
            // Right click logic: Close App
            System.Windows.Application.Current.Shutdown();
        }
    }
}
