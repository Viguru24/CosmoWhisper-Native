# Findings - Version Mismatch & Missing Assets

## Theme Mismatch
- **Target:** Sunset (Orange) theme.
- **Hex Code:** `#f59e0b` (Accent), `#ec4899` (Glow).
- **Current state:** `App.xaml` is using Ocean Blue (`#4f46e5`) as default resources.
- **Discovery:** `ThemeController.cs` has `ApplySunset()` which converts these hex codes. However, it requires a `DashboardWindow` to be initialized or manually called.

## Sound Assets
- **Target:** `chime_on.wav` and `chime_off.wav` for F9 recording.
- **Current state:** `SoundManager.cs` looks for these in the `Assets` folder relative to the EXE.
- **Error:** If they are missing from the `Assets` folder, it falls back to Windows sounds (`Speech On.wav`), which explains why the user heard "different sound".

## Git/Branch Status
- **Head:** `ebc7822` on `louis-personal-cosmo-whisper`.
- **Conflict:** The branch seems to have old `App.xaml` defaults compared to what was running on the user's desktop previously.
- **Push Issue:** `git push` fails due to large files (likely binaries in `bin/` or `obj/` that were accidentally tracked or large assets).

## Build Identity
- **Development:** `CosmoWhisperNative2.exe`
- **Personal:** `CosmoWhisperPersonal.exe`
- **Issue:** The `AssemblyName` was flipped back and forth, causing confusion in the build output paths.
