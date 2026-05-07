using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using CosmoWhisper.Managers;
using CosmoWhisper;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using TextBox = System.Windows.Controls.TextBox;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;
using Colors = System.Windows.Media.Colors;

namespace CosmoWhisper.Controllers
{
    public class PreferencesController : BaseViewController
    {
        public bool IsCapturingHotkey => _isCapturingHotkey;
        private bool _isCapturingHotkey = false;
        private bool _isRestoreInProgress = false;

        public PreferencesController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            var p = PreferenceManager.Shared.Preferences;

            if (Window.ToggleClipboard != null) UpdateToggle(Window.ToggleClipboard, p.RestoreClipboard);
            if (Window.ToggleAutoSubmit != null) UpdateToggle(Window.ToggleAutoSubmit, p.AutoSubmit);
            if (Window.ToggleAutoCopy != null) UpdateToggle(Window.ToggleAutoCopy, p.AutoCopy);
            if (Window.TxtBackupPath != null) Window.TxtBackupPath.Text = p.BackupDirectory;
            if (Window.TxtHotkey != null) UpdateActivationUI();

            // Intelligence View Manus Agent (grouped here for simple preference sync)
            if (Window.ToggleManusAgent != null) UpdateToggle(Window.ToggleManusAgent, p.EnableManusAgent);
            if (Window.ToggleManusNarration != null) UpdateToggle(Window.ToggleManusNarration, p.ManusNarrationEnabled);
            if (Window.ToggleLocalWhisper != null) UpdateToggle(Window.ToggleLocalWhisper, p.UseLocalWhisper);
            if (Window.ToggleForceCloudProxy != null) UpdateToggle(Window.ToggleForceCloudProxy, p.ForceCloudProxy);
            UpdateLocalOnlyUI();



            // Widget Opacity
            if (Window.SldWidgetOpacity != null)
            {
                Window.SldWidgetOpacity.Value = p.WidgetOpacity * 100;
                if (Window.TxtWidgetOpacityValue != null) Window.TxtWidgetOpacityValue.Text = $"{(int)(p.WidgetOpacity * 100)}%";
            }

            // UI Scale
            if (Window.SldUIScale != null)
            {
                Window.SldUIScale.Value = p.UIScale * 100;
                if (Window.TxtUIScaleValue != null) Window.TxtUIScaleValue.Text = $"{(int)(p.UIScale * 100)}%";

                // We attach the handler in DashboardWindow.xaml.cs to avoid issues, 
                // but we could also move it here.
            }



            // Language Settings
            if (Window.ToggleRegionalSpelling != null) UpdateToggle(Window.ToggleRegionalSpelling, p.EnableRegionalSpelling);
            if (Window.ToggleLaunchOnStartup != null) UpdateToggle(Window.ToggleLaunchOnStartup, p.LaunchOnStartup);

            // Date Formatting
            if (Window.ComboDateFormat != null)
            {
                if (string.IsNullOrEmpty(p.SelectedDateFormat)) p.SelectedDateFormat = "dd/MM/yyyy";

                foreach (ComboBoxItem item in Window.ComboDateFormat.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedDateFormat)
                    {
                        Window.ComboDateFormat.SelectedItem = item;
                        break;
                    }
                }
            }

            // Time Formatting
            if (Window.ComboTimeFormat != null)
            {
                if (string.IsNullOrEmpty(p.SelectedTimeFormat)) p.SelectedTimeFormat = "HH:mm";

                foreach (ComboBoxItem item in Window.ComboTimeFormat.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedTimeFormat)
                    {
                        Window.ComboTimeFormat.SelectedItem = item;
                        break;
                    }
                }
            }

        }


        public void UpdateToggle(Border toggle, bool isOn)
        {
            if (toggle == null) return;
            toggle.Background = isOn
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF"));

            if (toggle.Child is System.Windows.Shapes.Ellipse ellipse)
            {
                ellipse.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }

        public void ToggleClipboard()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.RestoreClipboard = !p.RestoreClipboard;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleClipboard, p.RestoreClipboard);
        }

        public void ToggleAutoSubmit()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.AutoSubmit = !p.AutoSubmit;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleAutoSubmit, p.AutoSubmit);
            Window.ShowToast(p.AutoSubmit ? "Auto-Submit Enabled" : "Auto-Submit Disabled", "\u26A1");
        }

        public void ToggleAutoCopy()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.AutoCopy = !p.AutoCopy;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleAutoCopy, p.AutoCopy);
            Window.ShowToast(p.AutoCopy ? "Auto-Copy Enabled" : "Auto-Copy Disabled", "\uD83D\uDCCB");
        }




        public void UpdateWidgetOpacity(double value)
        {
            if (Window.TxtWidgetOpacityValue != null) Window.TxtWidgetOpacityValue.Text = $"{(int)value}%";

            var p = PreferenceManager.Shared.Preferences;
            p.WidgetOpacity = value / 100.0;
            PreferenceManager.Shared.Save();

            // Apply to widget immediately
            var widget = Application.Current.Windows.OfType<WidgetWindow>().FirstOrDefault();
            widget?.ApplyWidgetTransparency();
        }


        public async void BackupNow(Button? btn = null)
        {
            if (btn == null) btn = Window.BtnBackupNow;
            if (btn == null) return;

            string originalContent = btn.Content?.ToString() ?? "Backup Now";

            try
            {
                // Prompt for Name AND Password
                var result = await Window.GetVaultPasswordAsync(false);
                string? name = result.name;
                string? password = result.password;

                if (string.IsNullOrWhiteSpace(name)) return; // User cancelled

                btn.Content = "\u23F3 Backing Up...";
                btn.IsEnabled = false;

                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;
                Directory.CreateDirectory(destDir);

                // Pass password to manager
                string backupPath = await VaultManager.Shared.CreateBackupAsync(name, password, destDir);
                
                await System.Threading.Tasks.Task.Delay(500);

                btn.Content = "\u2705 Secured";
                long size = new FileInfo(backupPath).Length;
                string sizeDisplay = size < 1024 * 1024 ? $"{(size / 1024.0):F1} KB" : $"{(size / (1024.0 * 1024.0)):F2} MB";
                string type = string.IsNullOrEmpty(password) ? "Unencrypted ZIP" : "Encrypted Vault";

                await Window.ShowDialogAsync("Backup Complete", 
                    $"Configuration successfully saved!\n\nFile: {Path.GetFileName(backupPath)}\nType: {type}\nSize: {sizeDisplay}", 
                    "\u2705");
            }
            catch (Exception ex)
            {
                await Window.ShowDialogAsync("Backup Failed", $"Error: {ex.Message}", "\u274C");
                btn.Content = "\u274C Failed";
            }
            finally
            {
                await System.Threading.Tasks.Task.Delay(2000);
                btn.Content = originalContent;
                btn.IsEnabled = true;
            }
        }

        public async void RestoreBackup()
        {
            if (_isRestoreInProgress) 
            {
                Window.ShowToast("Restore already in progress...", "\u23F3");
                return;
            }

            _isRestoreInProgress = true; 

            try
            {
                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;

                if (!Directory.Exists(destDir))
                {
                    await Window.ShowDialogAsync("Backup Folder Missing", $"Directory not found: {destDir}\n\nPlease check your backup path in Settings.", "\uD83D\uDCC2");
                    return;
                }

                Window.ShowToast("Scanning Backups...", "\uD83D\uDD0D");
                var backups = VaultManager.Shared.GetAvailableBackups(destDir);
                if (backups.Count == 0 || backups == null)
                {
                    await Window.ShowDialogAsync("Backup Not Found", "No backup files found in the backup directory.", "\uD83D\uDD0D");
                    return;
                }

                string? selectedBackup = await Window.ShowListDialogAsync("Restore Backup", "Select a restore point to apply:", backups, "\u23F3");
                if (string.IsNullOrEmpty(selectedBackup)) 
                {
                    return;
                }

                string selectedPath = Path.Combine(destDir, selectedBackup);
                
                string password = "";
                // Check extension
                if (Path.GetExtension(selectedPath).ToLower() == ".vault")
                {
                     // Ask for password
                     var result = await Window.GetVaultPasswordAsync(true);
                     if (result.password == null) return; // Cancelled
                     password = result.password;
                }

                // 1. Confirm Restore
                bool confirm = await Window.ShowDialogAsync("Confirm Restore?", 
                    "Restoring a backup requires an application restart.\n\nCosmoWhisper will close, apply your settings, and launch automatically.\n\nContinue?", 
                    "\u26A0\uFE0F", true);
                
                if (!confirm) 
                {
                    return;
                }

                // 2. Extract to Staging
                Window.ShowToast("Decrupting & Verifying...", "\uD83D\uDD13");
                string stagingPath = await VaultManager.Shared.ExtractToStaging(selectedPath, password);
                
                if (string.IsNullOrEmpty(stagingPath) || !Directory.Exists(stagingPath))
                {
                    throw new Exception("Failed to prepare staging environment.");
                }

                string appDataPath = PreferenceManager.Shared.AppDataFolder;
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string sessionGuid = Guid.NewGuid().ToString("N").Substring(0, 8);

                // 3. Create & Run PowerShell Helper Script
                string scriptPath = Path.Combine(Path.GetTempPath(), $"cosmo_restore_{sessionGuid}.ps1");
                string logPath = Path.Combine(Path.GetTempPath(), $"CosmoRestore_{sessionGuid}.log");
                
                string scriptContent = $@"
Add-Type -AssemblyName PresentationFramework
$ErrorActionPreference = 'Stop'
$log = '{logPath.Replace("\\", "\\\\")}'

function Log-Msg($msg) {{
    ""[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $msg"" | Out-File $log -Append
}}

try {{
    Log-Msg ""Starting Restore Session {sessionGuid}""
    Log-Msg ""Source: {stagingPath.Replace("\\", "\\\\")}""
    Log-Msg ""Target: {appDataPath.Replace("\\", "\\\\")}""
    Log-Msg ""ExePath: {exePath.Replace("\\", "\\\\")}""

    # 1. Wait for process closure
    Start-Sleep -Seconds 2
    $fullPath = '{exePath.Replace("\\", "\\\\")}'
    $pname = [System.IO.Path]::GetFileNameWithoutExtension($fullPath)
    
    $retry = 0
    while ($retry -lt 15) {{
        $proc = Get-Process $pname -ErrorAction SilentlyContinue
        if (-not $proc) {{ break }}
        Log-Msg ""Waiting for $pname to exit (Attempt $retry)...""
        if ($retry -gt 5) {{ $proc | Stop-Process -Force -ErrorAction SilentlyContinue }}
        Start-Sleep -Seconds 1
        $retry++
    }}

    # COOLDOWN
    Start-Sleep -Seconds 2

    # 2. Copy Files
    if (Test-Path '{stagingPath.Replace("\\", "\\\\")}') {{
        Log-Msg ""Applying files...""
        Get-ChildItem -Path '{stagingPath.Replace("\\", "\\\\")}' -File | ForEach-Object {{
            $dest = Join-Path '{appDataPath.Replace("\\", "\\\\")}' $_.Name
            Copy-Item $_.FullName -Destination $dest -Force
            Log-Msg ""Restored: $($_.Name)""
        }}
    }}

    # 3. Cleanup
    Remove-Item -Path '{stagingPath.Replace("\\", "\\\\")}' -Recurse -Force -ErrorAction SilentlyContinue
    
    # 4. Restart
    Start-Process -FilePath $fullPath -WorkingDirectory ""$(Split-Path -Path $fullPath)""
    Remove-Item -Path $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue
}} catch {{
    Log-Msg ""FAILURE: $($_.Exception.Message)""
    [System.Windows.MessageBox]::Show(""Restore Failed: $($_.Exception.Message)"", ""CosmoWhisper"")
}}
";
                File.WriteAllText(scriptPath, scriptContent, System.Text.Encoding.UTF8);

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                await Window.ShowDialogAsync("Restore Failure", $"Error: {ex.Message}", "\u274C");
            }
            finally
            {
                _isRestoreInProgress = false;
            }
        }

        public void UpdateActivationUI()
        {
            if (Window.TxtHotkey == null) return;
            var p = PreferenceManager.Shared.Preferences;
            
            if (p.MouseButton != "None") Window.TxtHotkey.Text = p.MouseButton;
            else Window.TxtHotkey.Text = p.ActivationKey;
        }

        public void StartActivationCapture()
        {
            _isCapturingHotkey = true;
            if (Window.TxtHotkey != null) Window.TxtHotkey.Visibility = Visibility.Collapsed;
            if (Window.TxtListeningPrompt != null) Window.TxtListeningPrompt.Visibility = Visibility.Visible;
            
            Window.KeyDown += HandleUniversalCapture;
            Window.PreviewMouseDown += HandleUniversalCaptureMouse;
        }

        public void StopActivationCapture()
        {
            _isCapturingHotkey = false;
            Window.KeyDown -= HandleUniversalCapture;
            Window.PreviewMouseDown -= HandleUniversalCaptureMouse;
            
            if (Window.TxtHotkey != null) 
            {
                Window.TxtHotkey.Visibility = Visibility.Visible;
                UpdateActivationUI();
            }
            if (Window.TxtListeningPrompt != null) Window.TxtListeningPrompt.Visibility = Visibility.Collapsed;
        }

        private void HandleUniversalCapture(object sender, KeyEventArgs e)
        {
            if (!_isCapturingHotkey) return;
            
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            
            if (key == Key.Escape)
            {
                StopActivationCapture();
                e.Handled = true;
                return;
            }

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            var p = PreferenceManager.Shared.Preferences;
            p.ActivationKey = key.ToString();
            p.VirtualKey = vk;
            p.MouseButton = "None";
            PreferenceManager.Shared.Save();

            StopActivationCapture();
            e.Handled = true;
        }

        private void HandleUniversalCaptureMouse(object sender, MouseButtonEventArgs e)
        {
            if (!_isCapturingHotkey) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                StopActivationCapture();
                e.Handled = true;
                return;
            }

            var p = PreferenceManager.Shared.Preferences;
            p.MouseButton = e.ChangedButton.ToString();
            p.ActivationKey = "NONE";
            p.VirtualKey = 0;
            PreferenceManager.Shared.Save();

            StopActivationCapture();
            e.Handled = true;
        }

        public void ClearHotkey()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.ActivationKey = "NONE";
            p.VirtualKey = 0;
            p.MouseButton = "None";
            PreferenceManager.Shared.Save();
            UpdateActivationUI();
        }

        public void ChangeBackupPath()
        {
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

                    if (Window.TxtBackupPath != null) Window.TxtBackupPath.Text = p.BackupDirectory;
                }
            }
        }



        public void ToggleRegionalSpelling()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.EnableRegionalSpelling = !p.EnableRegionalSpelling;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleRegionalSpelling, p.EnableRegionalSpelling);
        }

        public void ToggleLaunchOnStartup()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.LaunchOnStartup = !p.LaunchOnStartup;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleLaunchOnStartup, p.LaunchOnStartup);

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (p.LaunchOnStartup)
                        {
                            string exePath = Process.GetCurrentProcess().MainModule.FileName;
                            key.SetValue("CosmoWhisper", $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue("CosmoWhisper", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update startup registry: {ex.Message}");
            }
        }

        public void SetLanguage(string langCode)
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.InterfaceLanguage != langCode)
            {
                p.InterfaceLanguage = langCode;
                PreferenceManager.Shared.Save();
            }
        }

        public void SetDateFormat(string format)
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.SelectedDateFormat != format)
            {
                p.SelectedDateFormat = format;
                PreferenceManager.Shared.Save();
            }
        }

        public void SetTimeFormat(string format)
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.SelectedTimeFormat != format)
            {
                p.SelectedTimeFormat = format;
                PreferenceManager.Shared.Save();
            }
        }

        public void ToggleManusAgent()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.EnableManusAgent = !p.EnableManusAgent;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleManusAgent, p.EnableManusAgent);
        }

        public void ToggleManusNarration()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.ManusNarrationEnabled = !p.ManusNarrationEnabled;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleManusNarration, p.ManusNarrationEnabled);
        }

        public void ToggleLocalWhisper()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.UseLocalWhisper = !p.UseLocalWhisper;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleLocalWhisper, p.UseLocalWhisper);
            Window.ShowToast(p.UseLocalWhisper ? "Local Whisper Enabled" : "Local Whisper Disabled", "🔌");
        }

        public void ToggleForceCloudProxy()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.ForceCloudProxy = !p.ForceCloudProxy;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleForceCloudProxy, p.ForceCloudProxy);
            Window.ShowToast(p.ForceCloudProxy ? "VPN Mode Enabled" : "VPN Mode Disabled", "🛡️");
        }

        public void ToggleLocalOnly()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.UseLocalWhisperOnly = !p.UseLocalWhisperOnly;
            PreferenceManager.Shared.Save();
            UpdateLocalOnlyUI();
            Window.ShowToast(p.UseLocalWhisperOnly ? "Survival Mode Enabled" : "Hybrid Mode Enabled", p.UseLocalWhisperOnly ? "🛡️" : "🌐");
        }

        public void UpdateLocalOnlyUI()
        {
            if (Window.ToggleLocalOnly == null) return;
            var p = PreferenceManager.Shared.Preferences;
            bool isOn = p.UseLocalWhisperOnly;

            // Simple UpdateToggle helper
            UpdateToggle(Window.ToggleLocalOnly, isOn);

            // Update Text & Icons for premium feel
            if (Window.LocalModeStatusTitle != null) 
                Window.LocalModeStatusTitle.Text = isOn ? "Survival Mode: Local Only" : "Cloud-Optimized (Hybrid)";

            if (Window.LocalModeStatusDesc != null)
                Window.LocalModeStatusDesc.Text = isOn ? "Zero data leaves this machine. AI model running locally." : "Uses super-fast Groq API with local fallback.";

            if (Window.LocalOverlayIcon != null)
                Window.LocalOverlayIcon.Text = isOn ? "🛡️" : "🌐";
        }
    }
}




