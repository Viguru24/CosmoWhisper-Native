# Task Plan: CosmoWhisper Production Hardening (Legacy Decommissioning)

## Phase 1: Surgical Code Removal [COMPLETED]
- [x] Purge `InsertionMethod` branching from `AudioRecorder.cs`.
- [x] Remove `SetInsertionMode` and `UpdateInsertionUI` from `PreferencesController.cs`.
- [x] Excised `BtnFastPaste` and `BtnDirectType` property bridges from `DashboardWindow.xaml.cs`.
- [x] Fix orphaned `UpdateInsertionUI` call in `PreferencesController` init.

## Phase 2: UI Cleanup & Validation [COMPLETED]
- [x] Final audit of `PreferencesView.xaml` for leftover hidden controls.
- [x] Evaluate "Survival Mode" (Local Only) status - preserved for fallback support.
- [x] Verify clean compile with `dotnet build`.

## Phase 3: Production Build [COMPLETED]
- [x] Execute `.\build_msix.ps1` for production validation (v2.2.113.0).
