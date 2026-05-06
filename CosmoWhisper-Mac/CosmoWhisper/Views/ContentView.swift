import SwiftUI
import AppKit

struct ContentView: View {
    @EnvironmentObject var audioRecorder: AudioRecorder
    @State private var isHovering = false
    @AppStorage("widgetOpacity") private var widgetOpacity = 0.8
    @State private var isAccessibilityTrusted = AXIsProcessTrusted()
    @ObservedObject var theme = ThemeManager.shared

    var body: some View {
        HStack(spacing: 12) {
            ZStack {
                Circle()
                    .fill(orbGradient)
                    .frame(width: 18, height: 18)
                    .scaleEffect(audioRecorder.isRecording ? (1.0 + CGFloat(max(0, audioRecorder.audioLevel + 50) / 100)) : 1.0)
                    .shadow(color: orbColor.opacity(0.8), radius: audioRecorder.isRecording ? 8 : 3, x: 0, y: 0)

                if audioRecorder.isProcessing {
                    Circle()
                        .stroke(theme.accentGradient, lineWidth: 2)
                        .frame(width: 24, height: 24)
                        .animation(.linear(duration: 2).repeatForever(autoreverses: false), value: audioRecorder.isProcessing)
                }

                UnifiedClickDetector(
                    onLeftClick: {
                        LogManager.shared.log("UI: Mic Orb Left-Clicked")
                        audioRecorder.toggleRecording()
                    },
                    onRightClick: {
                        LogManager.shared.log("UI: Mic Orb Right-Click - Show Dashboard")
                        WindowManager.shared.showDashboard()
                    }
                )
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
            .frame(width: 40, height: 40)

            VoiceVisualizerView(audioLevel: audioRecorder.audioLevel, isRecording: audioRecorder.isRecording)
                .frame(width: 80, height: 20)

            HStack(spacing: 8) {
                if !isAccessibilityTrusted && !audioRecorder.isRecording {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .foregroundColor(.red)
                        .font(.system(size: 10))
                }

                ZStack {
                    Image(systemName: "gear")
                        .font(.system(size: 14))
                        .foregroundColor(Color.white.opacity(0.8))
                        .padding(6)
                        .background(Color.white.opacity(0.1))
                        .clipShape(Circle())

                    UnifiedClickDetector(
                        onLeftClick: {
                            LogManager.shared.log("UI: Gear icon Left-Click - Show Dashboard")
                            WindowManager.shared.showDashboard()
                        },
                        onRightClick: {
                            LogManager.shared.log("UI: Gear icon Right-Click - Quit")
                            WindowManager.shared.showGoodbyeAndQuit()
                        }
                    )
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
                .frame(width: 28, height: 28)
            }
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 8)
        .opacity(widgetOpacity)
        .background(theme.backgroundSurface)
        .clipShape(Capsule())
        .overlay(
            Capsule()
                .stroke(isAccessibilityTrusted ? theme.borderColor : theme.error, lineWidth: 1)
        )
        .shadow(color: Color.black.opacity(0.4), radius: 8, x: 0, y: 4)
        .onHover { hover in
            withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                isHovering = hover
            }
        }
        .overlay(alignment: .bottom) {
            if isHovering || audioRecorder.isRecording || audioRecorder.isProcessing || audioRecorder.hasError {
                Text(statusText)
                    .font(.system(size: 9, weight: .bold, design: .rounded))
                    .foregroundColor(theme.textSecondary)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 2)
                    .background(Color.black.opacity(0.4))
                    .cornerRadius(4)
                    .offset(y: 20)
                    .transition(.opacity)
            }
        }
        .frame(width: 200, height: 60)
        .onReceive(NotificationCenter.default.publisher(for: NSNotification.Name("OpenDashboard"))) { _ in
            WindowManager.shared.showDashboard()
        }
        .onReceive(Timer.publish(every: 3.0, on: .main, in: .common).autoconnect()) { _ in
            let axTrusted = AXIsProcessTrusted()
            let autoTrusted = InputController.shared.isAutomationTrusted
            let combined = axTrusted && autoTrusted
            if combined != isAccessibilityTrusted {
                isAccessibilityTrusted = combined
            }
        }
    }

    var orbGradient: LinearGradient {
        if !isAccessibilityTrusted && !audioRecorder.isRecording { return errorGradient }
        if audioRecorder.hasError { return errorGradient }
        if audioRecorder.isProcessing { return theme.accentGradient }
        if audioRecorder.isRecording { return recordingGradient }
        return theme.accentGradient
    }

    var orbColor: Color {
        if !isAccessibilityTrusted && !audioRecorder.isRecording { return theme.error }
        if audioRecorder.hasError { return theme.error }
        if audioRecorder.isProcessing { return theme.accent }
        if audioRecorder.isRecording { return theme.error }
        return theme.accent
    }

    var recordingGradient: LinearGradient {
        LinearGradient(colors: [theme.error, theme.warning], startPoint: .topLeading, endPoint: .bottomTrailing)
    }

    var errorGradient: LinearGradient {
        LinearGradient(colors: [theme.error, Color(red: 0.5, green: 0, blue: 0)], startPoint: .topLeading, endPoint: .bottomTrailing)
    }

    var statusText: String {
        if !isAccessibilityTrusted && !audioRecorder.isRecording { return "Permissions" }
        if let error = audioRecorder.errorMessage, audioRecorder.hasError {
            if error.contains("timed out") { return "Timeout" }
            if error.contains("401") { return "Bad API Key" }
            return "Error: \(error.prefix(10))..."
        }
        if audioRecorder.hasError { return "Error" }
        if audioRecorder.isProcessing { return "Thinking..." }
        if audioRecorder.isRecording { return "Listening" }
        return "Idle"
    }
}

struct UnifiedClickDetector: NSViewRepresentable {
    var onLeftClick: () -> Void
    var onRightClick: () -> Void

    func makeNSView(context: Context) -> NSView {
        let view = TransparentClickView()
        view.onLeftClick = onLeftClick
        view.onRightClick = onRightClick
        return view
    }

    func updateNSView(_ nsView: NSView, context: Context) {}
}

class TransparentClickView: NSView {
    var onLeftClick: (() -> Void)?
    var onRightClick: (() -> Void)?

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        self.wantsLayer = true
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        self.wantsLayer = true
    }

    override func mouseDown(with event: NSEvent) {
        onLeftClick?()
    }
    override func rightMouseDown(with event: NSEvent) {
        onRightClick?()
    }

    override func hitTest(_ point: NSPoint) -> NSView? {
        return self.bounds.contains(point) ? self : nil
    }
}

struct VoiceVisualizerView: View {
    let audioLevel: Float
    let isRecording: Bool

    var body: some View {
        HStack(spacing: 3) {
            ForEach(0..<3) { i in
                RoundedRectangle(cornerRadius: 2)
                    .fill(isRecording ? recordingActiveGradient : idleGradient)
                    .frame(width: 3, height: barHeight(for: i))
                    .animation(.spring(response: 0.2, dampingFraction: 0.6), value: audioLevel)
            }
        }
    }

    private var recordingActiveGradient: LinearGradient {
        LinearGradient(colors: [Color.blue, Color.purple], startPoint: .top, endPoint: .bottom)
    }

    private var idleGradient: LinearGradient {
        LinearGradient(colors: [Color.white.opacity(0.1)], startPoint: .top, endPoint: .bottom)
    }

    private func barHeight(for index: Int) -> CGFloat {
        if !isRecording { return 4 }
        let normalizedLevel = CGFloat(max(0, audioLevel + 50) / 40)
        let baseHeight: CGFloat = 4
        let variation = CGFloat.random(in: 0.5...1.5)
        return min(25, baseHeight + (normalizedLevel * 20 * variation))
    }
}
