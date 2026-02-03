import SwiftUI

struct SidebarItem: View {
    let title: String
    let icon: String
    let isSelected: Bool
    let isCompact: Bool // Added for Phase 2
    let action: () -> Void
    
    @ObservedObject var theme = ThemeManager.shared
    @ObservedObject var recorder = AudioRecorder.shared
    @State private var isHovered = false
    @State private var pulse = 1.0
    
    var body: some View {
        HStack(spacing: isCompact ? 0 : 14) {
            ZStack {
                if icon == "mic" && recorder.isRecording {
                    Circle()
                        .fill(Color.red.opacity(0.3))
                        .frame(width: 30, height: 30)
                        .scaleEffect(pulse)
                        .onAppear {
                            withAnimation(.easeInOut(duration: 0.8).repeatForever(autoreverses: true)) {
                                pulse = 1.4
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
            
            if !isCompact {
                Text(title)
                    .font(.system(size: 14, weight: isSelected ? .bold : .medium))
                
                Spacer()
                
                if isSelected {
                    theme.accentGradient
                        .frame(width: 3, height: 16)
                        .cornerRadius(2)
                }
            }
        }
        .padding(.horizontal, isCompact ? 8 : 16)
        .padding(.vertical, 10)
        .frame(maxWidth: .infinity, alignment: isCompact ? .center : .leading)
        .background(
            ZStack {
                if isSelected {
                    theme.currentTheme.accent.opacity(0.15)
                    RoundedRectangle(cornerRadius: 12)
                        .stroke(theme.currentTheme.accent.opacity(0.3), lineWidth: 1)
                } else if isHovered {
                    Color.white.opacity(0.08)
                }
            }
        )
        .foregroundColor(isSelected ? .white : (isHovered ? .white : .secondary))
        .cornerRadius(12)
        .shadow(color: isSelected ? theme.currentTheme.accent.opacity(0.2) : .clear, radius: 8, x: 0, y: 4)
        .contentShape(Rectangle())
        .onHover { hovering in
            withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                isHovered = hovering
            }
        }
        .onTapGesture {
            action()
        }
        .scaleEffect(isHovered ? 1.02 : 1.0)
        .help(isCompact ? title : "") // Tooltip in compact mode
    }
}
