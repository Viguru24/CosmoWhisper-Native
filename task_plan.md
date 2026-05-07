# Task Plan - Restoring Voice Recognition & Command Reliability

## Phase 1: Diagnostics & Code Review 🔍
- [x] Audit `TextProcessor.cs` regex patterns for "new line" / "new paragraph".
- [x] Review `AIService.cs` prompt for over-aggressive noise filtering instructions.
- [x] Check `AudioRecorder.cs` for potential audio clipping at the start/end of recording.
- [x] Analyze the "Race Mode" logic to ensure quality is not sacrificed for speed.

## Phase 2: Prompt Optimization ✍️
- [x] Soften the "non-speech" instruction in `AIService.cs`.
- [x] Add explicit instructions to the model to recognize formatting commands like "new line".
- [x] Test if removing the "Ignore non-speech sounds" instruction restores accuracy.

## Phase 3: Command & Filtering Logic Refinement 🛠️
- [x] Expand Regex in `TextProcessor.cs` to handle more variations (e.g., "newline", "next paragraph").
- [x] Ensure formatting commands are not being stripped by the "Garbage" filter.
- [x] Increase audio padding in `AudioRecorder.cs` slightly (from 100ms to 300ms) to prevent clipping.

## Phase 4: Quality & Performance Tuning 🏎️
- [x] Evaluate if "Race Mode" should have a minimum quality threshold before accepting a Local result.
- [x] Implement duration-based heuristic to favor Cloud for long sentences.

## Phase 5: Verification ✅
- [ ] Test long sentence dictation.
- [ ] Test "new line" and "new paragraph" commands in various contexts.
- [ ] Verify that word loss is minimized.

