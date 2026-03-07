# CosmoWhisper Build Standards (CRITICAL)

This document defines the visual and functional standards for different builds of CosmoWhisper. READ THIS EVERY TIME BEFORE STARTING.

## 1. Gear Icon Indicator (The "Floaty" Widget)
The gear icon on the floating widget MUST reflect the build type:
- **ORANGE:** Development Build (`DEBUG` mode / `run_dev.bat`).
- **GREEN:** Personal Build (`CosmoWhisperPersonal.exe`).
- **GREY:** Production Build (Standard release).

## 2. Audio Standards (F9 Toggle)
- **Start Sound:** MUST be `chime_on.wav` from the `Assets` folder.
- **Stop Sound:** MUST be `chime_off.wav` from the `Assets` folder.
- Fallback to Windows sounds is only allowed if assets are physically missing, but the target is the bundled melodic chirps.

## 3. Build Identities
- **Development EXE Name:** `CosmoWhisperNative2.exe` (mapped to `run_dev.bat`).
- **Personal EXE Name:** `CosmoWhisperPersonal.exe` (mapped to Desktop shortcut).
- **App Data Folders:** 
  - Development: `CosmoWhisper`
  - Personal: `CosmoWhisper_Personal`
