import SwiftUI

enum AppTheme: String, CaseIterable, Identifiable {
    case cosmos = "Cosmos"
    case emerald = "Emerald"
    case monochrome = "Monochrome"
    
    var id: String { self.rawValue }
    
    var accent: Color {\n        switch self {\n        case .cosmos: return Color(red: 0.23, green: 0.51, blue: 0.97) // Vibrant Blue #3B82F6\n        case .emerald: return Color(red: 0.16, green: 0.73, blue: 0.51) // Emerald Green #10B981\n        case .monochrome: return Color.white\n        }\n    }
    }
    
    var secondaryAccent: Color {\n        switch self {\n        case .cosmos: return Color(red: 0.55, green: 0.35, blue: 0.96) // Muted Purple #8B5CF6\n        case .emerald: return Color(red: 0.04, green: 0.84, blue: 0.65) // Teal #06D6A0\n        case .monochrome: return Color(white: 0.6)\n        }\n    }
    }
    
    var backgroundColors: [Color] {
        switch self {
        case .cosmos: 
            return [
                Color(red: 0.05, green: 0.08, blue: 0.15), // Deep Navy
                Color(red: 0.1, green: 0.05, blue: 0.2),   // Dark Purple
                Color(red: 0.02, green: 0.1, blue: 0.15)  // Deep Teal
            ]
        case .emerald:
            return [
                Color(red: 0.02, green: 0.1, blue: 0.05),
                Color(red: 0.05, green: 0.15, blue: 0.1),
                Color(red: 0.01, green: 0.05, blue: 0.02)
            ]
        case .monochrome:
            return [
                Color.black,
                Color(white: 0.05),
                Color(white: 0.1)
            ]
        }
    }
}

class ThemeManager: ObservableObject {
    static let shared = ThemeManager()
    
    @AppStorage("selectedTheme") var currentTheme: AppTheme = .cosmos
    
    var accentGradient: LinearGradient {
        LinearGradient(
            gradient: Gradient(colors: [currentTheme.accent, currentTheme.secondaryAccent]),
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }
    
    var glassBackground: Color {
        Color.black.opacity(0.15)
    }
    
    var glassBorder: Color {
        Color.white.opacity(0.1)
    }
}


