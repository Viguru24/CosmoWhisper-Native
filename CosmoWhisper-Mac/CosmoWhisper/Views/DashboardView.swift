import SwiftUI
import AppKit

struct DashboardView: View {
    @State private var isAccessibilityTrusted = false
    @State private var selectedTab = "dashboard"
    @ObservedObject var theme = ThemeManager.shared
    @ObservedObject var context = ContextManager.shared
    @AppStorage("hasCompletedOnboarding") private var hasCompletedOnboarding = false

    var body: some View {
        HStack(spacing: 0) {
            VStack(alignment: .leading, spacing: 20) {
                appHeader

                ScrollView {
                    VStack(alignment: .leading, spacing: 24) {
                        sidebarSection(title: "EXPLORE") {
                            SidebarItem(title: "Overview", icon: "square.grid.2x2", isSelected: selectedTab == "dashboard", action: { selectedTab = "dashboard" })
                            SidebarItem(title: "Commands", icon: "command", isSelected: selectedTab == "commands", action: { selectedTab = "commands" })
                            SidebarItem(title: "Library", icon: "books.vertical.fill", isSelected: selectedTab == "library", action: { selectedTab = "library" })
                        }

                        sidebarSection(title: "AI LAB") {
                            SidebarItem(title: "Intelligence", icon: "bolt.fill", isSelected: selectedTab == "intelligence", action: { selectedTab = "intelligence" })
                            SidebarItem(title: "Vocabulary", icon: "character.book.closed", isSelected: selectedTab == "vocab", action: { selectedTab = "vocab" })
                            SidebarItem(title: "Narration", icon: "speaker.wave.2", isSelected: selectedTab == "narration", action: { selectedTab = "narration" })
                        }

                        sidebarSection(title: "PREFERENCES") {
                            SidebarItem(title: "Settings", icon: "gearshape.fill", isSelected: selectedTab == "settings", action: { selectedTab = "settings" })
                            SidebarItem(title: "Language", icon: "globe", isSelected: selectedTab == "language", action: { selectedTab = "language" })
                            SidebarItem(title: "Account", icon: "person.circle", isSelected: selectedTab == "account", action: { selectedTab = "account" })
                            SidebarItem(title: "Logs", icon: "doc.text.magnifyingglass", isSelected: selectedTab == "logs", action: { selectedTab = "logs" })
                        }
                    }
                }

                Spacer()

                VStack(spacing: 12) {
                    contextStatus
                    themeToggle
                }
            }
            .frame(width: 240)
            .padding(.vertical, 24)
            .background(theme.backgroundSurface.opacity(0.8))

            ZStack {
                theme.currentTheme.backgroundPrimary.ignoresSafeArea()

                ScrollView {
                    VStack(alignment: .leading, spacing: 24) {
                        switch selectedTab {
                        case "dashboard": OverviewView(isAccessibilityTrusted: $isAccessibilityTrusted).transition(.opacity.combined(with: .move(edge: .bottom)))
                        case "commands": CommandsView().transition(.opacity)
                        case "library": LibraryView().transition(.opacity)
                        case "vocab": VocabularyView().transition(.opacity)
                        case "narration": NarrationView().transition(.opacity)
                        case "intelligence": IntelligenceView().transition(.opacity)
                        case "settings": SettingsView(isAccessibilityTrusted: $isAccessibilityTrusted).transition(.opacity)
                        case "language": LanguageView().transition(.opacity)
                        case "account": AccountView().transition(.opacity)
                        case "logs": LogsView().transition(.opacity)
                        default: Text("Coming Soon").foregroundColor(theme.textSecondary)
                        }
                    }
                    .padding(.top, 48)
                    .padding(.horizontal, 32)
                    .padding(.bottom, 24)
                    .animation(.spring(response: 0.4, dampingFraction: 0.8), value: selectedTab)
                }
            }
        }

        if !hasCompletedOnboarding {
            WelcomeView()
                .transition(.opacity.combined(with: .scale))
                .zIndex(100)
        }
    }
}

extension DashboardView {
    @ViewBuilder
    private func sidebarSection<Content: View>(title: String, @ViewBuilder content: () -> Content) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            Text(title)
                .font(.system(size: 10, weight: .black))
                .foregroundColor(theme.currentTheme.accent.opacity(0.8))
                .padding(.horizontal, 16)
                .padding(.top, 12)
                .tracking(1.5)
            content()
        }
    }

    private var appHeader: some View {
        HStack(spacing: 12) {
            ZStack {
                theme.accentGradient
                    .frame(width: 48, height: 48)
                    .cornerRadius(14)

                Image(nsImage: NSApp.applicationIconImage ?? NSImage())
                    .resizable()
                    .frame(width: 34, height: 34)
                    .brightness(-0.1)
            }
            .shadow(color: theme.currentTheme.accent.opacity(0.3), radius: 10, x: 0, y: 5)

            VStack(alignment: .leading, spacing: 0) {
                HStack(alignment: .lastTextBaseline, spacing: 6) {
                    Text("COSMO")
                        .font(.system(size: 18, weight: .black))
                        .tracking(1.5)
                        .foregroundColor(.white) +
                    Text(" WHISPER")
                        .font(.system(size: 18, weight: .bold))
                        .foregroundColor(theme.currentTheme.accent)

                    Text("v" + (Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0"))
                        .font(.system(size: 10, weight: .bold, design: .monospaced))
                        .foregroundColor(.white.opacity(0.4))
                }

                Text("PRO EDITION")
                    .font(.system(size: 9, weight: .black))
                    .foregroundColor(theme.currentTheme.accent.opacity(0.7))
                    .tracking(2)
            }
        }
        .padding(.horizontal, 20)
        .padding(.bottom, 12)
    }

    private var contextStatus: some View {
        HStack(spacing: 8) {
            ZStack {
                Circle()
                    .fill(theme.currentTheme.accent.opacity(0.1))
                    .frame(width: 32, height: 32)

                Image(systemName: context.currentCategory.icon)
                    .font(.system(size: 14))
                    .foregroundColor(theme.currentTheme.accent)
            }

            VStack(alignment: .leading, spacing: 2) {
                Text(context.currentCategory.rawValue.uppercased())
                    .font(.system(size: 9, weight: .black))
                    .tracking(1)
                    .foregroundColor(theme.currentTheme.accent)

                Text(context.currentApp)
                    .font(.system(size: 11, weight: .medium))
                    .foregroundColor(.white.opacity(0.6))
                    .lineLimit(1)
            }
        }
        .padding(.horizontal, 20)
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(.bottom, 4)
    }

    private var themeToggle: some View {
        HStack {
            Text("THEME")
                .font(.system(size: 10, weight: .black))
                .foregroundColor(.secondary.opacity(0.6))
                .tracking(1)
            Spacer()

            HStack(spacing: 10) {
                ForEach(AppTheme.allCases) { t in
                    Circle()
                        .fill(t.accent)
                        .frame(width: 16, height: 16)
                        .overlay(
                            Circle()
                                .stroke(Color.white, lineWidth: theme.currentTheme == t ? 2 : 0)
                        )
                        .shadow(color: t.accent.opacity(0.3), radius: 4)
                        .onTapGesture {
                            withAnimation(.spring()) {
                                theme.currentTheme = t
                            }
                        }
                }
            }
        }
        .padding(.horizontal, 20)
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}
