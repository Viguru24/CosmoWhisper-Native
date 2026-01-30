# CosmoWhisper Build & Maintenance Guide

## 🚀 How to Build & Install (The "Golden" Method)

To ensure the application works consistently every time, even after a reboot, you must use the **Force Install** script. This script handles all the complex macOS permission ownership issues automatically.

### Step 1: Run the Install Script
Open your terminal and run the following command. This will:
1.  Kill any running instances of the app.
2.  Compile the latest code.
3.  **Force-install** it to `/Applications/CosmoWhisper.app` using your admin password.
4.  Launch the app.

```bash
bash scripts/GENTLE-BUILD.sh
```

*(Note: You will be asked for your password. This is normal and ensures the app has the correct "Owner" permissions on your Mac.)*

---

## 🛡️ Handling Permissions (The "One-Time" Fix)

If macOS ever complains about permissions (e.g., "CosmoWhisper would like to control this computer"), follow this strictly:

1.  Open **System Settings**.
2.  Go to **Privacy & Security** -> **Accessibility**.
3.  Find **CosmoWhisper** in the list.
    *   **If it's there:** Toggle user switch **OFF**, wait 1 second, then toggle **ON**.
    *   **If it's NOT there:** Click the **(+)** button, navigate to `/Applications`, and select `CosmoWhisper.app`.
4.  Repeat this check in **Privacy & Security** -> **Input Monitoring** if prompted.

### Why do I have to do this?
When we fundamentally change the app's code (like adding new "Direct Typing" features), its digital signature changes. macOS treats it as a "new" app for security reasons. Once you stop strictly developing and start just *using* the app, this will stop happening.

---

## 🆘 Troubleshooting

### "The app won't open"
Run the nuclear fix script. This resets all permissions for the app so you can start fresh.
```bash
bash scripts/FIX-PERMISSIONS-NOW.sh
```

### "My settings (Hotkeys/Vocabulary) aren't saving"
The app now uses a secure `VaultManager`. Ensure you are creating backups in **Settings -> Security & Backup** if you want to transfer settings between machines or clean installs.

---

## ❓ FAQ

**Q: Are we using Manus?**
No, we are NOT using Manus. We are using **Groq** (specifically the Llama 3 model) for all AI logic and transcription. There is no code related to Manus in this project.
