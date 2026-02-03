import SwiftUI
import AppKit

@main
struct CosmoWhisperApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    
    var body: some Scene {
        Settings {
            EmptyView()
        }
        .onOpenURL { url in
            handleURL(url)
        }
    }
    
    func handleURL(_ url: URL) {
        guard url.scheme == "cosmowhisper" else { return }
        
        let components = URLComponents(url: url, resolvingAgainstBaseURL: true)
        let action = url.host // e.g., cosmowhisper://unlock -> host is "unlock"
        
        if action == "unlock" {
            let key = components?.queryItems?.first(where: { $0.name == "key" })?.value
            if key == "COSMO_PREMIUM_2024" {
                UserDefaults.standard.set(true, forKey: "isAIUnlocked")
                LogManager.shared.log("AUTH: AI Unlocked via Protocol!")
                // Show a native alert
                let alert = NSAlert()
                alert.messageText = "Cosmo Unlocked!"
                alert.informativeText = "The website has verified your access. Galactic features are now online."
                alert.addButton(withTitle: "Blast Off!")
                alert.runModal()
            }
        } else if action == "configure" {
            if let groqKey = components?.queryItems?.first(where: { $0.name == "groq" })?.value {
                UserDefaults.standard.set(groqKey, forKey: "groqApiKey")
                LogManager.shared.log("AUTH: Groq API Key synced via Protocol!")
            }
        } else if action == "license" {
            if let token = components?.queryItems?.first(where: { $0.name == "token" })?.value {
                UserDefaults.standard.set(token, forKey: "licenseToken")
                LogManager.shared.log("AUTH: License Token synced via Protocol!")
                
                Task {
                    let success = await LicenseManager.shared.syncStatus()
                    if success {
                        DispatchQueue.main.async {
                            let alert = NSAlert()
                            alert.messageText = "License Activated!"
                            alert.informativeText = "Your web account has been successfully linked to CosmoWhisper."
                            alert.addButton(withTitle: "Great")
                            alert.runModal()
                        }
                    }
                }
            }
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
            
            await MainActor.run {
                self.triggerAutomationPrompt()
            }
        }
    }
    
    func triggerAutomationPrompt() {
        LogManager.shared.log("PERMISSIONS: Triggering Automation (System Events) prompt via NSAppleScript (Strict)...")
        let scriptSource = "tell application \"System Events\" to get POSIX path of (path to frontmost application)"
        
        if let script = NSAppleScript(source: scriptSource) {
            var error: NSDictionary?
            script.executeAndReturnError(&error)
            if let err = error {
                let errCode = err[NSAppleScript.errorNumber] as? Int ?? 0
                LogManager.shared.log("PERMISSIONS: Trigger failed or denied: \(errCode)")
            } else {
                LogManager.shared.log("PERMISSIONS: Trigger successful.")
            }
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
