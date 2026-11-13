using System.Collections.Generic;

namespace CosmoWhisper.Interfaces
{
    public interface IVocabularyManager
    {
        Dictionary<string, string> Replacements { get; }
        string TranscriptionHints { get; set; }
        void Load();
        void Save();
        void LoadDefaults();
        void AddReplacement(string key, string value);
        void RemoveReplacement(string key);
        void SetContext(string appName);
        string ApplyCorrections(string text);
        bool IsSoloCorrection(string processedText);
        string GetActiveHints();
    }
}
