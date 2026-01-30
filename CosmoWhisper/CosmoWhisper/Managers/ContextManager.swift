import Foundation
import AppKit
import Combine

public enum ContextCategory: String, CaseIterable {
    case general = "General"
    case coding = "Coding"
    case writing = "Writing & Docs"
    case communication = "Communication"
    case presentation = "Presentation"
    
    var icon: String {
        switch self {
        case .general: return "square.stack.3d.up.fill"
        case .coding: return "curlybraces"
        case .writing: return "doc.text.fill"
        case .communication: return "bubble.left.and.bubble.right.fill"
        case .presentation: return "play.display"
        }
    }
    
    var instructions: String {
        switch self {
        case .general:
            return ""
        case .coding:
            return "CONTEXT: User is in a code editor. Ensure technical terms are spelled correctly. Use PascalCase or camelCase for identifiers if appropriate. Prefer Markdown for snippets."
        case .writing:
            return "CONTEXT: User is writing a document. Focus on formal grammar, varied vocabulary, and professional tone. Use proper heading structures if implied."
        case .communication:
            return "CONTEXT: User is messaging. Keep it natural, conversational, and concise. Use emojis sparingly and only if it fits the tone."
        case .presentation:
            return "CONTEXT: User is creating a presentation. Use punchy, high-impact language and bullet points. Focus on clarity and brevity."
        }
    }
}

public class ContextManager: ObservableObject {
    public static let shared = ContextManager()
    
    @Published public var currentApp: String = "Unknown"
    @Published public var currentCategory: ContextCategory = .general
    
    private var cancellables = Set<AnyCancellable>()
    
    // Mapping from bundle identifier prefix or name to category
    private let appMappings: [String: ContextCategory] = [
        "com.apple.dt.Xcode": .coding,
        "com.microsoft.VSCode": .coding,
        "com.sublimetext": .coding,
        "jetbrains": .coding,
        "com.microsoft.Word": .writing,
        "com.apple.iWork.Pages": .writing,
        "com.apple.Notes": .writing,
        "com.google.Chrome": .general, // Often general, but could be specific URLs
        "com.apple.Safari": .general,
        "com.apple.iChat": .communication, // Messages
        "com.microsoft.PowerPoint": .presentation,
        "com.apple.iWork.Keynote": .presentation,
        "com.apple.TextEdit": .writing,
        "com.google.android.Notepad": .writing // Common name
    ]
    
    private init() {
        startMonitoring()
        updateCurrentApp()
    }
    
    private func startMonitoring() {
        NSWorkspace.shared.notificationCenter.publisher(for: NSWorkspace.didActivateApplicationNotification)
            .sink { [weak self] notification in
                self?.updateCurrentApp()
            }
            .store(in: &cancellables)
    }
    
    public func updateCurrentApp() {
        guard let frontApp = NSWorkspace.shared.frontmostApplication else { return }
        
        let name = frontApp.localizedName ?? "Unknown"
        let bundleId = frontApp.bundleIdentifier ?? ""
        
        DispatchQueue.main.async {
            self.currentApp = name
            self.currentCategory = self.determineCategory(name: name, bundleId: bundleId)
            LogManager.shared.log("Context: App changed to \(name) (\(bundleId)) -> Category: \(self.currentCategory)")
        }
    }
    
    private func determineCategory(name: String, bundleId: String) -> ContextCategory {
        // 1. Check direct bundle ID mapping
        if let category = appMappings[bundleId] { return category }
        
        // 2. Check prefix/keywords in bundle ID
        for (key, category) in appMappings {
            if bundleId.lowercased().contains(key.lowercased()) {
                return category
            }
        }
        
        // 3. Check name keywords
        let lowerName = name.lowercased()
        if lowerName.contains("code") || lowerName.contains("studio") || lowerName.contains("xcode") {
            return .coding
        }
        if lowerName.contains("word") || lowerName.contains("pages") || lowerName.contains("text") {
            return .writing
        }
        if lowerName.contains("whatsapp") || lowerName.contains("slack") || lowerName.contains("discord") || lowerName.contains("messenger") {
            return .communication
        }
        
        return .general
    }
}
