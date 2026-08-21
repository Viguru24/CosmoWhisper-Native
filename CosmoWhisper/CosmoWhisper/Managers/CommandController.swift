import Foundation
import AppKit

@MainActor
public class CommandController {
    public static let shared = CommandController()
    private let synthesizer: NSSpeechSynthesizer = {
        let synth = NSSpeechSynthesizer()
        // Priority list of high-quality/modern voices
        let preferredVoices = [
            "com.apple.speech.synthesis.voice.samantha.premium",
            "com.apple.speech.synthesis.voice.ava.premium",
            "com.apple.speech.synthesis.voice.allison.premium",
            "com.apple.speech.synthesis.voice.tom.premium",
            "com.apple.speech.voice.Alex"
        ]
        
        for voice in preferredVoices {
            if NSSpeechSynthesizer.availableVoices.contains(where: { $0.rawValue == voice }) {
                synth.setVoice(NSSpeechSynthesizer.VoiceName(rawValue: voice))
                break
            }
        }
        return synth
    }()
    
    // --- LEGACY MAPPINGS ----
    private let legacyWebShortcuts: [String: String] = [
        "google": "https://google.com",
        "github": "https://github.com",
        "groq": "https://groq.com",
        "chatgpt": "https://chatgpt.com",
        "claude": "https://claude.ai",
        "reddit": "https://reddit.com",
        "twitter": "https://x.com",
        "x": "https://x.com",
        "facebook": "https://facebook.com",
        "instagram": "https://instagram.com",
        "linkedin": "https://linkedin.com",
        "netflix": "https://netflix.com",
        "amazon": "https://amazon.com",
        "wikipedia": "https://wikipedia.org",
        "gmail": "https://mail.google.com",
        "outlook": "https://outlook.live.com",
        "twitch": "https://twitch.tv",
        "youtube": "https://youtube.com",
        "cosmowhisper": "https://cosmowhisper.com"
    ]
    
    private let legacyAppShortcuts: [String: String] = [
        "word": "Microsoft Word",
        "microsoft word": "Microsoft Word",
        "excel": "Microsoft Excel",
        "microsoft excel": "Microsoft Excel",
        "powerpoint": "Microsoft PowerPoint",
        "microsoft powerpoint": "Microsoft PowerPoint",
        "outlook": "Microsoft Outlook",
        "chrome": "Google Chrome",
        "google chrome": "Google Chrome",
        "firefox": "Firefox",
        "mozilla firefox": "Firefox",
        "edge": "Microsoft Edge",
        "microsoft edge": "Microsoft Edge",
        "calculator": "Calculator",
        "notepad": "TextEdit",
        "terminal": "Terminal",
        "code": "Visual Studio Code",
        "vscode": "Visual Studio Code",
        "visual studio code": "Visual Studio Code",
        "spotify": "Spotify",
        "vlc": "VLC",
        "discord": "Discord",
        "browser": "Safari",
        "explorer": "Finder",
        "file explorer": "Finder",
        "settings": "System Settings"
    ]
    
    func handle(text: String) async -> Bool {
        // 1. Clean Punctuation & Whitespace
        let cleaned = text.components(separatedBy: CharacterSet.punctuationCharacters).joined(separator: "")
        var cmd = cleaned.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        
        // 2. Handle merged "Open[App]" (common transcription error)
        if cmd.hasPrefix("open") && !cmd.hasPrefix("open ") && cmd.count > 4 {
            let appName = String(cmd.dropFirst(4))
            cmd = "open \(appName)" // Normalize to "open appname"
        }
        
        // Helper to check if a command is triggered at the start
        func isTriggered(_ triggers: [String]) -> Bool {
            for trigger in triggers {
                if cmd == trigger { return true }
                if cmd.hasPrefix("\(trigger) ") {
                    // Check if trigger is within the first 1-2 words
                    let words = cmd.split(separator: " ")
                    let triggerWords = trigger.split(separator: " ")
                    if words.count >= triggerWords.count {
                        let potentialTrigger = words.prefix(triggerWords.count).joined(separator: " ")
                        if potentialTrigger == trigger {
                            return true
                        }
                    }
                }
            }
            return false
        }
        
        // --- 1. CONFIG TOGGLES ---
        if isTriggered(["enable typing mode", "switch to typing mode", "use typing mode"]) {
            UserDefaults.standard.set(true, forKey: "useSimulation")
            notify("Switched to Typing Mode")
            return true
        }
        if isTriggered(["enable paste mode", "switch to paste mode", "use paste mode"]) {
            UserDefaults.standard.set(false, forKey: "useSimulation")
            notify("Switched to Paste Mode")
            return true
        }
        
        // --- 2. TEXT FORMATTING ---
        if isTriggered(["make uppercase", "all caps", "upper case"]) {
            triggerFormat("uppercase")
            return true
        }
        if isTriggered(["make lowercase", "lower case", "all lowercase"]) {
            triggerFormat("lowercase")
            return true
        }
        if isTriggered(["make title case", "title case"]) {
            triggerFormat("titlecase")
            return true
        }
        if cmd == "select all" {
            triggerKey("a", mask: [.command])
            return true
        }
        if cmd == "undo" || cmd == "undo that" {
            triggerKey("z", mask: [.command])
            return true
        }
        if isTriggered(["paste", "paste that", "paste here"]) {
            triggerKey("v", mask: [.command])
            return true
        }
        
        if isTriggered(["delete all", "clear all", "clear field", "delete everything"]) {
            triggerKey("a", mask: [.command]) // Select All
            try? await Task.sleep(nanoseconds: 100_000_000)
            triggerKey("delete", mask: []) // Delete
            return true
        }
        
        // --- 3. SYSTEM ---
        if isTriggered(["insert date", "todays date", "current date"]) {
            let formatter = DateFormatter()
            formatter.dateStyle = .full
            insertText(formatter.string(from: Date()))
            return true
        }
        if isTriggered(["insert time", "current time"]) {
            let formatter = DateFormatter()
            formatter.timeStyle = .short
            insertText(formatter.string(from: Date()))
            return true
        }
        if isTriggered(["screenshot", "screen capture", "take a screenshot", "take screenshot"]) {
            triggerScreenshot()
            return true
        }
        if isTriggered(["volume up", "louder"]) { return updateVolume(up: true) }
        if isTriggered(["volume down", "quieter"]) { return updateVolume(up: false) }
        
        // --- 4. WEB NAVIGATION ---
        if cmd.hasPrefix("visit ") || cmd.hasPrefix("go to ") {
            let rawSite = cmd.replacingOccurrences(of: "visit ", with: "").replacingOccurrences(of: "go to ", with: "").trimmingCharacters(in: .whitespaces)
            return openSite(rawSite)
        }
        
        // Specific YouTube Search (Common request)
        if cmd.hasPrefix("youtube ") || cmd.hasPrefix("open youtube ") {
            var query = cmd.replacingOccurrences(of: "open youtube ", with: "").replacingOccurrences(of: "youtube ", with: "")
            query = query.trimmingCharacters(in: .whitespaces)
            if query.isEmpty {
                 return openSite("youtube")
            }
            let url = "https://www.youtube.com/results?search_query=\(query.addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed) ?? query)"
            if let urlObj = URL(string: url) {
                NSWorkspace.shared.open(urlObj)
                return true
            }
        }
        
        // --- 5. SEARCH ---
        if cmd.hasPrefix("search ") || cmd.hasPrefix("find ") {
            let query = cmd.replacingOccurrences(of: "search ", with: "").replacingOccurrences(of: "find ", with: "")
            _ = openSite("google", query: query)
            return true
        }
        
        // --- 6. APP LAUNCHING ---
        if cmd.hasPrefix("open ") || cmd.hasPrefix("launch ") {
            let appName = cmd.replacingOccurrences(of: "open ", with: "").replacingOccurrences(of: "launch ", with: "").trimmingCharacters(in: .whitespaces)
            return launchApp(named: appName)
        }
        
        // --- 7. AI SMART COMMANDS ---
        if cmd.hasPrefix("ask ") {
            let question = cmd.replacingOccurrences(of: "ask ", with: "")
            processAI(prompt: "You are a helpful assistant. Answer the user's question concisely.", context: question)
            return true
        }
        
        if cmd.hasPrefix("write about ") || cmd.hasPrefix("write ") {
            let topic = cmd.replacingOccurrences(of: "write about ", with: "").replacingOccurrences(of: "write ", with: "")
            processAI(prompt: "You are a professional writer. Write a concise, high-quality response about the topic.", context: topic)
            return true
        }
        
        // Context-Aware Commands (Requires Selection)
        if isTriggered(["summarize", "summarize this", "summarize that", "give me the gist", "summarize selection"]) {
            processAIOnSelection(prompt: "Summarize the following text into 2-3 concise sentences.")
            return true
        }
        
        if isTriggered(["fix grammar", "fix this", "fix that", "polish logic", "polish"]) {
           processAIOnSelection(prompt: "Fix the grammar and improve the flow of this text. Maintain the original meaning.")
           return true
        }
        
        if isTriggered(["reply to this", "reply to that", "draft reply"]) {
            processAIOnSelection(prompt: "Draft a professional and polite reply to this text.")
            return true
        }
        
        if isTriggered(["make bullet list", "bullet points"]) {
            processAIOnSelection(prompt: "Convert this text into a clean bulleted list using '•'.")
            return true
        }
        
        // --- 8. NEW SMART COMMANDS (LOGIC FILL) ---
        
        // CLIPBOARD
        if isTriggered(["cut all", "cut everything"]) {
            triggerKey("a", mask: [.command])
            try? await Task.sleep(nanoseconds: 100_000_000)
            triggerKey("x", mask: [.command])
            return true
        }
        if isTriggered(["copy that", "copy all", "copy this"]) {
             if cmd == "copy all" { triggerKey("a", mask: [.command]); try? await Task.sleep(nanoseconds: 100_000_000) }
             triggerKey("c", mask: [.command])
             return true
        }
        if isTriggered(["paste here", "paste that", "paste this", "paste"]) {
            triggerKey("v", mask: [.command])
            return true
        }
        
        // SEARCH
        if cmd.hasPrefix("google ") {
            let query = cmd.replacingOccurrences(of: "google ", with: "")
            _ = openSite("google", query: query)
            return true
        }
        if cmd.hasPrefix("youtube ") {
            let query = cmd.replacingOccurrences(of: "youtube ", with: "")
            let url = "https://www.youtube.com/results?search_query=\(query.addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed) ?? query)"
            NSWorkspace.shared.open(URL(string: url)!)
            return true
        }
        
        // AI EDITING & FORMATTING
        if isTriggered(["fix everything"]) {
             processAIOnSelection(prompt: "Fix the grammar, spelling, and punctuation of this text. Return only the corrected text.")
             return true
        }
        if isTriggered(["shorter", "make shorter", "condense", "condense this", "condense that"]) {
            processAIOnSelection(prompt: "Make the following text concise and punchy. Keep the meaning but remove fluff.")
            return true
        }
        if isTriggered(["expand", "expand this", "expand that", "lengthen", "lengthen this", "lengthen that"]) {
            processAIOnSelection(prompt: "Expand on this text to make it more descriptive and engaging. Add natural flow and detail.")
            return true
        }
        if isTriggered(["flesh out", "flesh out this", "flesh out that"]) {
             processAIOnSelection(prompt: "Turn these brief notes into multiple detailed, well-written paragraphs.")
             return true
        }
        
        // --- NEW COMMANDS ---
        if isTriggered(["rewrite this", "rewrite that", "rewrite", "rephrase", "rephrase this"]) {
            processAIOnSelection(prompt: "Rewrite the following text to be clearer and more effective. Keep the same meaning.")
            return true
        }
        
        if isTriggered(["make professional", "professionalize", "professional tone"]) {
            processAIOnSelection(prompt: "Rewrite the following text to sound professional, corporate, and polite.")
            return true
        }
        
        if isTriggered(["make friendly", "casual tone", "make casual"]) {
            processAIOnSelection(prompt: "Rewrite the following text to sound friendly, casual, and approachable.")
            return true
        }
        
        if isTriggered(["translate to spanish", "translate into spanish", "translate this into spanish", "translate this to spanish", "translate that to spanish", "translate that into spanish", "spanish translation", "make spanish"]) {
            processAIOnSelection(prompt: "Translate the following text into Spanish. Output ONLY the translated Spanish text.")
            return true
        }
        
        if isTriggered(["translate to french", "translate into french", "translate this into french", "translate this to french", "translate that to french", "translate that into french", "french translation", "make french"]) {
             processAIOnSelection(prompt: "Translate the following text into French. Output ONLY the translated French text.")
             return true
        }
        
        if isTriggered(["extract action items", "todo list", "action items"]) {
            processAIOnSelection(prompt: "Extract all action items and tasks from the following text into a clean bulleted list.")
            return true
        }
        
        if isTriggered(["explain this", "explain that", "explain selection"]) {
            processAIOnSelection(prompt: "Explain the following text in simple terms, as if to a beginner.")
            return true
        }
        
        // FUN / DEV FORMATTING
        if isTriggered(["numeral", "digits", "digitizer"]) {
            processAIOnSelection(prompt: "Convert all written numbers (e.g. 'ten') into digits (e.g. '10') in the following text. Keep everything else the same.")
            return true
        }
        if isTriggered(["words", "spelled out", "word smith"]) {
             processAIOnSelection(prompt: "Convert all digits (e.g. '10') into written words (e.g. 'ten') in the following text. Keep everything else the same.")
             return true
        }
        if isTriggered(["camel case", "humpcase", "camelcase"]) {
            processAIOnSelection(prompt: "Convert the following text to camelCase (e.g. 'myVariableName'). Return only the code-safe string.")
            return true
        }
        if isTriggered(["snake case", "snake_case", "snakecase"]) {
             processAIOnSelection(prompt: "Convert the following text to snake_case (e.g. 'my_variable_name'). Return only the code-safe string.")
             return true
        }
        
        // RICH TEXT (Best Effort via Shortcuts)
        if isTriggered(["bold that", "bold text", "make bold", "bold it", "make it bold", "bold selection"]) {
            triggerKey("b", mask: [.command])
            return true
        }
        if isTriggered(["italicize", "make italic", "italic that", "italic text", "italics"]) {
            triggerKey("i", mask: [.command])
            return true
        }
        if isTriggered(["underline that", "underliner", "underline it", "make underline"]) {
             triggerKey("u", mask: [.command])
             return true
        }
        
        // SYSTEM CONTROL (MOCK / FUN)
        if cmd.hasPrefix("system control ") || cmd.hasPrefix("execute ") {
             let instruction = cmd.replacingOccurrences(of: "system control ", with: "").replacingOccurrences(of: "execute ", with: "")
             // Safety: We don't actually run shell commands blindly. We'll generate it and copy to clipboard.
             processAI(prompt: "Generate a macOS Terminal command to do the following. Return ONLY the command code, no '```' blocks.", context: instruction)
             notify("Generated command on clipboard (Safety Mode)")
             return true
        }
        
        // --- 8.5 TEXT TO SPEECH ---
        if isTriggered(["read this", "read selection", "speak", "read it"]) {
            readSelection()
            return true
        }
        
        if isTriggered(["stop reading", "stop speaking", "shush"]) {
            stopReading()
            return true
        }
        return false
    }
    
    // --- IMPLEMENTATION HELPERS ---
    
    private func triggerFormat(_ format: String) {
        NotificationCenter.default.post(name: NSNotification.Name("FormatText"), object: format)
    }

    private func processAI(prompt: String, context: String) {
        Task {
            notify("Thinking...")
            do {
                let result = try await AIService.shared.processCommand(prompt: prompt, context: context)
                insertText(result)
            } catch {
                notify("AI Error: \(error.localizedDescription)")
            }
        }
    }
    
    private func processAIOnSelection(prompt: String) {
        Task {
            notify("Reading selection...")
            
            // 1. Let any active hotkey/mouse button release
            try? await Task.sleep(nanoseconds: 120_000_000)
            
            let oldPasteboard = NSPasteboard.general.string(forType: .string)
            
            // Clear pasteboard to detect fresh copy
            NSPasteboard.general.clearContents()
            
            triggerKey("c", mask: [.command]) // Cmd+C
            
            // Poll for clipboard arrival up to 350ms
            var selection: String? = nil
            for _ in 0..<7 {
                try? await Task.sleep(nanoseconds: 50_000_000)
                if let current = NSPasteboard.general.string(forType: .string), !current.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                    selection = current
                    break
                }
            }
            
            // Fallback: If nothing was newly copied, check old pasteboard
            if selection == nil || selection?.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty == true {
                selection = oldPasteboard
            }
            
            guard let validSelection = selection, !validSelection.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
                notify("No text selected!")
                return
            }
            
            // 2. Process
            notify("Processing...")
            do {
                let result = try await AIService.shared.processCommand(prompt: prompt, context: validSelection)
                // 3. Paste Result
                await InputController.shared.pasteText(result)
            } catch {
                notify("AI Error: \(error.localizedDescription)")
            }
        }
    }
    
    private func triggerKey(_ key: String, mask: NSEvent.ModifierFlags) {
        InputController.shared.executeKeystroke(key: key, modifiers: mask)
    }
    
    private func insertText(_ text: String) {
        NotificationCenter.default.post(name: NSNotification.Name("InsertText"), object: text)
    }
    
    private func notify(_ msg: String) {
        LogManager.shared.log("Command: \(msg)")
    }
    
    private func triggerScreenshot() {
        LogManager.shared.log("Command: Taking screenshot...")
        
        let hasAccess = CGPreflightScreenCaptureAccess()
        LogManager.shared.log("Command: Screen Recording Status: \(hasAccess)")
        
        if !hasAccess {
            LogManager.shared.log("Command: Screen Recording Permission Missing. Requesting...")
            CGRequestScreenCaptureAccess()
            
            // Only show alert if we definitely don't have access
            Task { @MainActor in
                let alert = NSAlert()
                alert.messageText = "Screen Recording Permission"
                alert.informativeText = "CosmoWhisper needs 'Screen Recording' permission to take screenshots. \n\nIf you've already granted it, please try the command again. If not, click 'Open Settings'."
                alert.alertStyle = .informational
                alert.addButton(withTitle: "Open Settings")
                alert.addButton(withTitle: "Cancel")
                
                if alert.runModal() == .alertFirstButtonReturn {
                    if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture") {
                        NSWorkspace.shared.open(url)
                    }
                }
            }
            // We return here because we need to wait for the user to grant it.
            // However, next time they trigger, hasAccess should be true.
            return
        }
        
        Task.detached {
            let p = Process()
            p.launchPath = "/usr/sbin/screencapture"
            p.arguments = ["-i", "-c"] // Interactive + Clipboard
            p.launch()
        }
    }
    
    private func updateVolume(up: Bool) -> Bool {
         let scriptSource = up ? "set volume output volume ((output volume of (get volume settings)) + 10)" : "set volume output volume ((output volume of (get volume settings)) - 10)"
         Task {
             _ = NSAppleScript(source: scriptSource)?.executeAndReturnError(nil)
         }
        return true
    }
    
    private func readSelection() {
        Task {
            notify("Reading selection...")
            // 1. Capture Selection (Reuse logic)
            let oldPasteboard = NSPasteboard.general.string(forType: .string)
            NSPasteboard.general.clearContents()
            
            triggerKey("c", mask: [.command])
            try? await Task.sleep(nanoseconds: 200_000_000)
            
            guard let selection = NSPasteboard.general.string(forType: .string), !selection.isEmpty else {
                notify("Nothing selected to read.")
                if let old = oldPasteboard { NSPasteboard.general.setString(old, forType: .string) }
                return
            }
            
            // 2. Speak
            synthesizer.startSpeaking(selection)
            notify("Speaking...")
            
            // Restore? Optional. Let's keep clipboard as selected text for now as user might want to paste it elsewhere.
        }
    }
    
    private func stopReading() {
        synthesizer.stopSpeaking()
        notify("Stopped speaking.")
    }
    
    private func openSite(_ name: String, query: String? = nil) -> Bool {
        var finalUrl = ""
        
        if let mapped = legacyWebShortcuts[name] {
            finalUrl = mapped
        } else if name.contains(".") {
            finalUrl = "https://\(name)"
        } else {
             finalUrl = "https://duckduckgo.com/?q=!+\(name)" // I'm Feeling Lucky search
        }
        
        if let q = query {
            // If it's a search query
            finalUrl = "https://google.com/search?q=\(q.addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed) ?? q)"
        }
        
        if let url = URL(string: finalUrl) {
            NSWorkspace.shared.open(url)
            return true
        }
        return false
    }
    
    private func launchApp(named name: String) -> Bool {
        let mappedName = legacyAppShortcuts[name] ?? name
        
        // 1. Try NSWorkspace (Best for known apps)
        if let appUrl = NSWorkspace.shared.urlForApplication(withName: mappedName) {
            NSWorkspace.shared.openApplication(at: appUrl, configuration: NSWorkspace.OpenConfiguration())
            return true
        }
        
// 2. Fallback: Try to find URL by name manually if urlForApplication failed or returns nil
        // Note: urlForApplication is usually sufficient, but if not, we can try to construct a URL.
        // The deprecated launchApplication(name) does a search. We can replicate this safely.
        
        // As a robust fallback, we will just log failure if not found, rather than using deprecated API.
        // OR we can try to use URL(fileURLWithPath: "/Applications/\(mappedName).app") if desperate.
        
        let commonPaths = [
            "/Applications/\(mappedName).app",
            "/System/Applications/\(mappedName).app",
            "/System/Applications/Utilities/\(mappedName).app"
        ]
        
        for path in commonPaths {
            let url = URL(fileURLWithPath: path)
            if FileManager.default.fileExists(atPath: path) {
                NSWorkspace.shared.openApplication(at: url, configuration: NSWorkspace.OpenConfiguration())
                return true
            }
        }

        LogManager.shared.log("Command: Failed to launch app '\(mappedName)'")
        return false
    }
}


