# Audio Chime & Routing Fix Report - Final State

## 1. The Core Problems Resolved
*   **Volume Snapping:** Windows was automatically muting or lowering the app's volume to 1% on startup.
*   **Asset Incompatibility:** Local `.wav` files were occasionally failing due to strict header checks or file corruption.
*   **Static Routing:** Hard-coded device targeting prevented sounds from switching when the user changed outputs (e.g., from Headset to Speakers).
*   **Sound Quality:** Early iterations were perceived as "tinny" or "robotic" (system errors).

## 2. Final Implementation Details

### **A. Professional Audio Sources (The "Speech" Suite)**
We migrated to the **Official Windows Dictation** media group.
*   **Start Chime:** `Speech On.wav` — A rising, melodic chime designed for voice recognition activation.
*   **Stop Chime:** `Speech Off.wav` — A falling, melodic chime designed for microphone closure.
*   **Result:** High-fidelity, melodic feedback that feels like a native OS feature rather than a simple "click" or "beep."

### **B. Infinite Audio Routing (Anti-Hardcoding)**
The engine was switched to the **Windows Default Audio Mapper (Index -1)**.
*   **How it works:** The app no longer looks for "Arctis Nova" or any specific name. It subscribes to the Windows "Default Output" stream.
*   **Benefit:** Whenever you change your sound output in the Windows Taskbar (e.g., switching from your Nova headset to Laptop Speakers or a TV), CosmoWhisper's chimes follow you instantly without a restart.

### **C. Volume Guard (Active Enforcement)**
A background thread runs every 5 seconds to manage the **Windows Audio Session**.
*   **Logic:** It scans all active audio end-points, finds the "CosmoWhisper" session, and forces the volume slider to 100% and un-mutes it.
*   **Benefit:** This prevents Windows "Auto-Ducking" or silent startups from ruining the user experience.

### **D. Hardware Acceleration**
By using `NAudio.WaveOutEvent`, we bypass the newer WinRT "UWP" audio layers which are prone to sandboxing issues, ensuring a direct, low-latency path to the hardware drivers.

## 3. Usage
The chimes are now fully automatic. As long as "Interaction Sounds" are enabled in the Dashboard, the app will handle the routing and volume enforcement silently in the background.
