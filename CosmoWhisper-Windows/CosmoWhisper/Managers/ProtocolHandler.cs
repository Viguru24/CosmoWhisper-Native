using System;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using CosmoWhisper.Managers;
using CosmoWhisper;
using System.IO;
using System.Threading.Tasks;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers
{
    public class ProtocolHandler
    {
        public static string ProtocolName => PreferenceManager.Shared.IsDevMode ? "cosmowhisper-dev" : "cosmowhisper";
        private static readonly object _dedupeLock = new object();
        private static string _lastHandledUrl = "";
        private static DateTime _lastHandledTime = DateTime.MinValue;

        private static void LogEvent(string msg)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "cosmo_debug.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - [Protocol] {msg}\n");
            }
            catch { }
        }

        public static void Register()
        {
            // Skip manual registry registration for Store/Packaged versions (it's in the manifest)
            if (PreferenceManager.Shared.Preferences.IsStoreVersion) return;

            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // Use CurrentUser instead of ClassesRoot to avoid requiring Administrator privileges
                using (var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes"))
                using (var key = classes.CreateSubKey(ProtocolName))
                {
                    key.SetValue("", "URL:CosmoWhisper Protocol");
                    key.SetValue("URL Protocol", "");

                    using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", $"\"{exePath}\",1");
                    }

                    using (var command = key.CreateSubKey(@"shell\open\command"))
                    {
                        command.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register protocol: {ex.Message}");
                // Fallback to HKCR if HKCU fails for some reason (rare)
                try
                {
                    using (var key = Registry.ClassesRoot.CreateSubKey(ProtocolName))
                    {
                        key.SetValue("", "URL:CosmoWhisper Protocol");
                        key.SetValue("URL Protocol", "");
                        using (var command = key.CreateSubKey(@"shell\open\command"))
                        {
                            command.SetValue("", $"\"{Process.GetCurrentProcess().MainModule.FileName}\" \"%1\"");
                        }
                    }
                }
                catch { }
            }
        }

        public static void Handle(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith(ProtocolName + ":", StringComparison.OrdinalIgnoreCase)) return;

            lock (_dedupeLock)
            {
                if (string.Equals(url, _lastHandledUrl, StringComparison.Ordinal) && (DateTime.UtcNow - _lastHandledTime).TotalSeconds < 2.0)
                {
                    return;
                }
                _lastHandledUrl = url;
                _lastHandledTime = DateTime.UtcNow;
            }

            try
            {
                string command = url;
                if (command.StartsWith(ProtocolName + "://", StringComparison.OrdinalIgnoreCase))
                    command = command.Substring((ProtocolName + "://").Length);
                else if (command.StartsWith(ProtocolName + ":", StringComparison.OrdinalIgnoreCase))
                    command = command.Substring((ProtocolName + ":").Length);
                    
                if (command.Contains("?"))
                {
                    string action = command.Split('?')[0].TrimEnd('/');
                    string query = command.Split('?')[1];
                    
                    LogEvent($"Protocol Action: {action}, Query: {query}");

                    if (action.Equals("auth", StringComparison.OrdinalIgnoreCase))
                    {
                        string token = GetQueryParam(query, "token");
                        string email = GetQueryParam(query, "email");
                        string tier = GetQueryParam(query, "tier");

                        if (!string.IsNullOrEmpty(token))
                        {
                            // Step 1: Save to prefs immediately (synchronous)
                            var p = PreferenceManager.Shared.Preferences;
                            p.AuthToken = token;
                            if (!string.IsNullOrEmpty(email)) p.UserEmail = email;
                            if (!string.IsNullOrEmpty(tier)) p.UserTier = tier;
                            PreferenceManager.Shared.Save();

                            // Step 1.5: Force clear blocked state and start heartbeat
                            AppState.Shared.IsAccessBlocked = false;
                            AccountService.Shared.StartHeartbeat();

                            // Step 2: Update UI
                            _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                                var dashboard = DashboardWindow.Instance;
                                if (dashboard == null)
                                {
                                    // Create it if it doesn't exist
                                    dashboard = new DashboardWindow();
                                }

                                dashboard.Show();
                                dashboard.WindowState = WindowState.Normal;
                                dashboard.Activate();
                                dashboard.Topmost = true;
                                dashboard.Topmost = false; // Kick to front but don't stay topmost
                                
                                if (dashboard._navigation != null)
                                {
                                    dashboard._navigation.ShowDashboard();
                                }
                                
                                await UIEvents.Shared.ShowMessageAsync("Signed In", $"Welcome, {(string.IsNullOrEmpty(email) ? "user" : email)}! 🎉", "👋");
                                await dashboard.CheckAuthStatus();
                            });
                        }
                        else
                        {
                            LogEvent("Auth token was empty, skipping UI update.");
                        }
                    }
                    else if (action.Equals("license", StringComparison.OrdinalIgnoreCase))
                    {
                        string token = GetQueryParam(query, "token");
                        LogEvent($"License action received. Token: {!string.IsNullOrEmpty(token)}");
                        if (!string.IsNullOrEmpty(token))
                        {
                            PreferenceManager.Shared.Preferences.LicenseToken = token;
                            PreferenceManager.Shared.Save();
                            LogEvent("License token saved to preferences.");

                            _ = Task.Run(async () =>
                            {
                                LogEvent("Attempting to sync license status in background.");
                                var result = await BackendService.Shared.SyncStatus();
                                if (result == SyncResult.Success)
                                {
                                    await UIEvents.Shared.ShowMessageAsync("Access Granted", "License Token successfully activated!", "🎟️");
                                    LogEvent("License activated message shown.");
                                }
                                else
                                {
                                    LogEvent("License sync failed.");
                                }
                            });
                        }
                    }
                    else if (action.Equals("unlock", StringComparison.OrdinalIgnoreCase))
                    {
#if DEV_BUILD
                        var key = GetQueryParam(query, "key");
                        if (PreferenceManager.Shared.IsDevMode && key == "COSMO_PREMIUM_2024")
                        {
                            PreferenceManager.Shared.Preferences.IsAIUnlocked = true;
                            PreferenceManager.Shared.Save();
                            _ = UIEvents.Shared.ShowMessageAsync("Protocol Activation", "Dev build intelligence unlocked.", "👤");
                            LogEvent("AI Unlocked via dev protocol.");
                        }
#else
                        LogEvent("Protocol unlock skipped in production build.");
#endif
                    }
                }
                else
                {
                    LogEvent("Command does not contain a query parameter.");
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error handling protocol: {ex.Message}");
            }
        }

        private static string? GetQueryParam(string query, string name)
        {
            var parts = query.Split('&');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length >= 2 && kv[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    // Handle values that might contain '=' (like JWT padding or nested params)
                    return Uri.UnescapeDataString(string.Join("=", kv.Skip(1)));
                }
            }
            return null;
        }
    }
}
