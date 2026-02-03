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
        private readonly string _filePath;

        public VocabularyManager()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "vocabulary.json");
            Load();

            if (Replacements.Count == 0)
            {
                // Seed with Example Data if empty
                Replacements["my address"] = "1007 Mountain Drive, Gotham City, NJ";
                Replacements["my email"] = "bruce.wayne@wayne-enterprises.com";
                Replacements["my phone"] = "+1 555-010-1939";
                Replacements["batman"] = "I am Vengeance. I am the Night. I am Batman.";
                Replacements["alfred"] = "At your service, Master Wayne.";
                Save();
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    Replacements = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Replacements, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
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
