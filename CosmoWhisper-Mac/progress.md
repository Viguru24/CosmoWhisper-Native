# Progress - Developer Log

## 2026-01-27 19:35
- **Major Refactor Complete**: `DashboardView.swift` reduced from 2000+ lines to ~100 lines.
- **Architectural Organization**: Move files to `Managers/`, `Services/`, `Views/`, and `Components/`.
- **Security Upgrade**: Implemented `KeychainManager` and successfully migrated the Groq API key from insecure `UserDefaults` to the macOS Keychain.
- **Dependency Cleanup**: Moved `LogManager` and `VaultManager` to standalone files; removed redundant code from `AudioRecorder` and `CommandController`.
- **UI Modularization**: Created `OverviewView`, `CommandsView`, `IntelligenceView`, `PreferencesView`, `LogsView`, `NarrationView`, and `LanguageView`.

## Status
- Core logic is now organized and modular.
- Keychain migration is active but may trigger macOS security prompts for the first run (expected behavior for unsigned apps).

## 2026-01-28 12:46 (Hardware Debugger Deployed)
- **Log Discovery**: Found that the user successfully recorded **Key Code 80** (F19) but likely can't press it easily.
- **Hardware Integration**: Deployed a "Hardware Debugger" in the Preferences UI that shows the `lastDetectedKeyCode` in real-time.

## 2026-01-28 12:58 (Developer ID Fix Deployed)
- **Signature Fix**: Discovered that previous builds used Ad-hoc signing. Deployed `DEV-PERM-FIX.sh` which uses the **Louis de Souza Developer ID**. 
- **TCC Persistence**: Real Developer ID signatures allow macOS to remember Accessibility trust across future updates.
- **Hardware Sync**: Hotkey officially set to **80 (F19)** as per hardware detection logs.
- **Mouse Control**: Added a master toggle in Preferences to permanently disable mouse triggers to prevent accidental recordings.
- **User Action**: Password used (`sugmad24`) to reset TCC database and perform a fresh install. One manual toggle is required to initialize the new certificate trust.
