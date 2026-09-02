<div align="center">

# 🎙️ CosmoWhisper Native

### High-Performance Native AI Voice Dictation & Real-Time Transcription
*100% Offline • Zero Cloud • Push-to-Talk Into Any App • Built with Swift (macOS) & .NET 8 / C++ (Windows)*

<br/>

[![Download macOS DMG](https://img.shields.io/badge/📥%20Download-macOS_DMG_v2.1.2-000000?style=for-the-badge&logo=apple&logoColor=white)](https://github.com/Viguru24/CosmoWhisper-Native/releases/download/v2.1.2-mac/CosmoWhisper_v2.1.2_macOS.dmg)
[![Download Windows Setup](https://img.shields.io/badge/📥%20Download-Windows_Setup_v2.2.18-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/Viguru24/CosmoWhisper-Downloads/releases/download/v2.2.18/CosmoWhisper-Setup-2.2.18.exe)
[![Privacy First](https://img.shields.io/badge/Privacy-100%25%20On--Device-00ff88?style=for-the-badge&logo=shield)](https://github.com/Viguru24/CosmoWhisper-Native)
[![License](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](LICENSE)

<p align="center">
  <a href="#-quick-install">📥 <b>Quick Install</b></a> •
  <a href="#-key-features">✨ <b>Features</b></a> •
  <a href="#-feature-comparison">📊 <b>Comparison</b></a> •
  <a href="#-architecture">🛠️ <b>Architecture</b></a> •
  <a href="#-ecosystem">🌟 <b>Ecosystem</b></a>
</p>

</div>

---

## ✨ Key Features

- **🔒 100% On-Device Sovereignty:** Powered by local OpenAI Whisper models. No audio is ever recorded, transmitted, or uploaded to cloud servers.
- **⚡ Global Push-to-Talk Hotkey:** Hold your custom hotkey (e.g. `Fn`, `Right Alt`, or `Caps Lock`) to dictate anywhere — your code editor, Slack, Word, Discord, or browser.
- **🏎️ Native Performance & Low Overhead:** Written in pure **Swift / SwiftUI** on macOS (Metal GPU accelerated) and **.NET 8 / C++** on Windows (DirectML GPU accelerated). No heavy Electron framework.
- **🎯 Intelligent Punctuation & Formatting:** Auto-formats paragraphs, capitalizes proper nouns, and strips filler words (*"um"*, *"ah"*).
- **🌍 99+ Languages Supported:** Seamless multilingual speech recognition with automatic language detection.

---

## 📊 Feature Comparison

| Feature | Superwhisper ($10/mo) | Wispr Flow ($15/mo) | MacWhisper Pro ($35) | 🎙️ **CosmoWhisper** |
| :--- | :---: | :---: | :---: | :---: |
| **100% Free & Open Source** | ❌ | ❌ | ❌ | **✅ Free Forever** |
| **100% On-Device AI** | ✅ | ❌ (Cloud) | ✅ | **✅ Local AI** |
| **System-Wide Push-to-Talk** | ✅ | ✅ | ⚠️ (Manual) | **✅ Native Global Hook** |
| **Cross-Platform (macOS + Windows)** | ❌ (Mac only) | ❌ (Mac only) | ❌ (Mac only) | **✅ macOS & Windows** |
| **No Subscription Required** | ❌ | ❌ | ⚠️ | **✅ 100% Sovereign** |

---

## ⚡ Quick Install

### macOS (Apple Silicon M1/M2/M3/M4 & Intel)
1. **[📥 Download CosmoWhisper_v2.1.2_macOS.dmg](https://github.com/Viguru24/CosmoWhisper-Native/releases/download/v2.1.2-mac/CosmoWhisper_v2.1.2_macOS.dmg)**.
2. Drag `CosmoWhisper.app` to your **Applications** folder.
3. Grant **Microphone** and **Accessibility** permissions on first launch.

### Windows 10 & 11 (64-bit)
1. **[📥 Download CosmoWhisper-Setup-2.2.18.exe](https://github.com/Viguru24/CosmoWhisper-Downloads/releases/download/v2.2.18/CosmoWhisper-Setup-2.2.18.exe)**.
2. Run the installer and launch CosmoWhisper from your System Tray.

---

## 🛠️ macOS Development Setup (Swift & Xcode)

```bash
# Clone the repository
git clone https://github.com/Viguru24/CosmoWhisper-Native.git
cd CosmoWhisper-Native
```

1. Open Xcode -> **Create New macOS SwiftUI App**.
2. Drag and drop all files from `/Sources` into your Xcode project navigator.
3. In **Signing & Capabilities**, add **App Sandbox** -> Enable **Microphone**.
4. Press `Cmd + R` to build and run!

---

## 🌟 The Ecosystem

Explore other high-performance sovereign software:
- 🎬 **[Vixz YouTube Player (Android)](https://github.com/Viguru24/YouTube)** — Ad-free YouTube player with AI summaries and fluid gestures.
- 🌌 **[Cosmo Symphony (Windows)](https://github.com/Viguru24/Video)** — GPU-accelerated video & photo studio built with Rust & Tauri v2.
- 🖥️ **[Vixz Desktop (Windows)](https://github.com/Viguru24/VixzDesktop)** — Modern glassmorphic desktop YouTube client.

---

<div align="center">
  <sub>Released under the MIT License • Built with ❤️ by <a href="https://github.com/Viguru24">Viguru24</a></sub>
</div>
