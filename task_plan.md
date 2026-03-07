# Task Plan - Restoring Final Version and Fixes

The goal is to restore the "final" state of the application (Orange theme, specific sounds) while maintaining the build fixes for `CosmoWhisperPersonal.exe`.

## Phase 1: Research & Diagnosis (DONE)
- [x] Check git history for "Orange" related changes.
- [x] Locate theme definitions in `ThemeController.cs`.
- [x] Compare current `App.xaml` and `WidgetWindow.xaml` with historical backups.
- [x] Verify SoundManager logic for F9 chimes.

## Phase 2: Restoring "Final" Visuals & Sounds
- [ ] Update `App.xaml` resources to match the "Sunset" (Orange) theme by default or via initialization.
- [ ] Ensure `SoundManager.cs` is correctly playing `chime_on.wav` and `chime_off.wav`.
- [ ] Verify `Assets` folder contains the correct sound files.
- [ ] Ensure `WidgetWindow.xaml.cs` toggle logic uses the correct theme colors.

## Phase 3: Build & Deployment Cleanup
- [ ] Fix the `git push` error (Large files/Commit issues).
- [ ] Consolidate `CosmoWhisper.csproj` to handle `CosmoWhisperPersonal` vs `CosmoWhisperNative2` cleanly via build configurations.
- [ ] Update shortcuts on Desktop for both versions.

## Phase 4: Final Validation
- [ ] Run Dev app -> Verify Orange theme + F9 Sounds.
- [ ] Run Personal app -> Verify Orange theme + F9 Sounds.
- [ ] Push all fixes to `louis-personal-cosmo-whisper`.
