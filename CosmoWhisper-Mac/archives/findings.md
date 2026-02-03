# Findings & Discoveries

## Architecture & Data Flow
- **InputController**: Manages global and local event monitoring.
- **DashboardView**: UI for settings. Recording mouse buttons happens here.
- **UnifiedClickDetector**: Custom NSView wrapper to catch clicks in the floating widget.

## Codebase Insights
- **InputController.swift**: Recently added `setupLocalMouseRecording` to catch side buttons while the app is focused (Dashboard open). Global monitor handles background triggers.
- **Mouse Buttons**: Button 0 is Left Click. Side buttons usually start at 3.
- **Permissions**: TCC (Transparency, Consent, and Control) often blocks `CGEvent` taps unless Accessibility is granted. Resetting TCC database (`tccutil reset`) clears "ghost" blocks.

## Known Constraints
- **Terminal Sudo**: Running `sudo` commands via agent tools hangs because we cannot interactively supply the password.
- **Widget Window**: `NSPanel` with `.nonactivatingPanel` style can miss SwiftUI tap gestures; improved by `TransparentClickView` with `wantsLayer = true`.
