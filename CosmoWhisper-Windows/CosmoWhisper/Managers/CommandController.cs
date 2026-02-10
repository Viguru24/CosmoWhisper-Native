using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers
{
    public class CommandController
    {
        public static CommandController Shared { get; } = new CommandController();
        public event Action? CommandExecuted;

        private readonly Dictionary<string, string> _webShortcuts = new()
        {
            {"google", "https://google.com"},
            {"github", "https://github.com"},
            {"groq", "https://groq.com"},
            {"chatgpt", "https://chatgpt.com"},
            {"claude", "https://claude.ai"},
            {"reddit", "https://reddit.com"},
            {"twitter", "https://x.com"},
            {"x", "https://x.com"},
            {"facebook", "https://facebook.com"},
            {"instagram", "https://instagram.com"},
            {"linkedin", "https://linkedin.com"},
            {"netflix", "https://netflix.com"},
            {"amazon", "https://amazon.com"},
            {"wikipedia", "https://wikipedia.org"},
            {"gmail", "https://mail.google.com"},
            {"outlook", "https://outlook.live.com"},
            {"twitch", "https://twitch.tv"},
            {"youtube", "https://youtube.com"},
            {"cosmowhisper", "https://cosmowhisper.com"},
            {"library", "https://cosmowhisper.com/smart-commands"}
        };

        private readonly Dictionary<string, string> _appShortcuts = new()
        {
            {"word", "winword"},
            {"microsoft word", "winword"},
            {"excel", "excel"},
            {"microsoft excel", "excel"},
            {"powerpoint", "powerpnt"},
            {"microsoft powerpoint", "powerpnt"},
            {"outlook", "outlook"},
            {"microsoft outlook", "outlook"},
            {"chrome", "chrome"},
            {"google chrome", "chrome"},
            {"firefox", "firefox"},
            {"mozilla firefox", "firefox"},
            {"edge", "msedge"},
            {"microsoft edge", "msedge"},
            {"calculator", "calc"},
            {"notepad", "notepad"},
            {"terminal", "wt"}, // Windows Terminal
            {"windows terminal", "wt"},
            {"cmd", "cmd"},
            {"command prompt", "cmd"},
            {"code", "code"}, // VS Code
            {"vscode", "code"},
            {"visual studio code", "code"},
            {"spotify", "spotify"},
            {"vlc", "vlc"},
            {"discord", "discord"},
            {"whatsapp", "whatsapp:"},
            {"whats app", "whatsapp:"},
            {"browser", "chrome"},
            {"explorer", "explorer"},
            {"file explorer", "explorer"},
            {"settings", "ms-settings:"},
            {"task manager", "taskmgr"},
            {"paint", "mspaint"},
            {"veracrypt", @"C:\Program Files\VeraCrypt\VeraCrypt.exe"},
            {"shredder", @"C:\Program Files (x86)\File Shredder\fileshredder.exe"}
        };

        public async Task<bool> Handle(string text)
        {
            // Check for Macros first (ITEM 4)
            if (await MacroManager.Shared.TryExecuteMacro(text)) return true;

            string cmd = Regex.Replace(text.ToLower(), @"[^\w\s]", "").Trim();

            if (string.IsNullOrWhiteSpace(cmd)) return false;

            // Debug logging
            LogToFile($"[CommandController] Original: '{text}' | Processed: '{cmd}'");

            // Normalize "openapp" -> "open app"
            if (cmd.StartsWith("open") && !cmd.StartsWith("open ") && cmd.Length > 4)
            {
                cmd = "open " + cmd.Substring(4);
            }
            
            // Normalize "launchapp" -> "launch app"
            if (cmd.StartsWith("launch") && !cmd.StartsWith("launch ") && cmd.Length > 6)
            {
                cmd = "launch " + cmd.Substring(6);
            }


            // Helper for trigger matching
            bool IsTriggered(params string[] triggers)
                => triggers.Any(t => cmd == t || cmd.StartsWith(t + " "));

            // --- 1. CONFIG / MODES ---
            if (IsTriggered("typing mode", "use typing mode")) { return true; } // Mocked setting
            if (IsTriggered("paste mode", "use paste mode")) { return true; }

            // --- 2. TEXT FORMATTING ---
            if (IsTriggered("uppercase", "all caps")) { await ProcessAIOnSelection("Make the text uppercase."); return true; }
            if (IsTriggered("lowercase", "all lowercase")) { await ProcessAIOnSelection("Make the text lowercase."); return true; }
            if (IsTriggered("title case")) { await ProcessAIOnSelection("Convert the text to Title Case."); return true; }

            if (cmd == "select all") { InputController.Shared.ExecuteKeystroke("a", ctrl: true); return true; }
            if (cmd == "undo" || cmd == "undo that") { InputController.Shared.ExecuteKeystroke("z", ctrl: true); return true; }
            if (cmd == "redo" || cmd == "redo that") { InputController.Shared.ExecuteKeystroke("y", ctrl: true); return true; }
            if (cmd == "paste" || cmd == "paste that") { InputController.Shared.ExecuteKeystroke("v", ctrl: true); return true; }

            if (cmd == "delete all" || cmd == "clear all")
            {
                InputController.Shared.ExecuteKeystroke("a", ctrl: true);
                await Task.Delay(100);
                InputController.Shared.ExecuteKeystroke("backspace");
                return true;
            }

            // --- 3. CLIPBOARD ---
            if (IsTriggered("cut all", "cut everything"))
            {
                InputController.Shared.ExecuteKeystroke("a", ctrl: true);
                await Task.Delay(100);
                InputController.Shared.ExecuteKeystroke("x", ctrl: true);
                return true;
            }
            if (IsTriggered("copy that", "copy all"))
            {
                if (cmd == "copy all") { InputController.Shared.ExecuteKeystroke("a", ctrl: true); await Task.Delay(100); }
                InputController.Shared.ExecuteKeystroke("c", ctrl: true);
                return true;
            }

            // --- 4. SYSTEM ---
            // --- 4. SYSTEM ---
            if (IsTriggered("insert date", "todays date", "current date"))
            {
                string fmt = PreferenceManager.Shared.Preferences.SelectedDateFormat;
                await InputController.Shared.PasteText(DateTime.Now.ToString(fmt) + " ", false, false);
                CommandExecuted?.Invoke(); return true;
            }
            if (IsTriggered("insert time", "current time"))
            {
                string fmt = PreferenceManager.Shared.Preferences.SelectedTimeFormat;
                await InputController.Shared.PasteText(DateTime.Now.ToString(fmt) + " ", false, false);
                CommandExecuted?.Invoke(); return true;
            }
            if (IsTriggered("insert date and time"))
            {
                string dFmt = PreferenceManager.Shared.Preferences.SelectedDateFormat;
                string tFmt = PreferenceManager.Shared.Preferences.SelectedTimeFormat;
                await InputController.Shared.PasteText(DateTime.Now.ToString($"{dFmt} {tFmt}"), false, false);
                CommandExecuted?.Invoke(); return true;
            }

            if (IsTriggered("volume up", "louder")) { InputController.Shared.SendKey(0xAF, false); InputController.Shared.SendKey(0xAF, true); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("volume down", "quieter")) { InputController.Shared.SendKey(0xAE, false); InputController.Shared.SendKey(0xAE, true); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("mute")) { InputController.Shared.SendKey(0xAD, false); InputController.Shared.SendKey(0xAD, true); CommandExecuted?.Invoke(); return true; }

            // --- 5. WEB / SEARCH ---
            if (cmd.StartsWith("visit ") || cmd.StartsWith("go to "))
            {
                string site = cmd.Replace("visit ", "").Replace("go to ", "").Trim();
                bool ok = OpenSite(site);
                if (ok) CommandExecuted?.Invoke();
                return ok;
            }
            if (cmd.StartsWith("search ") || cmd.StartsWith("google "))
            {
                string query = cmd.Replace("search ", "").Replace("google ", "").Trim();
                Process.Start(new ProcessStartInfo($"https://www.google.com/search?q={Uri.EscapeDataString(query)}") { UseShellExecute = true });
                CommandExecuted?.Invoke(); return true;
            }
            if (cmd.StartsWith("youtube "))
            {
                string query = cmd.Replace("youtube ", "").Trim();
                Process.Start(new ProcessStartInfo($"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}") { UseShellExecute = true });
                CommandExecuted?.Invoke(); return true;
            }

            // --- 6. DEVELOPER TOOLS ---
            if (cmd == "dev tools" || cmd == "devtools" || cmd == "developer tools" ||
                cmd == "f12" || cmd == "inspect" || cmd == "inspector" || cmd == "console")
            {
                InputController.Shared.SendKey(0x7B, false); // F12 key
                InputController.Shared.SendKey(0x7B, true);
                CommandExecuted?.Invoke(); return true;
            }

            // --- 7. APP LAUNCHING ---
            if (cmd.StartsWith("open ") || cmd.StartsWith("launch "))
            {
                string appQuery = cmd.Replace("open ", "").Replace("launch ", "").Trim();
                LogToFile($"[CommandController] Attempting to launch app: '{appQuery}'");
                
                // 1. Try exact match
                bool ok = await LaunchApp(appQuery);
                
                // 2. Try identifying the app name within a longer sentence (e.g. "Launch WhatsApp is not working")
                if (!ok)
                {
                    var knownApp = _appShortcuts.Keys
                        .OrderByDescending(k => k.Length)
                        .FirstOrDefault(k => appQuery.StartsWith(k + " ") || appQuery == k);
                        
                    if (knownApp != null)
                    {
                        LogToFile($"[CommandController] Found partial match: '{knownApp}' in '{appQuery}'");
                        ok = await LaunchApp(knownApp);
                    }
                }
                
                LogToFile($"[CommandController] Launch result: {ok}");
                if (ok) CommandExecuted?.Invoke();
                return ok;
            }

            // --- 7. AI SMART COMMANDS ---
            if (cmd.StartsWith("ask "))
            {
                string question = text.Substring(text.ToLower().IndexOf("ask ") + 4);
                _ = ProcessAI(question, "You are a helpful assistant. Answer concisely.");
                CommandExecuted?.Invoke(); return true;
            }

            if (IsTriggered("summarize", "summarize this", "summarise", "summarise this")) { await SummarizeAndRead(); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("fix grammar", "polish", "fix this")) { await ProcessAIOnSelection("Fix the grammar and improve the flow of this text. Maintain the original meaning."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("make professional", "professionalize")) { await ProcessAIOnSelection("Rewrite the following text to sound professional, corporate, and polite."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("make friendly")) { await ProcessAIOnSelection("Rewrite the following text to sound friendly and approachable."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to spanish")) { await ProcessAIOnSelection("Translate the following text into Spanish."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to french")) { await ProcessAIOnSelection("Translate the following text into French."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("bullet points", "make bullet list")) { await ProcessAIOnSelection("Convert this text into a clean bulleted list using '•'."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("shorter", "condense")) { await ProcessAIOnSelection("Make the following text concise and punchy."); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("expand", "flesh out")) { await ProcessAIOnSelection("Expand on this text to make it more descriptive."); CommandExecuted?.Invoke(); return true; }

            // --- 8. OPEN ACCESSIBILITY ---
            if (IsTriggered("read this", "read selection", "speak this", "say this")) { await ReadSelection(); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("stop reading", "stop speaking", "stop playback", "shush", "hush")) { NarrationManager.Shared.CancelSpeech(); CommandExecuted?.Invoke(); return true; }

            return false;
        }

        private async Task<string> CaptureSelectionRobust()
        {
            // 1. Send Ctrl+C to try to update clipboard with current selection
            InputController.Shared.ExecuteKeystroke("c", ctrl: true);
            
            // 2. Wait for clipboard to update (progressive checks)
            for (int i = 0; i < 4; i++)
            {
                await Task.Delay(250); // Checks at 250, 500, 750, 1000ms
                
                string current = "";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try { if (System.Windows.Clipboard.ContainsText()) current = System.Windows.Clipboard.GetText(); } catch { }
                });

                // If we found text, great! But is it new?
                // We can't easily know if it's new or old without clearing, 
                // but clearing breaks the "Manual Copy -> Summarize" workflow.
                // So we'll accept whatever is there, assuming the user's intent 
                // matches the clipboard state (either auto-copied or manually copied).
                if (!string.IsNullOrWhiteSpace(current)) return current;
            }

            // Fallback: Check one last time without waiting
            string finalCheck = "";
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try { if (System.Windows.Clipboard.ContainsText()) finalCheck = System.Windows.Clipboard.GetText(); } catch { }
            });
            
            return finalCheck;
        }

        private async Task ReadSelection()
        {
            try
            {
                string selection = await CaptureSelectionRobust();

                if (string.IsNullOrWhiteSpace(selection))
                {
                    await NarrationManager.Shared.SpeakAsync("I didn't see any text selected. Please highlight the text and try again.");
                    return;
                }

                // Speak
                await NarrationManager.Shared.SpeakAsync(selection);
            }
            catch (Exception ex)
            {
                LogToFile($"ReadSelection Error: {ex.Message}");
            }
        }

        private async Task SummarizeAndRead()
        {
            try
            {
                LogToFile("SummarizeAndRead started.");

                string selection = await CaptureSelectionRobust();

                if (string.IsNullOrWhiteSpace(selection))
                {
                    LogToFile("SummarizeAndRead: No selection found.");
                    await NarrationManager.Shared.SpeakAsync("I couldn't find any selected text. Please select the text you want me to summarize.");
                    return;
                }

                await NarrationManager.Shared.SpeakAsync("Summarizing selection...");

                // AI Summarize
                string prompt = "Provide an extremely brief summary. Maximum 2 short sentences. Focus only on the core meaning.";
                string summary = await AIService.Shared.ProcessCommand(prompt, selection);

                if (summary.StartsWith("Error:"))
                {
                    LogToFile($"SummarizeAndRead AI Error: {summary}");
                    await NarrationManager.Shared.SpeakAsync("I encountered an error trying to summarize the text.");
                    return;
                }

                LogToFile($"SummarizeAndRead: Summary generated ({summary.Length} chars). Reading aloud.");
                await NarrationManager.Shared.SpeakAsync(summary);
            }
            catch (Exception ex)
            {
                LogToFile($"SummarizeAndRead Error: {ex.Message}");
            }
        }

        private bool OpenSite(string name)
        {
            if (_webShortcuts.TryGetValue(name, out string url))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }
            if (name.Contains("."))
            {
                Process.Start(new ProcessStartInfo("https://" + name) { UseShellExecute = true });
                CommandExecuted?.Invoke();
                return true;
            }
            return false;
        }

        private string NormalizeAppName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            // Remove spaces and non-alphanumeric characters for fuzzy matching
            return Regex.Replace(name.ToLower(), @"[^a-z0-9]", "");
        }

        private string FindAppInStartMenu(string appName)
        {
            try
            {
                string normSearch = NormalizeAppName(appName);
                if (string.IsNullOrEmpty(normSearch)) return null;

                // Common locations for Start Menu shortcuts
                string[] locations = {
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs)
                };

                var candidates = new List<(string path, int score)>();

                foreach (var loc in locations)
                {
                    if (string.IsNullOrEmpty(loc) || !Directory.Exists(loc)) continue;

                    // Search for .lnk files (shortcuts)
                    var files = Directory.GetFiles(loc, "*.lnk", SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string normFile = NormalizeAppName(fileName);

                        // SCORING LOGIC:
                        // 100: Exact match
                        if (fileName.Equals(appName, StringComparison.OrdinalIgnoreCase)) candidates.Add((file, 100));
                        // 90: Normalized exact match (e.g., "Cosmo Vault" == "CosmoVault")
                        else if (normFile == normSearch) candidates.Add((file, 90));
                        // 80: Starts with
                        else if (fileName.StartsWith(appName, StringComparison.OrdinalIgnoreCase)) candidates.Add((file, 80));
                        // 70: Normalized starts with
                        else if (normFile.StartsWith(normSearch)) candidates.Add((file, 70));
                        // 60: Contains
                        else if (fileName.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0) candidates.Add((file, 60));
                        // 50: Normalized contains
                        else if (normFile.Contains(normSearch)) candidates.Add((file, 50));
                    }
                }

                var bestMatch = candidates.OrderByDescending(c => c.score).FirstOrDefault();
                if (bestMatch.path != null)
                {
                    LogToFile($"[SearchStartMenu] Found fuzzy match (score {bestMatch.score}): {bestMatch.path}");
                    return bestMatch.path;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"[SearchStartMenu] Error: {ex.Message}");
            }

            return null;
        }

        private async Task<bool> LaunchApp(string name)
        {
            string target = null;
            
            // 1. Check hardcoded dictionary (Exact)
            if (_appShortcuts.TryGetValue(name.ToLower(), out string shortcut))
            {
                target = shortcut;
            }

            // 1b. Check hardcoded dictionary (Normalized)
            if (target == null)
            {
                string normSearch = NormalizeAppName(name);
                target = _appShortcuts
                    .Where(kvp => NormalizeAppName(kvp.Key) == normSearch)
                    .Select(kvp => kvp.Value)
                    .FirstOrDefault();
            }
            
            // 2. Dynamic Search in Start Menu (Background Task)
            if (target == null)
            {
                target = await Task.Run(() => FindAppInStartMenu(name));
            }

            // 3. Try to launch whatever we found (or the original name as a last resort)
            string launchPath = target ?? name;
            
            try
            {
                Process.Start(new ProcessStartInfo(launchPath) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"[LaunchApp] Error opening '{launchPath}': {ex.Message}");
                
                // Show pop-up if the app absolutely could not be opened
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    System.Windows.MessageBox.Show($"The application '{name}' could not be opened or found.", 
                        "CosmoWhisper - App Launch Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                });
                
                return false;
            }
        }

        private async Task ProcessAI(string context, string prompt)
        {
            try
            {
                string result = await AIService.Shared.ProcessCommand(prompt, context);
                if (!result.StartsWith("Error:"))
                {
                    await InputController.Shared.PasteText(result, false, false);
                }
                else
                {
                    LogToFile($"AI Error: {result}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"AI Exception: {ex.Message}");
            }
        }

        private async Task ProcessAIOnSelection(string prompt)
        {
            try
            {
                LogToFile($"ProcessAIOnSelection started with prompt: {prompt}");

                string selection = await CaptureSelectionRobust();

                if (string.IsNullOrWhiteSpace(selection))
                {
                    LogToFile("No selection found. Aborting.");
                    await NarrationManager.Shared.SpeakAsync("Please select some text first.");
                    return;
                }

                // Process
                string result = await AIService.Shared.ProcessCommand(prompt, selection);
                LogToFile($"AI Result received: {result.Length} chars");

                // Paste back
                if (!result.StartsWith("Error:"))
                {
                    await InputController.Shared.PasteText(result, false, false);
                    LogToFile("Result pasted back successfully.");
                }
                else
                {
                    LogToFile($"AI Processing Failed: {result}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"AI Selection Error: {ex.Message}");
            }
        }


        private void LogToFile(string msg)
        {
            try
            {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                Directory.CreateDirectory(logPath);
                File.AppendAllText(Path.Combine(logPath, "command_debug.txt"), $"{DateTime.Now}: {msg}\n");
            }
            catch { }
        }
    }
}
