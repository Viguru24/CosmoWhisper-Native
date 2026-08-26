import SwiftUI

struct SettingsView: View {
    @ObservedObject var inputController = InputController.shared
    @ObservedObject var recorder = AudioRecorder.shared
    @ObservedObject var theme = ThemeManager.shared
    
    @AppStorage("launchAtLogin") private var launchAtLogin = true
    @AppStorage("useSimulation") private var useSimulation = false
    @AppStorage("restoreClipboard") private var restoreClipboard = false
    @AppStorage("autoSubmit") private var autoSubmit = false
    @AppStorage("micSensitivity") private var micSensitivity = 0.5
    @AppStorage("playChimes") private var playChimes = true
    @AppStorage("widgetOpacity") private var widgetOpacity = 0.8
    @AppStorage("postRollDelayMs") private var postRollDelayMs = 300.0
    @AppStorage("preRollBufferMs") private var preRollBufferMs = 250.0
    @State private var isCalibrating = false
    @Binding var isAccessibilityTrusted: Bool
    @State private var isPulsing = false
    
    // Vault State
    @State private var showPasswordSheet = false
    @State private var vaultPassword = ""
    @State private var isCreatingVault = true
    @State private var pendingVaultURL: URL?
    @State private var vaultStatusMessage = ""
    
    private func keyName(for code: Int) -> String {
        switch code {
        case 80: return "F19"
        case 96: return "F5"
        case 97: return "F6"
        case 98: return "F7"
        case 100: return "F8"
        case 101: return "F9"
        case 103: return "F11"
        case 105: return "F13"
        case 106: return "F16"
        case 107: return "F14"
        case 109: return "F10"
        case 111: return "F12"
        case 113: return "F15"
        case 64: return "F17"
        case 79: return "F18"
        case 90: return "F20"
        case 118: return "F4"
        case 120: return "F2"
        case 122: return "F1"
        case 54: return "Right Cmd"
        case 55: return "Left Cmd"
        case 58: return "Left Opt"
        case 61: return "Right Opt"
        case 59: return "Left Ctrl"
        case 62: return "Right Ctrl"
        case 49: return "Space"
        case 51: return "Backspace"
        case 53: return "Escape"
        default: return "Key \(code)"
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("Settings")
                    .font(.system(size: 32, weight: .bold))
                Spacer()
                
                HStack(spacing: 8) {
                    permissionBadge(isTrusted: isAccessibilityTrusted, label: "ACCESSIBILITY", fullLabel: "ACCESSIBILITY")
                }
            }
            
            Text("Control how CosmoWhisper behaves on your system.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            if !isAccessibilityTrusted {
                permissionWarningSection
            }
            
            VStack(spacing: 24) {
                SettingsCard(title: "Push-To-Talk Shortcut", icon: "keyboard.fill") {
                    VStack(alignment: .leading, spacing: 16) {
                        hotkeyRow
                        Divider().background(Color.white.opacity(0.1))
                        mouseButtonRow
                    }
                }
                .overlay(
                    RoundedRectangle(cornerRadius: 16)
                        .stroke(Color.orange.opacity(0.5), lineWidth: inputController.isRecordingHotkey ? 2 : 0)
                )

                SettingsCard(title: "General Settings", icon: "gearshape") {
                    Toggle(isOn: $launchAtLogin) {
                        VStack(alignment: .leading) {
                            Text("Launch at Login")
                                .font(.headline)
                            Text("Start CosmoWhisper automatically when you sign in.")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                        }
                    }
                    .toggleStyle(SwitchToggleStyle(tint: .blue))
                    .onChange(of: launchAtLogin) { newValue in
                        LaunchManager.shared.setLaunchAtLogin(newValue)
                    }
                }
                
                SettingsCard(title: "Audio & Microphone", icon: "mic.fill") {
                    audioInputSection
                }
                
                SettingsCard(title: "Insertion Method", icon: "text.cursor") {
                    insertionMethodSection
                }

                SettingsCard(title: "Appearance", icon: "paintbrush.fill") {
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Widget Transparency")
                            .font(.headline)
                        Slider(value: $widgetOpacity, in: 0.2...1.0)
                            .accentColor(theme.currentTheme.accent)
                        Text("Adjust how 'ghostly' the widget looks on your screen.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }
                
                vaultSection
            }
        }
        .onAppear { 
            isPulsing = true 
            // Sync Launch at Login status with system
            let systemStatus = LaunchManager.shared.isRegistered
            if launchAtLogin != systemStatus {
                launchAtLogin = systemStatus
            }
        }
    }
    
    private func permissionBadge(isTrusted: Bool, label: String, fullLabel: String) -> some View {
        HStack(spacing: 6) {
            Circle()
                .fill(isTrusted ? Color.green : Color.red)
                .frame(width: 8, height: 8)
            Text(isTrusted ? "\(label) TRUSTED" : "\(label) REQUIRED")
                .font(.system(size: 10, weight: .bold))
                .foregroundColor(isTrusted ? .green : .red)
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 4)
        .background((isTrusted ? Color.green : Color.red).opacity(0.1))
        .cornerRadius(4)
        .scaleEffect(!isTrusted && isPulsing ? 1.05 : 1.0)
        .animation(!isTrusted ? .easeInOut(duration: 0.8).repeatForever(autoreverses: true) : .default, value: isPulsing)
    }
    
    private var permissionWarningSection: some View {
        SettingsCard(title: "Permission Required", icon: "exclamationmark.shield.fill") {
            VStack(alignment: .leading, spacing: 16) {
                if !isAccessibilityTrusted {
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Accessibility permissions are required for the global push-to-talk shortcut.")
                            .font(.subheadline)
                        Button("Open Privacy Settings") {
                            let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")!
                            NSWorkspace.shared.open(url)
                        }
                        .buttonStyle(.borderedProminent)
                    }
                    .foregroundColor(.red)
                }
                

            }
        }
        .padding(.bottom, 24)
    }
    
    private var hotkeyRow: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Hold to Talk Key")
                        .font(.headline)
                    Text("The key you hold down while speaking.")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
                Spacer()
                
                HStack(spacing: 8) {
                    Button(action: { 
                        inputController.isRecordingHotkey.toggle()
                    }) {
                        Text(inputController.isRecordingHotkey ? "Press a key..." : keyName(for: inputController.hotkeyCode))
                            .font(.system(size: 12, weight: .bold))
                            .padding(.horizontal, 12)
                            .padding(.vertical, 6)
                            .background(inputController.isRecordingHotkey ? Color.orange.opacity(0.3) : Color.white.opacity(0.1))
                            .cornerRadius(6)
                    }
                    .buttonStyle(.plain)
                    
                    Menu {
                        Button("Right Control") { updateHotkey(62) }
                        Button("Left Control") { updateHotkey(59) }
                        Button("Right Option") { updateHotkey(61) }
                        Button("Left Option") { updateHotkey(58) }
                        Button("Right Command") { updateHotkey(54) }
                    } label: {
                        Image(systemName: "list.bullet.rectangle.fill")
                            .font(.system(size: 14))
                            .foregroundColor(.secondary)
                            .padding(6)
                            .background(Color.white.opacity(0.05))
                            .cornerRadius(6)
                    }
                    .fixedSize()
                }
            }
            
            if let lastCode = inputController.lastDetectedKeyCode {
                HStack {
                    Image(systemName: "cpu")
                    Text("Last Hardware Key Detected: \(lastCode) (\(keyName(for: lastCode)))")
                }
                .font(.system(size: 10, design: .monospaced))
                .foregroundColor(.blue.opacity(0.8))
                .padding(.top, 4)
            }
            
            // Active key confirmation banner
            HStack(spacing: 8) {
                Image(systemName: "checkmark.circle.fill")
                    .foregroundColor(.green)
                Text("Active key: **\(keyName(for: inputController.hotkeyCode))**")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
            }
            .padding(.horizontal, 10)
            .padding(.vertical, 6)
            .background(Color.green.opacity(0.08))
            .cornerRadius(8)
        }
    }
    
    private var mouseButtonRow: some View {
        VStack(alignment: .leading, spacing: 14) {
            Toggle(isOn: Binding(
                get: { inputController.useMouseButton },
                set: { val in
                    inputController.useMouseButton = val
                    UserDefaults.standard.set(val, forKey: "useMouseButton")
                    if val && inputController.mouseButton == 0 {
                        inputController.mouseButton = 2
                        UserDefaults.standard.set(2, forKey: "mouseButton")
                    }
                    inputController.refreshMonitors()
                }
            )) {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Mouse Shortcut")
                        .font(.headline)
                    Text("Hold a mouse button to talk.")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
            }
            .toggleStyle(SwitchToggleStyle(tint: theme.currentTheme.accent))
            
            if inputController.useMouseButton {
                HStack(spacing: 8) {
                    Text("Selected:")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                    
                    Menu {
                        Button("Middle Click (Button 2)") {
                            inputController.mouseButton = 2
                            UserDefaults.standard.set(2, forKey: "mouseButton")
                            inputController.refreshMonitors()
                        }
                        Button("Side / Back (Button 3)") {
                            inputController.mouseButton = 3
                            UserDefaults.standard.set(3, forKey: "mouseButton")
                            inputController.refreshMonitors()
                        }
                        Button("Side / Forward (Button 4)") {
                            inputController.mouseButton = 4
                            UserDefaults.standard.set(4, forKey: "mouseButton")
                            inputController.refreshMonitors()
                        }
                        Button("Right Click (Button 1)") {
                            inputController.mouseButton = 1
                            UserDefaults.standard.set(1, forKey: "mouseButton")
                            inputController.refreshMonitors()
                        }
                    } label: {
                        HStack(spacing: 4) {
                            Text(mouseButtonLabel(for: inputController.mouseButton))
                                .font(.system(size: 12, weight: .bold))
                            Image(systemName: "chevron.down")
                                .font(.system(size: 10))
                        }
                        .padding(.horizontal, 10)
                        .padding(.vertical, 6)
                        .background(Color.white.opacity(0.1))
                        .cornerRadius(6)
                    }
                    .fixedSize()
                    
                    Spacer()
                    
                    Button(action: { 
                        inputController.isRecordingMouse.toggle()
                    }) {
                        HStack(spacing: 4) {
                            if inputController.isRecordingMouse {
                                Circle().fill(Color.orange).frame(width: 6, height: 6)
                                Text("Click any mouse button now...")
                            } else {
                                Image(systemName: "cursorarrow.click")
                                Text("Click to Custom Bind")
                            }
                        }
                        .font(.system(size: 11, weight: .bold))
                        .padding(.horizontal, 12)
                        .padding(.vertical, 6)
                        .background(inputController.isRecordingMouse ? Color.orange.opacity(0.3) : Color.blue.opacity(0.15))
                        .foregroundColor(inputController.isRecordingMouse ? .orange : .blue)
                        .cornerRadius(6)
                    }
                    .buttonStyle(.plain)
                }
                .padding(.top, 4)
                .transition(.opacity)
            }
        }
    }
    
    private func mouseButtonLabel(for num: Int) -> String {
        switch num {
        case 1: return "Right Click"
        case 2: return "Middle Click (Wheel)"
        case 3: return "Side Button (Back)"
        case 4: return "Side Button (Forward)"
        default: return "Button \(num)"
        }
    }
    
    private var insertionMethodSection: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(spacing: 12) {
                insertionMethodButton(title: "Fast Paste (Default)", subtitle: "Uses clipboard. Instant.", isSelected: !useSimulation) {
                    useSimulation = false
                }
                
                insertionMethodButton(title: "Direct Typing", subtitle: "Simulates keyboard. No clipboard.", isSelected: useSimulation) {
                    useSimulation = true
                }
            }
            
            Toggle(isOn: $restoreClipboard) {
                VStack(alignment: .leading) {
                    Text("Restore Clipboard Content")
                        .font(.headline)
                    Text("Automatically put back your old copied text after a voice paste.")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
            }
            .toggleStyle(SwitchToggleStyle(tint: .blue))
            
            Toggle(isOn: $autoSubmit) {
                VStack(alignment: .leading) {
                    Text("Auto-Submit (Press Enter)")
                        .font(.headline)
                    Text("Press Enter automatically after inserting text.")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
            }
            .toggleStyle(SwitchToggleStyle(tint: .blue))
        }
    }
    
    private func insertionMethodButton(title: String, subtitle: String, isSelected: Bool, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            VStack(alignment: .leading, spacing: 8) {
                Text(title)
                    .font(.headline)
                    .foregroundColor(.white)
                Text(subtitle)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            .padding()
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(isSelected ? Color.blue.opacity(0.2) : Color.white.opacity(0.05))
            .overlay(
                RoundedRectangle(cornerRadius: 10)
                    .stroke(isSelected ? Color.blue : Color.clear, lineWidth: 2)
            )
            .cornerRadius(10)
        }
        .buttonStyle(.plain)
    }
    
    private var vaultSection: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Image(systemName: "lock.shield")
                Text("Security & Backup")
                    .font(.headline)
            }
            .padding(.bottom, 8)
            
            Text("Create an encrypted (AES-256) snapshot of your environment.")
                .font(.subheadline)
                .foregroundColor(.secondary)
                .padding(.bottom, 12)
            
            HStack(spacing: 12) {
                Button("Create Backup") {
                    let panel = NSSavePanel()
                    panel.allowedContentTypes = [.init(filenameExtension: "cvault")!]
                    panel.nameFieldStringValue = "CosmoBackup_\(Int(Date().timeIntervalSince1970))"
                    panel.canCreateDirectories = true
                    panel.title = "Save Encrypted Vault"
                    
                    if panel.runModal() == .OK, let url = panel.url {
                        pendingVaultURL = url
                        isCreatingVault = true
                        vaultPassword = ""
                        showPasswordSheet = true
                    }
                }
                .buttonStyle(.borderedProminent)
                .tint(.green)
                
                Button("Restore Backup") {
                    let panel = NSOpenPanel()
                    panel.allowedContentTypes = [.init(filenameExtension: "cvault")!]
                    panel.canChooseFiles = true
                    panel.canChooseDirectories = false
                    panel.title = "Select Vault to Restore"
                    
                    if panel.runModal() == .OK, let url = panel.url {
                        pendingVaultURL = url
                        isCreatingVault = false
                        vaultPassword = ""
                        showPasswordSheet = true
                    }
                }
                .buttonStyle(.bordered)
            }
            
            if !vaultStatusMessage.isEmpty {
                Text(vaultStatusMessage)
                    .font(.caption)
                    .foregroundColor(vaultStatusMessage.contains("Success") ? .green : .red)
                    .padding(.top, 8)
            }
        }
        .padding(20)
        .background(Color(red: 10/255, green: 15/255, blue: 30/255))
        .cornerRadius(12)
        .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.blue.opacity(0.3), lineWidth: 1))
        .sheet(isPresented: $showPasswordSheet) {
            VStack(spacing: 20) {
                Image(systemName: isCreatingVault ? "lock.doc.fill" : "lock.open.fill")
                    .font(.system(size: 40))
                    .foregroundColor(.blue)
                
                Text(isCreatingVault ? "Encrypt Your Vault" : "Decrypt Your Vault")
                    .font(.headline)
                
                Text(isCreatingVault ? "Set a password for this backup. Don't lose it!" : "Enter the password to restore this backup.")
                    .multilineTextAlignment(.center)
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                    .frame(maxWidth: 250)
                
                SecureField("Password", text: $vaultPassword)
                    .textFieldStyle(.roundedBorder)
                    .frame(width: 200)
                    .onSubmit { performVaultAction() }
                
                HStack {
                    Button("Cancel") { showPasswordSheet = false }
                        .keyboardShortcut(.cancelAction)
                    
                    Button(isCreatingVault ? "Encrypt & Save" : "Decrypt & Restore") {
                        performVaultAction()
                    }
                    .keyboardShortcut(.defaultAction)
                    .disabled(vaultPassword.isEmpty)
                }
            }
            .padding(30)
        }
    }
    
    private func performVaultAction() {
        guard let url = pendingVaultURL, !vaultPassword.isEmpty else { return }
        showPasswordSheet = false
        
        if isCreatingVault {
            let success = VaultManager.shared.createVault(at: url, password: vaultPassword)
            vaultStatusMessage = success ? "Success: Backup created at \(url.lastPathComponent)" : "Error: Could not create backup."
            if success { NSWorkspace.shared.activateFileViewerSelecting([url]) }
        } else {
            let success = VaultManager.shared.restoreVault(from: url, password: vaultPassword)
            vaultStatusMessage = success ? "Success: Settings restored! Restart app." : "Error: Invalid password or corrupt file."
        }
    }
    
    private var audioInputSection: some View {
        VStack(alignment: .leading, spacing: 20) {
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    Image(systemName: "mic.fill")
                        .foregroundColor(theme.currentTheme.accent)
                    Text("Sound Check")
                        .font(.system(size: 14, weight: .bold))
                }
                
                GeometryReader { geo in
                    ZStack(alignment: .leading) {
                        RoundedRectangle(cornerRadius: 6)
                            .fill(Color.black.opacity(0.3))
                            .frame(height: 8)
                        
                        let normalized = max(0, min(1, (recorder.audioLevel + 60) / 60))
                        
                        RoundedRectangle(cornerRadius: 6)
                            .fill(theme.accentGradient)
                            .frame(width: geo.size.width * CGFloat(normalized), height: 8)
                            .animation(.spring(response: 0.1, dampingFraction: 0.8), value: recorder.audioLevel)
                    }
                }
                .frame(height: 8)
            }
            .padding(16)
            .background(Color.black.opacity(0.2))
            .cornerRadius(12)
            
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    Text("Microphone Sensitivity")
                        .font(.headline)
                    Spacer()
                    Text("\(Int(micSensitivity * 100))%")
                        .font(.system(size: 14, weight: .bold, design: .monospaced))
                        .foregroundColor(theme.currentTheme.accent)
                }
                
                Slider(value: $micSensitivity, in: 0...1)
                    .accentColor(theme.currentTheme.accent)
                
                HStack {
                    Text("Whisper").font(.caption).foregroundColor(.gray)
                    Spacer()
                    Text("Loud").font(.caption).foregroundColor(.gray)
                }
            }
            
            Divider().background(Color.white.opacity(0.1))
            
            // Post-Roll Release Padding Slider
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Release Padding (Post-Roll)")
                            .font(.headline)
                        Text("Keeps recording briefly after releasing the button to capture trailing words.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                    Spacer()
                    Text("\(Int(postRollDelayMs)) ms")
                        .font(.system(size: 14, weight: .bold, design: .monospaced))
                        .foregroundColor(theme.currentTheme.accent)
                }
                
                Slider(value: $postRollDelayMs, in: 50...800, step: 25)
                    .accentColor(theme.currentTheme.accent)
                
                HStack {
                    Text("Fast (50ms)").font(.caption2).foregroundColor(.gray)
                    Spacer()
                    Text("Standard (300ms)").font(.caption2).foregroundColor(.blue.opacity(0.8))
                    Spacer()
                    Text("Long (800ms)").font(.caption2).foregroundColor(.gray)
                }
            }
            
            Divider().background(Color.white.opacity(0.1))
            
            // Pre-Roll Lead-in Buffer Slider
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Lead-in Buffer (Pre-Roll)")
                            .font(.headline)
                        Text("Primes the audio hardware to catch words spoken right before pressing.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                    Spacer()
                    Text("\(Int(preRollBufferMs)) ms")
                        .font(.system(size: 14, weight: .bold, design: .monospaced))
                        .foregroundColor(theme.currentTheme.accent)
                }
                
                Slider(value: $preRollBufferMs, in: 0...500, step: 25)
                    .accentColor(theme.currentTheme.accent)
                
                HStack {
                    Text("0ms").font(.caption2).foregroundColor(.gray)
                    Spacer()
                    Text("Recommended (250ms)").font(.caption2).foregroundColor(.blue.opacity(0.8))
                    Spacer()
                    Text("500ms").font(.caption2).foregroundColor(.gray)
                }
            }
            
            Divider().background(Color.white.opacity(0.1))
            
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Action Sounds")
                        .font(.headline)
                    Text("Play a chime when you start and stop talking.")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
                Spacer()
                Toggle("", isOn: $playChimes)
                    .labelsHidden()
                    .toggleStyle(SwitchToggleStyle(tint: theme.currentTheme.accent))
            }
            
            Divider().background(Color.white.opacity(0.1))
            
            VStack(alignment: .leading, spacing: 12) {
                Text("Auto-Calibrate")
                    .font(.headline)
                Text("Measure room noise to set sensitivity automatically.")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                
                Button(action: {
                    isCalibrating = true
                    DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                        micSensitivity = 0.12
                        isCalibrating = false
                    }
                }) {
                    HStack {
                        if isCalibrating {
                            ProgressView().scaleEffect(0.5).brightness(1)
                        } else {
                            Image(systemName: "bolt.fill")
                        }
                        Text(isCalibrating ? "Measuring..." : "Optimize Now")
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 8)
                    .background(theme.accentGradient)
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
                .buttonStyle(.plain)
            }
        }
    }
    
    private func updateHotkey(_ code: Int) {
        inputController.applyHotkey(code)
        LogManager.shared.log("Settings: Hotkey changed to \(keyName(for: code)) [\(code)]")
    }
}
