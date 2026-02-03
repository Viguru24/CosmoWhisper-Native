# Findings - Codebase State

## Refactoring Success
- **Modularity**: The codebase is now highly modular. `DashboardView` is just a shell, making it infinitely easier to maintain.
- **Security**: The app no longer stores API keys in plain text within `UserDefaults`.

## Technical Insights: Hotkey Detection
- **Detection PROOF**: Logs show `HwInput: Key [80]`, `Key [9]`, and `Key [36]` being detected. This confirms the Keyboard Global Monitor **IS** working.
- **Problem**: The app is likely filtering the wrong keys or the user is selecting keys that conflict with system events.
- **Key Code 54 (Right Cmd)**: Confirmed as the target, but the user just recorded **80** (F19).
- **AX Status**: The app is detecting keys even when it thinks AX is untrusted, which suggests the `NSEvent` fallback is successfully firing for certain event types.

## Manus Protocol (Antigravity Optimization)
- **Concept**: The "Manus" protocol is a persistent state-tracking system using `task_plan.md`, `findings.md`, and `progress.md`. It acts as the AI's "working memory" on disk.
- **Rule Compliance**: Following the `planning-with-files` skill instructions to maintain these files every 2-3 actions.

## Hardware Constraints
- **Missing Function Keys**: User's keyboard lacks traditional F-keys (F1-F12). 
- **Consequence**: The previous default of F8 (100) is unusable for this user.
- **Available Keys**: Control, Option, Command, and standard alphanumeric.
- **Goal**: Re-pivot the default hotkey to a globally available key that doesn't conflict with common system shortcuts.

## Potential Issues
- **Keychain Permissions**: As seen in recent system prompts, macOS is cautious about unsigned apps accessing the `login` keychain. Users must authorize access.
- **File Paths**: Deep reorganization of files may require updating the Xcode project file (.pbxproj) if building via Xcode, although script-based builds (like `deploy-mac.sh`) should be updated to find the new paths.

## To Watch
- Ensure `KeychainManager` handles "User Canceled" or "Locked Keychain" gracefully in future iterations.
- Verify `NotificationCenter` posts still reach the correct managers after they were moved.
