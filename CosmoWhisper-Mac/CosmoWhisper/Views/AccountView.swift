import SwiftUI

struct AccountView: View {
    @AppStorage("licenseToken") private var licenseToken: String = ""
    @AppStorage("userTier") private var userTier: String = "free"
    @AppStorage("usageMinutes") private var usageMinutes: Double = 0.0
    @AppStorage("usageLimitMinutes") private var usageLimitMinutes: Int = 10

    @ObservedObject var theme = ThemeManager.shared

    var isPro: Bool {
        userTier.lowercased() == "pro"
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            VStack(alignment: .leading, spacing: 8) {
                Text("Account")
                    .font(.system(size: 28, weight: .bold))
                    .foregroundColor(theme.textPrimary)
                Text("Manage your subscription and profile.")
                    .foregroundColor(theme.textSecondary)
            }

            SettingsCard(title: "Profile", icon: "person.crop.circle") {
                VStack(alignment: .leading, spacing: 20) {
                    HStack(spacing: 16) {
                        Circle()
                            .fill(theme.currentTheme.accent.opacity(0.2))
                            .frame(width: 60, height: 60)
                            .overlay(Text(isPro ? "💎" : "👤").font(.system(size: 24)))

                        VStack(alignment: .leading, spacing: 4) {
                            Text(isPro ? "Pro Member" : "Free Explorer")
                                .font(.system(size: 16, weight: .medium))
                                .foregroundColor(theme.textPrimary)
                            Text(licenseToken.isEmpty ? "No account linked" : "Linked via Web Control")
                                .font(.system(size: 13))
                                .foregroundColor(theme.textSecondary)
                        }
                        Spacer()

                        if !licenseToken.isEmpty {
                            Button("Sync Status") {
                                Task { await LicenseManager.shared.syncStatus() }
                            }
                            .buttonStyle(.bordered)
                        }
                    }

                    Divider().opacity(0.1)

                    VStack(alignment: .leading, spacing: 6) {
                        Text("Monthly Usage")
                            .font(.system(size: 12, weight: .medium))
                            .foregroundColor(theme.textSecondary)
                        Text(isPro ? "\(String(format: "%.1f", usageMinutes)) / ∞" : "\(String(format: "%.1f", usageMinutes)) / \(usageLimitMinutes) min")
                            .font(.system(size: 18, weight: .bold))
                            .foregroundColor(theme.textPrimary)
                    }
                }
            }
        }
    }
}
