using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CosmoWhisper.Interfaces;


namespace CosmoWhisper.Managers
{
    public class VocabularyManager : IVocabularyManager

    {
        public static VocabularyManager Shared => (VocabularyManager)ServiceLocator.GetService<IVocabularyManager>();
        private readonly IPreferenceManager _preferenceManager;

        public Dictionary<string, string> Replacements { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string TranscriptionHints { get; set; } = "";
        private readonly string _filePath;

        public VocabularyManager(IPreferenceManager preferenceManager)
        {
            _preferenceManager = preferenceManager;
            string folder = _preferenceManager.AppDataFolder;
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "vocabulary.json");
            Load();

            // Only seed if completely empty (brand new install logic approx)
            if (Replacements.Count == 0 && !File.Exists(_filePath))
            {
                LoadDefaults();
            }
        }

        public void LoadDefaults()
        {
            try
            {
                // Source of truth file that the user can check manually
                string defaultsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Managers", "VocabularyDefaults.json");
                
                // Fallback for dev environment path
                if (!File.Exists(defaultsPath))
                {
                    defaultsPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Managers", "VocabularyDefaults.json");
                }

                if (File.Exists(defaultsPath))
                {
                    string json = File.ReadAllText(defaultsPath);
                    var data = JsonSerializer.Deserialize<VocabularyData>(json);
                    
                    if (data != null)
                    {
                        Replacements.Clear();
                        foreach (var kvp in data.Replacements)
                        {
                            Replacements[kvp.Key] = kvp.Value;
                        }
                        TranscriptionHints = data.TranscriptionHints;
                        Save();
                        return;
                    }
                }
            }
            catch { }

            // Emergency hardcoded fallback if file is missing
            Replacements.Clear();
            TranscriptionHints = "ExampleName, example.com";
            Save();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    
                    // Try to parse as new format (VocabularyData)
                    try 
                    {
                        var data = JsonSerializer.Deserialize<VocabularyData>(json);
                        if (data != null && data.Replacements != null)
                        {
                            Replacements = new Dictionary<string, string>(data.Replacements, StringComparer.OrdinalIgnoreCase);
                            TranscriptionHints = data.TranscriptionHints ?? "";
                            return;
                        }
                    }
                    catch { }

                    // Fallback: Try to parse as old format (Dictionary)
                    Replacements = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    TranscriptionHints = "";
                }
                else
                {
                    LoadDefaults();
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                var data = new VocabularyData
                {
                    Replacements = Replacements,
                    TranscriptionHints = TranscriptionHints
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }

        private class VocabularyData
        {
            public Dictionary<string, string> Replacements { get; set; } = new Dictionary<string, string>();
            public string TranscriptionHints { get; set; } = "";
        }

        public void AddReplacement(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            Replacements[key] = value;
            Save();
        }

        public void RemoveReplacement(string key)
        {
            if (Replacements.ContainsKey(key))
            {
                Replacements.Remove(key);
                Save();
            }
        }

        private string _currentAppContext = "Global";
        private Dictionary<string, string> _appReplacements = new(StringComparer.OrdinalIgnoreCase);
        private string _appHints = "";

        public void SetContext(string appName)
        {
            if (string.IsNullOrEmpty(appName)) appName = "Global";
            if (_currentAppContext == appName) return;

            _currentAppContext = appName;
            LoadAppContext(appName);
        }

        private void LoadAppContext(string appName)
        {
            try
            {
                _appReplacements.Clear();
                _appHints = "";

                string folder = Path.Combine(_preferenceManager.AppDataFolder, "Contexts");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, $"{appName}.json");

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<VocabularyData>(json);
                    if (data != null)
                    {
                        _appReplacements = new Dictionary<string, string>(data.Replacements, StringComparer.OrdinalIgnoreCase);
                        _appHints = data.TranscriptionHints ?? "";
                    }
                }
            }
            catch { }
        }

        public string ApplyCorrections(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            string processed = text;

            // Blend App-Specific Replacements with Global ones (App takes precedence)
            var blendedReplacements = new Dictionary<string, string>(Replacements, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _appReplacements) blendedReplacements[kvp.Key] = kvp.Value;

            // STEP 1: Apply smart email/domain corrections FIRST (if enabled)
            if (_preferenceManager.Preferences.EnableSmartEmailCorrections)
            {
                // 1. Smart "at" to "@" conversion (Only if it looks like an email)
                processed = Regex.Replace(processed, @"(\S+)\s+at\s+([a-zA-Z0-9-]+\.[a-zA-Z]{2,})", "$1@$2", RegexOptions.IgnoreCase);
                processed = Regex.Replace(processed, @"\s*@\s*", "@");
                // Collapse spaces around dots only for known domain TLDs or email addresses (never normal sentence punctuation like "Hello. world")
                processed = Regex.Replace(processed, @"([a-zA-Z0-9-]+)\s*\.\s*(com|org|net|io|ai|app|dev|edu|gov|co(?:\.uk)?)\b", "$1.$2", RegexOptions.IgnoreCase);
                processed = Regex.Replace(processed, @"([a-zA-Z0-9._%+-]+@[a-zA-Z0-9-]+)\s*\.\s*([a-zA-Z]{2,})", m => $"{m.Groups[1].Value}.{m.Groups[2].Value}".ToLower(), RegexOptions.IgnoreCase);
                processed = Regex.Replace(processed, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", m => m.Value.ToLower());
            }

            // STEP 2: Apply replacements (Blended)
            var sortedReplacements = blendedReplacements.OrderByDescending(kvp => kvp.Key.Length);
            
            foreach (var kvp in sortedReplacements)
            {
                string key = kvp.Key;
                string value = kvp.Value;
                
                processed = Regex.Replace(processed, $@"\b{Regex.Escape(key)}\b", value, RegexOptions.IgnoreCase);
            }

            // STEP 3: Apply Transcription Hints (Casing, Acronyms & Whole-Word Alignment)
            string activeHints = GetActiveHints();
            if (!string.IsNullOrWhiteSpace(activeHints))
            {
                var hintsList = activeHints.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(h => h.Trim())
                                           .Where(h => !string.IsNullOrEmpty(h))
                                           .OrderByDescending(h => h.Length);

                foreach (var hint in hintsList)
                {
                    string pattern = (char.IsLetterOrDigit(hint[0]) ? @"\b" : "") +
                                     Regex.Escape(hint) +
                                     (char.IsLetterOrDigit(hint[hint.Length - 1]) ? @"\b" : "");
                    processed = Regex.Replace(processed, pattern, hint, RegexOptions.IgnoreCase);
                }
            }

            // STEP 4: Handle Automatic Sign-Off (Signature)
            var p = PreferenceManager.Shared.Preferences;
            if (p.EnableSignOff && !string.IsNullOrWhiteSpace(p.SignOffSignature))
            {
                // Triggers: "sign off", "regards" (if not already part of a replacement)
                string signature = p.SignOffSignature.Replace("\\n", "\n").Replace("\r", "");
                
                // Case 1: "Sign off" -> Replacement with full signature
                if (Regex.IsMatch(processed, @"\bsign\s*off\b", RegexOptions.IgnoreCase))
                {
                    processed = Regex.Replace(processed, @"\bsign\s*off\b", signature, RegexOptions.IgnoreCase);
                }
                // Case 2: "Regards" at the very end -> Append the rest of signature if not already present
                else if (processed.TrimEnd().EndsWith("regards", StringComparison.OrdinalIgnoreCase))
                {
                    // If the signature starts with "Regards", we just append the rest
                    if (signature.StartsWith("regards", StringComparison.OrdinalIgnoreCase))
                    {
                        processed = Regex.Replace(processed, @"\bregards\b\s*$", signature, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // Append full signature if it doesn't contain "Regards" already
                        processed = processed.TrimEnd() + "\n\n" + signature;
                    }
                }
            }

            return processed;
        }

        /// <summary>
        /// Checks if the resulting text consists entirely of an instant correction value.
        /// Useful for suppressing spaces on passwords/IDs.
        /// </summary>
        public bool IsSoloCorrection(string processedText)
        {
            if (string.IsNullOrWhiteSpace(processedText)) return false;
            
            // Normalize: Trim whitespace and trailing punctuation
            string normalized = processedText.Trim().TrimEnd('.', ',');
            
            // Check against both global and app replacements
            return Replacements.Values.Any(v => v.Equals(normalized, StringComparison.OrdinalIgnoreCase)) ||
                   _appReplacements.Values.Any(v => v.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        public string GetActiveHints()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(TranscriptionHints)) parts.Add(TranscriptionHints.Trim().TrimEnd(','));
            if (!string.IsNullOrWhiteSpace(_appHints)) parts.Add(_appHints.Trim().TrimEnd(','));
            return string.Join(", ", parts);
        }
    }
}


