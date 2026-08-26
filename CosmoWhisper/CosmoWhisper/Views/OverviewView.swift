import SwiftUI

struct OverviewView: View {
    @AppStorage("transcriptionCount") private var transcriptionCount = 0
    @AppStorage("transcriptionEngine") private var transcriptionEngine = "online"
    @ObservedObject var recorder = AudioRecorder.shared
    @ObservedObject var inputController = InputController.shared
    @ObservedObject var localSpeech = LocalSpeechService.shared
    @Binding var isAccessibilityTrusted: Bool
    @AppStorage("showHints") private var showHints = true
    @State private var recentItems: [TranscriptionItem] = []
    
    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Your Command Center")
                    .font(.system(size: 32, weight: .bold))
                Text("Let's see how brilliant you've been today.")
                    .foregroundColor(.secondary)
            }
            .padding(.bottom, 8)
            
            HStack(spacing: 20) {
                StatCard(title: "Transcriptions", value: "\(transcriptionCount)", icon: "waveform", color: .blue)
                StatCard(title: "Life Reclaimed", value: transcriptionCount == 0 ? "0.0h" : String(format: "%.1fh", Double(transcriptionCount) * 0.05), icon: "clock.fill", color: .green)
            }
            
            engineSelectionCard
            
            SettingsCard(title: "System Permissions", icon: "lock.shield") {
                VStack(alignment: .leading, spacing: 24) {
                    permissionRow(
                        title: "Accessibility (Hotkeys)",
                        description: "Allows CosmoWhisper to listen for your Right Option or Mouse buttons globally.",
                        isTrusted: isAccessibilityTrusted,
                        action: { InputController.shared.requestAccessibility() }
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
    
    private var engineSelectionCard: some View {
        SettingsCard(title: "Transcription Engine & Privacy", icon: "cpu") {
            VStack(alignment: .leading, spacing: 20) {
                Text("Select where your voice is processed. Toggle between complete offline privacy and maximum cloud accuracy.")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                
                HStack(spacing: 16) {
                    // Option 1: Local Model (100% Private)
                    Button(action: {
                        withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                            transcriptionEngine = "local"
                        }
                    }) {
                        VStack(alignment: .leading, spacing: 14) {
                            HStack {
                                ZStack {
                                    Circle()
                                        .fill(transcriptionEngine == "local" ? Color.green.opacity(0.2) : Color.white.opacity(0.05))
                                        .frame(width: 42, height: 42)
                                    Image(systemName: "lock.shield.fill")
                                        .font(.system(size: 20))
                                        .foregroundColor(transcriptionEngine == "local" ? .green : .secondary)
                                }
                                Spacer()
                                if transcriptionEngine == "local" {
                                    HStack(spacing: 4) {
                                        Circle().fill(Color.green).frame(width: 6, height: 6)
                                        Text("ACTIVE")
                                            .font(.system(size: 10, weight: .bold))
                                            .foregroundColor(.green)
                                    }
                                    .padding(.horizontal, 8)
                                    .padding(.vertical, 4)
                                    .background(Color.green.opacity(0.12))
                                    .cornerRadius(6)
                                }
                            }
                            
                            VStack(alignment: .leading, spacing: 4) {
                                Text("Local Model (On-Device)")
                                    .font(.system(size: 16, weight: .bold))
                                    .foregroundColor(.white)
                                Text("Whisper Tiny (75 MB) • 100% Offline")
                                    .font(.caption)
                                    .foregroundColor(.green.opacity(0.9))
                            }
                            
                            VStack(alignment: .leading, spacing: 6) {
                                featureBadge(icon: "shield.lefthalf.filled", text: "100% On-Device Privacy", color: .green)
                                featureBadge(icon: "wifi.slash", text: "Zero Internet Needed", color: .green)
                                featureBadge(icon: "bolt.fill", text: "Instant Local Neural Inference", color: .green)
                            }
                            .padding(.top, 4)
                            
                            if !localSpeech.isModelReady || localSpeech.isDownloading {
                                Button(action: {
                                    Task { await localSpeech.downloadTinyModel() }
                                }) {
                                    HStack {
                                        if localSpeech.isDownloading {
                                            ProgressView().scaleEffect(0.5)
                                            Text("Downloading (\(Int(localSpeech.downloadProgress * 100))%)...")
                                        } else {
                                            Image(systemName: "arrow.down.circle.fill")
                                            Text("Download Tiny Model (75 MB)")
                                        }
                                    }
                                    .font(.caption.bold())
                                    .frame(maxWidth: .infinity)
                                    .padding(.vertical, 8)
                                    .background(Color.green.opacity(0.2))
                                    .foregroundColor(.green)
                                    .cornerRadius(8)
                                }
                                .buttonStyle(.plain)
                                .disabled(localSpeech.isDownloading)
                            }
                        }
                        .padding(18)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .background(transcriptionEngine == "local" ? Color.green.opacity(0.08) : Color.white.opacity(0.02))
                        .cornerRadius(14)
                        .overlay(
                            RoundedRectangle(cornerRadius: 14)
                                .stroke(transcriptionEngine == "local" ? Color.green.opacity(0.6) : Color.white.opacity(0.08), lineWidth: transcriptionEngine == "local" ? 2 : 1)
                        )
                    }
                    .buttonStyle(.plain)
                    
                    // Option 2: Online Model (Groq Cloud)
                    Button(action: {
                        withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                            transcriptionEngine = "online"
                        }
                    }) {
                        VStack(alignment: .leading, spacing: 14) {
                            HStack {
                                ZStack {
                                    Circle()
                                        .fill(transcriptionEngine == "online" ? Color.blue.opacity(0.2) : Color.white.opacity(0.05))
                                        .frame(width: 42, height: 42)
                                    Image(systemName: "bolt.horizontal.fill")
                                        .font(.system(size: 20))
                                        .foregroundColor(transcriptionEngine == "online" ? .blue : .secondary)
                                }
                                Spacer()
                                if transcriptionEngine == "online" {
                                    HStack(spacing: 4) {
                                        Circle().fill(Color.blue).frame(width: 6, height: 6)
                                        Text("ACTIVE")
                                            .font(.system(size: 10, weight: .bold))
                                            .foregroundColor(.blue)
                                    }
                                    .padding(.horizontal, 8)
                                    .padding(.vertical, 4)
                                    .background(Color.blue.opacity(0.12))
                                    .cornerRadius(6)
                                }
                            }
                            
                            VStack(alignment: .leading, spacing: 4) {
                                Text("Online Cloud (Groq LPU)")
                                    .font(.system(size: 16, weight: .bold))
                                    .foregroundColor(.white)
                                Text("Whisper Large-v3 + Llama 3.3 Brain")
                                    .font(.caption)
                                    .foregroundColor(.blue.opacity(0.9))
                            }
                            
                            VStack(alignment: .leading, spacing: 6) {
                                featureBadge(icon: "sparkles", text: "Maximum Accuracy & Vocabulary", color: .blue)
                                featureBadge(icon: "character.book.closed.fill", text: "Understands Heavy Jargon & Accents", color: .blue)
                                featureBadge(icon: "brain.head.profile", text: "Context-Aware Grammar Polishing", color: .blue)
                            }
                            .padding(.top, 4)
                        }
                        .padding(18)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .background(transcriptionEngine == "online" ? Color.blue.opacity(0.08) : Color.white.opacity(0.02))
                        .cornerRadius(14)
                        .overlay(
                            RoundedRectangle(cornerRadius: 14)
                                .stroke(transcriptionEngine == "online" ? Color.blue.opacity(0.6) : Color.white.opacity(0.08), lineWidth: transcriptionEngine == "online" ? 2 : 1)
                        )
                    }
                    .buttonStyle(.plain)
                }
                
                // Comparative Privacy Guarantee
                HStack(spacing: 12) {
                    Image(systemName: transcriptionEngine == "local" ? "checkmark.shield.fill" : "info.circle.fill")
                        .foregroundColor(transcriptionEngine == "local" ? .green : .blue)
                    Text(transcriptionEngine == "local" ? "**Privacy Mode Active**: Audio is transcribed entirely on-device by Apple Silicon Neural Engine. Zero audio bytes ever leave your Mac." : "**High-Precision Mode Active**: Audio is transcribed via Groq's high-speed cloud infrastructure for maximum multi-lingual accuracy and AI formatting.")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
                .padding(12)
                .frame(maxWidth: .infinity, alignment: .leading)
                .background(Color.white.opacity(0.02))
                .cornerRadius(8)
            }
        }
    }
    
    private func featureBadge(icon: String, text: String, color: Color) -> some View {
        HStack(spacing: 6) {
            Image(systemName: icon)
                .font(.system(size: 11))
                .foregroundColor(color)
            Text(text)
                .font(.system(size: 11))
                .foregroundColor(.white.opacity(0.8))
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
