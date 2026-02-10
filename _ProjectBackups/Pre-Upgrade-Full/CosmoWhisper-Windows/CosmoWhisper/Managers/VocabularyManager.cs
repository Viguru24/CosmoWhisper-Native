using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CosmoWhisper.Managers
{
    public class VocabularyManager
    {
        public static VocabularyManager Shared { get; } = new VocabularyManager();

        public Dictionary<string, string> Replacements { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string TranscriptionHints { get; set; } = "";
        private readonly string _filePath;

        public VocabularyManager()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
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

        public string ApplyCorrections(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            string processed = text;

            // STEP 1: Apply smart email/domain corrections FIRST (if enabled)
            // This way, natural speech like "user at gmail dot com" gets formatted properly
            if (PreferenceManager.Shared.Preferences.EnableSmartEmailCorrections)
            {
                // 1. Smart "at" to "@" conversion (Only if it looks like an email)
                // Matches [something] at [something].[domain]
                processed = Regex.Replace(processed, @"(\S+)\s+at\s+([a-zA-Z0-9-]+\.[a-zA-Z]{2,})", "$1@$2", RegexOptions.IgnoreCase);

                // 2. Clean up email-like spacing (e.g., "user @ gmail . com" -> "user@gmail.com")
                // Snaps spaces around '@'
                processed = Regex.Replace(processed, @"\s*@\s*", "@");
                
                // 3. Snap dots ONLY in email/domain contexts (not sentence periods)
                // Match patterns like "gmail . com" or "user@domain . co . uk"
                // But NOT "sentence. Next" (capital letter after period = new sentence)
                processed = Regex.Replace(processed, @"([a-zA-Z0-9@])\s*\.\s*([a-z0-9])", "$1.$2");

                // 4. Lowercase email addresses
                processed = Regex.Replace(processed, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", m => m.Value.ToLower());
            }

            // STEP 2: Apply user-defined vocabulary replacements with intelligent matching
            // Sort by key length (longest first) to handle overlapping matches correctly
            var sortedReplacements = Replacements.OrderByDescending(kvp => kvp.Key.Length);
            
            foreach (var kvp in sortedReplacements)
            {
                string key = kvp.Key;
                string value = kvp.Value;
                
                // Strategy 1: Exact word boundary match (most precise)
                // Matches "my password" in "my password is great" -> replaces entire phrase
                string pattern = $@"\b{Regex.Escape(key)}(?:\s+(?:is|was|are|were|be|been|being))?\b";
                var match = Regex.Match(processed, pattern, RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    // Replace the matched phrase (including trailing helper words) with the exact value
                    processed = processed.Substring(0, match.Index) + value + processed.Substring(match.Index + match.Length);
                }
                else
                {
                    // Strategy 2: Standard word boundary match (fallback)
                    pattern = $@"\b{Regex.Escape(key)}\b";
                    processed = Regex.Replace(processed, pattern, value, RegexOptions.IgnoreCase);
                }
            }

            return processed;
        }
    }
}
