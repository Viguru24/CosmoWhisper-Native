import SwiftUI

struct CommandCard: View {
    let title: String
    let icon: String
    let triggers: [String]
    let desc: String
    
    @ObservedObject var theme = ThemeManager.shared
    @State private var isHovered = false
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(alignment: .top) {
                Image(systemName: icon)
                    .font(.system(size: 16, weight: .bold))
                    .foregroundColor(isHovered ? .white : theme.currentTheme.accent)
                    .frame(width: 36, height: 36)
                    .background(isHovered ? theme.accentGradient : LinearGradient(gradient: Gradient(colors: [theme.currentTheme.accent.opacity(0.15), theme.currentTheme.accent.opacity(0.05)]), startPoint: .top, endPoint: .bottom))
                    .cornerRadius(8)
                
                Text(title)
                    .font(.system(size: 16, weight: .bold))
                    .foregroundColor(.white)
                
                Spacer()
            }
            
            // Triggers
            FlowLayout(spacing: 6) {
                ForEach(triggers, id: \.self) { trigger in
                    Text("\"\(trigger)\"")
                        .font(.system(size: 10, weight: .bold, design: .monospaced))
                        .foregroundColor(theme.currentTheme.accent)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(Color.black.opacity(0.3))
                        .cornerRadius(6)
                        .overlay(RoundedRectangle(cornerRadius: 6).stroke(theme.currentTheme.accent.opacity(0.4), lineWidth: 1))
                }
            }
            
            Text(desc)
                .font(.system(size: 12))
                .foregroundColor(.white.opacity(0.6))
                .fixedSize(horizontal: false, vertical: true)
                .lineLimit(3)
            
            Spacer()
        }
        .padding(18)
        .frame(height: 190) 
        .frame(maxWidth: .infinity)
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
                .stroke(isHovered ? theme.currentTheme.accent.opacity(0.5) : Color.white.opacity(0.1), lineWidth: 1)
        )
        .shadow(color: isHovered ? theme.currentTheme.accent.opacity(0.2) : .clear, radius: 10, x: 0, y: 5)
        .onHover { hovering in
            withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                isHovered = hovering
            }
        }
        .scaleEffect(isHovered ? 1.02 : 1.0)
    }
}
