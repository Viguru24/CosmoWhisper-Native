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

                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "Contexts");
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
            if (PreferenceManager.Shared.Preferences.EnableSmartEmailCorrections)
            {
                // 1. Smart "at" to "@" conversion (Only if it looks like an email)
                processed = Regex.Replace(processed, @"(\S+)\s+at\s+([a-zA-Z0-9-]+\.[a-zA-Z]{2,})", "$1@$2", RegexOptions.IgnoreCase);
                processed = Regex.Replace(processed, @"\s*@\s*", "@");
                processed = Regex.Replace(processed, @"([a-zA-Z0-9@])\s*\.\s*([a-z0-9])", "$1.$2");
                processed = Regex.Replace(processed, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", m => m.Value.ToLower());
            }

            // STEP 2: Apply replacements (Blended)
            var sortedReplacements = blendedReplacements.OrderByDescending(kvp => kvp.Key.Length);
            
            foreach (var kvp in sortedReplacements)
            {
                string key = kvp.Key;
                string value = kvp.Value;
                
                string pattern = $@"\b{Regex.Escape(key)}(?:\s+(?:is|was|are|were|be|been|being))?\b";
                var match = Regex.Match(processed, pattern, RegexOptions.IgnoreCase);
                
                if (match.Success)
                    processed = processed.Substring(0, match.Index) + value + processed.Substring(match.Index + match.Length);
                else
                    processed = Regex.Replace(processed, $@"\b{Regex.Escape(key)}\b", value, RegexOptions.IgnoreCase);
            }

            return processed;
        }

        // Expose hints for AI Prompt
        public string GetActiveHints()
        {
            return string.IsNullOrWhiteSpace(_appHints) ? TranscriptionHints : $"{TranscriptionHints}, {_appHints}";
        }
    }
}
