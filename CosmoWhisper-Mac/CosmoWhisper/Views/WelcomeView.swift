import SwiftUI

struct OnboardingItem: Identifiable {
    let id = UUID()
    let title: String
    let subtitle: String
    let icon: String
    let color: Color
}

struct WelcomeView: View {
    @AppStorage("hasCompletedOnboarding") private var hasCompletedOnboarding = false
    @State private var currentPage = 0
    @ObservedObject var theme = ThemeManager.shared

    let items = [
        OnboardingItem(title: "Ultra-Fast Transcription", subtitle: "Talk, don't type. Convert your thoughts to text instantly.", icon: "waveform.circle.fill", color: .blue),
        OnboardingItem(title: "AI Spells Everywhere", subtitle: "Transform text in any app with just your voice.", icon: "sparkles", color: .purple),
        OnboardingItem(title: "Deep Integration", subtitle: "Works where you do. Seamlessly send messages and draft emails.", icon: "link", color: .orange),
        OnboardingItem(title: "Private & Secure", subtitle: "Your voice, your privacy. Everything stays on your Mac.", icon: "lock.shield.fill", color: .green)
    ]

    var body: some View {
        ZStack {
            theme.currentTheme.backgroundPrimary.ignoresSafeArea()

            VStack(spacing: 40) {
                HStack {
                    Spacer()
                    Button("Skip") {
                        withAnimation { hasCompletedOnboarding = true }
                    }
                    .buttonStyle(.plain)
                    .foregroundColor(theme.textSecondary)
                    .font(.system(size: 14, weight: .medium))
                    .padding(20)
                }

                Spacer()

                VStack(spacing: 30) {
                    ZStack {
                        Circle()
                            .fill(items[currentPage].color.opacity(0.2))
                            .frame(width: 120, height: 120)
                            .blur(radius: 20)

                        Image(systemName: items[currentPage].icon)
                            .font(.system(size: 60))
                            .foregroundColor(items[currentPage].color)
                    }

                    VStack(spacing: 16) {
                        Text(items[currentPage].title)
                            .font(.system(size: 28, weight: .bold))
                            .multilineTextAlignment(.center)
                            .foregroundColor(theme.textPrimary)

                        Text(items[currentPage].subtitle)
                            .font(.system(size: 16))
                            .multilineTextAlignment(.center)
                            .foregroundColor(theme.textSecondary)
                            .padding(.horizontal, 40)
                            .frame(maxWidth: 500)
                    }
                }
                .transition(.asymmetric(insertion: .move(edge: .trailing).combined(with: .opacity), removal: .move(edge: .leading).combined(with: .opacity)))
                .id(currentPage)

                Spacer()

                HStack(spacing: 8) {
                    ForEach(0..<items.count, id: \.self) { index in
                        Capsule()
                            .fill(index == currentPage ? items[currentPage].color : Color.white.opacity(0.2))
                            .frame(width: index == currentPage ? 24 : 8, height: 8)
                            .animation(.spring(), value: currentPage)
                    }
                }

                Button(action: {
                    if currentPage < items.count - 1 {
                        withAnimation(.spring(response: 0.5, dampingFraction: 0.8)) {
                            currentPage += 1
                        }
                    } else {
                        withAnimation { hasCompletedOnboarding = true }
                    }
                }) {
                    Text(currentPage == items.count - 1 ? "Get Started" : "Next")
                        .font(.system(size: 16, weight: .bold))
                        .foregroundColor(.white)
                        .padding(.horizontal, 48)
                        .padding(.vertical, 12)
                        .background(items[currentPage].color)
                        .cornerRadius(24)
                }
                .buttonStyle(.plain)
                .padding(.bottom, 40)
            }
        }
    }
}
