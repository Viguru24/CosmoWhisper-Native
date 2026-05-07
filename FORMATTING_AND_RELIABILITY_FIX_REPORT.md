# CosmoWhisper: Formatting & Reliability Fix Report (v2.2.80.0)

This document outlines the technical changes implemented to resolve the regression in voice recognition accuracy, formatting command failures, and privacy policy enforcement.

## 1. Resolved: Valid Text Loss (Garbage Filter)
**Issue**: The `IsGarbage` filter was too aggressive, deleting valid long sentences if they contained common "hallucination" words like "thanks" or "subtitle".
**Fix**: Refactored `TextProcessor.IsGarbage` to perform **exact string matches** against known Whisper artifacts instead of a partial `Contains` check. 
**Result**: Valid dictation is no longer discarded, while short hallucinations (like "Thank you." at the end of a clip) are still filtered correctly.

## 2. Resolved: Privacy & "Local Only" Enforcement
**Issue**: A "Race Mode" heuristic was forcing cloud-based transcription for audio clips longer than a few seconds, even when the user selected "Local Only".
**Fix**: Reverted the heuristic in `AIService.cs`. The application now strictly respects the user's infrastructure preference regardless of sentence length or duration.
**Result**: Guaranteed local-only processing for users who prioritize privacy.

## 3. Resolved: Formatting Command Failures
**Issue**: Commands like "New line" and "New paragraph" were being transcribed as literal text or ignored.
**Fix**: 
- **Regex Hardening**: Expanded patterns in `TextProcessor.cs` to handle:
    - Plurals (e.g., "New paragraphs").
    - Trailing commas (e.g., "New line,").
    - Mid-word punctuation (e.g., "New. Paragraph.").
- **Virtual Key Injection**: Updated `InputController.TypeText` to detect `\n` characters and translate them into physical **VK_RETURN (Enter)** key events.
**Result**: Commands now work reliably across all Windows applications, even those that don't support Unicode newline characters.

## 4. Resolved: Audio Clipping
**Issue**: Final words of sentences were being cut off due to short recording padding.
**Fix**: Increased end-of-speech padding from 100ms to **300ms** in `AudioRecorder.cs`.
**Result**: Improved transcription accuracy for the tail end of long utterances.

## 5. UI & Environment Fixes
- **Encoding**: Added `chcp 65001` to `run_dev.bat` to fix broken emoji rendering in the console.
- **Version Display**: Updated the hardcoded version string in `DashboardWindow.xaml` to match the project version **v2.2.80.0**.
- **Repository Health**: Restored `.gitignore` to prevent binary build artifacts from cluttering the source control.

---
**Status**: Stable
**Current Version**: 2.2.80.0
**Target OS**: Windows (Native)
