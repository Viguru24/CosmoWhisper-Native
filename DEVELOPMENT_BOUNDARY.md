# ⚠️ CRITICAL SECURITY BOUNDARY: READ THIS FIRST ⚠️

## THE ABSOLUTE RULE
**NEVER, under any circumstances, write real user personal details (Addresses, Phone Numbers, Emails) into the source code files.**

### 🚫 The "Reedham Boundary"
- **Source Code (GitHub Repository)**: Must remain **100% Generic and Anonymous**. We use the **Batman/Gotham City** test suite as the mandatory placeholder for all examples, default settings, and test cases.
- **Production Data (Local Only)**: The user's real life (e.g., Reedham Drive details) lives **only** in the `%APPDATA%\CosmoWhisper_Native` folder on their local disk.
- **Development Data (Local Only)**: The development instance uses `%APPDATA%\CosmoWhisper_Dev` which is seeded from the Generic Batman code.

---

## 🎯 Mandatory Agent Directives
1. **Zero Exposure**: When creating MSIX installers or pushing to GitHub, ensure `VocabularyDefaults.json` contains ONLY Batman references.
2. **Isolation Enforcement**:
    - **Green Version (Production)**: `AppData\Roaming\CosmoWhisper_Native` -> **DO NOT TOUCH.**
    - **Orange Version (Development)**: `AppData\Roaming\CosmoWhisper_Dev` -> Safe for testing/resetting.
3. **Stat Neutrality**: Default preferences in the repository must have:
    - `TotalWords = 0`
    - `TotalTranscriptions = 0`
    - `UsageMinutes = 0.0`
4. **Port 8338**: This port is the clashing point. Always kill the production usage of 8338 before starting the dev build, but **never** modify the production config to "fix" it.

---

## 🛠️ Fail-Safe Build Workflow
Before running `/build_windows_msix`, the agent **must** run a grep check for any strings matching "Reedham" or the user's real postcode. If found, the build **must be aborted immediately**.

---

## ⚡ Identity Reminder
The user is **NOT** Bruce Wayne. We use Bruce Wayne because he is a **TEST CHARACTER**. The user's real identity is private and localized.

**STAY ON THE BATMAN SIDE OF THE LINE.**
