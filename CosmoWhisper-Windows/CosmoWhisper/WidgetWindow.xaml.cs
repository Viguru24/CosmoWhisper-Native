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
using CosmoWhisper.Manus;

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
        private DashboardWindow _dashboard;
        private Storyboard _soundWaveStoryboard;
        private Storyboard _rippleStoryboard;
        private Storyboard _idleBreathingStoryboard;

        public WidgetWindow()
        {
            InitializeComponent();
            
            // Apply blur
            this.SourceInitialized += (s, e) => {
                BlurManager.EnableBlur(this);
                _hotKeyManager.Register(this, PreferenceManager.Shared.Preferences.VirtualKey);
                _mouseButtonManager.Register(PreferenceManager.Shared.Preferences.MouseButton);
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

            // Wire up Audio Events
            AudioRecorder.Shared.IsRecordingChanged += OnIsRecordingChanged;
            AudioRecorder.Shared.AudioLevelChanged += OnAudioLevelChanged;
            AudioRecorder.Shared.TranscriptionReceived += OnTranscriptionReceived;
            AudioRecorder.Shared.ErrorOccurred += OnErrorOccurred;

            // Wire up Manus Events
            ManusAgent.Shared.ManusStatusChanged += OnManusStatusChanged;
            ManusAgent.Shared.ManusResponseReceived += OnManusResponseReceived;

            // Command feedback
            CommandController.Shared.CommandExecuted += OnCommandExecuted;

            // Narration feedback
            NarrationManager.Shared.SpeechStarted += OnSpeechStarted;
            NarrationManager.Shared.SpeechEnded += OnSpeechEnded;

            // Wire up HotKey for push-to-talk
            _hotKeyManager.KeyPressed += () => {
                if (!AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StartRecording();
                }
                Dispatcher.Invoke(() => {
                    if (HotkeyIndicator != null) HotkeyIndicator.Visibility = Visibility.Visible;
                });
            };
            _hotKeyManager.KeyReleased += () => {
                if (AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StopRecording();
                }
                Dispatcher.Invoke(() => {
                    if (HotkeyIndicator != null) HotkeyIndicator.Visibility = Visibility.Collapsed;
                });
            };
            _hotKeyManager.ErrorOccurred += (msg) => {
                Dispatcher.Invoke(() => {
                    string keyName = PreferenceManager.Shared.Preferences.ActivationKey;
                    // StatusLabel removed - no longer used
                });
            };
            
            // Wire up Mouse Button for push-to-talk
            _mouseButtonManager.ButtonPressed += () => {
                if (!AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StartRecording();
                }
            };
            _mouseButtonManager.ButtonReleased += () => {
                if (AudioRecorder.Shared.IsRecording)
                {
                    AudioRecorder.Shared.StopRecording();
                }
            };
            _mouseButtonManager.ErrorOccurred += (msg) => {
                Dispatcher.Invoke(() => {
                    // Handle mouse button errors silently
                });
            };
            
            PreferenceManager.Shared.PreferencesUpdated += () => {
                Dispatcher.Invoke(() => {
                    _hotKeyManager.Register(this, PreferenceManager.Shared.Preferences.VirtualKey);
                    _mouseButtonManager.Register(PreferenceManager.Shared.Preferences.MouseButton);
                    ApplyWidgetTransparency();
                });
            };

            this.Closed += (s, e) => {
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
            Dispatcher.Invoke(() => {
                if (isRecording)
                {
                    // Show colorful sound wave, hide visualizer
                    VisualizerPanel.Visibility = Visibility.Collapsed;
                    SoundWavePanel.Visibility = Visibility.Visible;
                    _soundWaveStoryboard.Begin();
                    SetTheme(Colors.Red, "");
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
            Dispatcher.Invoke(() => {
                // Scale Orb based on level
                float scale = 1.0f + (normalized * 0.5f);
                OrbScale.ScaleX = scale;
                OrbScale.ScaleY = scale;

                // Animate Bars
                foreach (var bar in _bars)
                {
                    double variation = _rnd.NextDouble() * 0.5 + 0.5;
                    bar.Height = Math.Max(4, normalized * 30 * variation);
                    bar.Fill = new SolidColorBrush(isRecordingActive ? Colors.White : System.Windows.Media.Color.FromRgb(255, 255, 255)) { Opacity = 0.8 };
                }
            });
        }

        private void OnAudioLevelChanged(float db)
        {
            float normalized = Math.Max(0, db + 60) / 60f; // 0 to 1
            UpdateVolumeIndicator(normalized);
        }

        private bool isRecordingActive => AudioRecorder.Shared.IsRecording;

        private void OnTranscriptionReceived(string text)
        {
            Dispatcher.Invoke(async () => {
                string snippet = text.Length > 20 ? text.Substring(0, 17) + "..." : text;
                // StatusLabel removed - no longer used
                
                await Task.Delay(3000);
                
                // Return to neutral state if not recording again
                if (!isRecordingActive)
                {
                    // StatusLabel removed - no longer used
                }
            });
        }

        private void OnManusStatusChanged(string status)
        {
            Dispatcher.Invoke(() => {
                // StatusLabel removed - no longer used
                SetTheme(Colors.MediumPurple, status.ToUpper());
                if (status.Contains("THINKING"))
                {
                    // Visual feedback for thinking
                }
                else
                {
                    // Reset
                }
            });
        }

        private void OnManusResponseReceived(string response)
        {
            Dispatcher.Invoke(async () => {
                // Show first line of response in StatusLabel
                var firstLine = response.Split('\n')[0];
                if (firstLine.Length > 30) firstLine = firstLine.Substring(0, 27) + "...";
                
                // StatusLabel removed - no longer used
                
                // If it contains a plan, maybe we want to provide more feedback
                if (response.Contains("Plan:"))
                {
                    OrbGlow.Fill = Brushes.Gold;
                }

                // Speak response if it's not too long or just speak the summary
                var summary = firstLine;
                if (response.Contains("Plan:")) summary = "I have updated the project plan.";
                
                if (PreferenceManager.Shared.Preferences.ManusNarrationEnabled)
                {
                    await NarrationManager.Shared.SpeakAsync(summary);
                }

                await Task.Delay(5000);
                
                if (!isRecordingActive)
                {
                    // StatusLabel removed - no longer used
                    SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), "IDLE");
                }
            });
        }

        private void OnCommandExecuted()
        {
            Dispatcher.Invoke(() => {
                _rippleStoryboard?.Begin();
                
                // Extra flash
                var colorAnim = new ColorAnimation(Colors.Gold, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                OrbGlow.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            });
        }

        private void OnSpeechStarted()
        {
            Dispatcher.Invoke(() => {
                SetTheme(Colors.DeepPink, "SPEAKING");
                _pulseStoryboard?.Begin();
            });
        }

        private void OnSpeechEnded()
        {
            Dispatcher.Invoke(() => {
                SetTheme(System.Windows.Media.Color.FromRgb(0, 122, 255), "READY");
                _pulseStoryboard?.Stop();
            });
        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() => {
                // Show full error in status (don't use SetTheme as it overwrites the text)
                // StatusLabel removed - no longer used
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
            if (MainBorder != null && MainBorder.Background is SolidColorBrush brush)
            {
                brush.Opacity = p.WidgetOpacity;
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

        private void Gear_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Ensure dashboard is valid
                if (_dashboard == null || !_dashboard.IsLoaded)
                {
                    _dashboard = new DashboardWindow();
                }
                
                if (_dashboard.IsVisible && _dashboard.WindowState == WindowState.Normal)
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open Dashboard: {ex.Message}\n\n{ex.StackTrace}", "Widget Error");
                // Log to Desktop
                try 
                { 
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CosmoWhisper_CrashLog.txt"),
                        $"{DateTime.Now}: Widget Gear Crash: {ex}\n----------------\n");
                } catch { }
            }
        }

        private void Gear_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right click logic: Close App
            System.Windows.Application.Current.Shutdown();
        }
    }
}
