import SwiftUI
import AppKit

struct ContentView: View {
    @EnvironmentObject var audioRecorder: AudioRecorder
    @State private var isHovering = false
    
    // Premium Gradients
    let darkGradient = LinearGradient(
        gradient: Gradient(colors: [
            Color(red: 30/255, green: 41/255, blue: 59/255, opacity: 0.98), // Slate 800
            Color(red: 15/255, green: 23/255, blue: 42/255, opacity: 0.98)  // Slate 900
        ]),
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
    
    let processingGradient = LinearGradient(
        gradient: Gradient(colors: [Color.blue, Color.purple]),
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
    
    let errorGradient = LinearGradient(
        gradient: Gradient(colors: [Color.red, Color(red: 0.5, green: 0, blue: 0)]),
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
    
    let recordingGradient = LinearGradient(
        gradient: Gradient(colors: [Color.red, Color.orange]),
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
    
    @AppStorage("widgetOpacity") private var widgetOpacity = 0.8
    @State private var isAccessibilityTrusted = AXIsProcessTrusted()
    @State private var isPulsing = false
    
    var body: some View {
        ZStack {
            // 1. Shooting Stars Background
            StarFieldView()
                .opacity(audioRecorder.isRecording ? 0.8 : 0.2)
                .allowsHitTesting(false)
                .animation(.easeInOut(duration: 1.0), value: audioRecorder.isRecording)
            
            HStack(spacing: 12) {
                // 1. Mic / Recording Orb
                ZStack {
                    // Background Glow Pulse
                    if audioRecorder.isRecording {
                        Circle()
                            .fill(orbColor.opacity(0.4))
                            .frame(width: 40, height: 40)
                            .blur(radius: 12)
                            .scaleEffect(isPulsing ? 1.2 : 0.8)
                            .opacity(isPulsing ? 0.6 : 0.3)
                            .onAppear {
                                withAnimation(.easeInOut(duration: 1.2).repeatForever(autoreverses: true)) {
                                    isPulsing = true
                                }
                            }
                    }

                    // Actual Orb
                    Circle()
                        .fill(orbGradient)
                        .frame(width: 18, height: 18)
                        .scaleEffect(audioRecorder.isRecording ? (1.0 + CGFloat(max(0, audioRecorder.audioLevel + 50) / 100)) : 1.0)
                        .shadow(color: orbColor.opacity(0.8), radius: audioRecorder.isRecording ? 12 : 4, x: 0, y: 0)
                    
                    if audioRecorder.isProcessing {
                        Circle()
                            .stroke(processingGradient, lineWidth: 2)
                            .frame(width: 24, height: 24)
                            .rotationEffect(.degrees(isHovering ? 360 : 0))
                            .animation(.linear(duration: 2).repeatForever(autoreverses: false), value: audioRecorder.isProcessing)
                    }
                    
                    // The Click Detector
                    UnifiedClickDetector(
                        onLeftClick: {
                            LogManager.shared.log("UI: Mic Orb Left-Clicked")
                            audioRecorder.toggleRecording()
                        },
                        onRightClick: {
                            LogManager.shared.log("UI: Mic Orb Right-Clicked (Toggle Dashboard)")
                            WindowManager.shared.toggleDashboard()
                        }
                    )
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
                .frame(width: 40, height: 40)
                
                // 2. Voice Visualization (Middle)
                VoiceVisualizerView(audioLevel: audioRecorder.audioLevel, isRecording: audioRecorder.isRecording)
                    .frame(width: 80, height: 20)
                
                // 3. Status or Settings
                HStack(spacing: 8) {
                    if !isAccessibilityTrusted && !audioRecorder.isRecording {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .foregroundColor(.red)
                            .font(.system(size: 10))
                    }
                    
                    // Gear Icon
                    ZStack {
                        Image(systemName: "gear")
                            .font(.system(size: 14))
                            .foregroundColor(Color.white.opacity(0.8))
                            .padding(6)
                            .background(Color.white.opacity(0.1))
                            .clipShape(Circle())
                        
                        UnifiedClickDetector(
                            onLeftClick: {
                                LogManager.shared.log("UI: Gear icon Left-Click - Toggling Dashboard")
                                WindowManager.shared.toggleDashboard()
                            },
                            onRightClick: {
                                LogManager.shared.log("UI: Gear icon Right-Click - Quitting")
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
        }
        .opacity(widgetOpacity)
        .background {
            ZStack {
                VisualEffectView(material: .hudWindow, blendingMode: .behindWindow)
                Color.black.opacity(0.4)
            }
            .allowsHitTesting(false)
        }
        .clipShape(Capsule())
        .overlay(
            Capsule()
                .stroke(isAccessibilityTrusted ? Color.white.opacity(0.15) : Color.red.opacity(0.6), lineWidth: 1.5)
        )
        .shadow(color: Color.black.opacity(0.5), radius: 15, x: 0, y: 8)
        .onHover { hover in 
            withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                isHovering = hover 
            }
        }
        .overlay(alignment: .bottom) {
            if isHovering || audioRecorder.isRecording || audioRecorder.isProcessing || audioRecorder.hasError {
                Text(statusText)
                    .font(.system(size: 9, weight: .bold, design: .rounded))
                    .foregroundColor(.white.opacity(0.8))
                    .padding(.horizontal, 8)
                    .padding(.vertical, 2)
                    .background(Color.black.opacity(0.4))
                    .cornerRadius(4)
                    .offset(y: 20)
                    .transition(.opacity)
            }
        }
        .frame(width: 220, height: 70) // Increased height for label
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
        if audioRecorder.isProcessing { return processingGradient }
        if audioRecorder.isRecording { return recordingGradient }
        return processingGradient // Idle same as processing for now or a default
    }
    
    var orbColor: Color {
        if !isAccessibilityTrusted && !audioRecorder.isRecording { return .red }
        if audioRecorder.hasError { return .red }
        if audioRecorder.isProcessing { return .blue }
        if audioRecorder.isRecording { return .red }
        return .blue
    }
    
    var statusText: String {
        if !isAccessibilityTrusted && !audioRecorder.isRecording { return "Permissions" }
        if let error = audioRecorder.errorMessage, audioRecorder.hasError {
            // If it's a timeout or specific error, show it
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
        print("DEBUG: TransparentClickView MouseDown") // Direct print for console
        onLeftClick?() 
    }
    override func rightMouseDown(with event: NSEvent) { 
        print("DEBUG: TransparentClickView RightMouseDown")
        onRightClick?() 
    }
    
    override func hitTest(_ point: NSPoint) -> NSView? {
        return self.bounds.contains(point) ? self : nil
    }
}

// MARK: - Shooting Stars Component
struct StarFieldView: View {
    @State private var stars: [Star] = (0..<15).map { _ in Star() }
    let timer = Timer.publish(every: 0.05, on: .main, in: .common).autoconnect()
    
    var body: some View {
        GeometryReader { geo in
            ZStack {
                ForEach(stars) { star in
                    Circle()
                        .fill(Color.white.opacity(star.opacity))
                        .frame(width: star.size, height: star.size)
                        .position(x: star.x * geo.size.width, y: star.y * geo.size.height)
                        .blur(radius: 1)
                }
            }
            .onReceive(timer) { _ in
                for i in 0..<stars.count {
                    stars[i].x -= stars[i].speed
                    if stars[i].x < -0.1 {
                        stars[i].x = 1.1
                        stars[i].y = CGFloat.random(in: 0...1)
                    }
                }
            }
        }
    }
    
    struct Star: Identifiable {
        let id = UUID()
        var x = CGFloat.random(in: 0...1)
        var y = CGFloat.random(in: 0...1)
        let size = CGFloat.random(in: 1...3)
        let opacity = Double.random(in: 0.3...0.8)
        let speed = CGFloat.random(in: 0.002...0.01)
    }
}

// MARK: - Voice Visualizer Component
struct VoiceVisualizerView: View {
    let audioLevel: Float
    let isRecording: Bool
    
    var body: some View {
        HStack(spacing: 3) {
            ForEach(0..<12) { i in
                RoundedRectangle(cornerRadius: 2)
                    .fill(isRecording ? recordingActiveGradient : idleGradient)
                    .frame(width: 3, height: barHeight(for: i))
                    .animation(.spring(response: 0.2, dampingFraction: 0.6), value: audioLevel)
            }
        }
    }
    
    private var recordingActiveGradient: LinearGradient {
        LinearGradient(colors: [.blue, .purple], startPoint: .top, endPoint: .bottom)
    }
    
    private var idleGradient: LinearGradient {
        LinearGradient(colors: [Color.white.opacity(0.1)], startPoint: .top, endPoint: .bottom)
    }
    
    private func barHeight(for index: Int) -> CGFloat {
        if !isRecording { return 4 }
        let normalizedLevel = CGFloat(max(0, audioLevel + 50) / 40) // 0 to 1ish
        let baseHeight: CGFloat = 4
        let variation = CGFloat.random(in: 0.5...1.5)
        return min(25, baseHeight + (normalizedLevel * 20 * variation))
    }
}
