import Foundation
import Carbon
import AppKit
import CoreGraphics

@MainActor
class InputController: ObservableObject {
    static let shared = InputController()
    
    // Thread-safe state for the EventTap
    private class InputState {
        var hotkeyCode: Int = 62
        var isRecordingHotkey: Bool = false
        var isHotkeyDown: Bool = false
        var lastCaptureTime: Date = Date().addingTimeInterval(-10.0)
        private let lock = NSLock()
        
        func update(hotkey: Int? = nil, recording: Bool? = nil, down: Bool? = nil, captured: Bool = false) {
            lock.lock(); defer { lock.unlock() }
            if let h = hotkey { hotkeyCode = h }
            if let r = recording { isRecordingHotkey = r }
            if let d = down { isHotkeyDown = d }
            if captured { lastCaptureTime = Date() }
        }
        
        var currentHotkey: Int { lock.lock(); defer { lock.unlock() }; return hotkeyCode }
        var recording: Bool { lock.lock(); defer { lock.unlock() }; return isRecordingHotkey }
        var isDown: Bool { lock.lock(); defer { lock.unlock() }; return isHotkeyDown }
        var timeSinceCapture: TimeInterval { lock.lock(); defer { lock.unlock() }; return abs(Date().timeIntervalSince(lastCaptureTime)) }
    }
    
    private let state = InputState()
    
    private var globalKeyDownMonitor: Any?
    private var globalKeyUpMonitor: Any?
    private var localKeyDownMonitor: Any?
    private var localKeyUpMonitor: Any?
    private var mouseMonitor: Any?
    private var flagsMonitor: Any?
    private var localFlagsMonitor: Any?
    private var localMouseMonitor: Any?
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    private var lastFlags: CGEventFlags = []
    private var permissionWatchdog: Timer?
    @Published var isHotkeyDown = false {
        didSet { state.update(down: isHotkeyDown) }
    }
    @Published var isRecordingHotkey = false {
        didSet { state.update(recording: isRecordingHotkey) }
    }
    @Published var isRecordingMouse = false
    @Published var isAutomationTrusted = false
    @Published var lastDetectedKeyCode: Int?
    
    private var settingsDebounceTimer: Timer?
    
    @Published var hotkeyCode: Int = 62
    @Published var mouseButton: Int = 0
    @Published var useMouseButton: Bool = true
    
    private func updateInternalSettings() {
        if let stored = UserDefaults.standard.object(forKey: "hotkeyCode") as? Int {
            hotkeyCode = stored
        } else {
            hotkeyCode = 62
        }
        state.update(hotkey: hotkeyCode)
        
        mouseButton = UserDefaults.standard.integer(forKey: "mouseButton")
        useMouseButton = UserDefaults.standard.bool(forKey: "useMouseButton")
        if UserDefaults.standard.object(forKey: "useMouseButton") == nil {
            useMouseButton = true // Default to true if not set
        }
        LogManager.shared.log("InputController: Settings Cached (Hotkey: \(hotkeyCode), Mouse: \(mouseButton), UseMouse: \(useMouseButton))")
    }
    
    init() {
        updateInternalSettings()
        
        if hotkeyCode == 80 || hotkeyCode == 0 {
            LogManager.shared.log("InputController: Defaulting/Migrating to Right Control (62)")
            hotkeyCode = 62
            UserDefaults.standard.set(62, forKey: "hotkeyCode")
        }
        
        setupGlobalHotkey()
        setupLocalMonitors()
        setupMouseMonitoring()
        setupLocalMouseRecording()
        startPermissionWatchdog()
        
        NotificationCenter.default.addObserver(forName: UserDefaults.didChangeNotification, object: nil, queue: .main) { [weak self] _ in
            guard let self = self else { return }
            self.settingsDebounceTimer?.invalidate()
            self.settingsDebounceTimer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: false) { [weak self] _ in
                guard let self = self else { return }
                Task { @MainActor in
                    self.processSettingsChange()
                }
            }
        }
        
        NotificationCenter.default.addObserver(forName: NSNotification.Name("InsertText"), object: nil, queue: .main) { [weak self] notification in
            guard let self = self, let text = notification.object as? String else { return }
            Task {
                await self.pasteText(text)
            }
        }
        
        NotificationCenter.default.addObserver(forName: NSNotification.Name("FormatText"), object: nil, queue: .main) { [weak self] notification in
            guard let self = self, let format = notification.object as? String else { return }
            Task {
                await self.handleFormatting(format)
            }
        }
    }
    
    private var lastHotkeyCode: Int?
    private var lastMouseButton: Int?
    
    @objc private func processSettingsChange() {
        Task { await checkForSettingChanges() }
    }
    
    private func checkForSettingChanges() async {
        let oldHotkey = hotkeyCode
        let oldMouse = mouseButton
        
        if let stored = UserDefaults.standard.object(forKey: "hotkeyCode") as? Int {
            hotkeyCode = stored
        }
        mouseButton = UserDefaults.standard.integer(forKey: "mouseButton")
        
        let oldUseMouse = useMouseButton
        useMouseButton = UserDefaults.standard.bool(forKey: "useMouseButton")
        if UserDefaults.standard.object(forKey: "useMouseButton") == nil {
            useMouseButton = true
        }
        
        if hotkeyCode != oldHotkey || mouseButton != oldMouse || useMouseButton != oldUseMouse {
            LogManager.shared.log("InputController: Settings Changed -> Hotkey: \(hotkeyCode), Mouse: \(mouseButton), UseMouse: \(useMouseButton)")
            refreshMonitors()
        }
    }
    
    func refreshMonitors() {
        LogManager.shared.log("InputController: Refreshing monitors...")
        
        // 1. Full cleanup of all monitors
        stopAllMonitors()
        
        // 2. Setup based on current trust level
        setupGlobalHotkey()     // This handles EventTap OR Global NSEvent Fallback
        setupLocalMonitors()    // ALWAYS enabled for focus-responsiveness
        setupMouseMonitoring() // Handles Mouse Button logic
    }
    
    /// Call this from the UI to change the hotkey and immediately activate it.
    func applyHotkey(_ code: Int) {
        hotkeyCode = code
        UserDefaults.standard.set(code, forKey: "hotkeyCode")
        state.update(hotkey: code, captured: true)
        lastDetectedKeyCode = code
        LogManager.shared.log("InputController: Hotkey applied -> \(code)")
        refreshMonitors()
    }
    
    private func stopAllMonitors() {
        if let monitor = globalKeyDownMonitor { NSEvent.removeMonitor(monitor); globalKeyDownMonitor = nil }
        if let monitor = globalKeyUpMonitor { NSEvent.removeMonitor(monitor); globalKeyUpMonitor = nil }
        if let monitor = localKeyDownMonitor { NSEvent.removeMonitor(monitor); localKeyDownMonitor = nil }
        if let monitor = localKeyUpMonitor { NSEvent.removeMonitor(monitor); localKeyUpMonitor = nil }
        if let monitor = flagsMonitor { NSEvent.removeMonitor(monitor); flagsMonitor = nil }
        if let monitor = localFlagsMonitor { NSEvent.removeMonitor(monitor); localFlagsMonitor = nil }
        if let monitor = localMouseMonitor { NSEvent.removeMonitor(monitor); localMouseMonitor = nil }
        if let monitor = mouseMonitor { NSEvent.removeMonitor(monitor); mouseMonitor = nil }
        
        if let source = runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetCurrent(), source, .commonModes)
            runLoopSource = nil
        }
        eventTap = nil
    }
    
    func setupMouseMonitoring() {
        if let monitor = mouseMonitor { NSEvent.removeMonitor(monitor); mouseMonitor = nil }
        
        let mask: NSEvent.EventTypeMask = [.otherMouseDown, .otherMouseUp, .rightMouseDown, .rightMouseUp]
        mouseMonitor = NSEvent.addGlobalMonitorForEvents(matching: mask) { [weak self] event in
            guard let self = self else { return }
            
            Task { @MainActor in
                let targetButton = self.mouseButton
                
                // Allow recording ANY mouse button (except Left Click 0 to avoid accidental lockouts)
                if self.isRecordingMouse {
                    if event.buttonNumber != 0 {
                        UserDefaults.standard.set(event.buttonNumber, forKey: "mouseButton")
                        self.isRecordingMouse = false
                        LogManager.shared.log("Input: Global Recorded Mouse Button [\(event.buttonNumber)]")
                    }
                    return
                }

                if !self.useMouseButton || targetButton == 0 { return }
                
                let isDown = (event.type == .otherMouseDown || event.type == .rightMouseDown)
                let isUp = (event.type == .otherMouseUp || event.type == .rightMouseUp)
                
                if isDown && event.buttonNumber == targetButton {
                     LogManager.shared.log("Input: Mouse Button \(targetButton) DOWN - Starting Record")
                    if !self.isHotkeyDown {
                        self.isHotkeyDown = true
                        Task { @MainActor in
                            AudioRecorder.shared.startRecording()
                        }
                    }
                } else if isUp && event.buttonNumber == targetButton {
                    if self.isHotkeyDown {
                        LogManager.shared.log("Input: Mouse Button \(targetButton) UP - Stopping Record")
                        self.isHotkeyDown = false
                        Task { @MainActor in
                            AudioRecorder.shared.stopRecording()
                        }
                    }
                }
            }
        }
        
        // Setup Local Monitor for Mouse too (in case user clicks while focused on app)
        setupLocalMouseRecording()
    }
    
    func setupGlobalHotkey() {
        let isTrusted = AXIsProcessTrusted()
        LogManager.shared.log("InputController: Initializing Hotkey Layer [Code: \(hotkeyCode)]. AX Trusted: \(isTrusted)")
        
        // We handle Global input here. Local is handled in setupLocalMonitors separately.
        
        if isTrusted {
            let mask = (1 << CGEventType.keyDown.rawValue) | 
                       (1 << CGEventType.keyUp.rawValue) | 
                       (1 << CGEventType.flagsChanged.rawValue)
            
            guard let tap = CGEvent.tapCreate(
                tap: .cgSessionEventTap,
                place: .headInsertEventTap,
                options: .defaultTap,
                eventsOfInterest: CGEventMask(mask),
                callback: { (proxy, type, event, refcon) -> Unmanaged<CGEvent>? in
                    if type.rawValue == 14 || type.rawValue == 15 {
                        // kCGEventTapDisabledByTimeout or kCGEventTapDisabledByUserInput - Re-enable
                        Task { @MainActor in
                            if let tap = InputController.shared.eventTap {
                                CGEvent.tapEnable(tap: tap, enable: true)
                                LogManager.shared.log("InputController: EventTap re-enabled after timeout.")
                            }
                        }
                        return Unmanaged.passRetained(event)
                    }
                    let keyCode = event.getIntegerValueField(.keyboardEventKeycode)
                    return InputController.shared.handleTapEvent(proxy: proxy, type: type, event: event, keyCode: Int(keyCode))
                },
                userInfo: nil
            ) else {
                LogManager.shared.log("ERROR: Failed to create CGEventTap despite AX trust. Falling back to NSEvent.")
                setupGlobalFallbacks()
                return
            }
            
            self.eventTap = tap
            self.runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
            CFRunLoopAddSource(CFRunLoopGetCurrent(), self.runLoopSource, .commonModes)
            CGEvent.tapEnable(tap: tap, enable: true)
            LogManager.shared.log("InputController: EventTap ACTIVE")
        } else {
            LogManager.shared.log("WARNING: AX NOT trusted. Enabling Global NSEvent Fallback.")
            setupGlobalFallbacks()
        }
    }
    
    /// This function runs low-level and mediates whether to 'sink' (block) the key or let it pass through.
    /// It is NOT on the MainActor to ensure it stays responsive. 
    nonisolated func handleTapEvent(proxy: CGEventTapProxy, type: CGEventType, event: CGEvent, keyCode: Int) -> Unmanaged<CGEvent>? {
        let isModifier = (keyCode >= 54 && keyCode <= 63)
        let targetKey = state.currentHotkey
        
        let isDown: Bool
        if type == .flagsChanged && isModifier {
            let flags = event.flags
            switch keyCode {
            case 54, 55: isDown = flags.contains(.maskCommand)
            case 56, 60: isDown = flags.contains(.maskShift)
            case 58, 61: isDown = flags.contains(.maskAlternate)
            case 59, 62: isDown = flags.contains(.maskControl)
            case 57: isDown = flags.contains(.maskAlphaShift)
            case 63: isDown = flags.contains(.maskSecondaryFn)
            default: isDown = false
            }
        } else if type == .keyDown || type == .keyUp {
            isDown = (type == .keyDown)
        } else {
            return Unmanaged.passRetained(event)
        }

        // 1. Check if we are RECORDING a new key
        if state.recording {
            if isDown {
                Task { @MainActor in
                     LogManager.shared.log("Input: RECORDING CAPTURED - Key [\(keyCode)]")
                    self.hotkeyCode = keyCode
                    UserDefaults.standard.set(keyCode, forKey: "hotkeyCode")
                    self.isRecordingHotkey = false
                    self.lastDetectedKeyCode = keyCode
                    self.state.update(hotkey: keyCode, recording: false, captured: true)
                    self.refreshMonitors()
                }
                return Unmanaged.passRetained(event)
            }
        }
        
        // 2. Check if it's our target Hotkey (supports matching Left/Right Control interchangeably)
        let isTargetKey = (keyCode == targetKey) || ((targetKey == 62 || targetKey == 59) && (keyCode == 62 || keyCode == 59))

        if isTargetKey && state.timeSinceCapture > 0.6 {
            if isDown {
                Task { @MainActor in
                    if !self.isHotkeyDown {
                        LogManager.shared.log("Input: Hotkey DOWN [\(keyCode)] (EventTap) - Starting Record")
                        self.isHotkeyDown = true
                        self.state.update(down: true)
                        AudioRecorder.shared.startRecording()
                    }
                }
            } else {
                Task { @MainActor in
                    if self.isHotkeyDown {
                        LogManager.shared.log("Input: Hotkey UP [\(keyCode)] (EventTap) - Stopping Record")
                        self.isHotkeyDown = false
                        self.state.update(down: false)
                        AudioRecorder.shared.stopRecording()
                    }
                }
            }
            
            // --- THE SINK ---
            return nil 
        }
        
        // Default: PASS THROUGH
        return Unmanaged.passRetained(event)
    }
    private func setupNSEventFallbacks() {
        // Redundant - we now call setupLocalMonitors and setupGlobalFallbacks explicitly in refreshMonitors
    }

    private func setupGlobalFallbacks() {
        if let monitor = globalKeyDownMonitor { NSEvent.removeMonitor(monitor); globalKeyDownMonitor = nil }
        globalKeyDownMonitor = NSEvent.addGlobalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard let self = self else { return }
            let keyCode = Int(event.keyCode)
            
            if self.isRecordingHotkey {
                Task { @MainActor in
                    self.hotkeyCode = keyCode
                    UserDefaults.standard.set(keyCode, forKey: "hotkeyCode")
                    self.isRecordingHotkey = false
                    self.refreshMonitors()
                }
            } else if keyCode == self.hotkeyCode {
                 Task { @MainActor in 
                    if !self.isHotkeyDown { 
                         LogManager.shared.log("Input: Hotkey DOWN [\(event.keyCode)] (Global NSEvent Monitor) - Starting Record")
                         self.isHotkeyDown = true
                         AudioRecorder.shared.startRecording() 
                    }
                }
            }
        }
        
        if let monitor = globalKeyUpMonitor { NSEvent.removeMonitor(monitor); globalKeyUpMonitor = nil }
        globalKeyUpMonitor = NSEvent.addGlobalMonitorForEvents(matching: .keyUp) { [weak self] event in
            guard let self = self else { return }
            if Int(event.keyCode) == self.hotkeyCode {
                Task { @MainActor in 
                    if self.isHotkeyDown { 
                        LogManager.shared.log("Input: Hotkey UP [\(event.keyCode)] (Global NSEvent Monitor) - Stopping Record")
                        self.isHotkeyDown = false
                        AudioRecorder.shared.stopRecording() 
                    }
                }
            }
        }

        if let monitor = flagsMonitor { NSEvent.removeMonitor(monitor); flagsMonitor = nil }
        flagsMonitor = NSEvent.addGlobalMonitorForEvents(matching: .flagsChanged) { [weak self] event in
            guard let self = self else { return }
            self.handleNSEventFlagsChanged(event, isGlobal: true)
        }
    }

    private func setupLocalMonitors() {
        if let monitor = localKeyDownMonitor { NSEvent.removeMonitor(monitor); localKeyDownMonitor = nil }
        localKeyDownMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard let self = self else { return event }
            LogManager.shared.log("LocalMonitor: KeyDown [\(event.keyCode)] Recording: \(self.isRecordingHotkey)")
            if event.isARepeat { return event }
            
            let keyCode = Int(event.keyCode)
            
            if self.state.recording {
                self.isRecordingHotkey = false
                self.hotkeyCode = keyCode
                UserDefaults.standard.set(keyCode, forKey: "hotkeyCode")
                LogManager.shared.log("Input: RECORDING SUCCESS (Local) - Key [\(keyCode)]")
                self.state.update(hotkey: keyCode, recording: false, captured: true)
                self.refreshMonitors()
                return nil
            }

            if keyCode == self.state.currentHotkey && self.state.timeSinceCapture > 0.6 {
                if !self.isHotkeyDown {
                    LogManager.shared.log("Input: Hotkey DOWN [\(keyCode)] (Local Monitor) - Starting Record")
                    self.isHotkeyDown = true
                    self.state.update(down: true)
                    AudioRecorder.shared.startRecording()
                }
                return nil
            }
            return event
        }
        
        if let monitor = localKeyUpMonitor { NSEvent.removeMonitor(monitor); localKeyUpMonitor = nil }
        localKeyUpMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyUp) { [weak self] event in
            guard let self = self else { return event }
            if self.isHotkeyDown && Int(event.keyCode) == self.state.currentHotkey {
                LogManager.shared.log("Input: Hotkey UP [\(event.keyCode)] (Local Monitor) - Stopping Record")
                self.isHotkeyDown = false
                self.state.update(down: false)
                AudioRecorder.shared.stopRecording()
                return nil
            }
            return event
        }

        if let monitor = localFlagsMonitor { NSEvent.removeMonitor(monitor); localFlagsMonitor = nil }
        localFlagsMonitor = NSEvent.addLocalMonitorForEvents(matching: .flagsChanged) { [weak self] event in
            guard let self = self else { return event }
            self.handleNSEventFlagsChanged(event, isGlobal: false)
            return event
        }
    }

    private func handleNSEventFlagsChanged(_ event: NSEvent, isGlobal: Bool) {
        let keyCode = Int(event.keyCode)
        let flags = event.modifierFlags
        LogManager.shared.log("FlagsMonitor: Key [\(keyCode)] isGlobal: \(isGlobal) Recording: \(self.isRecordingHotkey)")
        
        // DEBUG: Explicit check for Right Option
        if keyCode == 61 {
             LogManager.shared.log("DEBUG: Right Option (61) Detected. Flags: \(flags.rawValue). Contains Option: \(flags.contains(.option))")
        }

        let isModifier = (keyCode >= 54 && keyCode <= 63)
        guard isModifier else { return }

        // Use device-independent flags for consistency
        let isDown: Bool
        switch keyCode {
        case 54, 55: isDown = flags.contains(.command)
        case 56, 60: isDown = flags.contains(.shift)
        case 58, 61: isDown = flags.contains(.option)
        case 59, 62: isDown = flags.contains(.control)
        case 57: isDown = flags.contains(.capsLock)
        case 63: isDown = flags.contains(.function)
        default: isDown = false
        }
        
        if keyCode == 61 {
             LogManager.shared.log("DEBUG: Right Option isDown determined as: \(isDown)")
        }
        
        if isDown {
            Task { @MainActor in self.lastDetectedKeyCode = keyCode }
        }

        if state.recording && isDown {
            LogManager.shared.log("Input: RECORDING SUCCESS (Modifier) - Key [\(keyCode)] (isGlobal: \(isGlobal))")
            self.hotkeyCode = keyCode
            UserDefaults.standard.set(keyCode, forKey: "hotkeyCode")
            self.isRecordingHotkey = false
            self.lastDetectedKeyCode = keyCode
            self.state.update(hotkey: keyCode, recording: false, captured: true)
            self.refreshMonitors()
            return
        }

        let isTargetKey = (keyCode == state.currentHotkey) || ((state.currentHotkey == 62 || state.currentHotkey == 59) && (keyCode == 62 || keyCode == 59))
        if isTargetKey {
            if state.timeSinceCapture <= 0.6 {
                LogManager.shared.log("DEBUG: Ignoring trigger due to timeSinceCapture: \(state.timeSinceCapture)")
            }
             
            if isDown {
                if !self.isHotkeyDown {
                    LogManager.shared.log("Input: Modifier DOWN [\(keyCode)] (Flags Monitor, isGlobal: \(isGlobal)) - Starting Record")
                    self.isHotkeyDown = true
                    self.state.update(down: true)
                    Task { @MainActor in
                        AudioRecorder.shared.startRecording()
                    }
                }
            } else {
                if self.isHotkeyDown {
                    LogManager.shared.log("Input: Modifier UP [\(keyCode)] (Flags Monitor, isGlobal: \(isGlobal)) - Stopping Record")
                    self.isHotkeyDown = false
                    self.state.update(down: false)
                    Task { @MainActor in
                        AudioRecorder.shared.stopRecording()
                    }
                }
            }
        }
    }
    
    func executeKeystroke(key: String, modifiers: NSEvent.ModifierFlags) {
        // Use nil source to avoid inheriting current keyboard state (modifiers held down by user)
        let source = CGEventSource(stateID: .hidSystemState)
        
        let keyCode: CGKeyCode
        switch key.lowercased() {
        case "v": keyCode = 9
        case "a": keyCode = 0
        case "c": keyCode = 8
        case "x": keyCode = 7
        case "z": keyCode = 6
        case "b": keyCode = 11
        case "i": keyCode = 34
        case "u": keyCode = 32
        case "delete", "backspace": keyCode = 51
        case "return", "enter": keyCode = 36
        default: return
        }
        
        let keyDown = CGEvent(keyboardEventSource: source, virtualKey: keyCode, keyDown: true)
        let keyUp = CGEvent(keyboardEventSource: source, virtualKey: keyCode, keyDown: false)
        
        var flags: CGEventFlags = []
        if modifiers.contains(.command) { flags.insert(.maskCommand) }
        if modifiers.contains(.shift) { flags.insert(.maskShift) }
        if modifiers.contains(.option) { flags.insert(.maskAlternate) }
        
        keyDown?.flags = flags
        keyUp?.flags = flags
        
        keyDown?.post(tap: .cghidEventTap)
        keyUp?.post(tap: .cghidEventTap)
    }
    
    private func handleFormatting(_ format: String) async {
        Task {
            print("Formatting requested: \(format)")
        }
    }
    
    deinit {
        if let monitor = globalKeyDownMonitor { NSEvent.removeMonitor(monitor) }
        if let monitor = globalKeyUpMonitor { NSEvent.removeMonitor(monitor) }
        if let monitor = mouseMonitor { NSEvent.removeMonitor(monitor) }
        if let monitor = localMouseMonitor { NSEvent.removeMonitor(monitor) }
    }
    
    func pasteText(_ text: String) async {
        let isTrusted = AXIsProcessTrusted()
        LogManager.shared.log("InputController: Pasting text (length: \(text.count)). AX Trusted: \(isTrusted)")

        let useDirectTyping = UserDefaults.standard.bool(forKey: "useSimulation")
        let shouldRestoreClipboard = UserDefaults.standard.bool(forKey: "restoreClipboard")
        let shouldAutoSubmit = UserDefaults.standard.bool(forKey: "autoSubmit")
        
        // --- MODE 1: DIRECT TYPING ---
        if useDirectTyping {
            LogManager.shared.log("InputController: Mode = Direct Typing")
            // We use AppleScript for safer typing of long strings than raw CGEvents loop
            let escapedText = text.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
            let scriptSource = "tell application \"System Events\" to keystroke \"\(escapedText)\""
            
            Task {
                var error: NSDictionary?
                if let script = NSAppleScript(source: scriptSource) {
                    script.executeAndReturnError(&error)
                    if let err = error {
                        LogManager.shared.log("Typing Error: \(err)")
                        return
                    }
                    if shouldAutoSubmit {
                        try? await Task.sleep(nanoseconds: 100_000_000)
                        self.executeKeystroke(key: "return", modifiers: [])
                    }
                }
            }
            return
        }
        
        // --- MODE 2: FAST PASTE (Default) ---
        LogManager.shared.log("InputController: Mode = Fast Paste")
        
        // 1. Save Old Clipboard
        var oldContent: String? = nil
        if shouldRestoreClipboard {
            oldContent = NSPasteboard.general.string(forType: .string)
        }
        
        // 2. Set New Content
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(text, forType: .string)
        
        // 3. Trigger Paste (Cmd+V)
        try? await Task.sleep(nanoseconds: 150_000_000)
        executeKeystroke(key: "v", modifiers: [.command])
        LogManager.shared.log("Native Paste Executed: \(text.prefix(15))...")
        
        // 4. Auto-Submit
        if shouldAutoSubmit {
            try? await Task.sleep(nanoseconds: 200_000_000) // Wait for paste
            executeKeystroke(key: "return", modifiers: [])
            LogManager.shared.log("Auto-Submit Executed")
        }
        
        // 5. Restore Clipboard
        if shouldRestoreClipboard, let safeOld = oldContent {
            // Wait enough time for the paste to actually happen (0.5s safety)
            try? await Task.sleep(nanoseconds: 500_000_000)
            NSPasteboard.general.clearContents()
            NSPasteboard.general.setString(safeOld, forType: .string)
            LogManager.shared.log("Clipboard Restored to previous state")
        }
    }
    
    func checkAutomationPermission() -> Bool {
        // We need a robust check that actually triggers the "Control System Events" permission.
        // Merely asking for "name" is sometimes allowed without full automation access.
        // We will try to get the 'processes' list, which is a common restricted action,
        // OR simply try the keystroke action itself (safely).
        
        let scriptSource = "tell application \"System Events\" to packet id 1" // harmless but check permissions? No.
        // Let's try: tell application "System Events" to get running
        let strictSource = "tell application \"System Events\" to get POSIX path of (path to frontmost application)"
        
        var error: NSDictionary?
        if let script = NSAppleScript(source: strictSource) {
            let result = script.executeAndReturnError(&error)
            
            if let err = error {
                let errCode = err[NSAppleScript.errorNumber] as? Int ?? 0
                let errMsg = err[NSAppleScript.errorMessage] as? String ?? "No message"
                
                if errCode == -1743 {
                    LogManager.shared.log("PERMISSIONS: Automation DENIED (-1743) for strict check.")
                } else {
                    LogManager.shared.log("PERMISSIONS: Automation Strict Check Failed (\(errCode)): \(errMsg)")
                }
                return false
            }
            return true
        }
        return false
    }
    
    /// Forces a prompt for Automation by attempting to talk to System Events.
    func requestAutomation() {
        LogManager.shared.log("PERMISSIONS: Manually requesting Automation via NSAppleScript (Strict)...")
        
        // This command should definitely trigger the prompt if not allowed
        let scriptSource = "tell application \"System Events\" to get POSIX path of (path to frontmost application)"
        var error: NSDictionary?
        
        if let script = NSAppleScript(source: scriptSource) {
            script.executeAndReturnError(&error)
            
            if let err = error {
                let errCode = err[NSAppleScript.errorNumber] as? Int ?? 0
                LogManager.shared.log("PERMISSIONS: Automation request result code: \(errCode)")
                
                if errCode == -1743 {
                    LogManager.shared.log("PERMISSIONS: Automation is DENIED. Opening System Settings...")
                    let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Automation")!
                    NSWorkspace.shared.open(url)
                } else {
                    LogManager.shared.log("PERMISSIONS: Automation prompt should have appeared (Error \(errCode))")
                }
            } else {
                LogManager.shared.log("PERMISSIONS: Automation already granted.")
                self.isAutomationTrusted = true
            }
        }
    }
    
    func requestAccessibility() {
        LogManager.shared.log("PERMISSIONS: Requesting Accessibility via OS prompt...")
        let options: [String: Any] = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true]
        AXIsProcessTrustedWithOptions(options as CFDictionary)
    }
    
    func resetPermissions() {
        LogManager.shared.log("PERMISSIONS: Running forceful TCC reset...")
        let bundleID = Bundle.main.bundleIdentifier ?? "com.cosmowhisper.CosmoWhisper"
        
        Task.detached(priority: .userInitiated) {
            // 1. Reset Permissions (Core Privacy)
            let categories = ["Accessibility", "Microphone", "AppleEvents", "ScreenCapture", "All"]
            for cat in categories {
                let p = Process()
                p.launchPath = "/usr/bin/tccutil"
                p.arguments = ["reset", cat, bundleID]
                try? p.run()
                p.waitUntilExit()
            }
            
            // 2. Clear Local State (UserDefaults & Application Support)
            // This ensures a clean slate for all app-internal logic
            let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
            let appDir = appSupport.appendingPathComponent("CosmoWhisper")
            try? FileManager.default.removeItem(at: appDir)
            
            // Clean Preferences
            let defaults = UserDefaults.standard
            let dict = defaults.dictionaryRepresentation()
            dict.keys.forEach { defaults.removeObject(forKey: $0) }
            defaults.synchronize()

            // 3. Kill System Events to clear hung states
            let p4 = Process()
            p4.launchPath = "/usr/bin/killall"
            p4.arguments = ["System Events"]
            p4.launch()
            p4.waitUntilExit()
            
            LogManager.shared.log("PERMISSIONS: TCC Reset complete for \(bundleID). Relaunching...")
            
            await MainActor.run {
                // Try to trigger prompts again (optional, since we are restarting)
                _ = self.checkAutomationPermission()
                
                // --- RELAUNCH LOGIC ---
                let url = Bundle.main.bundleURL
                let configuration = NSWorkspace.OpenConfiguration()
                NSWorkspace.shared.openApplication(at: url, configuration: configuration) { _, error in
                    if let error = error {
                        LogManager.shared.log("RELAUNCH ERROR: \(error.localizedDescription)")
                    }
                    DispatchQueue.main.async {
                        NSApp.terminate(nil)
                    }
                }
            }
        }
        
        // Ensure app actually quits even if wait fails
        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            NSApp.terminate(nil)
        }
    }
    
    private var lastAXTrusted = false
    
    private func startPermissionWatchdog() {
        permissionWatchdog?.invalidate()
        lastAXTrusted = AXIsProcessTrusted()
        
        permissionWatchdog = Timer.scheduledTimer(withTimeInterval: 5.0, repeats: true) { [weak self] _ in
            guard let strongSelf = self else { return }
            Task { @MainActor in
                let axTrusted = AXIsProcessTrusted()
                if axTrusted != strongSelf.lastAXTrusted {
                    strongSelf.lastAXTrusted = axTrusted
                    LogManager.shared.log("InputController: Accessibility status changed to: \(axTrusted)")
                    if axTrusted {
                        LogManager.shared.log("InputController: Refreshing monitors in 0.5s (AX trusted)...")
                        try? await Task.sleep(nanoseconds: 500_000_000)
                        strongSelf.refreshMonitors()
                    }
                } else if axTrusted && strongSelf.eventTap == nil {
                     // RETRY Logic: If we are trusted but have no Tap, try making it again
                     LogManager.shared.log("InputController: Trusted but no EventTap. Retrying creation...")
                     strongSelf.setupGlobalHotkey()
                }
                
                Task { @MainActor in
                    strongSelf.isAutomationTrusted = axTrusted
                }
            }
        }
    }

    private func setupLocalMouseRecording() {
        // Use a local monitor to catch mouse buttons while Dashboard is focused
        let mask: NSEvent.EventTypeMask = [.leftMouseDown, .rightMouseDown, .otherMouseDown, .otherMouseUp, .otherMouseDragged]
        
        // Remove existing if any
        if let monitor = localMouseMonitor { NSEvent.removeMonitor(monitor); localMouseMonitor = nil }
        
        localMouseMonitor = NSEvent.addLocalMonitorForEvents(matching: mask) { [weak self] event in
            guard let self = self else { return event }
            
            let targetButton = self.mouseButton
    
            if self.isRecordingMouse {
                LogManager.shared.log("DEBUG: Local Mouse Event: \(event.type.rawValue), Button: \(event.buttonNumber)")
                if event.buttonNumber != 0 {
                    UserDefaults.standard.set(event.buttonNumber, forKey: "mouseButton")
                    self.isRecordingMouse = false
                    LogManager.shared.log("Input: SUCCESS - Locally Recorded Button [\(event.buttonNumber)]")
                    return nil
                }
            } else if self.useMouseButton && targetButton != 0 && event.buttonNumber == targetButton {
                // Handle Local Triggering
                let isDown = (event.type == .otherMouseDown || event.type == .rightMouseDown)
                let isUp = (event.type == .otherMouseUp || event.type == .rightMouseUp)
                
                if isDown && !self.isHotkeyDown {
                    LogManager.shared.log("Input: Local Mouse Button \(targetButton) DOWN")
                    self.isHotkeyDown = true
                    AudioRecorder.shared.startRecording()
                } else if isUp && self.isHotkeyDown {
                     LogManager.shared.log("Input: Local Mouse Button \(targetButton) UP")
                     self.isHotkeyDown = false
                     AudioRecorder.shared.stopRecording()
                }
            }
            return event
        }
    }
}
