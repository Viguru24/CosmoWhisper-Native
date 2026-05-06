import SwiftUI

struct OverviewView: View {
    @AppStorage("transcriptionCount") private var transcriptionCount = 0
    @ObservedObject var recorder = AudioRecorder.shared
    @ObservedObject var inputController = InputController.shared
    @Binding var isAccessibilityTrusted: Bool
    @AppStorage("showHints") private var showHints = true
    @State private var recentItems: [TranscriptionItem] = []

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            VStack(alignment: .leading, spacing: 8) {
                Text("Command Center")
                    .font(.system(size: 28, weight: .bold))
                    .foregroundColor(.white)
                Text("Monitor your productivity and manage settings")
                    .foregroundColor(.secondary)
            }

            HStack(spacing: 16) {
                StatCard(title: "Transcriptions", value: "\(transcriptionCount)", icon: "waveform", color: .blue)
                StatCard(title: "Time Saved", value: transcriptionCount == 0 ? "0.0h" : String(format: "%.1fh", Double(transcriptionCount) * 0.05), icon: "clock.fill", color: .green)
            }

            SettingsCard(title: "Permissions", icon: "lock.shield") {
                VStack(alignment: .leading, spacing: 16) {
                    PermissionRow(
                        title: "Accessibility",
                        description: "Global hotkey detection",
                        isTrusted: isAccessibilityTrusted,
                        action: { InputController.shared.requestAccessibility() }
                    )

                    Divider().opacity(0.1)

                    PermissionRow(
                        title: "Automation",
                        description: "Text insertion in other apps",
                        isTrusted: inputController.isAutomationTrusted,
                        action: { inputController.requestAutomation() }
                    )
                }
            }

            if showHints {
                HintsBanner(onDismiss: { showHints = false })
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

struct PermissionRow: View {
    let title: String
    let description: String
    let isTrusted: Bool
    let action: () -> Void

    @ObservedObject var theme = ThemeManager.shared

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.system(size: 14, weight: .medium))
                Text(description)
                    .font(.system(size: 12))
                    .foregroundColor(theme.textSecondary)
            }
            Spacer()

            if isTrusted {
                Image(systemName: "checkmark.circle.fill")
                    .foregroundColor(theme.success)
            } else {
                Button(action: action) {
                    Text("Grant")
                        .padding(.horizontal, 12)
                        .padding(.vertical, 6)
                        .background(theme.error.opacity(0.2))
                        .foregroundColor(theme.error)
                        .cornerRadius(6)
                }
                .buttonStyle(.plain)
            }
        }
    }
}

struct HintsBanner: View {
    var onDismiss: () -> Void
    let hints = [
        Hint(title: "Right Option", icon: "keyboard", text: "Hold to speak, release to paste."),
        Hint(title: "Smart Commands", icon: "bolt.fill", text: "Say 'new line', 'comma', or 'delete that'."),
        Hint(title: "Translation", icon: "globe", text: "Select text and say 'Translate to French'.")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("Quick Tips")
                    .font(.system(size: 14, weight: .bold))
                Spacer()
                Button(action: onDismiss) {
                    Image(systemName: "xmark")
                        .foregroundColor(.secondary)
                }
                .buttonStyle(.plain)
            }

            HStack(spacing: 12) {
                ForEach(hints) { hint in
                    HStack(spacing: 8) {
                        Image(systemName: hint.icon)
                            .foregroundColor(.blue)
                            .font(.system(size: 14))
                        VStack(alignment: .leading, spacing: 2) {
                            Text(hint.title)
                                .font(.system(size: 12, weight: .medium))
                            Text(hint.text)
                                .font(.system(size: 11))
                                .foregroundColor(.secondary)
                        }
                    }
                    .padding(8)
                    .background(Color.white.opacity(0.03))
                    .cornerRadius(8)
                }
            }
        }
        .padding(16)
        .background(Color.blue.opacity(0.1))
        .cornerRadius(12)
    }
}

struct Hint: Identifiable {
    let id = UUID()
    let title: String
    let icon: String
    let text: String
}
