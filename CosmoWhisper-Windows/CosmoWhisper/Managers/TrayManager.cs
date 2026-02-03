using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows;

namespace CosmoWhisper.Managers
{
    /// <summary>
    /// Manages the System Tray (notification area) icon and context menu.
    /// Allows users to control the app from the Windows taskbar.
    /// </summary>
    public class TrayManager : IDisposable
    {
        public static TrayManager? Shared { get; private set; }

        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _contextMenu;

        public event Action? ShowDashboardRequested;
        public event Action? ToggleCapsuleRequested;
        public event Action? ExitRequested;

        public void Initialize()
        {
            if (_trayIcon != null) return;

            Shared = this;

            // Create context menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.BackColor = Color.FromArgb(30, 30, 35);
            _contextMenu.ForeColor = Color.White;
            _contextMenu.ShowImageMargin = false;
            _contextMenu.Font = new Font("Segoe UI", 10);

            var showDashboard = new ToolStripMenuItem("📊 Open Dashboard");
            showDashboard.Click += (s, e) => ShowDashboardRequested?.Invoke();

            var toggleCapsule = new ToolStripMenuItem("🎙️ Toggle Capsule");
            toggleCapsule.Click += (s, e) => ToggleCapsuleRequested?.Invoke();

            var separator = new ToolStripSeparator();

            var pauseRecording = new ToolStripMenuItem("⏸️ Pause Recording");
            pauseRecording.Click += (s, e) => TogglePause(pauseRecording);

            var startWithWindows = new ToolStripMenuItem("🚀 Start with Windows");
            startWithWindows.Checked = PreferenceManager.Shared.Preferences.LaunchOnStartup;
            startWithWindows.Click += (s, e) => ToggleStartup(startWithWindows);

            var separator2 = new ToolStripSeparator();

            var exitApp = new ToolStripMenuItem("🚪 Exit CosmoWhisper");
            exitApp.ForeColor = Color.FromArgb(255, 69, 58);
            exitApp.Click += (s, e) => ExitRequested?.Invoke();

            _contextMenu.Items.Add(showDashboard);
            _contextMenu.Items.Add(toggleCapsule);
            _contextMenu.Items.Add(separator);
            _contextMenu.Items.Add(pauseRecording);
            _contextMenu.Items.Add(startWithWindows);
            _contextMenu.Items.Add(separator2);
            _contextMenu.Items.Add(exitApp);

            // Create tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = GetAppIcon(),
                Text = "CosmoWhisper - Voice Control for Windows",
                Visible = true,
                ContextMenuStrip = _contextMenu
            };

            // Double-click to open dashboard
            _trayIcon.DoubleClick += (s, e) => ShowDashboardRequested?.Invoke();
        }

        private Icon GetAppIcon()
        {
            try
            {
                // 1. Try to load specific app.ico from directory (Best Quality)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var icoPath = System.IO.Path.Combine(baseDir, "app.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }

                // 2. Fallback to embedded icon
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    return Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application;
                }
            }
            catch { }
            
            return SystemIcons.Application;
        }

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;
        public event Action<bool>? PauseStateChanged;
        
        private void TogglePause(ToolStripMenuItem item)
        {
            _isPaused = !_isPaused;
            item.Text = _isPaused ? "▶️ Resume Recording" : "⏸️ Pause Recording";
            
            // Update tray tooltip
            if (_trayIcon != null)
            {
                _trayIcon.Text = _isPaused 
                    ? "CosmoWhisper - PAUSED" 
                    : "CosmoWhisper - Voice Control for Windows";
            }

            // Notify listeners of pause state change
            PauseStateChanged?.Invoke(_isPaused);
            
            if (_isPaused)
            {
                ShowBalloon("Paused", "Voice recording is paused. Click to resume.", ToolTipIcon.Info);
            }
            else
            {
                ShowBalloon("Resumed", "Voice recording is active.", ToolTipIcon.Info);
            }
        }

        private void ToggleStartup(ToolStripMenuItem item)
        {
            var prefs = PreferenceManager.Shared.Preferences;
            prefs.LaunchOnStartup = !prefs.LaunchOnStartup;
            item.Checked = prefs.LaunchOnStartup;
            PreferenceManager.Shared.Save();

            // Actually register/unregister from Windows startup
            if (prefs.LaunchOnStartup)
            {
                StartupManager.Enable();
            }
            else
            {
                StartupManager.Disable();
            }
        }

        public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.None)
        {
            _trayIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        public void UpdateStatus(string status)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Text = $"CosmoWhisper - {status}";
            }
        }

        public void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            _contextMenu?.Dispose();
            Shared = null;
        }
    }

    /// <summary>
    /// Manages Windows startup registration via Registry
    /// </summary>
    public static class StartupManager
    {
        private const string AppName = "CosmoWhisper";
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static void Enable()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup Enable Error: {ex.Message}");
            }
        }

        public static void Disable()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true);
                key?.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup Disable Error: {ex.Message}");
            }
        }

        public static bool IsEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
