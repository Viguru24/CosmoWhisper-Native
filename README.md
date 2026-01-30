# CosmoWhisper Native (Swift) Setup

You have successfully switched to the Native Mac track. Here is how to launch your new app.

## 1. Create the Project
1.  Open **Xcode**.
2.  Select **"Create New Project..."**
3.  Choose **macOS** -> **App**.
4.  Product Name: `CosmoWhisper`
5.  Interface: **SwiftUI**
6.  Language: **Swift**
7.  Save it anywhere (e.g., inside `Documents/GitHub/CosmoWhisper-Native`).

## 2. Import the Files
1.  In Xcode's Project Navigator (left sidebar), Delete `ContentView.swift` and `CosmoWhisperApp.swift` (Move to Trash).
2.  Open Finder to: `Documents/GitHub/CosmoWhisper-Native/Sources/`
3.  **Drag and Drop** all 4 files (`CosmoWhisperApp.swift`, `ContentView.swift`, `AudioRecorder.swift`, `InputController.swift`) into the Xcode Project Navigator (under the yellow folder).
    *   *Make sure "Copy items if needed" is CHECKED.*

## 3. Configure Signing & Capabilities
1.  Click the Blue Project Icon at the top left of Xcode.
2.  Go to **Signing & Capabilities**.
3.  Click **+ Capability**.
4.  Add **App Sandbox** (if not there) and ensure:
    *   **Hardware -> Microphone**: CHECKED.
    *   **File Access -> User Selected File**: Read/Write.

## 4. Run
Press **Command + R** (or the Play button).

## Result
You will see a perfectly transparent, native widget. No red borders. No Electron memory hogging.
