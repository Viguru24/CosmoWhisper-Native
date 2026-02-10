# Task Plan - CosmoWhisper Native

**Phase 1: Foundation (The Ghost Machine)**
- [x] **Project Restructuring**
    - [x] Move `CosmoWhisper-Windows` to root
    - [x] Create `Managers`, `Planning`, `File` directories
    - [x] Initialize Solution
- [x] **Core Audio Engine**
    - [x] Port `AudioRecorder.swift` -> `Managers/AudioRecorder.cs` (NAudio)
    - [x] Verify Mic & Loopback recording
- [x] **Agentic Core (Manus)**
    - [x] Implement `ManusAgent.cs`
    - [x] Implement `PlanningTool.cs`
    - [x] Implement `FileOperator.cs`

**Phase 2: Use Interface**
- [x] **WPF Setup**
    - [x] Install Modern WPF Theme
    - [x] Create Main Window Shell
- [ ] **View Migration**
    - [x] DashboardView
    - [x] IntelligenceView
    - [x] VocabularyView
    - [x] SettingsView
    - [x] MicrophoneView
    - [x] NarrationView
    - [ ] HistoryView
    - [ ] CommandsView
    - [ ] ContactsView
    - [ ] AccountView
    - [ ] LoginView
    - [ ] LogsView
    - [ ] OverviewView
    - [ ] WelcomeView
    - [ ] LanguageView
    - [x] ContentView (Main Layout)

**Phase 3: Integration**
- [ ] **System Integration**
    - [x] Tray Icon
    - [x] Global Hotkeys
    - [x] Agent Mode (Manus)
    - [x] Refine Agent Interaction (Fix chatter)
    - [x] Installer

**Phase 4: Stabilization & Bug Fixes**
- [x] **Preferences Crash Fix**
    - [x] Fix `Password=""` XAML error
    - [x] Restore Manus dependencies to prevent missing class errors
- [x] **UI Polish**
    - [x] Fix garbled Unicode icons in Sidebar/Menu
    - [x] Hide "Manus Project Manager" UI (User Request)
- [x] **Persistence**
    - [x] Fix GUI Scale not saving/loading Correctly
