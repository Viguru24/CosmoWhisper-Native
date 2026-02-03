# Task Plan - Codebase Cleanup and Refactoring

## Phase 1: Physical Cleanup (Done)
- [x] Create `.gitignore` to resolve Git overload
- [x] Organize root directory (scripts, logs, archives)
- [x] Delete redundant build folders and logs

## Phase 2: Architectural Refactoring (Done)
- [x] Extract `LogManager` from `AudioRecorder`
- [x] Extract `VaultManager` and utility extensions
- [x] Modularize `DashboardView` into feature-specific views
- [x] Create standardized UI components folder
- [x] Organize managers into `/Managers` and services into `/Services`

## Phase 3: Security & Stability (Done)
- [x] Implement `KeychainManager` for secure storage
- [x] Migrate Groq API Key to system Keychain
- [x] Fix UI reactivity for Keychain-stored variables

## Phase 4: Validation & Polish (In Progress)
- [x] Verify build stability after deep reorganization (Stable with GENTLE-BUILD.sh)
- [x] Check for missing imports or internal links between new files
- [x] Add error handling for Keychain access failures
- [x] Final UI/UX pass on refactored views (Vocabulary, Narration, Preferences polished)
- [x] Create Standard Operating Procedures (`BUILD_GUIDE.md`)

## Phase 5: Release Readiness (In Progress)
- [x] Enable Hardened Runtime for Release builds (Critical for Mic access)
- [x] Build and Verify Universal DMG
- [x] Integrate `LaunchManager` for reboot-persistent operation
- [x] Pivot: Hardware constraint identified (No F-keys)
- [x] Select and implement a universal non-F-key default (Default: **Key 80**)
- [x] Implement **Hybrid Hotkey Strategy**: Added `flagsChanged` support for modifiers.
- [x] Fix: Deployed **Developer ID signing** (Louis de Souza) to ensure TCC permissions persist across builds.
- [x] Feature: Added **Mouse Shortcut Toggle** to permanently disable mouse triggers if needed.
- [ ] Implement **Local Key Verifier** in UI to confirm hardware/local software sync regardless of global focus.
