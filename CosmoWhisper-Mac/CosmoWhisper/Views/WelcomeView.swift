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
        OnboardingItem(
            title: "Ultra-Fast Transcription",
            subtitle: "Talk, don't type. Convert your thoughts to text instantly with near-perfect accuracy using Groq-powered AI.",
            icon: "waveform.circle.fill",
            color: Color.blue
        ),
        OnboardingItem(
            title: "AI Spells Everywhere",
            subtitle: "Transform text in any app. Bold highlights, translate languages, or fix grammar with just your voice.",
            icon: "sparkles",
            color: Color.purple
        ),
        OnboardingItem(
            title: "Deep Integration",
            subtitle: "Works where you do. Seamlessly send messages, draft emails, and control your system without leaving your current workspace.",
            icon: "link",
            color: Color.orange
        ),
        OnboardingItem(
            title: "Private & Secure",
            subtitle: "Your voice, your privacy. Everything stays on your Mac, protected by optional AES-256 encrypted backups.",
            icon: "lock.shield.fill",
            color: Color.green
        )
    ]
    
    var body: some View {
        ZStack {
            // Cosmic Background
            CosmicBackground()
                .ignoresSafeArea()
            
            VStack(spacing: 40) {
                // Header
                HStack {
                    Spacer()
                    Button("Skip") {
                        withAnimation {
                            hasCompletedOnboarding = true
                        }
                    }
                    .buttonStyle(.plain)
                    .foregroundColor(.white.opacity(0.6))
                    .font(.system(size: 14, weight: .medium))
                    .padding(20)
                }
                
                Spacer()
                
                // Content
                VStack(spacing: 30) {
                    // Icon with glow
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
                            .font(.system(size: 32, weight: .black))
                            .multilineTextAlignment(.center)
                        
                        Text(items[currentPage].subtitle)
                            .font(.system(size: 18))
                            .multilineTextAlignment(.center)
                            .foregroundColor(.white.opacity(0.7))
                            .padding(.horizontal, 40)
                            .frame(maxWidth: 500)
                    }
                }
                .transition(.asymmetric(
                    insertion: .move(edge: .trailing).combined(with: .opacity),
                    removal: .move(edge: .leading).combined(with: .opacity)
                ))
                .id(currentPage)
                
                Spacer()
                
                // Pagination Dots
                HStack(spacing: 8) {
                    ForEach(0..<items.count, id: \.self) { index in
                        Capsule()
                            .fill(index == currentPage ? items[currentPage].color : Color.white.opacity(0.2))
                            .frame(width: index == currentPage ? 24 : 8, height: 8)
                            .animation(.spring(), value: currentPage)
                    }
                }
                
                // Footer Button
                Button(action: {
                    if currentPage < items.count - 1 {
                        withAnimation(.spring(response: 0.5, dampingFraction: 0.8)) {
                            currentPage += 1
                        }
                    } else {
                        withAnimation {
                            hasCompletedOnboarding = true
                        }
                    }
                }) {
                    Text(currentPage == items.count - 1 ? "Get Started" : "Next")
                        .font(.system(size: 18, weight: .bold))
                        .foregroundColor(.white)
                        .padding(.horizontal, 60)
                        .padding(.vertical, 16)
                        .background(items[currentPage].color)
                        .cornerRadius(30)
                        .shadow(color: items[currentPage].color.opacity(0.4), radius: 20)
                }
                .buttonStyle(.plain)
                .padding(.bottom, 60)
            }
        }
    }
}
