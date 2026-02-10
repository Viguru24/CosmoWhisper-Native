import SwiftUI

struct LibraryView: View {
    @ObservedObject var theme = ThemeManager.shared
    
    var body: some View {
        VStack(alignment: .leading, spacing: 32) {
            headerSection
            
            mainCard
            
            quickLinks
            
            Spacer()
        }
    }
    
    private var headerSection: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Cloud Library")
                .font(.system(size: 32, weight: .bold))
                .foregroundColor(.white)
            
            Text("Your master repository for commands and automation.")
                .font(.body)
                .foregroundColor(.secondary)
        }
    }
    
    private var mainCard: some View {
        VStack(spacing: 24) {
            ZStack {
                Circle()
                    .fill(theme.currentTheme.accent.opacity(0.1))
                    .frame(width: 80, height: 80)
                
                Image(systemName: "globe")
                    .font(.system(size: 40))
                    .foregroundColor(theme.currentTheme.accent)
            }
            
            VStack(spacing: 8) {
                Text("Master Command List")
                    .font(.system(size: 24, weight: .bold))
                
                Text("Explore the full master list of commands online. Includes system triggers, formatting rules, and AI personas.")
                    .font(.system(size: 14))
                    .foregroundColor(.secondary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 40)
            }
            
            Button(action: {
                if let url = URL(string: "https://cosmowhisper-app.web.app/features") {
                    NSWorkspace.shared.open(url)
                }
            }) {
                Text("Open Master List")
                    .font(.system(size: 14, weight: .bold))
                    .foregroundColor(.white)
                    .padding(.horizontal, 40)
                    .padding(.vertical, 14)
                    .background(theme.currentTheme.accent)
                    .cornerRadius(12)
            }
            .buttonStyle(.plain)
        }
        .padding(48)
        .frame(maxWidth: .infinity)
        .background(Color.white.opacity(0.03))
        .cornerRadius(24)
        .overlay(
            RoundedRectangle(cornerRadius: 24)
                .stroke(Color.white.opacity(0.05), lineWidth: 1)
        )
    }
    
    private var quickLinks: some View {
        HStack(spacing: 20) {
            LinkCard(icon: "doc.text.fill", title: "User Manual", desc: "How to use Cosmo", url: "https://cosmowhisper-app.web.app/faq")
            LinkCard(icon: "terminal.fill", title: "Open Source", desc: "View on GitHub", url: "https://github.com/Viguru24/CosmoWhisper-Native")
        }
    }
}

struct LinkCard: View {
    @ObservedObject var theme = ThemeManager.shared
    let icon: String
    let title: String
    let desc: String
    let url: String
    
    var body: some View {
        Button(action: {
            if let urlObj = URL(string: url) {
                NSWorkspace.shared.open(urlObj)
            }
        }) {
            HStack(spacing: 16) {
                Image(systemName: icon)
                    .font(.system(size: 24))
                    .foregroundColor(theme.currentTheme.accent)
                    .frame(width: 48, height: 48)
                    .background(theme.currentTheme.accent.opacity(0.1))
                    .cornerRadius(12)
                
                VStack(alignment: .leading, spacing: 2) {
                    Text(title)
                        .font(.system(size: 16, weight: .bold))
                    Text(desc)
                        .font(.system(size: 12))
                        .foregroundColor(.secondary)
                }
                
                Spacer()
            }
            .padding(20)
            .background(Color.white.opacity(0.03))
            .cornerRadius(16)
            .overlay(
                RoundedRectangle(cornerRadius: 16)
                    .stroke(Color.white.opacity(0.05), lineWidth: 1)
            )
        }
        .buttonStyle(.plain)
    }
}
