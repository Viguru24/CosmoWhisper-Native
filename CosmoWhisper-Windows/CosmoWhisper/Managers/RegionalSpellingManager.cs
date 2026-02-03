using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Managers
{
    public class RegionalSpellingManager
    {
        public static RegionalSpellingManager Shared { get; } = new RegionalSpellingManager();

        // US to UK dictionary
        private readonly Dictionary<string, string> _usToUk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // -or to -our
            { "color", "colour" },
            { "colors", "colours" },
            { "flavor", "flavour" },
            { "flavors", "flavours" },
            { "humor", "humour" },
            { "behavior", "behaviour" },
            { "neighbor", "neighbour" },
            { "neighbors", "neighbours" },
            { "labor", "labour" },
            { "harbor", "harbour" },
            { "favorite", "favourite" },
            { "glamor", "glamour" },

            // -ize to -ise
            { "organize", "organise" },
            { "organized", "organised" },
            { "organizing", "organising" },
            { "organization", "organisation" },
            { "realize", "realise" },
            { "realized", "realised" },
            { "realizing", "realising" },
            { "recognize", "recognise" },
            { "recognized", "recognised" },
            { "recognizing", "recognising" },
            { "apologize", "apologise" },
            { "apologized", "apologised" },
            { "apologizing", "apologising" },
            { "analyze", "analyse" },
            { "analyzed", "analysed" },
            { "analyzing", "analysing" },
            { "paralyze", "paralyse" },
            { "emphasize", "emphasise" },
            { "prioritize", "prioritise" },
            { "optimize", "optimise" },
            { "customization", "customisation" },

            // -er to -re
            { "center", "centre" },
            { "centers", "centres" },
            { "theater", "theatre" },
            { "theaters", "theatres" },
            { "liter", "litre" },
            { "meter", "metre" },
            { "meters", "metres" },
            { "caliber", "calibre" },

            // Double consonants
            { "traveler", "traveller" },
            { "travelers", "travellers" },
            { "traveling", "travelling" },
            { "canceled", "cancelled" },
            { "canceling", "cancelling" },
            { "labeled", "labelled" },
            { "labeling", "labelling" },
            { "modeling", "modelling" },
            { "initialed", "initialled" },

            // Miscellaneous
            { "defense", "defence" },
            { "offense", "offence" },
            { "license", "licence" }, // Noun form
            { "practice", "practise" }, // Verb form (US uses practice for both)
            { "check", "cheque" }, // Bank check
            { "catalog", "catalogue" },
            { "analog", "analogue" },
            { "dialog", "dialogue" },
            { "gray", "grey" },
            { "sulfur", "sulphur" },
            { "program", "programme" }
        };

        // UK to US dictionary (Reverse of above, plus unique ones)
        private readonly Dictionary<string, string> _ukToUs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public RegionalSpellingManager()
        {
            // Build the reverse mapping
            foreach (var kvp in _usToUk)
            {
                if (!_ukToUs.ContainsKey(kvp.Value))
                {
                    _ukToUs[kvp.Value] = kvp.Key;
                }
            }
            
            // Add unique UK to US if any
            _ukToUs["aluminium"] = "aluminum";
        }

        public string Apply(string text)
        {
            var prefs = PreferenceManager.Shared.Preferences;
            if (!prefs.EnableRegionalSpelling) return text;

            // Determine target dialect
            // If InterfaceLanguage is en-GB, we want UK spellings (convert US to UK)
            // If InterfaceLanguage is en-US, we want US spellings (convert UK to US)
            
            bool targetUk = prefs.InterfaceLanguage == "en-GB";
            bool targetUs = prefs.InterfaceLanguage == "en-US";

            if (!targetUk && !targetUs) return text; // Only apply if one of these is explicitly chosen

            var dict = targetUk ? _usToUk : _ukToUs;
            return ReplaceFromDictionary(text, dict);
        }

        private string ReplaceFromDictionary(string text, Dictionary<string, string> dict)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            string processed = text;
            foreach (var kvp in dict)
            {
                // Use regex with word boundaries to avoid partial matches (e.g., "colorado" shouldn't become "colourado")
                // We also need to handle capitalization
                string pattern = $@"\b{Regex.Escape(kvp.Key)}\b";
                processed = Regex.Replace(processed, pattern, m => 
                {
                    return MatchCase(m.Value, kvp.Value);
                }, RegexOptions.IgnoreCase);
            }
            return processed;
        }

        private string MatchCase(string original, string replacement)
        {
            if (string.IsNullOrEmpty(original)) return replacement;
            
            // All uppercase
            if (original.ToUpper() == original) return replacement.ToUpper();
            
            // Title case (First letter upper)
            if (char.IsUpper(original[0]))
            {
                if (replacement.Length > 1)
                    return char.ToUpper(replacement[0]) + replacement.Substring(1);
                return replacement.ToUpper();
            }
            
            return replacement.ToLower();
        }
    }
}
