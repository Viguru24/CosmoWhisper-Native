# Task Plan - Resolving Idle CPU Usage & MSIX Build

## Phase 1: Investigation (Completed)
- [x] Identify `AudioRecorder` monitoring loop as the likely cause.
- [x] Diagnose why `run_dev.bat` was failing (incorrect `APP_PATH`).

## Phase 2: Implementation (Completed)
- [x] Optimize `AudioRecorder.cs` (bitwise extraction, integer math, throttled updates).
- [x] Optimize `WidgetWindow.xaml.cs` (throttled UI, BeginInvoke).
- [x] Optimize `WidgetWindow.xaml` (RenderingBias="Performance" on effects).
- [x] Fix `run_dev.bat` path.
- [x] Copy launcher to OneDrive Desktop.

## Phase 3: MSIX Build (In Progress)
- [x] Stop running instances.
- [/] Run `./build_msix.ps1`.
- [ ] Verify `CosmoWhisper.msix` output.

## Phase 4: Validation
- [ ] Verify CPU usage is lower (aiming for < 1% on modern hardware).
- [ ] Provide the MSIX installer to the user.

## Error Log
- `run_dev.bat` failing: Path `bin\x64\Debug` was incorrect; actual is `bin\Debug`.
- CPU Usage: Optimization attempt 1 (bitwise) helped but user reported 4%. Applied attempt 2 (throtte, buffer size, rendering bias).
