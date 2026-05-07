# Progress - Voice Recognition Session

## 2026-05-07 15:55
- [x] Refined `AIService.cs` prompt to include formatting commands and reduce aggressive noise filtering.
- [x] Expanded `TextProcessor.cs` regex patterns to handle "newline" and more variations.
- [x] Increased audio padding in `AudioRecorder.cs` to 300ms to prevent clipping.
- [ ] Next: Optimize Race Mode to favor Cloud for longer dictations (accuracy over speed).

## Session Log
- **15:48**: Started session. User reports commands not working and poor dictation quality.
- **15:52**: Found aggressive "non-speech" filtering in prompt and tight audio padding.
- **15:54**: Softened prompt and improved command regex. Increased audio padding.
- **15:55**: Identified "Race Mode" with `tiny` model as the likely cause for "terrible" long sentence dictation.
- **16:00**: **CRITICAL DISCOVERY**: Found a bug in `TextProcessor.IsGarbage` that was discarding any sentence containing phrases like "thank you" or "subtracting". This was the true cause of "losing words".
- **16:02**: Fixed `IsGarbage` to use exact matching. Reverted Cloud-forcing heuristic to respect user's Local-only preference. Adjusted prompt for better command recognition.


