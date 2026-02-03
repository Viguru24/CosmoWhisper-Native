using System;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Managers
{
    public class ProtocolHandler
    {
        private const string ProtocolName = "cosmowhisper";

        public static void Register()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                
                using (var key = Registry.ClassesRoot.CreateSubKey(ProtocolName))
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
            }
        }

        public static void Handle(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith(ProtocolName + "://")) return;

            try
            {
                string command = url.Substring((ProtocolName + "://").Length);
                if (command.Contains("?"))
                {
                    string action = command.Split('?')[0];
                    string query = command.Split('?')[1];
                    var parms = System.Web.HttpUtility.ParseQueryString(query);

                    if (action.Equals("unlock", StringComparison.OrdinalIgnoreCase))
                    {
                        var key = GetQueryParam(query, "key");
                        if (key == "COSMO_PREMIUM_2024") // Example key
                        {
                            PreferenceManager.Shared.Preferences.IsAIUnlocked = true;
                            PreferenceManager.Shared.Save();
                            System.Windows.MessageBox.Show("Welcome to the Inner Circle. Cosmo Intelligence Unlocked!", "Protocol Activation", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else if (action.Equals("configure", StringComparison.OrdinalIgnoreCase))
                    {
                        var groqKey = GetQueryParam(query, "groq");
                        if (!string.IsNullOrEmpty(groqKey))
                        {
                            PreferenceManager.Shared.Preferences.GroqApiKey = groqKey;
                            PreferenceManager.Shared.Save();
                            System.Windows.MessageBox.Show("Groq API Key synced from website!", "Configuration Synced", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else if (action.Equals("license", StringComparison.OrdinalIgnoreCase))
                    {
                        string token = GetQueryParam(query, "token");
                        if (!string.IsNullOrEmpty(token))
                        {
                            PreferenceManager.Shared.Preferences.LicenseToken = token;
                            PreferenceManager.Shared.Save();
                            
                            // Immediately sync status
                            Task.Run(async () => {
                                bool success = await LicenseManager.Shared.SyncStatusAsync();
                                if (success) {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                        System.Windows.MessageBox.Show("License Token successfully activated!", "Access Granted", MessageBoxButton.OK, MessageBoxImage.Information);
                                    });
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling protocol: {ex.Message}");
            }
        }

        private static string GetQueryParam(string query, string name)
        {
            var parts = query.Split('&');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(kv[1]);
                }
            }
            return null;
        }
    }
}
