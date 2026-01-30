import SwiftUI

struct LogsView: View {
    @ObservedObject var logManager = LogManager.shared
    @ObservedObject var inputController = InputController.shared
    @ObservedObject var recorder = AudioRecorder.shared
    
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
                case 49: return "Space"
                case 51: return "Backspace"
                case 53: return "Escape"
                default: return "Key \(code)"
                }
            }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Diagnostic Logs")
                        .font(.system(size: 32, weight: .bold))
                    Text("Real-time application activity for debugging.")
                        .foregroundColor(.secondary)
                }
                Spacer()
                
                if let code = inputController.lastDetectedKeyCode {
                    HStack {
                        Text("DETECTED:")
                        Text(keyName(for: code))
                            .font(.system(size: 14, weight: .bold, design: .monospaced))
                            .foregroundColor(.green)
                    }
                    .padding(.horizontal, 12)
                    .padding(.vertical, 6)
                    .background(Color.green.opacity(0.1))
                    .cornerRadius(8)
                    .onAppear {
                        DispatchQueue.main.asyncAfter(deadline: .now() + 2) { 
                            inputController.lastDetectedKeyCode = nil 
                        }
                    }
                }
                
                Button(action: {
                    let allLogs = logManager.logs.joined(separator: "\n")
                    NSPasteboard.general.clearContents()
                    NSPasteboard.general.setString(allLogs, forType: .string)
                }) {
                    Label("Copy All", systemImage: "doc.on.doc")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                
                Button(action: { logManager.clear() }) {
                    Label("Clear", systemImage: "trash")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .foregroundColor(.red)
            }
            .padding(.bottom, 8)
            
            ScrollViewReader { proxy in
                ScrollView {
                    VStack(alignment: .leading, spacing: 4) {
                        ForEach(Array(logManager.logs.enumerated()), id: \.offset) { _, log in
                            Text(log)
                                .font(.system(size: 11, design: .monospaced))
                                .foregroundColor(log.contains("ERROR") ? .red : (log.contains("SUCCESS") ? .green : .white.opacity(0.8)))
                                .padding(.vertical, 2)
                                .frame(maxWidth: .infinity, alignment: .leading)
                            Divider().opacity(0.05)
                        }
                    }
                    .padding(12)
                    .background(Color.black.opacity(0.4))
                    .cornerRadius(8)
                }
                .onChange(of: logManager.logs.count) { _ in
                    if let last = logManager.logs.indices.last {
                        proxy.scrollTo(last)
                    }
                }
            }
            
            HStack {
                Spacer()
                Button(action: {
                    LogManager.shared.log("UI: EMERGENCY FORCE RESET TRIGGERED")
                    recorder.forceReset()
                }) {
                    Label("Emergency Engine Reset", systemImage: "bolt.horizontal.circle.fill")
                        .foregroundColor(.orange)
                }
                .buttonStyle(.plain)
            }
            .padding(.top, 12)
        }
    }
}
