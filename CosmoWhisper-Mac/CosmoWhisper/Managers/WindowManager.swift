import SwiftUI
import AppKit

class DashboardWindow: NSWindow {
    override var canBecomeKey: Bool { return true }
    override var canBecomeMain: Bool { return true }
}

class WidgetPanel: NSPanel {
    override var canBecomeKey: Bool { return false }
    override var canBecomeMain: Bool { return false }
}

@MainActor
class WindowManager {
    static let shared = WindowManager()
    
    private var widgetWindow: WidgetPanel?
    private var dashboardWindow: DashboardWindow?
    private var goodbyeWindow: NSWindow?
    
    // Dependencies (passed from App)
    private var audioRecorder: AudioRecorder?
    
    func setup(recorder: AudioRecorder) {
        self.audioRecorder = recorder
    }
    
    func showWidget() {
        LogManager.shared.log("WindowManager: showWidget() requested.")
        if let existing = widgetWindow {
            LogManager.shared.log("WindowManager: Widget already exists, ordering to front.")
            existing.orderFront(nil)
            return
        }
        
        guard let recorder = audioRecorder else { return }
        let contentView = ContentView().environmentObject(recorder)
        let hostingController = NSHostingController(rootView: contentView)
        
        // Widget stays as a borderless floating panel that doesn't take focus
        let panel = WidgetPanel(
            contentRect: NSRect(x: 0, y: 0, width: 220, height: 85),
            styleMask: [.borderless, .nonactivatingPanel],
            backing: .buffered,
            defer: false
        )
        
        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.hasShadow = true
        panel.hasShadow = true
        panel.level = .statusBar // Higher than floating
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .ignoresCycle]
        panel.isMovableByWindowBackground = true
        panel.ignoresMouseEvents = false
        
        panel.contentViewController = hostingController
        
        // Restore Position or Default to top-right
        if let savedFrame = UserDefaults.standard.string(forKey: "WidgetFrame"), !savedFrame.isEmpty {
            LogManager.shared.log("WindowManager: Restoring widget frame from saved state.")
            panel.setFrame(from: savedFrame)
        } else if let screen = NSScreen.main {
            LogManager.shared.log("WindowManager: Using default widget position (Safe Top-Center).")
            // Use logical center top, slightly down
            let x = (screen.visibleFrame.width / 2) - 110 // Center
            let y = screen.visibleFrame.height - 250 // Top (Moved down to avoid Notch/Menu)
            panel.setFrameOrigin(NSPoint(x: x, y: y))
        } else {
            LogManager.shared.log("WindowManager: Positioning widget center (no screen found).")
            panel.center()
        }
        
        // Final sanity check: ensure it's visible on some screen
        let isVisible = NSScreen.screens.contains { screen in
            screen.frame.intersects(panel.frame)
        }
        
        if !isVisible || panel.frame.width < 50 || panel.frame.height < 50 {
             LogManager.shared.log("WindowManager WARNING: Widget was off-screen or too small. Centering on main screen.")
             if let screen = NSScreen.main {
                 let x = (screen.visibleFrame.width - 220) / 2
                 let y = (screen.visibleFrame.height - 85) / 2
                 panel.setFrame(NSRect(x: x, y: y, width: 220, height: 85), display: true)
             } else {
                 panel.center()
             }
        }
        
        // Listen for moves to save position
        NotificationCenter.default.addObserver(
            forName: NSWindow.didMoveNotification,
            object: panel,
            queue: .main
        ) { [weak panel] _ in
            guard let panel = panel else { return }
            Task { @MainActor in
                 // Re-access panel on main actor to read frameDescriptor safely
                 let frameDesc = panel.frameDescriptor
                 UserDefaults.standard.set(frameDesc, forKey: "WidgetFrame")
            }
        }
        
        self.widgetWindow = panel
        panel.orderFront(nil)
        panel.makeKeyAndOrderFront(nil) // Try forcing it even if non-activating
        LogManager.shared.log("WindowManager: Widget ACTIVE at \(panel.frame)")
    }
    
    func showDashboard() {
        LogManager.shared.log("WindowManager: showDashboard() called")
        if let existing = dashboardWindow {
            existing.makeKeyAndOrderFront(nil)
            NSApp.activate(ignoringOtherApps: true)
            return
        }
        
        let contentView = DashboardView()
        let hostingController = NSHostingController(rootView: contentView)
        
        let window = DashboardWindow(
            contentRect: NSRect(x: 0, y: 0, width: 850, height: 650),
            styleMask: [.titled, .closable, .miniaturizable, .resizable, .fullSizeContentView],
            backing: .buffered,
            defer: false
        )
        
        window.center()
        window.title = "CosmoWhisper Dashboard"
        window.titlebarAppearsTransparent = true
        window.titleVisibility = .visible
        window.contentViewController = hostingController
        window.isReleasedWhenClosed = false
        window.level = .normal
        
        // Remember position
        window.setFrameAutosaveName("CosmoDashboard")
        
        self.dashboardWindow = window
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
        
        NotificationCenter.default.addObserver(
            forName: NSWindow.willCloseNotification,
            object: window,
            queue: .main
        ) { [weak self] _ in
            guard let self = self else { return }
            Task { @MainActor in 
                self.dashboardWindow = nil
            }
        }
    }
    
    func toggleDashboard() {
        if let existing = dashboardWindow, existing.isVisible {
            existing.close()
            dashboardWindow = nil
        } else {
            showDashboard()
        }
    }
    
    func showGoodbyeAndQuit() {
        if goodbyeWindow != nil { return }
        
        let count = UserDefaults.standard.integer(forKey: "transcriptionCount")
        let goodbyeView = GoodbyeView(count: count)
        let hostingController = NSHostingController(rootView: goodbyeView)
        
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 450, height: 500),
            styleMask: [.borderless, .fullSizeContentView],
            backing: .buffered,
            defer: false
        )
        
        window.center()
        window.backgroundColor = .clear
        window.isOpaque = false
        window.hasShadow = true
        window.level = .floating
        window.contentViewController = hostingController
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        
        self.goodbyeWindow = window
        window.makeKeyAndOrderFront(nil)
        
        LogManager.shared.log("UI: Goodbye window shown. Quitting in 3s...")
        
        DispatchQueue.main.asyncAfter(deadline: .now() + 3.0) {
            NSApp.terminate(nil)
        }
    }
    
}


struct GoodbyeView: View {
    let count: Int
    @State private var opac = 0.0
    @State private var scale = 0.8
    
    var body: some View {
        VStack(spacing: 30) {
            // Animated Icon
            ZStack {
                Circle()
                    .fill(Color.blue.opacity(0.1))
                    .frame(width: 100, height: 100)
                
                Image(systemName: "sparkles")
                    .font(.system(size: 50))
                    .foregroundColor(.blue)
                    .rotationEffect(.degrees(opac * 360))
            }
            .scaleEffect(scale)
            
            VStack(spacing: 12) {
                Text("See you later, Cosmo!")
                    .font(.system(size: 28, weight: .bold, design: .rounded))
                    .foregroundColor(.white)
                
                Text("Today was productive.")
                    .font(.system(size: 16))
                    .foregroundColor(.white.opacity(0.6))
            }
            
            // Fun stats
            VStack(spacing: 15) {
                HStack(spacing: 20) {
                    StatInfo(icon: "waveform", text: "\(count) Transcriptions", color: .blue)
                    StatInfo(icon: "bolt.fill", text: "\(count * 5)s Saved", color: .orange)
                }
                
                HStack(spacing: 20) {
                    StatInfo(icon: "brain.head.profile", text: "AI Perfected", color: .purple)
                    StatInfo(icon: "face.smiling", text: "Zero Typo Day", color: .green)
                }
            }
            .padding()
            .background(Color.white.opacity(0.05))
            .cornerRadius(20)
            .overlay(RoundedRectangle(cornerRadius: 20).stroke(Color.white.opacity(0.1), lineWidth: 1))
            
            Text("Shutting down the engines...")
                .font(.system(size: 12, weight: .medium, design: .monospaced))
                .foregroundColor(.blue.opacity(0.7))
                .padding(.top, 10)
        }
        .padding(40)
        .frame(width: 450, height: 500)
        .background {
            ZStack {
                Color(red: 10/255, green: 15/255, blue: 30/255)
                VisualEffectView(material: .hudWindow, blendingMode: .withinWindow)
                
                // Animated background glows
                Circle()
                    .fill(Color.blue.opacity(0.1))
                    .frame(width: 300, height: 300)
                    .offset(x: -150, y: -150)
                    .blur(radius: 50)
                
                Circle()
                    .fill(Color.purple.opacity(0.1))
                    .frame(width: 300, height: 300)
                    .offset(x: 150, y: 150)
                    .blur(radius: 50)
            }
        }
        .clipShape(RoundedRectangle(cornerRadius: 32))
        .overlay(RoundedRectangle(cornerRadius: 32).stroke(Color.white.opacity(0.1), lineWidth: 1))
        .opacity(opac)
        .onAppear {
            withAnimation(.spring(response: 0.6, dampingFraction: 0.7)) {
                opac = 1.0
                scale = 1.0
            }
        }
    }
}

struct StatInfo: View {
    let icon: String
    let text: String
    let color: Color
    
    var body: some View {
        HStack(spacing: 8) {
            Image(systemName: icon)
                .foregroundColor(color)
            Text(text)
                .font(.system(size: 12, weight: .medium))
                .foregroundColor(.white.opacity(0.9))
        }
    }
}
