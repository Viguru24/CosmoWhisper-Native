import Foundation
import AVFoundation
import Combine
import AudioToolbox

// --- ACTOR FOR THREAD-SAFE AUDIO HARDWARE ---
actor AudioEngine {
    private var audioRecorder: AVAudioRecorder?
    private var isRecording = false
    
    enum EngineError: Error {
        case notAuthorized
        case setupFailed(String)
        case recordFailed
    }
    
    func checkPermission() async -> Bool {
        LogManager.shared.log("AudioEngine: Checking microphone authorization status...")
        let status = AVCaptureDevice.authorizationStatus(for: .audio)
        LogManager.shared.log("AudioEngine: Current authorization status: \(status.rawValue)")
        
        if status == .authorized { return true }
        if status == .denied || status == .restricted { 
            LogManager.shared.log("AudioEngine: Access denied or restricted.")
            return false 
        }
        
        LogManager.shared.log("AudioEngine: Requesting microphone access...")
        return await withCheckedContinuation { continuation in
            AVCaptureDevice.requestAccess(for: .audio) { allowed in
                LogManager.shared.log("AudioEngine: RequestAccess result: \(allowed)")
                continuation.resume(returning: allowed)
            }
        }
    }
    
    func startRecording(url: URL) throws {
        // 1. Setup Session
        LogManager.shared.log("AudioEngine: Preparing to start recording at \(url.path)...")
        
        let settings: [String: Any] = [
            AVFormatIDKey: Int(kAudioFormatMPEG4AAC),
            AVSampleRateKey: 44100,
            AVNumberOfChannelsKey: 1,
            AVEncoderAudioQualityKey: AVAudioQuality.high.rawValue
        ]
        
        do {
            let recorder = try AVAudioRecorder(url: url, settings: settings)
            recorder.isMeteringEnabled = true
            
            LogManager.shared.log("AudioEngine: Initialized AVAudioRecorder.")
            
            if recorder.prepareToRecord() {
                LogManager.shared.log("AudioEngine: prepareToRecord SUCCESS.")
                if recorder.record() {
                    self.audioRecorder = recorder
                    self.isRecording = true
                    LogManager.shared.log("AudioEngine: Recording Started [Actor]")
                } else {
                    LogManager.shared.log("AudioEngine: record() returned false.")
                    throw EngineError.recordFailed
                }
            } else {
                LogManager.shared.log("AudioEngine: prepareToRecord() returned false.")
                throw EngineError.setupFailed("prepareToRecord returned false")
            }
        } catch {
            LogManager.shared.log("AudioEngine: setupFailed with error: \(error.localizedDescription)")
            throw EngineError.setupFailed(error.localizedDescription)
        }
    }
    
    func stopRecording() {
        if let recorder = audioRecorder, recorder.isRecording {
            recorder.stop()
            LogManager.shared.log("AudioEngine: Recorder Stopped [Actor]")
        }
        audioRecorder = nil
        isRecording = false
    }
    
    func getLevels() -> Float {
        guard let recorder = audioRecorder, isRecording else { return -160.0 }
        recorder.updateMeters()
        return recorder.averagePower(forChannel: 0)
    }
    
    var isRunning: Bool {
        return isRecording
    }
}

// --- MAIN VIEW MODEL ---
@MainActor
class AudioRecorder: ObservableObject {
    static let shared = AudioRecorder()
    
    // Engine is isolated
    private let engine = AudioEngine()
    
    @Published var isRecording = false
    @Published var isProcessing = false
    @Published var hasError = false
    @Published var errorMessage: String?
    @Published var audioLevel: Float = -160.0
    
    private var timer: Timer?
    private var processingTask: Task<Void, Never>?
    
    // --- Device Management ---
    @Published var availableDevices: [Device] = []
    @Published var selectedDeviceID: String = "default" {
        didSet {
            UserDefaults.standard.set(selectedDeviceID, forKey: "selectedMicrophone")
            LogManager.shared.log("AudioRecorder: Selected microphone changed to: \(selectedDeviceID)")
        }
    }
    
    struct Device: Identifiable, Hashable {
        let id: String
        let name: String
    }
    
    init() {
        self.selectedDeviceID = UserDefaults.standard.string(forKey: "selectedMicrophone") ?? "default"
        
        Task.detached(priority: .background) {
            await self.fetchAudioDevices()
        }
        
        // Notifications
        NotificationCenter.default.addObserver(forName: NSNotification.Name("ToggleRecording"), object: nil, queue: .main) { _ in
            Task { @MainActor in AudioRecorder.shared.toggleRecording() }
        }
        NotificationCenter.default.addObserver(forName: NSNotification.Name("StartRecording"), object: nil, queue: .main) { _ in
            Task { @MainActor in AudioRecorder.shared.startRecording() }
        }
        NotificationCenter.default.addObserver(forName: NSNotification.Name("StopRecording"), object: nil, queue: .main) { _ in
            Task { @MainActor in AudioRecorder.shared.stopRecording() }
        }
    }
    
    func fetchAudioDevices() async {
        let discoverySession = AVCaptureDevice.DiscoverySession(
            deviceTypes: [.builtInMicrophone, .externalUnknown],
            mediaType: .audio,
            position: .unspecified
        )
        let devices = discoverySession.devices.map { Device(id: $0.uniqueID, name: $0.localizedName) }
        var finalDevices = devices
        finalDevices.insert(Device(id: "default", name: "System Default"), at: 0)
        
        await MainActor.run {
            self.availableDevices = finalDevices
        }
    }
    
    func toggleRecording() {
        if isProcessing {
            LogManager.shared.log("AudioRecorder: FORCE RESET TRIGGERED")
            processingTask?.cancel()
            isProcessing = false
            hasError = true
            errorMessage = "Processing cancelled"
            return
        }
        
        if isRecording {
            stopRecording()
        } else {
            startRecording()
        }
    }
    
    func startRecording() {
        guard !isRecording else { return }
        LogManager.shared.log("AudioRecorder: Starting... (Requesting Actor)")
        
        Task {
            // 1. Check Permissions
            let allowed = await engine.checkPermission()
            if !allowed {
                self.hasError = true
                self.errorMessage = "Mic Access Denied"
                LogManager.shared.log("AudioRecorder: Permission Denied")
                return
            }
            
            // 2. Start Engine
            do {
                let url = getFileURL()
                try await engine.startRecording(url: url)
                self.isRecording = true
                self.hasError = false
                self.errorMessage = nil
                self.startMonitoring()
                LogManager.shared.log("AudioRecorder: Recording Active")
            } catch {
                self.hasError = true
                self.errorMessage = "Mic Failed"
                LogManager.shared.log("AudioRecorder: Start Failed: \(error.localizedDescription)")
            }
        }
    }
    
    @Published var isPreviewing = false
    
    func startPreview() {
        // Sound check is handled passively
    }
    
    func stopPreview() {
        // Sound check is handled passively
    }
    
    func stopRecording() {
        guard isRecording else { return }
        LogManager.shared.log("AudioRecorder: Stopping...")
        
        // 1. Stop UI immediately
        stopMonitoring()
        isRecording = false
        isProcessing = true
        
        let postRollMs = UserDefaults.standard.object(forKey: "postRollDelayMs") == nil ? 300 : UserDefaults.standard.integer(forKey: "postRollDelayMs")
        
        // 2. Stop Hardware with Post-Roll Hangover Delay (Background)
        Task {
            if postRollMs > 0 {
                LogManager.shared.log("AudioRecorder: Applying Post-Roll padding (\(postRollMs)ms)...")
                try? await Task.sleep(nanoseconds: UInt64(postRollMs) * 1_000_000)
            }
            await engine.stopRecording()
            LogManager.shared.log("AudioRecorder: Engine Stopped.")
            LogManager.shared.log("AudioRecorder: Starting Processing.")
            await self.processAudioFile()
        }
    }
    
    private func startMonitoring() {
        timer?.invalidate()
        timer = Timer.scheduledTimer(withTimeInterval: 0.1, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            Task {
                let level = await self.engine.getLevels()
                await MainActor.run {
                    self.audioLevel = level
                }
            }
        }
    }
    
    private func stopMonitoring() {
        timer?.invalidate()
        timer = nil
        audioLevel = -160.0
    }
    
    private func getFileURL() -> URL {
        return FileManager.default.temporaryDirectory.appendingPathComponent("cosmo_recording.m4a")
    }
    
    func forceReset() {
        LogManager.shared.log("AudioRecorder: [FORCE RESET]")
        stopMonitoring()
        processingTask?.cancel()
        isRecording = false
        isProcessing = false
        hasError = false
        Task { await engine.stopRecording() }
        InputController.shared.isHotkeyDown = false
    }
    
    // --- TEXT PROCESSING PIPELINE ---
    private func processAudioFile() async {
        let fileURL = getFileURL()
        
        // Use a new Task to ensure it runs even if the calling Task finishes
        self.processingTask = Task {
            defer {
                self.isProcessing = false
                self.processingTask = nil
            }
            
            do {
                // Check File
                if !FileManager.default.fileExists(atPath: fileURL.path) {
                    throw NSError(domain: "Audio", code: 404, userInfo: [NSLocalizedDescriptionKey: "No Audio File"])
                }
                
                let attr = try FileManager.default.attributesOfItem(atPath: fileURL.path)
                let size = attr[.size] as? Int64 ?? 0
                if size < 500 {
                    LogManager.shared.log("AudioRecorder: File too small (\(size) bytes)")
                    return
                }
                
                // Quota Check for Online Cloud
                let engine = UserDefaults.standard.string(forKey: "transcriptionEngine") ?? "online"
                if engine == "online" && LicenseManager.shared.isOverQuota {
                    LogManager.shared.log("AudioRecorder: Free monthly quota reached (60 min/mo). Opening store.")
                    Task { @MainActor in
                        WindowManager.shared.showDashboard()
                        NotificationCenter.default.post(name: NSNotification.Name("OpenAccountTab"), object: nil)
                    }
                    return
                }
                
                LogManager.shared.log("AudioRecorder: Uploading...")
                
                // Track duration for usage reporting
                let audioAsset = AVURLAsset(url: fileURL)
                let durationSeconds = (try? await audioAsset.load(.duration).seconds) ?? 2.0
                let durationMs = max(500, Int(durationSeconds * 1000))
                
                // Transcribe
                let text = try await AIService.shared.transcribe(fileURL: fileURL)
                
                // Report Usage
                LicenseManager.shared.reportUsage(durationMs: durationMs)
                
                // Cleanup Text
                let cleaned = self.cleanText(text)
                
                // Validate
                if self.isGarbage(cleaned) {
                    LogManager.shared.log("AudioRecorder: Discarding garbage: \(cleaned)")
                    return
                }
                
                LogManager.shared.log("AudioRecorder: Got Text: \(cleaned)")
                
                // Command Check
                let wasCommand = await CommandController.shared.handle(text: cleaned)
                if !wasCommand {
                    var finalText = cleaned
                    
                    // AI Brain Check
                    let wordCount = cleaned.split(separator: " ").count
                    let enableGossip = UserDefaults.standard.bool(forKey: "enableGossip")
                    let isWriting = ContextManager.shared.currentCategory == .writing
                    
                    if wordCount >= 3 && enableGossip && !isWriting {
                        LogManager.shared.log("AudioRecorder: Sending to AI Brain...")
                        if let aiRes = try? await AIService.shared.process(text: cleaned), !aiRes.isEmpty {
                            finalText = aiRes
                        }
                    }
                    
                    // Ensure natural spacing for continuous dictation
                    if !finalText.isEmpty && !finalText.hasSuffix(" ") && !finalText.hasSuffix("\n") {
                        finalText += " "
                    }
                    
                    // Paste
                    try Task.checkCancellation()
                    LogManager.shared.log("AudioRecorder: Pasting...")
                    NotificationCenter.default.post(name: NSNotification.Name("InsertText"), object: finalText)
                    self.saveToRecentActivity(text: finalText)
                }
                
                // Cleanup File
                try? FileManager.default.removeItem(at: fileURL)
                let count = UserDefaults.standard.integer(forKey: "transcriptionCount")
                UserDefaults.standard.set(count + 1, forKey: "transcriptionCount")
                
            } catch {
                LogManager.shared.log("AudioRecorder Processing Error: \(error.localizedDescription)")
                self.hasError = true
                self.errorMessage = error.localizedDescription
            }
        }
        
        _ = await processingTask?.result
    }
    
    private func cleanText(_ input: String) -> String {
        var text = input
        
        // 1. Voice Formatting - Paragraphs Anywhere
        let paragraphPatterns = [
            "(?i)[,\\.\\?!;:]*\\s*\\b(?:new|next)\\s+paragraph\\b[,\\.\\?!;:]*\\s*",
            "(?i)[,\\.\\?!;:]*\\s*\\bparagraph\\s+break\\b[,\\.\\?!;:]*\\s*"
        ]
        for pattern in paragraphPatterns {
            if let regex = try? NSRegularExpression(pattern: pattern) {
                let range = NSRange(text.startIndex..<text.endIndex, in: text)
                text = regex.stringByReplacingMatches(in: text, options: [], range: range, withTemplate: "\n\n")
            }
        }
        
        // 2. Voice Formatting - New Lines Anywhere
        let linePatterns = [
            "(?i)[,\\.\\?!;:]*\\s*\\b(?:new|next)\\s+line\\b[,\\.\\?!;:]*\\s*",
            "(?i)[,\\.\\?!;:]*\\s*\\bline\\s+break\\b[,\\.\\?!;:]*\\s*"
        ]
        for pattern in linePatterns {
            if let regex = try? NSRegularExpression(pattern: pattern) {
                let range = NSRange(text.startIndex..<text.endIndex, in: text)
                text = regex.stringByReplacingMatches(in: text, options: [], range: range, withTemplate: "\n")
            }
        }
        
        // 3. Voice Formatting - Common Punctuation Spoken Tokens
        let punctuationReplacements: [(pattern: String, template: String)] = [
            ("(?i)\\s*\\bcomma\\b", ","),
            ("(?i)\\s*\\b(?:full stop|period)\\b", "."),
            ("(?i)\\s*\\bquestion mark\\b", "?"),
            ("(?i)\\s*\\b(?:exclamation mark|exclamation point)\\b", "!"),
            ("(?i)\\s*\\b(?:semicolon|semi-colon|semi colon)\\b", ";"),
            ("(?i)\\s*\\bcolon\\b", ":"),
            ("(?i)\\b(?:open quote|open quotation|start quote)\\b\\s*", "\""),
            ("(?i)\\s*\\b(?:close quote|close quotation|end quote)\\b", "\""),
            ("(?i)\\b(?:open parenthesis|open paren)\\b\\s*", "("),
            ("(?i)\\s*\\b(?:close parenthesis|close paren)\\b", ")")
        ]
        
        for (pattern, template) in punctuationReplacements {
            if let regex = try? NSRegularExpression(pattern: pattern) {
                let range = NSRange(text.startIndex..<text.endIndex, in: text)
                text = regex.stringByReplacingMatches(in: text, options: [], range: range, withTemplate: template)
            }
        }
        
        // 4. Clean spaces before/after line breaks
        text = text.replacingOccurrences(of: "[ \\t]+\\n", with: "\n", options: .regularExpression)
        text = text.replacingOccurrences(of: "\\n[ \\t]+", with: "\n", options: .regularExpression)
        
        // 5. Short sentence trailing punctuation trim (only if no line breaks)
        if !text.contains("\n") {
            let wordCount = text.trimmingCharacters(in: .whitespacesAndNewlines).split(separator: " ").count
            if wordCount < 4 {
                text = text.replacingOccurrences(of: "[\\.\\?!…]+[\\s]*$", with: "", options: .regularExpression)
            }
        }
        
        // 6. User Custom Vocabulary Replacements
        if let data = UserDefaults.standard.data(forKey: "replacementsJSON_v3"),
           let items = try? JSONDecoder().decode([ReplacementItem].self, from: data) {
            for item in items {
                let pattern = "(?i)\\b\(NSRegularExpression.escapedPattern(for: item.trigger))\\b"
                if let regex = try? NSRegularExpression(pattern: pattern) {
                    let range = NSRange(text.startIndex..<text.endIndex, in: text)
                    text = regex.stringByReplacingMatches(in: text, options: [], range: range, withTemplate: item.value)
                }
            }
        }
        
        return text
    }
    
    private func isGarbage(_ text: String) -> Bool {
        if text.isEmpty { return true }
        // Allow standalone newlines/paragraphs
        if text == "\n" || text == "\n\n" || text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            return false
        }
        
        let hallucinations = [
            "mbc", " дякую", "дякую!", "subtitles", "subtitle by", "watched by",
            "music", "bell", "sound", "bing", "ding", "chime"
        ]
        
        let low = text.lowercased().trimmingCharacters(in: .whitespacesAndNewlines)
        if hallucinations.contains(low) { return true }
        if text.components(separatedBy: "Bye").count > 3 { return true }
        if text.count < 3 && !text.contains(where: { $0.isNumber }) && !text.contains("\n") { return true }
        
        return false
    }
    
    private func saveToRecentActivity(text: String) {
        var items: [TranscriptionItem] = []
        if let data = UserDefaults.standard.data(forKey: "recentTranscriptions"),
           let decoded = try? JSONDecoder().decode([TranscriptionItem].self, from: data) {
            items = decoded
        }
        
        let newItem = TranscriptionItem(text: text, date: Date(), type: .transcription)
        items.insert(newItem, at: 0)
        if items.count > 20 { items = Array(items.prefix(20)) }
        
        if let encoded = try? JSONEncoder().encode(items) {
            UserDefaults.standard.set(encoded, forKey: "recentTranscriptions")
            NotificationCenter.default.post(name: NSNotification.Name("RecentActivityChanged"), object: nil)
        }
    }
    
    func clearRecentActivity() {
        LogManager.shared.log("AudioRecorder: Clearing Gossip Log...")
        UserDefaults.standard.removeObject(forKey: "recentTranscriptions")
        NotificationCenter.default.post(name: NSNotification.Name("RecentActivityChanged"), object: nil)
    }
}
