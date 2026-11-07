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
using CosmoWhisper.Interfaces;
using CosmoWhisper.Services;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using CosmoWhisper.ViewModels;


namespace CosmoWhisper
{
    public partial class WidgetWindow : Window
    {
        private readonly List<System.Windows.Shapes.Rectangle> _bars = new();
        private readonly Random _rnd = new();
        private readonly HotKeyManager _hotKeyManager = new();
        private readonly MouseButtonManager _mouseButtonManager = new();
        public static DashboardWindow? _dashboard;
        private readonly SolidColorBrush _cachedWhiteBrush;
        
        private readonly ILogger _logger;
        private readonly IColorProvider _colorProvider;
        private readonly Services.AnimationService _animationService;
        private readonly WidgetViewModel _viewModel;

        private double _sinePhase = 0.0;
        private double _currentAmplitude = 2.0;
        private System.Threading.Timer? _sineAnimTimer;
        private readonly System.Diagnostics.Stopwatch _visualizerThrottle = System.Diagnostics.Stopwatch.StartNew();
        private const int VisualizerFpsLimit = 16; // ~60fps

        public WidgetWindow(
            WidgetViewModel viewModel,
            ILogger logger,
            IColorProvider colorProvider,
            Services.AnimationService animationService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _logger = logger;
            _colorProvider = colorProvider;
            _animationService = animationService;
            
            DataContext = _viewModel;

            this.SourceInitialized += (s, e) =>
            {
                // Set gear color based on build type/mode
                if (PreferenceManager.Shared.IsDevMode) GearIcon.Fill = Brushes.Orange;
                else if (!PreferenceManager.Shared.Preferences.IsStoreVersion) GearIcon.Fill = new SolidColorBrush(Color.FromRgb(50, 215, 75)); 
                else GearIcon.Fill = Brushes.Gray;

                _hotKeyManager.Register(this, PreferenceManager.Shared.Preferences.VirtualKey);
                _mouseButtonManager.Register(PreferenceManager.Shared.Preferences.MouseButton);
                UpdateTopmostState();
            };

            this.Deactivated += (s, e) => UpdateTopmostState();
            this.Loaded += (s, e) => 
            {
                LoadPosition();
                this.LocationChanged += (s, e) => SavePosition();
                _animationService.StartIdleAnimations(this);
            };

            ApplyWidgetTransparency();

            foreach (var child in VisualizerPanel.Children)
            {
                if (child is System.Windows.Shapes.Rectangle rect) _bars.Add(rect);
            }

            _cachedWhiteBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
            if (_cachedWhiteBrush.CanFreeze) _cachedWhiteBrush.Freeze();

            WireEvents();
        }

        private void WireEvents()
        {
            AudioRecorder.Shared.IsRecordingChanged += OnIsRecordingChanged;
            AudioRecorder.Shared.AudioLevelChanged += OnAudioLevelChanged;
            AudioRecorder.Shared.TranscriptionReceived += OnTranscriptionReceived;
            AudioRecorder.Shared.TranscriptionSourceReceived += OnTranscriptionSourceReceived;
            AudioRecorder.Shared.ErrorOccurred += OnErrorOccurred;
            CommandController.Shared.CommandExecuted += OnCommandExecuted;

            _hotKeyManager.KeyPressed += () => { if (!_viewModel.IsRecording) _viewModel.ToggleRecording(); };
            _hotKeyManager.KeyReleased += () => { if (_viewModel.IsRecording) _viewModel.ToggleRecording(); };
            _mouseButtonManager.ButtonPressed += () => { if (!_viewModel.IsRecording) _viewModel.ToggleRecording(); };
            _mouseButtonManager.ButtonReleased += () => { if (_viewModel.IsRecording) _viewModel.ToggleRecording(); };

            PreferenceManager.Shared.PreferencesUpdated += () =>
            {
                if (!this.IsLoaded) return;
                Dispatcher.BeginInvoke(() => {
                    var p = PreferenceManager.Shared.Preferences;
                    _hotKeyManager.Register(this, p.VirtualKey);
                    _mouseButtonManager.Register(p.MouseButton);
                    ApplyWidgetTransparency();
                });
            };

            this.Closed += (s, e) => {
                SavePosition();
                _hotKeyManager.Dispose();
                _mouseButtonManager.Dispose();
                _sineAnimTimer?.Dispose();
            };
        }

        private void OnIsRecordingChanged(bool isRecording)
        {
            _viewModel.IsRecording = isRecording;
            Dispatcher.BeginInvoke(() =>
            {
                if (isRecording)
                {
                    UpdateTopmostState();
                    VisualizerPanel.Visibility = Visibility.Collapsed;
                    SineWaveCanvas.Visibility = Visibility.Visible;
                    _currentAmplitude = 2.0;

                    _sineAnimTimer?.Dispose();
                    _sineAnimTimer = new System.Threading.Timer(_ =>
                    {
                        _sinePhase += 0.18;
                        Dispatcher.BeginInvoke(() => _animationService.DrawSineWave(SineWaveCanvas, SineWavePath, SineWaveGlow, _sinePhase, _currentAmplitude));
                    }, null, 0, 16);

                    _animationService.StartRecordingAnimations(this);
                    SetTheme(Colors.White, "");
                    _animationService.SetSatelliteSpeed(this, 2.0);
                }
                else
                {
                    _sineAnimTimer?.Dispose();
                    _sineAnimTimer = null;
                    _animationService.StopThinkingAnimations(this);
                    _animationService.StartIdleAnimations(this);
                    SineWaveCanvas.Visibility = Visibility.Collapsed;
                    VisualizerPanel.Visibility = Visibility.Visible;
                    SetTheme(Color.FromRgb(0, 122, 255), ""); 
                    _animationService.SetSatelliteSpeed(this, 1.0);
                    ResetBars();
                }
            });
        }

        public void UpdateVolumeIndicator(float normalized)
        {
            _currentAmplitude = normalized;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                float scale = 1.0f + (normalized * 0.4f);
                OrbScale.ScaleX = scale;
                OrbScale.ScaleY = scale;
                foreach (var bar in _bars)
                {
                    bar.Height = Math.Max(4, normalized * 30 * (_rnd.NextDouble() * 0.5 + 0.5));
                    bar.Fill = _cachedWhiteBrush;
                }
            }));
        }

        private void OnAudioLevelChanged(float db)
        {
            if (_visualizerThrottle.ElapsedMilliseconds < VisualizerFpsLimit) return;
            _visualizerThrottle.Restart();

            float minDb = -80;
            float sensitivityFactor = (float)AudioRecorder.Shared.Sensitivity * 2.0f;
            float normalized = (db - minDb) / (0 - minDb);
            normalized *= sensitivityFactor;
            if (normalized < 0) normalized = 0;
            if (normalized > 1) normalized = 1;
            UpdateVolumeIndicator(normalized);
        }

        private void OnTranscriptionReceived(string text)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    ResetOrbToIdle();
                    return;
                }

                if (text.Contains("Thinking"))
                {
                    SineWaveCanvas.Visibility = Visibility.Collapsed;
                    _sineAnimTimer?.Dispose();
                    _sineAnimTimer = null;
                    ThinkingDotsPanel.Visibility = Visibility.Visible;
                    _animationService.StartThinkingAnimations(this);
                    ThinkingAura.Opacity = 1;
                    _animationService.SetSatelliteSpeed(this, 3.5); // Fast orbit for thinking
                    SetTheme(Color.FromRgb(245, 158, 11), ""); // Warm Amber for thinking (No-Purple compliance)
                }
                else
                {
                    _animationService.StopThinkingAnimations(this);
                    ThinkingDotsPanel.Visibility = Visibility.Collapsed;
                    ThinkingAura.Opacity = 0;
                    SetTheme(Color.FromRgb(0, 122, 255), "");

                    if (!AudioRecorder.Shared.IsRecording)
                    {
                        var colorAnim = new ColorAnimation(Colors.Cyan, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                        OrbMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                    }
                }
                await Task.Delay(2000);
            }));
        }

        private void OnTranscriptionSourceReceived(string source, long timeMs)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                string icon = source.Contains("Local") ? "🏠" : "☁️";
                string originalHint = TxtHotkeyHint.Text;
                TxtHotkeyHint.Text = $"{icon} {source}: {timeMs}ms";
                TxtHotkeyHint.Opacity = 1.0;
                TxtHotkeyHint.Foreground = source.Contains("Local") ? Brushes.Cyan : Brushes.White;

                if (source.Contains("Local"))
                {
                    var colorAnim = new ColorAnimation(Colors.Cyan, TimeSpan.FromMilliseconds(300)) { AutoReverse = true };
                    OrbMain.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                }
                await Task.Delay(3000);
                TxtHotkeyHint.Text = originalHint;
                TxtHotkeyHint.Opacity = 0.3;
                TxtHotkeyHint.Foreground = Brushes.White;
            }));
        }

        private void OnCommandExecuted()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _animationService.BeginRipple(this);
                var colorAnim = new ColorAnimation(Colors.Gold, TimeSpan.FromMilliseconds(200)) { AutoReverse = true };
                OrbGlow.Fill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            }));
        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OrbMain.Fill = new SolidColorBrush(Colors.OrangeRed);
                OrbGlow.Fill = new SolidColorBrush(Colors.OrangeRed);
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "Error: " + error;
                    TxtStatus.Visibility = Visibility.Visible;
                }
                Task.Delay(3000).ContinueWith(_ => Dispatcher.BeginInvoke(new Action(() => ResetOrbToIdle())));
            }));
        }

        private void ResetOrbToIdle()
        {
            SetTheme(Color.FromRgb(0, 122, 255), ""); 
            if (TxtStatus != null)
            {
                TxtStatus.Text = "Ready";
                TxtStatus.Visibility = Visibility.Collapsed;
            }
        }

        private void SetTheme(Color color, string statusText)
        {
            var brush = new SolidColorBrush(color);
            OrbMain.Fill = brush;
            OrbGlow.Fill = brush;
            SatelliteOrb.Fill = brush;
            // Only show status text if it's an error or important notification (not normal states like Thinking/Ready)
            if (!string.IsNullOrEmpty(statusText) && TxtStatus != null)
            {
                TxtStatus.Text = statusText;
                TxtStatus.Visibility = Visibility.Visible;
            }
            else if (TxtStatus != null && string.IsNullOrEmpty(statusText))
            {
                TxtStatus.Visibility = Visibility.Collapsed;
            }
        }

        public void ApplyWidgetTransparency()
        {
            double opacity = PreferenceManager.Shared.Preferences.WidgetOpacity;
            this.Opacity = opacity;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public void UpdateTopmostState()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    this.Topmost = false;
                    this.Topmost = true;

                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to update topmost state: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SavePosition()
        {
            _viewModel.SaveWidgetPosition(this.Left, this.Top);
        }


        private void LoadPosition()
        {
            var p = PreferenceManager.Shared.Preferences;
            double left = p.WidgetLeft;
            double top = p.WidgetTop;

            double screenLeft = SystemParameters.VirtualScreenLeft;
            double screenTop = SystemParameters.VirtualScreenTop;
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;

            // If the saved position is completely off-screen, reset to safe defaults
            if (left < screenLeft || left > (screenLeft + screenWidth - 50) ||
                top < screenTop || top > (screenTop + screenHeight - 50))
            {
                left = 100;
                top = 100;
                
                // Save corrected bounds immediately to prevent repeat occurrences on hard crashes
                _viewModel.SaveWidgetPosition(left, top);
            }

            this.Left = left;
            this.Top = top;
        }

        private void ResetBars()
        {
            foreach (var bar in _bars) bar.Height = 4;
        }

        public void ToggleDashboard()
        {
            if (_dashboard == null)
            {
                _dashboard = new DashboardWindow();
                _dashboard.Closed += (s, e) => _dashboard = null;
                _dashboard.Show();
            }
            else
            {
                if (_dashboard.IsVisible) _dashboard.Hide();
                else _dashboard.Show();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void MicControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
             _viewModel.ToggleRecording();
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
            exitWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            exitWindow.Topmost = true;
            
            if (exitWindow.ShowDialog() == true || exitWindow.ConfirmedExit)
            {
                _logger.LogInformation("User confirmed exit. Shutting down application.");
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
