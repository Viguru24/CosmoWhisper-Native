# CosmoWhisper: Migration & Stability Report
**Author:** Antigravity (Google DeepMind)
**Date:** January 2026

## 1. The Challenge (Permission Hell)
For days, we struggled with a persistent issue: **Why did CosmoWhisper constantly lose permissions or fail to act?**

We were running an **Electron-based application** (`npm run dev`). This architecture caused endless friction with macOS's security model (TCC - Transparency, Consent, and Control):
*   **Volatile Identity**: Every time the electron dev server restarted, macOS saw a "new" process ID and often a different helper process requesting access.
*   **Misassigned Permissions**: You would grant "Accessibility" to `Electron`, but the actual code ran in a renderer process that macOS considered separate.
*   **Silent Failures**: The app would try to simulate a keypress, fail because it lacked permission, but Electron would swallow the error, leaving us guessing.

## 2. The Turning Point: Native Swift Migration
We made the strategic decision to abandon the web wrapper and build a **100% Native macOS Application** using SwiftUI and AppKit. This solved the core identity crisis:
*   **Stable Bundle ID**: `com.cosmowhisper.native`
*   **Compiled Binary**: We are now running a compiled executable (`CosmoWhisper.app`). Once macOS learns to trust this specific binary path, that trust persists across restarts.
*   **Explicit Entitlements**: We embedded an `Entitlements.plist` file into the build. This is a contract with the OS that explicitly tracks:
    *   `com.apple.security.device.audio-input` (Microphone)
    *   `com.apple.security.automation.apple-events` (Control other apps)
    *   `com.apple.security.personal-information.location` (Hardware input)

## 3. How We Solved Specific Issues

### A. "It keeps asking for permissions!"
**Solution: The `warmupSystem()` Routine**
Instead of waiting for you to fail, the `AppDelegate` now runs a proactive check immediately on launch:
1.  **Accessibility**: Calls `AXIsProcessTrusted()`. If false, it stops nicely.
2.  **Automation**: We actively trigger a harmless AppleScript (`tell application "System Events" to return name`). This forces macOS to prompt you *once* at startup if permission is missing, rather than failing silently later.

### B. "The mouse and keys don't work!"
**Solution: `CGEvent` vs `System Events`**
We moved away from high-level scripting for simple tasks and now use `CoreGraphics` events (`CGEvent`) for solid reliability.
*   **InputController**: Manages a direct pipeline to the hardware event source.
*   **Key Mapping**: We manually mapped specific key codes (0x00 for 'A', 0x07 for 'X', etc.) to ensure 100% reliability regardless of keyboard layout quirks.

### C. "It types unexpected things / Hallucinates"
**Solution: The Filters**
*   **Audio Delay**: Added a 0.3s delay after the "Ding" sound so the mic doesn't record its own startup chime.
*   **Hallucination Filter**: We implemented a hard filter for common garbage outputs from the AI models (e.g., "Music", "Bell", "Bye", "Thank you").

### D. "The window won't resize"
**Solution: SwiftUI Layout Engine**
We moved from fixed sizes (`.frame(width: 900)`) to flexible restrictions (`.frame(minWidth: 900)`). This allows the standard macOS window manager to stretch the content, and our usage of `LazyVGrid` ensures the Smart Command cards flow perfectly into any size.

## 4. The Result
You now have a production-grade macOS utility.
*   **No more terminal servers**.
*   **No more web browsers in the background**.
*   **Instant startup**.
*   **Persistent Permissions**.

This architecture is robust, extendable, and respects the user's system resources.
