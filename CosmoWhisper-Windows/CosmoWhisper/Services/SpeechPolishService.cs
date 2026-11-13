using System;
using System.Text.RegularExpressions;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Services
{
    /// <summary>
    /// SpeechPolishService: Implements high-speed speech cleanup (inspired by ~600M SLM post-processing).
    /// Strips vocal disfluencies (um/uh), fixes false starts, stutters, and formats speech fluently.
    /// Can be toggled on/off instantly in Settings without rebuilding or reinstalling.
    /// </summary>
    public class SpeechPolishService
    {
        private static readonly Lazy<SpeechPolishService> _instance = new Lazy<SpeechPolishService>(() => new SpeechPolishService());
        public static SpeechPolishService Shared => _instance.Value;

        private static readonly Regex RegexFillersWithCommas = new Regex(
            @"(,\s*)?\b(um+|uh+|er+|ah+|erm|umm+|uhh+|err+|ahh+|hmm+)\b(\s*,)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexStutters = new Regex(
            @"\b(\w+)\s+\1\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string Polish(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return rawText;

            // Check if feature is enabled in user preferences
            bool isEnabled = PreferenceManager.Shared.Preferences.EnableSpeechPolish;
            if (!isEnabled)
            {
                // LEGACY MODE: Return raw text without speech polish
                return rawText;
            }

            string text = rawText;

            // 1. Remove stuttered duplicate words (e.g. "the the" -> "the", "we we" -> "we")
            text = RegexStutters.Replace(text, "$1");
            text = RegexStutters.Replace(text, "$1");

            // 2. Remove filler vocalizations including surrounding comma artifacts
            text = RegexFillersWithCommas.Replace(text, " ");

            // 3. Clean up conversational false start fillers if at beginning of speech
            text = Regex.Replace(text, @"^\s*(so,\s*|like,\s*|you\s+know,\s*|i\s+mean,\s*)", "", RegexOptions.IgnoreCase);

            // 4. Fix double spaces and orphaned commas created by removed fillers
            text = Regex.Replace(text, @"\s+,\s*", ", ");
            text = Regex.Replace(text, @",\s*,+", ", ");
            text = Regex.Replace(text, @"^\s*,\s*", "");
            text = Regex.Replace(text, @"\s{2,}", " ");

            // 5. Contextual Disambiguation & Phonetic Catchers
            // A. Culinary terms: map all phonetic variations ("Sou chef", "Sue Schiff", etc.) to "sous chef"/"sous chefs"
            text = Regex.Replace(text, @"\b(sou|sous|soux|sue|soo|su|sioux|souza|sousa|susa|sew|so)\s*(,\s*)?(chef|chefs|schiff|sheff|shiff)\b", m => {
                string chefWord = m.Groups[3].Value.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? "sous chefs" : "sous chef";
                return char.IsUpper(m.Value[0]) ? char.ToUpper(chefWord[0]) + chefWord[1..] : chefWord;
            }, RegexOptions.IgnoreCase);

            text = Regex.Replace(text, @"\bsee\s+you\s*(,\s*)?(chef|chefs)\b", m => {
                string chefWord = m.Groups[2].Value.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? "sous chefs" : "sous chef";
                return char.IsUpper(m.Value[0]) ? char.ToUpper(chefWord[0]) + chefWord[1..] : chefWord;
            }, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(sou|sous|soux|sue|soo|su|sioux|souza|sousa)\s*(-?\s*vide)\b", "sous-vide", RegexOptions.IgnoreCase);

            // C. Technical acronyms and terms
            // URL (Parakeet often transcribes as "You are L")
            text = Regex.Replace(text, @"\b(you\s+are\s+l|u\s*\.\s*r\s*\.\s*l)\b", "URL", RegexOptions.IgnoreCase);
            // localhost (often split as "local host")
            text = Regex.Replace(text, @"\blocal\s+host\b", "localhost", RegexOptions.IgnoreCase);
            // Jin Shin Jyutsu (caught across all phonetic variations: "Jenshin Jetsu", "Jin Shin Jetson", "Gen Shin Jutsu", "Jinsian", "Jin Shin Jitsu", etc.)
            text = Regex.Replace(text, @"\b(jin\s*shin|jen\s*shin|gen\s*shin|jenshin|genshin|jinsian)(\s+(jetsu|jetson|jitsu|jutsu|jyutsu|jitson|jitsou|jits))?\b", m => {
                return m.Groups[2].Success ? "Jin Shin Jyutsu" : "Jin Shin";
            }, RegexOptions.IgnoreCase);

            // Tech terms & multi-word hints
            text = Regex.Replace(text, @"\bcomfy\s+ui\b", "ComfyUI", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bvera\s+crypt\b", "VeraCrypt", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bcosmo\s+whisper(\.com)?\b", m => m.Groups[1].Success ? "cosmowhisper.com" : "CosmoWhisper", RegexOptions.IgnoreCase);

            // 6. Capitalize first letter if filler removal uncapitalized the start
            text = text.Trim();
            if (text.Length > 0 && char.IsLower(text[0]))
            {
                text = char.ToUpper(text[0]) + text.Substring(1);
            }

            return text;
        }
    }
}