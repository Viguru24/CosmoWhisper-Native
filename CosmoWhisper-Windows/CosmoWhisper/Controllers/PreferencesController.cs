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

            UpdateInsertionUI();

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

            if (Window.TxtApiKey != null)
            {
                _ = Window.TxtApiKey.Password; // Warm up
                Window.TxtApiKey.Password = p.OpenAIApiKey;
                Window.TxtApiKey.PasswordChanged += (s, e) =>
                {
                    if (p.OpenAIApiKey != Window.TxtApiKey.Password)
                    {
                        p.OpenAIApiKey = Window.TxtApiKey.Password;
                        if (Window.TxtOpenAIApiKey_Int != null) Window.TxtOpenAIApiKey_Int.Password = p.OpenAIApiKey;
                        PreferenceManager.Shared.Save();
                    }
                };
            }

            if (Window.TxtOpenAIApiKey_Int != null)
            {
                Window.TxtOpenAIApiKey_Int.Password = p.OpenAIApiKey;
                Window.TxtOpenAIApiKey_Int.PasswordChanged += (s, e) =>
                {
                    if (p.OpenAIApiKey != Window.TxtOpenAIApiKey_Int.Password)
                    {
                        p.OpenAIApiKey = Window.TxtOpenAIApiKey_Int.Password;
                        if (Window.TxtApiKey != null) Window.TxtApiKey.Password = p.OpenAIApiKey;
                        PreferenceManager.Shared.Save();
                    }
                };
            }

            if (Window.TxtGroqApiKey != null)
            {
                Window.TxtGroqApiKey.Password = p.GroqApiKey;
                Window.TxtGroqApiKey.PasswordChanged += (s, e) =>
                {
                    if (p.IsAIUnlocked)
                    {
                        p.GroqApiKey = Window.TxtGroqApiKey.Password;
                        PreferenceManager.Shared.Save();
                    }
                };
            }


            if (Window.TxtAnthropicApiKey != null)
            {
                Window.TxtAnthropicApiKey.Password = p.AnthropicApiKey;
                Window.TxtAnthropicApiKey.PasswordChanged += (s, e) =>
                {
                    if (p.IsAIUnlocked)
                    {
                        p.AnthropicApiKey = Window.TxtAnthropicApiKey.Password;
                        PreferenceManager.Shared.Save();
                    }
                };
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

        public void SetInsertionMode(InsertionMethod method)
        {
            var p = PreferenceManager.Shared.Preferences;
            p.InsertionMode = method;
            PreferenceManager.Shared.Save();
            UpdateInsertionUI();
        }

        public void UpdateInsertionUI()
        {
            if (Window.BtnFastPaste == null || Window.BtnDirectType == null) return;
            var p = PreferenceManager.Shared.Preferences;
            bool isFast = p.InsertionMode == InsertionMethod.FastPaste;

            Window.BtnFastPaste.Opacity = isFast ? 1.0 : 0.6;
            Window.BtnFastPaste.BorderBrush = isFast ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF")) : Brushes.Transparent;

            Window.BtnDirectType.Opacity = !isFast ? 1.0 : 0.6;
            Window.BtnDirectType.BorderBrush = !isFast ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007AFF")) : Brushes.Transparent;
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
                var (password, name) = await Window.GetVaultPasswordAsync();
                if (string.IsNullOrEmpty(password)) return;

                btn.Content = "🛡️ Securing Vault...";
                btn.IsEnabled = false;

                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;
                Directory.CreateDirectory(destDir);

                string vaultPath = await VaultManager.Shared.CreateVaultAsync(name, password, destDir);
                
                await System.Threading.Tasks.Task.Delay(1000);

                btn.Content = "✅ Vault Secured";
                long size = new FileInfo(vaultPath).Length;
                string sizeDisplay = size < 1024 * 1024 ? $"{(size / 1024.0):F1} KB" : $"{(size / (1024.0 * 1024.0)):F2} MB";

                await Window.ShowDialogAsync("Vault Created", 
                    $"Universal Encryption Successful!\n\nVault: {Path.GetFileName(vaultPath)}\nSize: {sizeDisplay}\n\nYour environment is now secured with 256-bit AES.", 
                    "\uD83D\uDEE1");
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
            // Force reset to prevent lockouts, relying on modal UI to prevent re-entry
            _isRestoreInProgress = true; 

            try
            {
                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;

                if (!Directory.Exists(destDir))
                {
                    await Window.ShowDialogAsync("No Backups", "No backup directory found.", "\uD83D\uDCC2");
                    return;
                }

                var vaults = VaultManager.Shared.GetAvailableVaults(destDir);
                if (vaults.Count == 0)
                {
                    await Window.ShowDialogAsync("Vault Not Found", "No encrypted vaults (.vault) found.", "\uD83D\uDD0D");
                    return;
                }

                string? selectedVaultName = await Window.ShowListDialogAsync("Select Vault", "Choose a restore point:", vaults, "\uD83D\uDEE1");
                if (string.IsNullOrEmpty(selectedVaultName)) return;

                string selectedVaultPath = Path.Combine(destDir, selectedVaultName);

                var (password, _) = await Window.GetVaultPasswordAsync(true);
                if (string.IsNullOrEmpty(password)) return;

                // 1. Verify Password First
                var (isValid, verifyMsg) = await VaultManager.Shared.VerifyVault(selectedVaultPath, password);
                if (!isValid)
                {
                    await Window.ShowDialogAsync("Verification Failed", verifyMsg, "\u274C");
                    return;
                }

                // 2. Warn User about Restart
                bool confirm = await Window.ShowDialogAsync("Restart Required", 
                    "To ensure a safe restore, CosmoWhisper must restart.\n\nThe app will close, apply settings, and reopen automatically.", 
                    "\u26A0\uFE0F", true); // Warning Icon
                
                if (!confirm) return;

                // 3. Extract to Staging
                string stagingPath = await VaultManager.Shared.ExtractToStaging(selectedVaultPath, password);
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // 4. Create & Run PowerShell Helper Script
                string scriptPath = Path.Combine(Path.GetTempPath(), "cosmo_restore_restart.ps1");
                string logPath = Path.Combine(Path.GetTempPath(), "CosmoRestoreLog.txt");
                
                string scriptContent = $@"
try {{
    ""[$(Get-Date)] Starting Restore Process..."" | Out-File '{logPath}'
    ""[$(Get-Date)] Staging: {stagingPath}"" | Out-File '{logPath}' -Append
    ""[$(Get-Date)] AppData: {appDataPath}"" | Out-File '{logPath}' -Append

    Start-Sleep -Seconds 2
    
    if (Test-Path '{stagingPath}') {{
        ""[$(Get-Date)] Copying files..."" | Out-File '{logPath}' -Append
        Copy-Item -Path '{stagingPath}\*' -Destination '{appDataPath}' -Recurse -Force -ErrorAction Stop
        ""[$(Get-Date)] Copy Complete."" | Out-File '{logPath}' -Append
    }} else {{
        ""[$(Get-Date)] ERROR: Staging path not found!"" | Out-File '{logPath}' -Append
    }}

    ""[$(Get-Date)] Restarting App: {exePath}"" | Out-File '{logPath}' -Append
    Start-Process '{exePath}'
}} catch {{
    ""[$(Get-Date)] FATAL ERROR: $_"" | Out-File '{logPath}' -Append
    [System.Windows.Forms.MessageBox]::Show(""Restore Error: $_"", ""CosmoWhisper Restore"")
}}
";
                File.WriteAllText(scriptPath, scriptContent);

                // Run visible for now to debug, but non-blocking
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden, // Keep hidden but log
                    CreateNoWindow = true
                };

                Process.Start(psi);

                // 5. Shutdown App
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                await Window.ShowDialogAsync("Restore Error", ex.Message, "\u274C");
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
    }
}


