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
            TranscriptionHints = "Batman, Arkham, Wayne Enterprises, Gotham City, Alfred Pennyworth";
            Replacements["my address"] = "1007 Mountain Drive, Gotham City, NJ";
            Replacements["my email"] = "bruce.wayne@wayne-enterprises.com";
            Replacements["my phone"] = "+1 555-010-1939";
            Replacements["batman"] = "I am Vengeance. I am the Night. I am Batman.";
            Replacements["alfred"] = "At your service, Master Wayne.";
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
            foreach (var kvp in Replacements)
            {
                string pattern = Regex.Escape(kvp.Key);
                processed = Regex.Replace(processed, $@"\b{pattern}\b", kvp.Value, RegexOptions.IgnoreCase);
            }
            return processed;
        }
    }
}
