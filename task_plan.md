# Task Plan - Versioning & Fallback Hardening

## [2026-04-29 19:10] Implementation Complete
- Fixed Gear Icon color logic in `WidgetWindow.xaml.cs` (Lime Green for Personal).
- Fixed Dashboard branding and Sidebar gear color in `DashboardWindow.xaml.cs`.
- Hardened `AIService.cs` transcription race logic to filter out `[BLANK_AUDIO]` and empty strings.
- Added explicit Cloud-to-Local fallback for valid but empty cloud results.
- Verified `PreferenceManager.cs` handles Louis's email override correctly.

## Status
- Ready for testing. Restart the app to see the Lime Green gear.

- [x] **Phase 1: Build & Version Fixes**
    - [x] Update `PreferenceManager.cs` to ensure `IsStoreVersion` robustly identifies Personal builds.
    - [x] Correct `WidgetWindow.xaml.cs` to show Lime Green gear for Personal builds.
    - [x] Align `DashboardWindow.xaml.cs` branding with widget indicators.

- [x] **Phase 2: Transcription Reliability**
    - [x] Harden `AIService.cs` race logic to skip `[BLANK_AUDIO]` or empty local results.
    - [x] Implement Cloud -> Local fallback for empty cloud results (not just errors).
    - [x] Verify visual feedback for "Cloud API" vs "Local Model" sources.

## Phase 3: UX Feedback Improvements
- [ ] Update `WidgetWindow.xaml.cs` to show a temporary badge or status when "Cloud API" is used vs "Local Model".
- [ ] Ensure the user knows when they are in "Personal" mode.

## Phase 4: Verification
- [ ] Run the app and check logs for "Lime Green Gear" confirmation.
- [ ] Test transcription with Local disabled and verify Groq output is pasted.
- [ ] Test transcription with both enabled and verify "Race winner" behavior handles blank audio correctly.
