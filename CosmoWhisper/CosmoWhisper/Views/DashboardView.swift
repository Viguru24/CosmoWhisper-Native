import SwiftUI
import AppKit

struct DashboardView: View {
    @State private var isAccessibilityTrusted = false
    @State private var selectedTab = "dashboard"
    @ObservedObject var theme = ThemeManager.shared
    @ObservedObject var context = ContextManager.shared
    @AppStorage("isSidebarCollapsed") private var isSidebarCollapsed = false
    @AppStorage("hasCompletedOnboarding") private var hasCompletedOnboarding = false

    
    let contentBackground = Color(red: 10/255, green: 14/255, blue: 24/255)
    
    var body: some View {
        ZStack {
            HStack(spacing: 0) {
            // MARK: - Sidebar
            VStack(alignment: .leading, spacing: 20) {
                appHeader
                
                ScrollView {
                    VStack(alignment: .leading, spacing: 32) {
                        // EXPLORE GROUP
                        sidebarSection(title: "EXPLORE") {
                            SidebarItem(title: "Overview", icon: "square.grid.2x2", isSelected: selectedTab == "dashboard", isCompact: isSidebarCollapsed) { selectedTab = "dashboard" }
                            // SidebarItem(title: "History", icon: "clock.arrow.circlepath", isSelected: selectedTab == "history", isCompact: isSidebarCollapsed) { selectedTab = "history" }
                            SidebarItem(title: "Spells", icon: "command", isSelected: selectedTab == "commands", isCompact: isSidebarCollapsed) { selectedTab = "commands" }
                        }
                        
                        // AI LAB GROUP
                        sidebarSection(title: "AI LAB") {
                            SidebarItem(title: "Intelligence", icon: "bolt.fill", isSelected: selectedTab == "intelligence", isCompact: isSidebarCollapsed) { selectedTab = "intelligence" }
                            SidebarItem(title: "Vocabulary", icon: "character.book.closed", isSelected: selectedTab == "vocab", isCompact: isSidebarCollapsed) { selectedTab = "vocab" }
                            SidebarItem(title: "Narration", icon: "speaker.wave.2", isSelected: selectedTab == "narration", isCompact: isSidebarCollapsed) { selectedTab = "narration" }
                        }
                        
                        
                        sidebarSection(title: "PREFERENCES") {
                            SidebarItem(title: "Account", icon: "person.crop.circle.fill", isSelected: selectedTab == "account", isCompact: isSidebarCollapsed) { selectedTab = "account" }
                            SidebarItem(title: "Settings", icon: "gearshape.fill", isSelected: selectedTab == "settings", isCompact: isSidebarCollapsed) { selectedTab = "settings" }
                        }
                    }
                }
                
                Spacer()
                
                // BOTTOM CONTROLS
                VStack(spacing: 12) {
                    contextStatus
                    themeToggle
                    
                    Button(action: {
                        withAnimation(.spring(response: 0.4, dampingFraction: 0.8)) {
                            isSidebarCollapsed.toggle()
                        }
                    }) {
                        HStack {
                            Image(systemName: isSidebarCollapsed ? "sidebar.right" : "sidebar.left")
                            if !isSidebarCollapsed {
                                Text("Collapse Sidebar")
                                    .font(.system(size: 11, weight: .medium))
                            }
                        }
                        .foregroundColor(.secondary)
                        .padding(.vertical, 8)
                        .frame(maxWidth: .infinity, alignment: isSidebarCollapsed ? .center : .leading)
                        .padding(.horizontal, isSidebarCollapsed ? 0 : 12)
                    }
                    .buttonStyle(.plain)
                }
            }
            .frame(width: isSidebarCollapsed ? 80 : 250)
            .padding(.vertical, 24)
            .background(VisualEffectView(material: .sidebar, blendingMode: .behindWindow))
            
            // MARK: - Main Content
            ZStack {
                CosmicBackground()
                
                ScrollView {
                    VStack(alignment: .leading, spacing: 32) {
                        switch selectedTab {
                        case "dashboard": OverviewView(isAccessibilityTrusted: $isAccessibilityTrusted).transition(.asymmetric(insertion: .move(edge: .bottom).combined(with: .opacity), removal: .opacity))
                        case "history": HistoryView().transition(.opacity.combined(with: .scale(scale: 0.98)))
                        case "commands": CommandsView().transition(.opacity.combined(with: .scale(scale: 0.95)))
                        case "vocab": VocabularyView().transition(.move(edge: .trailing).combined(with: .opacity))
                        case "narration": NarrationView().transition(.opacity)
                        case "intelligence": IntelligenceView().transition(.scale.combined(with: .opacity))
                        case "settings": SettingsView(isAccessibilityTrusted: $isAccessibilityTrusted).transition(.move(edge: .leading).combined(with: .opacity))
                        case "language": LanguageView().transition(.opacity)
                        case "account": AccountView().transition(.opacity)
                        default: Text("Coming Soon")
                        }
                    }
                    .padding(.top, 64)
                    .padding(.horizontal, 40)
                    .padding(.bottom, 32)
                    .id(selectedTab) // Force view reload for transitions
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
        .frame(minWidth: 1000, minHeight: 700)
        .preferredColorScheme(.dark)
        .onAppear {
            isAccessibilityTrusted = AXIsProcessTrusted()
        }
        .onReceive(Timer.publish(every: 2, on: .main, in: .common).autoconnect()) { _ in
            let trusted = AXIsProcessTrusted()
            if trusted != isAccessibilityTrusted {
                isAccessibilityTrusted = trusted
            }
        }
    }
    
    @ViewBuilder
    private func sidebarSection<Content: View>(title: String, @ViewBuilder content: () -> Content) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            if !isSidebarCollapsed {
                Text(title)
                    .font(.system(size: 10, weight: .black))
                    .foregroundColor(theme.currentTheme.accent.opacity(0.8))
                    .padding(.horizontal, 16)
                    .padding(.top, 12)
                    .tracking(1.5)
            } else {
                Divider()
                    .padding(.horizontal, 22)
                    .padding(.vertical, 8)
                    .opacity(0.2)
            }
            
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
            
            if !isSidebarCollapsed {
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
        }
        .padding(.horizontal, isSidebarCollapsed ? 0 : 20)
        .padding(.bottom, isSidebarCollapsed ? 0 : 12)
        .frame(maxWidth: .infinity, alignment: isSidebarCollapsed ? .center : .leading)
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
            
            if !isSidebarCollapsed {
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
        }
        .padding(.horizontal, isSidebarCollapsed ? 0 : 20)
        .frame(maxWidth: .infinity, alignment: isSidebarCollapsed ? .center : .leading)
        .padding(.bottom, 4)
    }
    
    private var themeToggle: some View {
        HStack {
            if !isSidebarCollapsed {
                Text("THEME")
                    .font(.system(size: 10, weight: .black))
                    .foregroundColor(.secondary.opacity(0.6))
                    .tracking(1)
                Spacer()
            }
            
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
        .padding(.horizontal, isSidebarCollapsed ? 0 : 20)
        .frame(maxWidth: .infinity, alignment: isSidebarCollapsed ? .center : .leading)
    }
}

// MARK: - Premium Background Components
struct CosmicBackground: View {
    @ObservedObject var theme = ThemeManager.shared
    @State private var animate = false
    
    var body: some View {
        ZStack {
            // Base layer
            theme.currentTheme.backgroundColors[0].ignoresSafeArea()
            
            // Mesh Gradient Emulation
            Canvas { context, size in
                context.addFilter(.blur(radius: 80))
                
                let colors = theme.currentTheme.backgroundColors
                
                // Color 1 - top left
                context.fill(Path(ellipseIn: CGRect(x: animate ? -100 : 0, y: animate ? -100 : 100, width: size.width * 0.8, height: size.height * 0.8)), with: .color(colors[1].opacity(0.5)))
                
                // Color 2 - bottom right
                context.fill(Path(ellipseIn: CGRect(x: animate ? size.width * 0.4 : size.width * 0.2, y: animate ? size.height * 0.4 : size.height * 0.6, width: size.width * 0.9, height: size.height * 0.9)), with: .color(colors[2].opacity(0.4)))
            }
            .onAppear {
                withAnimation(.easeInOut(duration: 10).repeatForever(autoreverses: true)) {
                    animate.toggle()
                }
            }
            
            StarField()
            
            Color.black.opacity(0.2).ignoresSafeArea()
        }
    }
}

struct StarField: View {
    @State private var stars: [Star] = (0..<150).map { _ in Star() }
    
    var body: some View {
        TimelineView(.animation) { timeline in
            Canvas { context, size in
                for star in stars {
                    let blink = sin(timeline.date.timeIntervalSinceReferenceDate * star.speed + star.offset) * 0.5 + 0.5
                    let opacity = star.opacity * blink
                    
                    context.opacity = opacity
                    context.fill(Path(ellipseIn: CGRect(x: star.x * size.width, y: star.y * size.height, width: star.size, height: star.size)), with: .color(.white))
                }
            }
        }
        .ignoresSafeArea()
    }
}

struct Star {
    let x = Double.random(in: 0...1)
    let y = Double.random(in: 0...1)
    let size = Double.random(in: 1...2)
    let opacity = Double.random(in: 0.1...0.6)
    let speed = Double.random(in: 0.5...2.0)
    let offset = Double.random(in: 0...100)
}
