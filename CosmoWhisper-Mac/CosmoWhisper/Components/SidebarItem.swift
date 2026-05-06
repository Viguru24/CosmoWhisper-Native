import SwiftUI

struct SidebarItem: View {
    let title: String
    let icon: String
    let isSelected: Bool
    let action: () -> Void

    @ObservedObject var theme = ThemeManager.shared
    @ObservedObject var recorder = AudioRecorder.shared
    @State private var isHovered = false

    var body: some View {
        HStack(spacing: 14) {
            ZStack {
                if icon == "mic" && recorder.isRecording {
                    Circle()
                        .fill(Color.red.opacity(0.3))
                        .frame(width: 30, height: 30)
                        .onAppear {
                            withAnimation(.easeInOut(duration: 0.8).repeatForever(autoreverses: true)) {
                            }
                        }
                }

                Image(systemName: icon)
                    .font(.system(size: 16, weight: isSelected ? .bold : .medium))
                    .symbolVariant(isSelected ? .fill : .none)
                    .frame(width: 24, height: 24)
                    .foregroundColor(icon == "mic" && recorder.isRecording ? .red : (isSelected ? theme.currentTheme.accent : (isHovered ? .white : .secondary)))
            }
            .frame(width: 24, height: 24)

            Text(title)
                .font(.system(size: 13, weight: isSelected ? .bold : .medium))

            Spacer()

            if isSelected {
                theme.accentGradient
                    .frame(width: 3, height: 16)
                    .cornerRadius(2)
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(
            ZStack {
                if isSelected {
                    theme.currentTheme.accent.opacity(0.15)
                    RoundedRectangle(cornerRadius: 10)
                        .stroke(theme.currentTheme.accent.opacity(0.3), lineWidth: 1)
                } else if isHovered {
                    Color.white.opacity(0.05)
                }
            }
        )
        .foregroundColor(isSelected ? .white : (isHovered ? .white : .secondary))
        .cornerRadius(10)
        .contentShape(Rectangle())
        .onHover { hovering in
            withAnimation(.easeInOut(duration: 0.2)) {
                isHovered = hovering
            }
        }
        .onTapGesture {
            action()
        }
    }
}
