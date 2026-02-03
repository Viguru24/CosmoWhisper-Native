import SwiftUI

struct StatCard: View {
    let title: String
    let value: String
    let icon: String
    let color: Color
    
    @ObservedObject var theme = ThemeManager.shared
    @State private var isHovered = false
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Image(systemName: icon)
                    .foregroundColor(isHovered ? theme.currentTheme.accent : color)
                    .font(.system(size: 14, weight: .bold))
                Spacer()
            }
            
            VStack(alignment: .leading, spacing: 4) {
                Text(value)
                    .font(.system(size: 24, weight: .black))
                    .foregroundColor(.white)
                Text(title)
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary)
            }
        }
        .padding(20)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(
            ZStack {
                RoundedRectangle(cornerRadius: 16)
                    .fill(Color.white.opacity(isHovered ? 0.08 : 0.04))
                
                // Content Blur
                VisualEffectView(material: .contentBackground, blendingMode: .withinWindow)
                    .clipShape(RoundedRectangle(cornerRadius: 16))
                    .opacity(0.1)
            }
        )
        .overlay(
            RoundedRectangle(cornerRadius: 16)
                .stroke(isHovered ? theme.currentTheme.accent.opacity(0.4) : Color.white.opacity(0.1), lineWidth: 1)
        )
        .shadow(color: isHovered ? theme.currentTheme.accent.opacity(0.15) : .clear, radius: 10, x: 0, y: 5)
        .onHover { hovering in
            withAnimation(.easeInOut(duration: 0.2)) {
                isHovered = hovering
            }
        }
    }
}
