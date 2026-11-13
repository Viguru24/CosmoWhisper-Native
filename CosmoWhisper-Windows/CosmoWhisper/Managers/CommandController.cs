using System;
using CosmoWhisper.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers
{
    public class CommandController : ICommandController
    {
        public static CommandController Shared => (CommandController)ServiceLocator.GetService<ICommandController>();
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
            {"microsoft", "https://microsoft.com"},
            {"microsoft.com", "https://microsoft.com"},
            {"cosmowhisper", "https://cosmowhisper.com"},
            {"library", "https://cosmowhisper.com/features"},
            {"commands", "https://cosmowhisper.com/features"}
        };

        private readonly Dictionary<string, string> _appShortcuts = new()
        {
            {"word", "winword"},
            {"microsoft word", "winword"},
            {"textmaker", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\TextMaker.exe"},
            {"text maker", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\TextMaker.exe"},
            {"excel", "excel"},
            {"microsoft excel", "excel"},
            {"planmaker", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\PlanMaker.exe"},
            {"plan maker", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\PlanMaker.exe"},
            {"powerpoint", "powerpnt"},
            {"microsoft powerpoint", "powerpnt"},
            {"presentations", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\Presentations.exe"},
            {"present maker", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\Presentations.exe"},
            {"presentation maker", @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\Presentations.exe"},
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

            string cmd = Regex.Replace(text.ToLower(), @"[^\w\s\.]", "").Trim();
            cmd = cmd.TrimStart('.', ' ', '\t');
            cmd = cmd.TrimEnd('.', '!', '?'); // Remove trailing punctuation
            cmd = cmd.Trim().ToLower();
            
            // CLEANUP: If the command starts with junk (like repeat hashes from a leaking hook), remove it.
            // This ensures commands like "Translate into French" are recognized even if some hashes leak.
            cmd = Regex.Replace(cmd, @"^[#\s]+", "").Trim();

            if (string.IsNullOrWhiteSpace(cmd)) return false;

            // Debug logging
            LogToFile($"[CommandController] Original: '{text}' | Processed: '{cmd}'");
            
            if (cmd.Contains("help")) { LogToFile($"[CommandController] HELP DETECTED: {cmd}"); }
            if (cmd.Contains("stop")) { LogToFile($"[CommandController] STOP DETECTED: {cmd}"); }

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
            {
                // 1. Strict match or starts with (e.g. "open WhatsApp")
                if (triggers.Any(t => cmd == t || cmd.StartsWith(t + " "))) return true;
                
                // 2. Flexible matching (ignore filler words like "okay", "please")
                // Only apply for specific smart commands
                var flexibleTriggers = new[] { "summarize", "summarise", "write summary", "right summary", "type summary", "paste summary", "translate", "polish" };
                if (triggers.Any(t => flexibleTriggers.Contains(t)))
                {
                    // Look for the trigger at the end of a command that might have junk at the start
                    foreach (var t in triggers)
                    {
                        if (cmd.EndsWith(" " + t) || cmd.EndsWith(t))
                        {
                            // Verify it's not preceded by something that negates it (e.g. "don't summarize")
                            // (Simple heuristic: if it's longer than just filler, we allow it)
                            return true;
                        }
                    }
                }
                
                return false;
            }

            // --- 1. CONFIG / MODES ---
            if (IsTriggered("typing mode", "use typing mode"))
            {
                PreferenceManager.Shared.Preferences.UsePasteMode = false;
                PreferenceManager.Shared.Save();
                CommandExecuted?.Invoke();
                return true;
            }
            if (IsTriggered("paste mode", "use paste mode"))
            {
                PreferenceManager.Shared.Preferences.UsePasteMode = true;
                PreferenceManager.Shared.Save();
                CommandExecuted?.Invoke();
                return true;
            }

            if (IsTriggered("press enter on", "enable press enter", "turn on press enter"))
            {
                PreferenceManager.Shared.Preferences.AutoSubmit = true;
                PreferenceManager.Shared.Save();
                _ = NarrationManager.Shared.SpeakAsync("Press enter enabled.");
                CommandExecuted?.Invoke();
                return true;
            }
            if (IsTriggered("press enter off", "disable press enter", "turn off press enter"))
            {
                PreferenceManager.Shared.Preferences.AutoSubmit = false;
                PreferenceManager.Shared.Save();
                _ = NarrationManager.Shared.SpeakAsync("Press enter disabled.");
                CommandExecuted?.Invoke();
                return true;
            }

            // --- 2. TEXT FORMATTING ---
            if (IsTriggered("uppercase", "upper case", "all caps", "i'll put case", "apple case", "up a case")) { await ProcessAIOnSelection("Make the text uppercase. Output ONLY the uppercase text.", usePersonality: false); return true; }
            if (IsTriggered("lowercase", "lower case", "all lowercase")) { await ProcessAIOnSelection("Make the text lowercase. Output ONLY the lowercase text.", usePersonality: false); return true; }
            if (IsTriggered("title case")) { await ProcessAIOnSelection("Convert the text to Title Case. Output ONLY the text.", usePersonality: false); return true; }

            if (IsTriggered("select all", "select everything")) { InputController.Shared.ExecuteKeystroke("a", ctrl: true); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("undo", "undo that")) { InputController.Shared.ExecuteKeystroke("z", ctrl: true); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("redo", "redo that")) { InputController.Shared.ExecuteKeystroke("y", ctrl: true); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("paste", "paste that", "paste all", "past all", "past that")) 
            { 
                if (cmd.Contains("all")) { InputController.Shared.ExecuteKeystroke("a", ctrl: true); await Task.Delay(100); }
                InputController.Shared.ExecuteKeystroke("v", ctrl: true); 
                CommandExecuted?.Invoke();
                return true; 
            }

            if (IsTriggered("delete all", "clear all", "delete everything", "clear everything"))
            {
                InputController.Shared.ExecuteKeystroke("a", ctrl: true);
                await Task.Delay(100);
                InputController.Shared.ExecuteKeystroke("backspace");
                CommandExecuted?.Invoke();
                return true;
            }

            // --- 3. CLIPBOARD ---
            if (IsTriggered("cut all", "cut everything"))
            {
                InputController.Shared.ExecuteKeystroke("a", ctrl: true);
                await Task.Delay(100);
                InputController.Shared.ExecuteKeystroke("x", ctrl: true);
                CommandExecuted?.Invoke();
                return true;
            }
            if (IsTriggered("copy that", "copy all", "copy everything"))
            {
                if (cmd.Contains("all") || cmd.Contains("everything")) { InputController.Shared.ExecuteKeystroke("a", ctrl: true); await Task.Delay(100); }
                InputController.Shared.ExecuteKeystroke("c", ctrl: true);
                CommandExecuted?.Invoke();
                return true;
            }

            // --- 3.5 SCREENSHOTS ---
            if (IsTriggered("screenshot", "take screenshot", "take screen shot", "take screen show", "screen clip", "snipping tool", "capture screen"))
            {
                Process.Start(new ProcessStartInfo { FileName = "ms-screenclip:", UseShellExecute = true });
                CommandExecuted?.Invoke();
                return true;
            }

            // --- 4. SYSTEM ---
            if (IsTriggered("insert date", "insert the date", "todays date", "current date"))
            {
                string fmt = PreferenceManager.Shared.Preferences.SelectedDateFormat;
                await InputController.Shared.PasteText(DateTime.Now.ToString(fmt) + " ", false, false);
                CommandExecuted?.Invoke(); return true;
            }
            if (IsTriggered("insert time", "insert the time", "current time"))
            {
                string fmt = PreferenceManager.Shared.Preferences.SelectedTimeFormat;
                await InputController.Shared.PasteText(DateTime.Now.ToString(fmt) + " ", false, false);
                CommandExecuted?.Invoke(); return true;
            }
            if (IsTriggered("insert date and time", "insert the date and time"))
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
                string site = (cmd.StartsWith("visit ") ? cmd.Substring(6) : cmd.Substring(6)).Trim();
                bool ok = OpenSite(site);
                if (ok) CommandExecuted?.Invoke();
                return ok;
            }
            // Search Google
            if (cmd.StartsWith("search ") || cmd.StartsWith("google "))
            {
                string query = (cmd.StartsWith("search ") ? cmd.Substring(7) : cmd.Substring(7)).Trim();
                if (query.StartsWith("for ")) query = query.Substring(4).Trim();
                if (query.StartsWith("youtube "))
                {
                    string ytQuery = query.Substring(8).Trim();
                    Process.Start(new ProcessStartInfo($"https://www.youtube.com/results?search_query={Uri.EscapeDataString(ytQuery)}") { UseShellExecute = true });
                    CommandExecuted?.Invoke(); return true;
                }
                Process.Start(new ProcessStartInfo($"https://www.google.com/search?q={Uri.EscapeDataString(query)}") { UseShellExecute = true });
                CommandExecuted?.Invoke(); return true;
            }

            // Search / Open YouTube with search query (e.g. "open youtube kitty cats", "youtube american politics")
            if (cmd.StartsWith("open youtube ") || cmd.StartsWith("open utube ") || cmd.StartsWith("open u tube ") ||
                cmd.StartsWith("youtube ") || cmd.StartsWith("utube ") || cmd.StartsWith("u tube "))
            {
                string query = cmd;
                if (cmd.StartsWith("open youtube ")) query = cmd.Substring(13);
                else if (cmd.StartsWith("open utube ")) query = cmd.Substring(11);
                else if (cmd.StartsWith("open u tube ")) query = cmd.Substring(12);
                else if (cmd.StartsWith("youtube ")) query = cmd.Substring(8);
                else if (cmd.StartsWith("utube ")) query = cmd.Substring(6);
                else if (cmd.StartsWith("u tube ")) query = cmd.Substring(7);
                
                query = query.Trim();
                if (query.StartsWith("for ")) query = query.Substring(4).Trim();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    Process.Start(new ProcessStartInfo($"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}") { UseShellExecute = true });
                    CommandExecuted?.Invoke();
                    return true;
                }
                else
                {
                    Process.Start(new ProcessStartInfo("https://youtube.com") { UseShellExecute = true });
                    CommandExecuted?.Invoke();
                    return true;
                }
            }

            // --- 6. DEVELOPER TOOLS ---
            if (cmd == "dev tools" || cmd == "devtools" || cmd == "developer tools" ||
                cmd == "f12" || cmd == "inspect" || cmd == "inspector" || cmd == "console")
            {
                InputController.Shared.SendKey(0x7B, false); // F12 key
                InputController.Shared.SendKey(0x7B, true);
                CommandExecuted?.Invoke(); return true;
            }

            // --- SYSTEM SETTINGS SHORTCUTS ---
            if (IsTriggered(
                "uninstall app", "remove app", "delete app",
                "uninstall program", "remove program", "delete program",
                "uninstall programme", "remove programme", "delete programme", // British spelling
                "removed program", "removed programme", "uninstalled programme", // Past tense/Speech error
                "add remove programs", "add or remove programs", "change or remove a program",
                "uninstalled program" // catch speech error
            ))
            {
                Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
                CommandExecuted?.Invoke();
                return true;
            }

            // --- 7. APP LAUNCHING ---
            if (cmd.StartsWith("open ") || cmd.StartsWith("launch "))
            {
                string appQuery = (cmd.StartsWith("open ") ? cmd.Substring(5) : cmd.Substring(7)).Trim();
                LogToFile($"[CommandController] Attempting to launch app or site: '{appQuery}'");
                
                // 1. Try if it's a known web shortcut or has a TLD suffix (e.g. "microsoft", "google.com")
                string nameWithoutTld = appQuery.TrimEnd('.');
                string[] commonTlds = { ".com", ".org", ".net", ".io", ".ai", ".app", ".dev" };
                bool isWebSite = _webShortcuts.ContainsKey(nameWithoutTld) || 
                                 nameWithoutTld.Contains(".") ||
                                 commonTlds.Any(tld => nameWithoutTld.EndsWith(tld));
                                 
                if (isWebSite)
                {
                    bool okWeb = OpenSite(appQuery);
                    if (okWeb)
                    {
                        CommandExecuted?.Invoke();
                        return true;
                    }
                }

                // 2. Try launching as local app
                bool ok = await LaunchApp(appQuery);
                
                // 3. Try identifying the app name within a longer sentence (e.g. "Launch WhatsApp is not working")
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
                
                // 4. Final fallback: try OpenSite if local app launching completely failed and it wasn't already run
                if (!ok && !isWebSite)
                {
                    ok = OpenSite(appQuery);
                }
                
                LogToFile($"[CommandController] Launch result: {ok}");
                if (ok) CommandExecuted?.Invoke();
                return ok;
            }

            // --- 7. AI SMART COMMANDS ---
            if (cmd.StartsWith("ask "))
            {
                string question = text.Substring(text.ToLower().IndexOf("ask ") + 4);
                _ = ProcessAI("You are a helpful assistant. Answer concisely.", question);
                CommandExecuted?.Invoke(); return true;
            }

            // Normalize common misspellings (summerise/summerize -> summarize)
            if (cmd.Contains("summerise")) cmd = cmd.Replace("summerise", "summarize");
            if (cmd.Contains("summerize")) cmd = cmd.Replace("summerize", "summarize");

            // Summarize (TEXT ONLY)
            if (IsTriggered("summarize", "summarise", "write summary", "type summary", "paste summary", "summarize that", "summarise that", "right summary") && !cmd.Contains("out loud") && !cmd.Contains("read")) 
            { 
                await ProcessAIOnSelection("Summarize the following text concisely. Capture key points only. Output ONLY the summary text.", usePersonality: false, useInterfaceLanguage: false); 
                CommandExecuted?.Invoke(); 
                return true; 
            }
            // Summarize (VOICE ONLY - Explicitly requested)
            if (IsTriggered("summarize out loud", "summarise out loud", "read summary", "tell me summary")) 
            { 
                await SummarizeAndRead(); 
                CommandExecuted?.Invoke(); 
                return true; 
            }
            if (IsTriggered("grammar", "correct grammar", "polish", "correct this")) { await ProcessAIOnSelection("Fix the grammar and improve the flow of this text. Maintain the original meaning. Output ONLY the fixed text.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("make professional", "professionalize")) { await ProcessAIOnSelection("Rewrite the following text to sound professional, corporate, and polite. Output ONLY the rewritten text without conversational filler or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("make friendly")) { await ProcessAIOnSelection("Rewrite the following text to sound friendly and approachable. Output ONLY the rewritten text without conversational filler or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to spanish", "translate into spanish", "translate spanish", "translate to castilian", "translate into castilian")) { await ProcessAIOnSelection("Translate the following text into Spanish. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to french", "translate into french", "translate french")) { await ProcessAIOnSelection("Translate the following text into French. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to german", "translate into german", "translate german")) { await ProcessAIOnSelection("Translate the following text into German. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to italian", "translate into italian", "translate italian")) { await ProcessAIOnSelection("Translate the following text into Italian. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to portuguese", "translate into portuguese", "translate portuguese")) { await ProcessAIOnSelection("Translate the following text into Portuguese. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to chinese", "translate into chinese", "translate chinese", "translate to mandarin")) { await ProcessAIOnSelection("Translate the following text into Chinese. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to japanese", "translate into japanese", "translate japanese")) { await ProcessAIOnSelection("Translate the following text into Japanese. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("translate to russian", "translate into russian", "translate russian")) { await ProcessAIOnSelection("Translate the following text into Russian. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }

            // --- EXTRACT LANGUAGE FROM COMMAND ---
            string language = null;
            // Only trigger translation if the word "translate" or "translation" is explicitly used
            var translationMatch = Regex.Match(cmd, @"(?:^|\s)(?:translate|translation)(?:\s+(?:it|this|that|the|text|message))?\s+(?:to|into|in)?\s+(\w+)(?:\s+translation)?");
            if (translationMatch.Success) 
            {
                language = translationMatch.Groups[1].Value.Trim();
            }
            else 
            {
                // Fallback for just "translate spanish"
                var simpleMatch = Regex.Match(cmd, @"(?:^|\s)translate\s+(\w+)$");
                if (simpleMatch.Success) language = simpleMatch.Groups[1].Value.Trim();
            }

            // Handle common languages without prefix ONLY if it's the exact command
            if (string.IsNullOrEmpty(language))
            {
                var exactCmd = cmd.Replace(" please", "").Trim();
                if (exactCmd == "to spanish" || exactCmd == "into spanish") language = "spanish";
                else if (exactCmd == "to french" || exactCmd == "into french") language = "french";
                else if (exactCmd == "to german" || exactCmd == "into german") language = "german";
                else if (exactCmd == "to italian" || exactCmd == "into italian") language = "italian";
                else if (exactCmd == "to portuguese" || exactCmd == "into portuguese") language = "portuguese";
                else if (exactCmd == "to chinese" || exactCmd == "into chinese") language = "chinese";
                else if (exactCmd == "to japanese" || exactCmd == "into japanese") language = "japanese";
                else if (exactCmd == "to russian" || exactCmd == "into russian") language = "russian";
            }

            if (!string.IsNullOrEmpty(language) && language != "the" && language != "this" && language != "that" && language != "please" && language != "now")
            {
                LogToFile($"[CommandController] Translation detected for language: '{language}' from cmd: '{cmd}'");
                await ProcessAIOnSelection($"Translate the following text into {language}. Output ONLY the translated text without conversational filler, explanations, or notes.", usePersonality: false, useInterfaceLanguage: false);
                CommandExecuted?.Invoke();
                return true;
            }
            if (IsTriggered("bullet points", "make bullet list", "organize", "organize this")) { await ProcessAIOnSelection("Format the following text into a logical bulleted list using '•'. Identify distinct concepts, sentences, or items, but do NOT simply make every single word a separate bullet point. Output ONLY the formatted list.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("shorter", "condense")) { await ProcessAIOnSelection("Make the following text concise and punchy. Output ONLY the condensed text.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("expand", "flesh out")) { await ProcessAIOnSelection("Expand on this text to make it more descriptive. Output ONLY the expanded text.", usePersonality: false, useInterfaceLanguage: false); CommandExecuted?.Invoke(); return true; }

            // --- 8. OPEN ACCESSIBILITY ---
            if (IsTriggered("read this", "read that", "read the", "read selection", "speak this", "say this")) { await ReadSelection(); CommandExecuted?.Invoke(); return true; }
            if (IsTriggered("stop reading", "stop speaking", "stop playback", "shush", "hush", "shut up", "quiet")) { NarrationManager.Shared.CancelSpeech(); CommandExecuted?.Invoke(); return true; }
            if (cmd == "stop" && AudioRecorder.Shared.IsPlaying) { NarrationManager.Shared.CancelSpeech(); CommandExecuted?.Invoke(); return true; }

            LogToFile($"[CommandController] No command match found for: '{cmd}'");
            return false;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private async Task ReadSelection()
        {
            try
            {
                string selection = await InputController.Shared.CaptureSelectedText();

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

                string selection = await InputController.Shared.CaptureSelectedText();

                if (string.IsNullOrWhiteSpace(selection))
                {
                    LogToFile("SummarizeAndRead: No selection found.");
                    await NarrationManager.Shared.SpeakAsync("I couldn't find any selected text. Please select the text you want me to summarize.");
                    return;
                }

                // Removed the "Summarizing selection..." voice announcement for a quieter experience.

                string prompt = "You are a concise summarization tool. TASK: Summarize the provided text in maximum 2 short sentences. CONSTRAINTS: Stick STRICTLY to the text provided. Do not add outside knowledge. No intro/outro.";
                string summary = await AIService.Shared.ProcessCommand(prompt, selection, usePersonality: false);

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
            // Normalize: remove trailing dot if it exists
            name = name.TrimEnd('.');

            // 1. Check exact dictionary match
            if (_webShortcuts.TryGetValue(name, out string url))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }

            // 2. Check if it's a known shortcut with a common TLD suffix (e.g. "github.com" matches "github")
            string[] commonTlds = { ".com", ".org", ".net", ".io", ".ai", ".app", ".dev" };
            foreach (var tld in commonTlds)
            {
                if (name.EndsWith(tld))
                {
                    string root = name.Substring(0, name.Length - tld.Length);
                    if (_webShortcuts.TryGetValue(root, out string shortcutUrl))
                    {
                        Process.Start(new ProcessStartInfo(shortcutUrl) { UseShellExecute = true });
                        return true;
                    }
                }
            }

            // 3. Last resort: if it has any dot, try to visit it directly
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

        private string? FindAppInStartMenu(string appName)
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
            
            // Only trust dictionary shortcuts that exist (or shell/protocol commands without directory separators)
            if (target != null && Path.IsPathRooted(target) && !File.Exists(target))
            {
                target = null;
            }

            // 2. Dynamic Search in Start Menu (Background Task)
            if (target == null)
            {
                target = await Task.Run(() => FindAppInStartMenu(name));
            }

            // Fallback chains for common suites (e.g. MS Office -> SoftMaker FreeOffice)
            var officeFallbacks = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "word", new[] { @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\TextMaker.exe", @"C:\Program Files\SoftMaker FreeOffice 2024\TextMaker.exe", "textmaker" } },
                { "winword", new[] { @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\TextMaker.exe", @"C:\Program Files\SoftMaker FreeOffice 2024\TextMaker.exe", "textmaker" } },
                { "excel", new[] { @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\PlanMaker.exe", @"C:\Program Files\SoftMaker FreeOffice 2024\PlanMaker.exe", "planmaker" } },
                { "powerpoint", new[] { @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\Presentations.exe", @"C:\Program Files\SoftMaker FreeOffice 2024\Presentations.exe", "presentations" } },
                { "powerpnt", new[] { @"C:\Program Files (x86)\SoftMaker FreeOffice 2024\Presentations.exe", @"C:\Program Files\SoftMaker FreeOffice 2024\Presentations.exe", "presentations" } },
                { "textmaker", new[] { "winword" } },
                { "planmaker", new[] { "excel" } },
                { "presentations", new[] { "powerpnt" } }
            };

            if (target != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                    return true;
                }
                catch (Exception ex)
                {
                    LogToFile($"[LaunchApp] Error opening '{target}': {ex.Message}. Checking fallback alternatives...");
                }
            }

            // Try suite fallback only for recognized app aliases
            string normKey = NormalizeAppName(name);
            string normTarget = target != null ? NormalizeAppName(target) : string.Empty;
            foreach (var kvp in officeFallbacks)
            {
                if (NormalizeAppName(kvp.Key) == normKey || (!string.IsNullOrEmpty(normTarget) && normTarget == NormalizeAppName(kvp.Key)))
                {
                    foreach (var alt in kvp.Value)
                    {
                        try
                        {
                            if (File.Exists(alt) || !alt.Contains(@"\"))
                            {
                                Process.Start(new ProcessStartInfo(alt) { UseShellExecute = true });
                                LogToFile($"[LaunchApp] Successfully launched fallback alternative: '{alt}' for '{name}'");
                                return true;
                            }
                        }
                        catch { }
                    }
                }
            }

            LogToFile($"[LaunchApp] Application '{name}' not found; skipping ShellExecute fallback.");
            return false;
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

        private async Task ProcessAIOnSelection(string prompt, bool usePersonality = true, bool useInterfaceLanguage = true)
        {
            try
            {
                LogToFile($"ProcessAIOnSelection started with prompt: {prompt}");

                string selection = await InputController.Shared.CaptureSelectedText();

                if (string.IsNullOrWhiteSpace(selection))
                {
                    LogToFile("No selection found. Aborting.");
                    await NarrationManager.Shared.SpeakAsync("Please select some text first.");
                    return;
                }

                // Process
                string result = await AIService.Shared.ProcessCommand(prompt, selection, usePersonality, useInterfaceLanguage);
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
                string logPath = Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
                Directory.CreateDirectory(logPath);
                File.AppendAllText(Path.Combine(logPath, "command_debug.txt"), $"{DateTime.Now}: {msg}\n");
            }
            catch { }
        }
    }
}


