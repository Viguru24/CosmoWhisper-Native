import SwiftUI

struct OverviewView: View {
    @AppStorage("transcriptionCount") private var transcriptionCount = 0
    @ObservedObject var recorder = AudioRecorder.shared
    @ObservedObject var inputController = InputController.shared
    @Binding var isAccessibilityTrusted: Bool
    @AppStorage("showHints") private var showHints = true
    @State private var recentItems: [TranscriptionItem] = []
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Your Command Center")
                .font(.system(size: 32, weight: .bold))
            Text("Let's see how brilliant you've been today.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            HStack(spacing: 20) {
                StatCard(title: "Transcriptions", value: "\(transcriptionCount)", icon: "waveform", color: .blue)
                StatCard(title: "Life Reclaimed", value: transcriptionCount == 0 ? "0.0h" : String(format: "%.1fh", Double(transcriptionCount) * 0.05), icon: "clock.fill", color: .green)
                
            }
            
            SettingsCard(title: "System Permissions", icon: "lock.shield") {
                VStack(alignment: .leading, spacing: 24) {
                    permissionRow(
                        title: "Accessibility (Hotkeys)",
                        description: "Allows CosmoWhisper to listen for your Right Option or Mouse buttons globally.",
                        isTrusted: isAccessibilityTrusted,
                        action: { InputController.shared.requestAccessibility() }
                    )
                    
                    Divider().opacity(0.1)
                    
                    permissionRow(
                        title: "Automation (Typing)",
                        description: "Allows CosmoWhisper to paste text into other applications.",
                        isTrusted: inputController.isAutomationTrusted,
                        action: { 
                            inputController.requestAutomation()
                        }
                    )
                    
                    Divider().opacity(0.1)
                    
                    HStack {
                        VStack(alignment: .leading, spacing: 4) {
                            Text("Microphone Access")
                                .font(.headline)
                            Text("Required to record your voice.")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                        }
                        Spacer()
                        Image(systemName: "checkmark.circle.fill")
                            .foregroundColor(.green)
                    }
                    
                    Divider().opacity(0.1)
                    
                    if !isAccessibilityTrusted {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("⚠️ macOS is blocking global hotkeys")
                                .font(.headline)
                                .foregroundColor(.orange)
                            Text("If you've granted access but it still doesn't work, macOS might be 'ghosting' the permission. This is common after app updates.")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            
                            Button(action: deepResetPermissions) {
                                Label("Self-Repair System Permissions", systemImage: "wrench.and.screwdriver.fill")
                                    .font(.system(size: 13, weight: .bold))
                                    .padding(.horizontal, 16)
                                    .padding(.vertical, 8)
                                    .background(Color.orange.opacity(0.15))
                                    .foregroundColor(.orange)
                                    .cornerRadius(8)
                            }
                            .buttonStyle(.plain)
                        }
                        .padding(.top, 8)
                    } else {
                        HStack(spacing: 16) {
                            Button(action: {
                                LogManager.shared.log("UI: RESETTING LIFESTYLE STATS")
                                transcriptionCount = 0
                            }) {
                                Label("Reset Lifetime Stats", systemImage: "arrow.counterclockwise")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                            .buttonStyle(.plain)
                            
                            Divider().frame(height: 12).opacity(0.2)
                            
                            Button(action: deepResetPermissions) {
                                Label("Reset All Permissions", systemImage: "trash.fill")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                            .buttonStyle(.plain)
                        }
                        .padding(.top, 8)
                    }
                }
            }
            
            if showHints {
                HintsViewLocal(onDismiss: { showHints = false })
            }
            
            /*
            SettingsCard(title: "The Gossip Log", icon: "bubble.left.and.bubble.right.fill") {
                VStack(alignment: .leading, spacing: 12) {
                    HStack {
                        Spacer()
                        Button(action: {
                            recorder.clearRecentActivity()
                        }) {
                            HStack(spacing: 4) {
                                Image(systemName: "trash")
                                Text("Clear Gossip")
                            }
                            .font(.caption.bold())
                            .foregroundColor(.red.opacity(0.8))
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background(Color.red.opacity(0.1))
                            .cornerRadius(6)
                        }
                        .buttonStyle(.plain)
                    }
                    .padding(.bottom, 4)
                    
                    if recentItems.isEmpty {
                        VStack(alignment: .center, spacing: 12) {
                            Image(systemName: "tray")
                                .font(.system(size: 32))
                                .foregroundColor(.secondary.opacity(0.3))
                            Text("No recent transcriptions available.")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 40)
                    } else {
                        ForEach(recentItems) { item in
                            HStack(alignment: .top, spacing: 12) {
                                Image(systemName: "quote.opening")
                                    .foregroundColor(.blue.opacity(0.5))
                                    .font(.caption)
                                    .padding(.top, 4)
                                
                                VStack(alignment: .leading, spacing: 4) {
                                    Text(item.text)
                                        .font(.system(size: 14))
                                        .foregroundColor(.white.opacity(0.9))
                                        .lineLimit(2)
                                    
                                    Text(item.date, style: .time)
                                        .font(.system(size: 10))
                                        .foregroundColor(.secondary)
                                }
                                Spacer()
                                
                                Button(action: {
                                    NSPasteboard.general.clearContents()
                                    NSPasteboard.general.setString(item.text, forType: .string)
                                }) {
                                    Image(systemName: "doc.on.doc")
                                        .font(.caption)
                                        .foregroundColor(.blue.opacity(0.8))
                                        .padding(8)
                                        .background(Color.blue.opacity(0.1))
                                        .cornerRadius(6)
                                }
                                .buttonStyle(.plain)
                            }
                            .padding(12)
                            .background(Color.white.opacity(0.02))
                            .cornerRadius(8)
                            
                            if item.id != recentItems.last?.id {
                                Divider().opacity(0.05)
                            }
                        }
                    }
                }
                .onAppear(perform: loadRecentActivity)
                .onReceive(NotificationCenter.default.publisher(for: NSNotification.Name("RecentActivityChanged"))) { _ in
                    loadRecentActivity()
                }
                .onReceive(NotificationCenter.default.publisher(for: UserDefaults.didChangeNotification)) { _ in
                    loadRecentActivity()
                }
            }
            */
        }
    }
    
    private func permissionRow(title: String, description: String, isTrusted: Bool, action: @escaping () -> Void) -> some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.headline)
                Text(description)
                    .font(.subheadline)
                    .foregroundColor(.secondary)
            }
            Spacer()
            
            if isTrusted {
                Image(systemName: "checkmark.circle.fill")
                    .foregroundColor(.green)
            } else {
                Button(action: action) {
                    Text("Request Access")
                        .padding(.horizontal, 12)
                        .padding(.vertical, 6)
                        .background(Color.red.opacity(0.2))
                        .foregroundColor(.red)
                        .cornerRadius(6)
                }
                .buttonStyle(.plain)
            }
        }
    }
    
    private func deepResetPermissions() {
        LogManager.shared.log("UI: DEEP PERMISSION RESET INITIATED")
        let bundleID = Bundle.main.bundleIdentifier ?? "com.cosmowhisper.CosmoWhisper"
        
        Task.detached(priority: .userInitiated) {
            let task = Process()
            task.launchPath = "/usr/bin/tccutil"
            task.arguments = ["reset", "All", bundleID]
            
            // Kill System Events to clear hung states
            let task3 = Process()
            task3.launchPath = "/usr/bin/killall"
            task3.arguments = ["System Events"]
            
            do {
                try task.run()
                try? task3.run()
                LogManager.shared.log("UI: Permissions reset triggered. Relaunching...")
                
                await MainActor.run {
                    let url = Bundle.main.bundleURL
                    let configuration = NSWorkspace.OpenConfiguration()
                    NSWorkspace.shared.openApplication(at: url, configuration: configuration) { _, _ in
                        DispatchQueue.main.async {
                            NSApp.terminate(nil)
                        }
                    }
                }
            } catch {
                LogManager.shared.log("UI ERROR: Failed to execute reset: \(error.localizedDescription)")
            }
        }
    }
    
    private func loadRecentActivity() {
        if let data = UserDefaults.standard.data(forKey: "recentTranscriptions"),
           let decoded = try? JSONDecoder().decode([TranscriptionItem].self, from: data) {
            self.recentItems = decoded
        }
    }
}

struct HintsViewLocal: View {
    var onDismiss: () -> Void
    let hints = [
        Hint(title: "The Power of Right Option", icon: "keyboard", text: "Hold your hotkey (Right Option or Mouse Button) to talk, release it to instantly paste. No clicking required."),
        Hint(title: "System Control", icon: "command", text: "Say 'Select all', 'Copy all', or 'Delete all' to manage text without touching the keyboard."),
        Hint(title: "Instant Translation", icon: "globe", text: "Select clear text and say 'Translate to Spanish' (or French) to instantly replace it."),
        Hint(title: "Smart Commands", icon: "bolt.fill", text: "Try saying 'new line', 'bold that', or 'comma' during transcription to format your text on the fly.")
    ]
    
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HStack {
                Text("Cheat Codes")
                Spacer()
                Button(action: onDismiss) {
                    Image(systemName: "xmark.circle.fill")
                        .foregroundColor(.white.opacity(0.3))
                }
                .buttonStyle(.plain)
            }
            
            VStack(spacing: 12) {
                ForEach(hints) { hint in
                    HStack(spacing: 16) {
                        Image(systemName: hint.icon)
                            .font(.system(size: 18))
                            .foregroundColor(.blue)
                            .frame(width: 32, height: 32)
                            .background(Color.blue.opacity(0.1))
                            .cornerRadius(8)
                        
                        VStack(alignment: .leading, spacing: 2) {
                            Text(hint.title)
                                .font(.system(size: 14, weight: .bold))
                            Text(hint.text)
                                .font(.system(size: 13))
                                .foregroundColor(.secondary)
                        }
                        Spacer()
                    }
                    .padding(12)
                    .background(Color.white.opacity(0.03))
                    .cornerRadius(12)
                }
            }
        }
        .padding(24)
        .background(Color(red: 30/255, green: 40/255, blue: 60/255).opacity(0.3))
        .cornerRadius(16)
        .overlay(RoundedRectangle(cornerRadius: 16).stroke(Color.white.opacity(0.05), lineWidth: 1))
    }
}

struct Hint: Identifiable {
    let id = UUID()
    let title: String
    let icon: String
    let text: String
}
