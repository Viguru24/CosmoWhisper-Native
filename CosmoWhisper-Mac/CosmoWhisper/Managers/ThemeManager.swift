import SwiftUI

enum AppTheme: String, CaseIterable, Identifiable {
    case cosmos = "Cosmos"
    case emerald = "Emerald"
    case monochrome = "Monochrome"
    
    var id: String { self.rawValue }
    
    var accent: Color {
        switch self {
        case .cosmos: return Color(red: 0.4, green: 0.5, blue: 0.9) // Vibrant cyan/teal
        case .emerald: return Color.green
        case .monochrome: return Color.white
        }
    }
    
    var secondaryAccent: Color {
        switch self {
        case .cosmos: return Color.purple
        case .emerald: return Color.teal
        case .monochrome: return Color.gray
        }
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
