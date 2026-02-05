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

        public PreferencesController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            var p = PreferenceManager.Shared.Preferences;

            if (Window.ToggleClipboard != null) UpdateToggle(Window.ToggleClipboard, p.RestoreClipboard);
            if (Window.ToggleAutoSubmit != null) UpdateToggle(Window.ToggleAutoSubmit, p.AutoSubmit);
            if (Window.ToggleAutoCopy != null) UpdateToggle(Window.ToggleAutoCopy, p.AutoCopy);
            if (Window.ToggleMouseButton != null) UpdateToggle(Window.ToggleMouseButton, p.MouseButton != "None");
            if (Window.TxtBackupPath != null) Window.TxtBackupPath.Text = p.BackupDirectory;
            if (Window.TxtHotkey != null) Window.TxtHotkey.Text = p.ActivationKey;

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

            UpdateMouseConfigUI();

            // Language Settings
            if (Window.ToggleRegionalSpelling != null) UpdateToggle(Window.ToggleRegionalSpelling, p.EnableRegionalSpelling);
            if (Window.ToggleLaunchOnStartup != null) UpdateToggle(Window.ToggleLaunchOnStartup, p.LaunchOnStartup);
            if (Window.ComboLanguage != null)
            {
                foreach (ComboBoxItem item in Window.ComboLanguage.Items)
                {
                    if (item.Tag?.ToString() == p.InterfaceLanguage)
                    {
                        Window.ComboLanguage.SelectedItem = item;
                        break;
                    }
                }
            }

            // DateTime Formatting
            if (Window.ComboDateTime != null)
            {
                if (string.IsNullOrEmpty(p.SelectedDateFormat)) p.SelectedDateFormat = "dd/MM/yyyy";

                foreach (ComboBoxItem item in Window.ComboDateTime.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedDateFormat)
                    {
                        Window.ComboDateTime.SelectedItem = item;
                        break;
                    }
                }
            }

            if (Window.TxtApiKey != null)
            {
                Window.TxtApiKey.Password = p.OpenAIApiKey;
                Window.TxtApiKey.PasswordChanged += (s, e) =>
                {
                    p.OpenAIApiKey = Window.TxtApiKey.Password;
                    PreferenceManager.Shared.Save();
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

            if (Window.TxtOpenAIApiKey_Int != null)
            {
                Window.TxtOpenAIApiKey_Int.Password = p.OpenAIApiKey;
                Window.TxtOpenAIApiKey_Int.PasswordChanged += (s, e) =>
                {
                    if (p.IsAIUnlocked)
                    {
                        p.OpenAIApiKey = Window.TxtOpenAIApiKey_Int.Password;
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
            Window.ShowToast(p.AutoSubmit ? "Auto-Submit Enabled" : "Auto-Submit Disabled", "⚡");
        }

        public void ToggleAutoCopy()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.AutoCopy = !p.AutoCopy;
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleAutoCopy, p.AutoCopy);
            Window.ShowToast(p.AutoCopy ? "Auto-Copy Enabled" : "Auto-Copy Disabled", "📋");
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
            if (btn == null) btn = Window.BtnBackupNow; // Fallback
            if (btn == null) return;

            string originalContent = btn.Content?.ToString() ?? "Backup Now";

            try
            {
                // Ask for password and name
                var (password, name) = await Window.GetVaultPasswordAsync();
                if (string.IsNullOrEmpty(password)) return;

                btn.Content = "Securing Vault...";
                btn.IsEnabled = false;

                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;
                Directory.CreateDirectory(destDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
                string safeName = string.IsNullOrWhiteSpace(name) ? "CosmoVault" : string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string finalVaultPath = Path.Combine(destDir, $"{safeName}_{timestamp}.vault");

                // 1. Create temporary zip in memory or temp file
                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                
                try
                {
                    string sourceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
                    string tempZip = Path.Combine(tempDir, "data.zip");

                    using (var archive = System.IO.Compression.ZipFile.Open(tempZip, System.IO.Compression.ZipArchiveMode.Create))
                    {
                        // Backup all JSON data files in the root folder
                        if (Directory.Exists(sourceFolder))
                        {
                            var dataFiles = Directory.GetFiles(sourceFolder, "*.json");
                            foreach (var fullPath in dataFiles)
                            {
                                string fileName = Path.GetFileName(fullPath);
                                archive.CreateEntryFromFile(fullPath, fileName);
                            }
                        }
                    }

                    // 2. Encrypt the zip file
                    SecurityManager.EncryptFile(tempZip, finalVaultPath, password);
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }

                await System.Threading.Tasks.Task.Delay(1000);

                btn.Content = "✅ Vault Secured";
                long size = new FileInfo(finalVaultPath).Length;
                
                string sizeDisplay;
                if (size < 1024 * 1024)
                {
                    double sizeInKb = size / 1024.0;
                    sizeDisplay = $"{sizeInKb:F1} KB";
                }
                else
                {
                    double sizeInMb = size / (1024.0 * 1024.0);
                    sizeDisplay = $"{sizeInMb:F2} MB";
                }

                await Window.ShowDialogAsync("Vault Created", $"Universal Encryption Successful!\n\nFile: CosmoVault_{timestamp}.vault\nSize: {sizeDisplay}\n\nYour environment is now secured with 256-bit AES.", "🛡️");
            }
            catch (Exception ex)
            {
                await Window.ShowDialogAsync("Backup Failed", $"Error: {ex.Message}", "❌");
                btn.Content = "❌ Failed";
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
            try
            {
                var p = PreferenceManager.Shared.Preferences;
                string destDir = p.BackupDirectory;

                if (!Directory.Exists(destDir))
                {
                await Window.ShowDialogAsync("No Backups", "No backup directory found.", "📁");
                    return;
                }

                var vaults = Directory.GetFiles(destDir, "*.vault")
                    .OrderByDescending(f => f)
                    .ToList();

                if (vaults.Count == 0)
                {
                    // Fallback search
                    var legacyDirs = Directory.GetDirectories(destDir, "CosmoVault_*")
                        .OrderByDescending(d => d)
                        .ToList();

                    if (legacyDirs.Count > 0)
                    {
                        var res = await Window.ShowDialogAsync("Legacy Backup", $"Found {legacyDirs.Count} unencrypted legacy backup(s). Restore the most recent one?", "⚠️", true);
                        if (res)
                        {
                            PreferenceManager.Shared.Restore(legacyDirs[0]);
                            Initialize();
                            await Window.ShowDialogAsync("Success", "Legacy restore successful.", "✅");
                        }
                    }
                    else
                    {
                        await Window.ShowDialogAsync("Vault Not Found", "No encrypted vaults (.vault) found in the backup folder.", "🔍");
                    }
                    return;
                }

                // Show selection list
                var vaultNames = vaults.Select(v => Path.GetFileName(v)).ToList();
                string? selectedVaultName = await Window.ShowListDialogAsync("Select Vault", "Choose a restore point from your secure history:", vaultNames, "🛡️");

                if (string.IsNullOrEmpty(selectedVaultName)) return;

                string selectedVaultPath = Path.Combine(destDir, selectedVaultName);

                // Wait for dialog to fully close before showing password overlay
                await System.Threading.Tasks.Task.Delay(400);

                // Ask for password
                var (password, _) = await Window.GetVaultPasswordAsync(true);
                if (string.IsNullOrEmpty(password)) return;

                // 1. Decrypt to temporary zip
                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    string tempZip = Path.Combine(tempDir, "data.zip");
                    SecurityManager.DecryptFile(selectedVaultPath, tempZip, password);

                    // 2. Unzip to AppData
                    string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
                    
                    using (var archive = System.IO.Compression.ZipFile.OpenRead(tempZip))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name)) continue;

                            string fullPath = Path.Combine(appDataFolder, entry.FullName);
                            string? directory = Path.GetDirectoryName(fullPath);
                            if (directory != null) Directory.CreateDirectory(directory);
                            
                            // Retry logic for locked files (like settings.json being written to)
                            int retries = 3;
                            bool success = false;
                            while (retries > 0 && !success)
                            {
                                try
                                {
                                    entry.ExtractToFile(fullPath, true);
                                    success = true;
                                }
                                catch (IOException ex) when (retries > 1)
                                {
                                    retries--;
                                    await System.Threading.Tasks.Task.Delay(500);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error extracting {entry.Name}: {ex.Message}");
                                    throw; // Re-throw to be caught by the outer catch
                                }
                            }
                        }
                    }

                    // 3. Reload Managers
                    PreferenceManager.Shared.Load();
                    VocabularyManager.Shared.Load();
                    
                    // 4. Force UI refresh
                    Window.InitializeAll(); 
                    
                    await Window.ShowDialogAsync("Restore Successful", "Your settings and vocabulary have been restored successfully.", "✨");
                }
                catch (CryptographicException)
                {
                    await Window.ShowDialogAsync("Restore Failed", "Incorrect vault password. Please try again.", "❌");
                }
                catch (Exception ex)
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cosmo_errors.log");
                    try { File.AppendAllText(logPath, $"{DateTime.Now}: Restore Error: {ex}\n"); } catch { }
                    await Window.ShowDialogAsync("Restore Failed", $"Restore failed: {ex.Message}", "❌");
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                await Window.ShowDialogAsync("Restore Error", $"An unexpected error occurred: {ex.Message}", "❌");
            }
        }

        public void StartHotkeyCapture()
        {
            _isCapturingHotkey = true;
            if (Window.TxtHotkey != null) Window.TxtHotkey.Text = "PRESS ANY KEY...";
            Window.KeyDown += HandleHotkeyCapture;
        }

        public void StopHotkeyCapture()
        {
            _isCapturingHotkey = false;
            Window.KeyDown -= HandleHotkeyCapture;
            if (Window.TxtHotkey != null)
            {
                var p = PreferenceManager.Shared.Preferences;
                Window.TxtHotkey.Text = p.ActivationKey;
            }
        }

        internal void HandleHotkeyCapture(object sender, KeyEventArgs e)
        {
            if (!_isCapturingHotkey) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            
            // If Escape is pressed, stop capture without saving
            if (key == Key.Escape)
            {
                StopHotkeyCapture();
                e.Handled = true;
                return;
            }

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            var p = PreferenceManager.Shared.Preferences;
            p.ActivationKey = key.ToString();
            p.VirtualKey = vk;
            PreferenceManager.Shared.Save();

            if (Window.TxtHotkey != null) Window.TxtHotkey.Text = p.ActivationKey;

            _isCapturingHotkey = false;
            Window.KeyDown -= HandleHotkeyCapture;
            e.Handled = true;
        }

        public void ClearHotkey()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.ActivationKey = "NONE";
            p.VirtualKey = 0;
            PreferenceManager.Shared.Save();
            if (Window.TxtHotkey != null) Window.TxtHotkey.Text = "NONE";
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

        public void ToggleMouseButton()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.MouseButton == "None") p.MouseButton = "Middle";
            else p.MouseButton = "None";
            PreferenceManager.Shared.Save();
            UpdateToggle(Window.ToggleMouseButton, p.MouseButton != "None");
        }

        public void UpdateMouseConfigUI()
        {
            if (Window.TxtMouseConfig == null) return;

            var p = PreferenceManager.Shared.Preferences;
            if (string.IsNullOrEmpty(p.MouseButton) || p.MouseButton == "None")
            {
                Window.TxtMouseConfig.Text = "Click to configure...";
                Window.TxtMouseConfig.Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.6 };
            }
            else
            {
                Window.TxtMouseConfig.Text = $"{p.MouseButton} Button Configured";
                Window.TxtMouseConfig.Foreground = Brushes.White;
            }
        }

        public void StartMouseCapture()
        {
            if (Window.TxtMouseConfig == null) return;

            Window.TxtMouseConfig.Text = "Press any mouse button...";
            Window.TxtMouseConfig.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF453A"));

            Window.PreviewMouseDown += CaptureMouseButton;
        }

        internal void CaptureMouseButton(object sender, MouseButtonEventArgs e)
        {
            Window.PreviewMouseDown -= CaptureMouseButton;

            if (e.ChangedButton == MouseButton.Left)
            {
                Window.TxtMouseConfig.Text = PreferenceManager.Shared.Preferences.MouseButton;
                Window.TxtMouseConfig.Foreground = Brushes.White;
                return;
            }

            string buttonName = e.ChangedButton.ToString();

            var p = PreferenceManager.Shared.Preferences;
            p.MouseButton = buttonName;
            PreferenceManager.Shared.Save();

            UpdateMouseConfigUI();
            e.Handled = true;
        }

        public void ClearMouseConfig()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.MouseButton = "None";
            PreferenceManager.Shared.Save();

            UpdateMouseConfigUI();
            UpdateToggle(Window.ToggleMouseButton, false);
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
