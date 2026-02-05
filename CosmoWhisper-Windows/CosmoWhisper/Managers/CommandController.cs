using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
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
            string cmd = Regex.Replace(text.ToLower(), @"[^\w\s]", "").Trim();

            if (string.IsNullOrWhiteSpace(cmd)) return false;

            try { System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "cosmo_commands.txt"), $"{DateTime.Now}: Processing '{cmd}' (Original: '{text}')\n"); } catch { }

            // Normalize "openapp" -> "open app"
            if (cmd.StartsWith("open") && !cmd.StartsWith("open ") && cmd.Length > 4)
            {
                cmd = "open " + cmd.Substring(4);
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
                await InputController.Shared.PasteText(DateTime.Now.ToString(fmt), false, false);
                CommandExecuted?.Invoke(); return true;
            }
            if (IsTriggered("insert time", "current time"))
            {
                string fmt = PreferenceManager.Shared.Preferences.SelectedTimeFormat;
                await InputController.Shared.PasteText(DateTime.Now.ToString(fmt), false, false);
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
                string app = cmd.Replace("open ", "").Replace("launch ", "").Trim();
                bool ok = LaunchApp(app);
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

        private async Task ReadSelection()
        {
            try
            {
                // 1. Copy selection
                InputController.Shared.ExecuteKeystroke("c", ctrl: true);
                await Task.Delay(350);

                string selection = "";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Clipboard.ContainsText()) selection = System.Windows.Clipboard.GetText();
                });

                if (string.IsNullOrWhiteSpace(selection)) return;

                // 2. Speak
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

                // 1. Copy selection
                InputController.Shared.ExecuteKeystroke("c", ctrl: true);
                await Task.Delay(350);

                string selection = "";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Clipboard.ContainsText()) selection = System.Windows.Clipboard.GetText();
                });

                if (string.IsNullOrWhiteSpace(selection))
                {
                    LogToFile("SummarizeAndRead: No selection found.");
                    return;
                }

                // 2. AI Summarize
                string prompt = "Summarize the following text into 2-3 concise sentences.";
                string summary = await AIService.Shared.ProcessCommand(prompt, selection);

                if (summary.StartsWith("Error:"))
                {
                    LogToFile($"SummarizeAndRead AI Error: {summary}");
                    return;
                }

                LogToFile($"SummarizeAndRead: Summary generated ({summary.Length} chars). Reading aloud.");

                // 3. Optional: Paste summary back? 
                // User didn't explicitly ask to replace text, just to "read it to you".
                // Let's just read it.
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

        private bool LaunchApp(string name)
        {
            if (_appShortcuts.TryGetValue(name, out string app))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(app) { UseShellExecute = true });
                    return true;
                }
                catch { }
            }
            try
            {
                Process.Start(new ProcessStartInfo(name) { UseShellExecute = true });
                return true;
            }
            catch { return false; }
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

                // 1. Copy selection
                InputController.Shared.ExecuteKeystroke("c", ctrl: true);
                await Task.Delay(350); // Increased delay for clipboard sync

                string selection = "";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        selection = System.Windows.Clipboard.GetText();
                        LogToFile($"Selection captured: {selection.Length} chars");
                    }
                    else
                    {
                        LogToFile("Clipboard empty after copy attempt.");
                    }
                });

                if (string.IsNullOrWhiteSpace(selection))
                {
                    LogToFile("No selection found. Aborting.");
                    return;
                }

                // 2. Process
                string result = await AIService.Shared.ProcessCommand(prompt, selection);
                LogToFile($"AI Result received: {result.Length} chars");

                // 3. Paste back
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
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "cosmo_commands.txt");
                System.IO.File.AppendAllText(path, $"{DateTime.Now}: {msg}\n");
            }
            catch { }
        }
    }
}
