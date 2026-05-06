import SwiftUI

struct HeroCard: View {
    let emoji: String
    let title: String
    let desc: String

    @ObservedObject var theme = ThemeManager.shared
    @State private var isHovered = false

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text(emoji)
                .font(.system(size: 28))
                .padding(8)
                .background(theme.backgroundElevated)
                .cornerRadius(10)

            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.system(size: 15, weight: .bold))
                    .foregroundColor(theme.textPrimary)

                Text(desc)
                    .font(.system(size: 12))
                    .foregroundColor(theme.textSecondary)
                    .lineLimit(2)
            }
        }
        .padding(16)
        .frame(width: 240)
        .background(theme.backgroundSurface)
        .cornerRadius(12)
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(isHovered ? theme.currentTheme.accent.opacity(0.4) : theme.borderColor, lineWidth: 1)
        )
        .onHover { hovering in
            withAnimation(.easeInOut(duration: 0.2)) {
                isHovered = hovering
            }
        }
    }
}
