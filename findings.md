# Findings - Performance & Packaging

## Discovery 1: Audio Monitoring Loop (OPTIMIZED v2)
The initial optimization helped, but the high callback frequency (100ms) was still causing unnecessary overhead.
**Further optimizations applied**:
- **Increased Buffer**: Set `BufferMilliseconds` to 150ms in `WaveInEvent` to reduce the number of context switches and callbacks per second.
- **Throttled Calculation**: Added a time-based throttle to skip amplitude calculations if less than 150ms has passed since the last update (when not recording).

## Discovery 2: UI Rendering Overhead (OPTIMIZED)
The WPF rendering engine was consuming CPU due to complex gradients and multiple blur/shadow effects being re-rendered frequently by the `IridescentAnimation`.
**Optimizations applied**:
- **RenderingBias**: Set `RenderingBias="Performance"` on all `DropShadowEffect` and `BlurEffect` elements in `WidgetWindow.xaml`. This forces the GPU to use a faster (though slightly less accurate) rendering path, significantly reducing CPU usage.

## Discovery 3: MSIX Build Manifest
The MSIX build was failing because the manifest referenced `CosmoWhisperNative.exe`, but the project is configured to output `CosmoWhisperNative2.exe`.
**Fix applied**:
- Updated `Package.appxmanifest` to correctly reference `CosmoWhisperNative2.exe`.

## Final Status
- **Idle CPU**: Should now be < 1% on most modern hardware.
- **Packaging**: Version 2.2.72.0 has been successfully built as an MSIX installer.
- **Launcher**: The working `run_dev.bat` and Desktop shortcut are configured correctly.
