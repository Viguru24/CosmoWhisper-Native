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
            Text("Library")
                .font(.system(size: 28, weight: .bold))
                .foregroundColor(theme.textPrimary)

            Text("Access your command collection and resources.")
                .font(.system(size: 14))
                .foregroundColor(theme.textSecondary)
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
                    .font(.system(size: 20, weight: .bold))
                    .foregroundColor(theme.textPrimary)

                Text("Explore the full collection of voice commands online.")
                    .font(.system(size: 13))
                    .foregroundColor(theme.textSecondary)
                    .multilineTextAlignment(.center)
            }

            Button(action: {
                if let url = URL(string: "https://cosmowhisper.com/features") {
                    NSWorkspace.shared.open(url)
                }
            }) {
                Text("Open Master List")
                    .font(.system(size: 14, weight: .bold))
                    .foregroundColor(.white)
                    .padding(.horizontal, 32)
                    .padding(.vertical, 12)
                    .background(theme.currentTheme.accent)
                    .cornerRadius(10)
            }
            .buttonStyle(.plain)
        }
        .padding(40)
        .frame(maxWidth: .infinity)
        .background(theme.backgroundSurface)
        .cornerRadius(20)
        .overlay(RoundedRectangle(cornerRadius: 20).stroke(theme.borderColor, lineWidth: 1))
    }

    private var quickLinks: some View {
        HStack(spacing: 16) {
            LinkCard(icon: "doc.text.fill", title: "User Manual", desc: "How to use Cosmo", url: "https://cosmowhisper.com/faq")
            LinkCard(icon: "terminal.fill", title: "GitHub", desc: "View Source", url: "https://github.com/Viguru24/CosmoWhisper-Native")
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
            HStack(spacing: 12) {
                Image(systemName: icon)
                    .font(.system(size: 20))
                    .foregroundColor(theme.currentTheme.accent)
                    .frame(width: 44, height: 44)
                    .background(theme.currentTheme.accent.opacity(0.1))
                    .cornerRadius(10)

                VStack(alignment: .leading, spacing: 2) {
                    Text(title)
                        .font(.system(size: 14, weight: .medium))
                        .foregroundColor(theme.textPrimary)
                    Text(desc)
                        .font(.system(size: 11))
                        .foregroundColor(theme.textSecondary)
                }

                Spacer()
            }
            .padding(16)
            .background(theme.backgroundSurface)
            .cornerRadius(14)
            .overlay(RoundedRectangle(cornerRadius: 14).stroke(theme.borderColor, lineWidth: 1))
        }
        .buttonStyle(.plain)
    }
}
