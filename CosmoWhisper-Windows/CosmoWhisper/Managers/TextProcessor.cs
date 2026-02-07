using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CosmoWhisper.Managers
{
    public static class TextProcessor
    {
        // Centralized list of known "Hallucinations" - common artifacts from Whisper models
        private static readonly HashSet<string> ShortHallucinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "yes", "good", "just", "the", "ok", "okay", "you", "thanks", "thank you", "the.", "bye", 
            "is", "a", "this", "l", "u", "po", "sti", "come", "wi", "st", "you.", "and", "but", "me", 
            "it", "so", "thank you."
        };

        private static readonly string[] PhraseHallucinations = new[]
        {
            "mbc", " дякую", "дякую!", "subtitles", "subtitle by", "watched by", "mbc news", 
            "translated by", "amara.org", "ted.com", "copyright", "all rights reserved", 
            "the end.", "bye bye.", "thanks for watching", "thank you for watching", "thank you.",
            "subtitle", "this is the end of the video",
            "subtracting", "help me", "subtracting help me", "subtracting, help me"
        };


        public static string CleanText(string input)
        {
            string text = input ?? "";
            text = Regex.Replace(text, @"\s+$", "");
            text = text.Trim();
            string low = text.ToLower();

            // Remove common Whisper hallucinations from the end of transcriptions
            foreach (var hallucination in PhraseHallucinations)
            {
                if (low.EndsWith(hallucination.ToLower()))
                {
                    text = text.Substring(0, text.Length - hallucination.Length).TrimEnd(',', ' ', '.');
                }
            }

            // Command Processing: Paragraphs and New Lines
            if (Regex.IsMatch(text, @"^(new|next) paragraph\s*", RegexOptions.IgnoreCase))
            {
                text = Regex.Replace(text, @"^(new|next) paragraph\s*", "", RegexOptions.IgnoreCase);
                text = "\n\n" + text;
            }

            if (Regex.IsMatch(text, @"^(new|next) line\s*", RegexOptions.IgnoreCase))
            {
                text = Regex.Replace(text, @"^(new|next) line\s*", "", RegexOptions.IgnoreCase);
                text = "\n" + text;
            }

            // Punctuation commands
            if (low.Contains("comma")) text = Regex.Replace(text, @"\bcomma\b", ",", RegexOptions.IgnoreCase);
            if (low.Contains("full stop") || low.Contains("period")) text = Regex.Replace(text, @"\b(full stop|period)\b", ".", RegexOptions.IgnoreCase);

            // Cleanup trailing punctuation if text is short segment
            int wordCount = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount < 6) text = Regex.Replace(text, @"[\.\\?!…]+[\s]*$", "");

            return text;
        }

        public static bool IsGarbage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            string low = text.ToLower().Trim();

            if (low.Length <= 2) return true;
            
            if (ShortHallucinations.Contains(low)) return true;
            if (PhraseHallucinations.Any(h => low.Contains(h))) return true;
            if (text.Split("Bye").Length > 3) return true;
            if (text.Length < 3 && !text.Any(char.IsDigit)) return true;

            return false;
        }
    }
}
