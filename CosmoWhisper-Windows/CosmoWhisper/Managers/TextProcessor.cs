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
            "the", "the.", "l", "u", "po", "sti", "come", "wi", "st", "and", "but", "me", 
            "it", "so"
        };

        private static readonly string[] TrailingHallucinations = new[]
        {
            "subtitles by the amara.org community", "subtitles by the amara.org", "amara.org community",
            "subtitle by", "subtitles by", "watched by", "mbc news", "translated by", 
            "thanks for watching", "thank you for watching", "this is the end of the video"
        };

        private static readonly string[] ExactSegmentHallucinations = new[]
        {
            "mbc", " дякую", "дякую!", "mbc news", "translated by", "amara.org", "ted.com", 
            "all rights reserved", "the end.", "the end", "bye bye.", "bye bye", 
            "thanks for watching", "thank you for watching", "thank you.", "thank you",
            "this is the end of the video",
            "subtracting help me", "subtracting, help me",
            "subtitles by the amara.org community", "subtitles by the amara.org", "amara.org community"
        };


        public static string CleanText(string input)
        {
            string text = input ?? "";
            text = Regex.Replace(text, @"\s+$", "");
            text = text.Trim();

            // Strip leading dot/period/ellipsis artifacts — a common Whisper hallucination
            // e.g. ". This is a test" → "This is a test", "...Hello" → "Hello", ", Hello" → "Hello"
            text = Regex.Replace(text, @"^[\.\,…]+\s*", "");

            // Remove common Whisper trailer hallucinations from the end of transcriptions safely
            string candidate = text.TrimEnd('.', '!', ' ');
            foreach (var hallucination in TrailingHallucinations)
            {
                if (candidate.Length >= hallucination.Length && candidate.EndsWith(hallucination, StringComparison.OrdinalIgnoreCase))
                {
                    text = candidate.Substring(0, candidate.Length - hallucination.Length).TrimEnd(',', ' ', '.');
                    break;
                }
            }

            // Command Processing: Paragraphs and New Lines (Handle anywhere and with optional punctuation including commas)
            // Hardened to handle plurals, different spacing, and Whisper's punctuation quirks.
            
            // 1. Hard Return (Paragraph) - Two Enters: visible gap in Notepad, double paragraph break in Word
            // Requires "new" or "next" to prevent triggering on general words like "paragraph"
            text = Regex.Replace(text, @"\s*\b(new|next)\s+(paragraph|para)\b[\.\?\!]*\s*", "\n\n", RegexOptions.IgnoreCase);
            
            // 2. Soft Return (New Line) - Maintains paragraph context (Shift+Enter in Word)
            // Added negative lookbehind to prevent "one line" or "1 line" from triggering a break.
            text = Regex.Replace(text, @"\s*(?<!\b(one|1)\s+)\b(new|next)\s+(line|return|enter)\b[\.\?\!]*\s*", "\v", RegexOptions.IgnoreCase);
            
            // 3. Fallback for standalone commands (if the entire segment is just "line" or "paragraph")
            if (text.Trim().Equals("line", StringComparison.OrdinalIgnoreCase)) text = "\v";
            if (text.Trim().Equals("paragraph", StringComparison.OrdinalIgnoreCase)) text = "\n\n";

            // Remove disfluencies and non-speech sounds (e.g., (coughs), [clears throat], *laughs*)
            // Enhanced to catch standalone artifacts without brackets if they appear as isolated tokens
            text = Regex.Replace(text, @"\s*[\(\[\*](coughs?|coughing|clears throat|sighs?|laughs?|laughter|background noise|unintelligible|breathing|music|silence)[\)\]\*]\s*", " ", RegexOptions.IgnoreCase);
            
            // Catch standalone artifacts like "Cough." or " (Cough) " more reliably
            text = Regex.Replace(text, @"^\s*(coughs?|coughing|clears throat|sighs?|laughs?|laughter|background noise|unintelligible|breathing|music)\.?\s*$", "", RegexOptions.IgnoreCase);
            
            text = Regex.Replace(text, @"\s*\[music\]\s*", " ", RegexOptions.IgnoreCase);

            // Punctuation commands
            if (text.Contains("comma", StringComparison.OrdinalIgnoreCase)) text = Regex.Replace(text, @"\bcomma\b", ",", RegexOptions.IgnoreCase);
            if (text.Contains("full stop", StringComparison.OrdinalIgnoreCase) || text.Contains("period", StringComparison.OrdinalIgnoreCase)) text = Regex.Replace(text, @"\b(full stop|period)\b", ".", RegexOptions.IgnoreCase);

            // Inverse Text Normalization (Numbers & Emails)
            text = NormalizeSpokenNumbersAndEmails(text);

            // Cleanup trailing punctuation if text is short segment (e.g. 1-2 words),
            // but ONLY if the segment contains actual letters/digits to avoid stripping punctuation-only commands.
            int wordCount = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount < 6 && text.Any(char.IsLetterOrDigit)) 
            {
                // We strip periods and commas for short segments, but PROTECT question marks and exclamation points.
                text = Regex.Replace(text, @"[\.,\\…]+[\s]*$", "");
            }

            // Final trim of spaces/tabs only to preserve formatting commands (newlines/paragraphs)
            return text.Trim(' ', '\t');
        }

        public static string NormalizeSpokenNumbersAndEmails(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            string text = input;

            // 1. Spoken Email Normalization (e.g., "louis at gmail dot com" -> "louis@gmail.com")
            text = Regex.Replace(text, @"\b([a-zA-Z0-9\._%+-]+)\s+at\s+([a-zA-Z0-9-]+)\s+dot\s+(com|org|net|edu|gov|co\.uk|io|ai|app|me|co)\b", "$1@$2.$3", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b([a-zA-Z0-9\._%+-]+)\s*@\s*([a-zA-Z0-9-]+)\s+dot\s+(com|org|net|edu|gov|co\.uk|io|ai|app|me|co)\b", "$1@$2.$3", RegexOptions.IgnoreCase);

            // 2. Spoken Digit Sequence Normalization
            // Converts sequences of 3 or more spoken digits (e.g. "O seven eight oh two four three six one five nine" or "zero seven eight zero two")
            // Dictionary mapping spoken digit words to digits
            var wordToDigit = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
            {
                { "zero", '0' }, { "oh", '0' }, { "o", '0' },
                { "one", '1' },
                { "two", '2' },
                { "three", '3' },
                { "four", '4' },
                { "five", '5' },
                { "six", '6' },
                { "seven", '7' },
                { "eight", '8' },
                { "nine", '9' }
            };

            // Match runs of digit words (including actual digits mixed in)
            string pattern = @"\b((?:(?:zero|oh|one|two|three|four|five|six|seven|eight|nine|\d|o)\s*){3,})\b";
            text = Regex.Replace(text, pattern, match =>
            {
                // Split match into individual tokens
                var tokens = Regex.Split(match.Value.Trim(), @"\s+");
                if (tokens.Length < 3) return match.Value;

                // Validate that all tokens in the matched run are valid digits/digit words
                var digits = new System.Text.StringBuilder();
                foreach (var tok in tokens)
                {
                    string cleanTok = tok.Trim().ToLowerInvariant();
                    if (wordToDigit.TryGetValue(cleanTok, out char d))
                    {
                        digits.Append(d);
                    }
                    else if (cleanTok.Length == 1 && char.IsDigit(cleanTok[0]))
                    {
                        digits.Append(cleanTok[0]);
                    }
                    else
                    {
                        return match.Value; // Not a pure digit sequence, keep original
                    }
                }

                // If sequence starts with '0' and is around UK mobile length (11 digits), format cleanly
                string digStr = digits.ToString();
                return digStr;
            }, RegexOptions.IgnoreCase);

            return text;
        }

        public static bool IsGarbage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) 
            {
                // Allow segments that are just formatting chars (result of formatting commands)
                if (text != null && (text.Contains("\n") || text.Contains("\r") || text.Contains("\v"))) return false;
                return true;
            }
            
            string low = text.ToLower().Trim();

            // Filter out repeating single character hallucinations (e.g. "wwww", "....", "hhhh")
            if (low.Length > 2 && low.Distinct().Count() == 1) return true;

            // Allow short but meaningful segments like "Hi", "Me", "Go"
            if (low.Length < 2 && !char.IsDigit(low[0])) return true; 
            
            if (ShortHallucinations.Contains(low.TrimEnd('.'))) return true;

            // Only discard if the ENTIRE segment is a known hallucination phrase.
            string normalized = low.TrimEnd('.', '!', '?', ',', ' ');
            if (ExactSegmentHallucinations.Any(h => normalized == h.ToLower().Trim())) 
            {
                return true;
            }
            
            if (Regex.Matches(text, @"\bbye\b", RegexOptions.IgnoreCase).Count > 3) return true;

            return false;
        }
    }
}


