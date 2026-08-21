import SwiftUI
import AppKit

@main
struct CosmoWhisperApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    
    var body: some Scene {
        Settings {
            EmptyView()
        }
    }
}

@MainActor
class AppDelegate: NSObject, NSApplicationDelegate {
    var statusItem: NSStatusItem?
    
    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)
        setupMenuBar()
        WindowManager.shared.setup(recorder: AudioRecorder.shared)
        checkAccessibilityPermissions()
        // --- 5. GOSSIP & ONBOARDING RESET MIGRATION (Final v1.0 Release) ---
        // User request: Gossip off, log cleared, and onboarding+hints enabled for the final build.
        if !UserDefaults.standard.bool(forKey: "final_release_v1") {
            LogManager.shared.log("🚀 FINAL RELEASE MIGRATION: Resetting settings for clean first run.")
            UserDefaults.standard.set(false, forKey: "enableGossip")
            UserDefaults.standard.removeObject(forKey: "recentTranscriptions")
            UserDefaults.standard.set(false, forKey: "hasCompletedOnboarding")
            UserDefaults.standard.set(true, forKey: "showHints")
            UserDefaults.standard.set(true, forKey: "final_release_v1")
        }
        warmupSystem()
        
        NotificationCenter.default.addObserver(self, selector: #selector(openSettings), name: NSNotification.Name("OpenDashboard"), object: nil)
        
        NSAppleEventManager.shared().setEventHandler(
            self,
            andSelector: #selector(handleGetURLEvent(_:withReplyEvent:)),
            forEventClass: AEEventClass(kInternetEventClass),
            andEventID: AEEventID(kAEGetURL)
        )
    }
    
    @objc func handleGetURLEvent(_ event: NSAppleEventDescriptor, withReplyEvent replyEvent: NSAppleEventDescriptor) {
        guard let urlString = event.paramDescriptor(forKeyword: AEKeyword(keyDirectObject))?.stringValue,
              let url = URL(string: urlString) else { return }
        LicenseManager.shared.handleAuthDeepLink(url: url)
    }
    
    func application(_ application: NSApplication, open urls: [URL]) {
        for url in urls {
            LicenseManager.shared.handleAuthDeepLink(url: url)
        }
    }
    
    func checkAccessibilityPermissions() {
        let options: [String: Any] = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true]
        let accessibilityEnabled = AXIsProcessTrustedWithOptions(options as CFDictionary)
        
        LogManager.shared.log("PERMISSIONS: Accessibility Check Result: \(accessibilityEnabled)")
        
        if !accessibilityEnabled {
            LogManager.shared.log("PERMISSIONS: Requesting Accessibility permissions with Prompt Option...")
            // Redundant call to ensure prompt is triggered if system allows
            let _ = AXIsProcessTrustedWithOptions(options as CFDictionary)
        }
    }
    
    func warmupSystem() {
        LogManager.shared.log("CosmoWhisper Warming Up (Backgrounded)...")
        
        // Instant UI Appearance
        DispatchQueue.main.async {
            LogManager.shared.log("AppDelegate: Triggering initial windows show...")
            WindowManager.shared.showWidget()
            WindowManager.shared.showDashboard()
        }
        
        // Heavy lifting in background
        Task.detached(priority: .background) {
            let _ = await AIService.shared
            let _ = await AudioRecorder.shared
            let _ = await InputController.shared
            let _ = await CommandController.shared
            
            // Pre-initialize audio hardware
            await AIService.shared.warmUp()
        }
    }
    
    func setupMenuBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let button = statusItem?.button {
            button.image = NSImage(systemSymbolName: "microphone.fill", accessibilityDescription: "CosmoWhisper")
            button.action = #selector(menuBarAction)
            button.target = self
        }
        
        let menu = NSMenu()
        let count = UserDefaults.standard.integer(forKey: "transcriptionCount")
        let infoItem = NSMenuItem(title: "Transcriptions: \(count)", action: nil, keyEquivalent: "")
        infoItem.isEnabled = false
        menu.addItem(infoItem)
        menu.addItem(NSMenuItem.separator())
        
        let modelMenu = NSMenu()
        let currentModel = UserDefaults.standard.string(forKey: "aiModel") ?? "llama-3.3-70b-versatile"
        let models = [
            ("Llama 3.3 70B", "llama-3.3-70b-versatile"),
            ("Llama 3.1 8B", "llama-3.1-8b-instant"),
            ("Mixtral 8x7B", "mixtral-8x7b-32768")
        ]
        
        for (name, tag) in models {
            let item = NSMenuItem(title: name, action: #selector(changeModel(_:)), keyEquivalent: "")
            item.representedObject = tag
            item.state = (currentModel == tag) ? .on : .off
            item.target = self
            modelMenu.addItem(item)
        }
        
        let modelSelectionItem = NSMenuItem(title: "AI Model", action: nil, keyEquivalent: "")
        modelSelectionItem.submenu = modelMenu
        menu.addItem(modelSelectionItem)
        menu.addItem(NSMenuItem.separator())
        
        menu.addItem(NSMenuItem(title: "Open Dashboard", action: #selector(openSettings), keyEquivalent: ","))
        menu.addItem(NSMenuItem(title: "Show Widget", action: #selector(showWidget), keyEquivalent: "w"))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Quit CosmoWhisper", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q"))
        
        statusItem?.menu = menu
    }
    
    @objc func changeModel(_ sender: NSMenuItem) {
        if let modelTag = sender.representedObject as? String {
            UserDefaults.standard.set(modelTag, forKey: "aiModel")
            setupMenuBar()
        }
    }
    
    @objc func showWidget() {
        WindowManager.shared.showWidget()
    }
    
    @objc func menuBarAction() {
        // Removed NSApp.activate to prevent focus stealing.
    }
    
    @objc func openSettings() {
        WindowManager.shared.showDashboard()
    }
}

struct VisualEffectView: NSViewRepresentable {
    var material: NSVisualEffectView.Material
    var blendingMode: NSVisualEffectView.BlendingMode
    
    func makeNSView(context: Context) -> NSVisualEffectView {
        let view = NSVisualEffectView()
        view.material = material
        view.blendingMode = blendingMode
        view.state = .active
        return view
    }
    
    func updateNSView(_ nsView: NSVisualEffectView, context: Context) {
        nsView.material = material
        nsView.blendingMode = blendingMode
    }
}
