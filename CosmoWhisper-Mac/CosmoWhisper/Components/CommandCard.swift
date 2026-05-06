import SwiftUI

struct CommandCard: View {
    let title: String
    let icon: String
    let triggers: [String]
    let desc: String

    @ObservedObject var theme = ThemeManager.shared
    @State private var isHovered = false

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack(alignment: .top) {
                Image(systemName: icon)
                    .font(.system(size: 14, weight: .bold))
                    .foregroundColor(.white)
                    .frame(width: 32, height: 32)
                    .background(theme.accentGradient)
                    .cornerRadius(8)

                Text(title)
                    .font(.system(size: 14, weight: .bold))
                    .foregroundColor(theme.textPrimary)

                Spacer()
            }

            FlowLayout(spacing: 4) {
                ForEach(triggers, id: \.self) { trigger in
                    Text("\"\(trigger)\"")
                        .font(.system(size: 10, weight: .medium, design: .monospaced))
                        .foregroundColor(theme.currentTheme.accent)
                        .padding(.horizontal, 6)
                        .padding(.vertical, 3)
                        .background(theme.backgroundElevated)
                        .cornerRadius(4)
                }
            }

            Text(desc)
                .font(.system(size: 11))
                .foregroundColor(theme.textSecondary)
                .lineLimit(2)

            Spacer()
        }
        .padding(14)
        .frame(height: 150)
        .frame(maxWidth: .infinity)
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
