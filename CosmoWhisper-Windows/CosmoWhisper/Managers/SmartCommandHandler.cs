using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CosmoWhisper.Managers
{
    /// <summary>
    /// Handles "Magic Commands" - voice commands that trigger system actions
    /// Ported from the Mac/Electron TypeScript implementation
    /// </summary>
    public class SmartCommandHandler
    {
        public static async Task<bool> Handle(string text)
        {
            var cmd = text.ToLower().Trim().TrimEnd('.', ',', '!', '?');
            Debug.WriteLine($"[SmartCommand] Processing: \"{cmd}\"");

            // ===== TEXT MANIPULATION =====
            if (cmd == "delete all" || cmd == "clear all")
            {
                SendKeys.SendWait("^a");
                await Task.Delay(100);
                SendKeys.SendWait("{BACKSPACE}");
                return true;
            }

            if (cmd == "select all")
            {
                SendKeys.SendWait("^a");
                return true;
            }

            if (cmd == "copy all" || cmd == "copy that")
            {
                SendKeys.SendWait("^a");
                await Task.Delay(100);
                SendKeys.SendWait("^c");
                return true;
            }

            if (cmd == "cut all")
            {
                SendKeys.SendWait("^a");
                await Task.Delay(100);
                SendKeys.SendWait("^x");
                return true;
            }

            if (cmd == "paste" || cmd == "paste that" || cmd == "paste here")
            {
                SendKeys.SendWait("^v");
                return true;
            }

            if (cmd == "undo" || cmd == "undo that" || cmd == "revert")
            {
                SendKeys.SendWait("^z");
                return true;
            }

            // ===== UTILITY =====
            if (cmd == "insert date" || cmd == "todays date" || cmd == "current date")
            {
                var dateStr = DateTime.Now.ToString("dddd, d MMMM yyyy"); // e.g., "Monday, 30 January 2026"
                SendKeys.SendWait(dateStr);
                return true;
            }

            if (cmd == "insert time" || cmd == "current time")
            {
                var timeStr = DateTime.Now.ToString("h:mm tt"); // e.g., "5:44 PM"
                SendKeys.SendWait(timeStr);
                return true;
            }

            // ===== APP LAUNCHER =====
            if (cmd.StartsWith("open ") || cmd.StartsWith("launch "))
            {
                var appName = cmd.Replace("open ", "").Replace("launch ", "").Trim();
                return LaunchApp(appName);
            }

            // ===== WEB NAVIGATION =====
            if (cmd.StartsWith("visit ") || cmd.StartsWith("go to "))
            {
                var site = cmd.Replace("visit ", "").Replace("go to ", "").Trim();
                return OpenWebsite(site);
            }

            return false; // Command not recognized
        }

        private static bool LaunchApp(string appName)
        {
            var apps = new System.Collections.Generic.Dictionary<string, string>
            {
                { "word", "winword" },
                { "microsoft word", "winword" },
                { "excel", "excel" },
                { "microsoft excel", "excel" },
                { "powerpoint", "powerpnt" },
                { "microsoft powerpoint", "powerpnt" },
                { "outlook", "outlook" },
                { "chrome", "chrome" },
                { "google chrome", "chrome" },
                { "firefox", "firefox" },
                { "edge", "msedge" },
                { "microsoft edge", "msedge" },
                { "calculator", "calc" },
                { "notepad", "notepad" },
                { "terminal", "wt" },
                { "windows terminal", "wt" },
                { "cmd", "cmd" },
                { "command prompt", "cmd" },
                { "code", "code" },
                { "vscode", "code" },
                { "visual studio code", "code" },
                { "spotify", "spotify" },
                { "discord", "discord" },
                { "explorer", "explorer" },
                { "file explorer", "explorer" },
                { "settings", "ms-settings:" },
                { "task manager", "taskmgr" },
                { "paint", "mspaint" },
                { "veracrypt", @"C:\Program Files\VeraCrypt\VeraCrypt.exe" },
                { "shredder", @"C:\Program Files (x86)\File Shredder\fileshredder.exe" }
            };

            if (apps.TryGetValue(appName, out var target))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = target,
                        UseShellExecute = true
                    });
                    Debug.WriteLine($"[SmartCommand] Launched: {appName} -> {target}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SmartCommand] Failed to launch {appName}: {ex.Message}");
                    return false;
                }
            }

            // Fallback: try to launch by name
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appName,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool OpenWebsite(string site)
        {
            var shortcuts = new System.Collections.Generic.Dictionary<string, string>
            {
                { "google", "https://google.com" },
                { "youtube", "https://youtube.com" },
                { "chatgpt", "https://chatgpt.com" },
                { "groq", "https://groq.com" },
                { "github", "https://github.com" },
                { "reddit", "https://reddit.com" },
                { "twitter", "https://x.com" },
                { "x", "https://x.com" },
                { "amazon", "https://amazon.com" },
                { "gmail", "https://mail.google.com" },
                { "outlook", "https://outlook.live.com" }
            };

            if (shortcuts.TryGetValue(site, out var url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    Debug.WriteLine($"[SmartCommand] Opened: {site} -> {url}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SmartCommand] Failed to open {site}: {ex.Message}");
                    return false;
                }
            }

            // Fallback: try .com or search
            try
            {
                var fallbackUrl = $"https://duckduckgo.com/?q=!+{Uri.EscapeDataString(site)}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = fallbackUrl,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
