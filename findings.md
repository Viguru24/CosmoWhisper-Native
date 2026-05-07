# Findings - Voice Recognition and Dictation Quality Issues

## CRITICAL BUG IDENTIFIED: Over-aggressive Garbage Filtering
- **Observation**: `TextProcessor.IsGarbage` uses `low.Contains(h)` for `PhraseHallucinations`.
- **Impact**: Any sentence containing common phrases like "thank you", "subtitle", or "subtracting" is being discarded entirely.
- **Cause**: Line 86 in `TextProcessor.cs`: `if (PhraseHallucinations.Any(h => low.Contains(h))) return true;`.
- **Resolution**: Change `Contains` to a check that only discards if the *entire* segment is the hallucination, or if the hallucination makes up the bulk of the (short) segment.

## Issue 1: "New line" and "New paragraph" commands not working
- **Observation**: The user says this wasn't a problem before.
- **Hypothesis**: The issue might be related to the `IsGarbage` check filtering out the commands if they are transcribed with any hallucination-like artifacts, OR the `CleanText` regexes were modified in a way that broke them (though my recent expansion should have helped).
- **Correction**: Revert Cloud-forcing heuristic in `AIService.cs` as the user explicitly wants Local-only if configured.

## Issue 2: Poor Dictation Quality / Losing Words
- **Direct Cause**: The `IsGarbage` bug mentioned above. If a user speaks a long sentence and it ends with "thank you" or "bye", the entire sentence is deleted.
- **Secondary Cause**: Prompt aggression.

## Plan for Implementation
1. **Revert** the Cloud-forcing heuristic in `AIService.cs`.
2. **Fix `IsGarbage`** in `TextProcessor.cs` to use `==` instead of `Contains` for hallucination detection, or at least only apply `Contains` if the text is very short.
3. **Refine `PhraseHallucinations`**: Move some phrases to `ShortHallucinations` or separate "discard if contains" vs "discard if matches".
