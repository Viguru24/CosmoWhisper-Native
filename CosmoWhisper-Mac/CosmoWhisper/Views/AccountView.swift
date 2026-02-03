import SwiftUI

struct AccountView: View {
    let accentColor = Color(red: 59/255, green: 130/255, blue: 246/255)
    
    @AppStorage("licenseToken") private var licenseToken: String = ""
    @AppStorage("userTier") private var userTier: String = "free"
    @AppStorage("usageMinutes") private var usageMinutes: Double = 0.0
    @AppStorage("usageLimitMinutes") private var usageLimitMinutes: Int = 10
    
    var isPro: Bool {
        userTier.lowercased() == "pro"
    }
    
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Account")
                .font(.system(size: 32, weight: .bold))
            Text("Manage your subscription and profile.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            SettingsCard(title: "Profile Info", icon: "person.crop.circle") {
                VStack(alignment: .leading, spacing: 24) {
                    HStack(spacing: 16) {
                        Circle()
                            .fill(accentColor.opacity(0.2))
                            .frame(width: 60, height: 60)
                            .overlay(Text(isPro ? "💎" : "👤").font(.system(size: 24)))
                        
                        VStack(alignment: .leading, spacing: 4) {
                            Text(isPro ? "Pro Member" : "Free Explorer")
                                .font(.headline)
                            Text(licenseToken.isEmpty ? "No account linked" : "Linked via Web Control")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
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
                    
                    HStack {
                        VStack(alignment: .leading, spacing: 4) {
                            Text("Monthly Usage")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            Text(isPro ? "\(String(format: "%.1f", usageMinutes)) / ∞" : "\(String(format: "%.1f", usageMinutes)) / \(usageLimitMinutes) min")
                                .font(.headline)
                        }
                        Spacer()
                        
                        let progress = isPro ? 0.05 : (usageMinutes / Double(usageLimitMinutes))
                        ProgressView(value: min(progress, 1.0))
                            .progressViewStyle(.linear)
                            .frame(width: 200)
                            .tint(progress >= 1.0 && !isPro ? .red : accentColor)
                    }
                }
            }
        }
    }
}
