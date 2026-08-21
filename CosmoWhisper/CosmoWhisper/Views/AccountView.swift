import SwiftUI
import StoreKit

struct AccountView: View {
    @ObservedObject var license = LicenseManager.shared
    @ObservedObject var storeKit = StoreKitManager.shared
    
    // Magic Code State
    @State private var emailInput: String = ""
    @State private var magicCodeInput: String = ""
    @State private var isCodeSent: Bool = false
    @State private var statusAlert: String? = nil
    
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Account & Subscriptions")
                    .font(.system(size: 32, weight: .bold))
                Text("Manage your CosmoWhisper subscription, restore Apple purchases, or sign in to sync across devices.")
                    .foregroundColor(.secondary)
            }
            .padding(.bottom, 8)
            
            // --- SECTION 1: PROFILE / SIGN IN ---
            if !license.isLoggedIn {
                // Multiplatform Sign In (Compliant with Apple Guideline 3.1.3b)
                SettingsCard(title: "Multiplatform Account Sign-In", icon: "person.badge.key.fill") {
                    VStack(alignment: .center, spacing: 20) {
                        VStack(spacing: 6) {
                            Text("Use an Existing Account or Windows License")
                                .font(.title3.bold())
                                .foregroundColor(.white)
                            
                            Text("Already have a CosmoWhisper account from Windows or Web? Sign in to unlock your subscription on this Mac.")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                                .multilineTextAlignment(.center)
                                .frame(maxWidth: 480)
                        }
                        
                        // 1. Google OAuth
                        Button(action: {
                            license.startGoogleAuth()
                        }) {
                            HStack(spacing: 12) {
                                Image(systemName: "globe")
                                    .font(.system(size: 16, weight: .semibold))
                                Text("Sign In with Google")
                                    .font(.system(size: 14, weight: .bold))
                            }
                            .frame(maxWidth: 340)
                            .padding(.vertical, 12)
                            .background(LinearGradient(colors: [.blue, .purple], startPoint: .leading, endPoint: .trailing))
                            .foregroundColor(.white)
                            .cornerRadius(10)
                            .shadow(color: .blue.opacity(0.3), radius: 8, x: 0, y: 4)
                        }
                        .buttonStyle(.plain)
                        
                        // Divider
                        HStack {
                            Rectangle().fill(Color.white.opacity(0.1)).frame(height: 1)
                            Text("OR SIGN IN WITH EMAIL CODE")
                                .font(.system(size: 10, weight: .bold))
                                .foregroundColor(.secondary)
                            Rectangle().fill(Color.white.opacity(0.1)).frame(height: 1)
                        }
                        .frame(maxWidth: 380)
                        
                        // 2. Magic Code Flow
                        VStack(spacing: 12) {
                            if !isCodeSent {
                                HStack(spacing: 10) {
                                    Image(systemName: "envelope.fill")
                                        .foregroundColor(.secondary)
                                        .frame(width: 20)
                                    TextField("Enter your account email", text: $emailInput)
                                        .textFieldStyle(.plain)
                                }
                                .padding(12)
                                .background(Color.white.opacity(0.06))
                                .cornerRadius(10)
                                .frame(maxWidth: 340)
                                
                                Button(action: requestMagicCode) {
                                    HStack {
                                        if license.isLoading {
                                            ProgressView().scaleEffect(0.5).frame(width: 16, height: 16)
                                        }
                                        Text("Send Verification Code")
                                            .font(.system(size: 13, weight: .bold))
                                    }
                                    .frame(maxWidth: 340)
                                    .padding(.vertical, 10)
                                    .background(Color.white.opacity(0.12))
                                    .foregroundColor(.white)
                                    .cornerRadius(8)
                                }
                                .buttonStyle(.plain)
                                .disabled(license.isLoading || !emailInput.contains("@"))
                            } else {
                                VStack(spacing: 10) {
                                    Text("Enter the 6-digit code sent to \(emailInput)")
                                        .font(.caption)
                                        .foregroundColor(.secondary)
                                    
                                    HStack(spacing: 10) {
                                        Image(systemName: "key.fill")
                                            .foregroundColor(.yellow)
                                            .frame(width: 20)
                                        TextField("6-Digit Code (e.g. 849201)", text: $magicCodeInput)
                                            .textFieldStyle(.plain)
                                            .font(.system(size: 16, weight: .bold, design: .monospaced))
                                    }
                                    .padding(12)
                                    .background(Color.white.opacity(0.06))
                                    .cornerRadius(10)
                                    .frame(maxWidth: 340)
                                    
                                    Button(action: verifyMagicCode) {
                                        HStack {
                                            if license.isLoading {
                                                ProgressView().scaleEffect(0.5).frame(width: 16, height: 16)
                                            }
                                            Text("Verify & Activate")
                                                .font(.system(size: 13, weight: .bold))
                                        }
                                        .frame(maxWidth: 340)
                                        .padding(.vertical, 10)
                                        .background(Color.green.opacity(0.8))
                                        .foregroundColor(.white)
                                        .cornerRadius(8)
                                    }
                                    .buttonStyle(.plain)
                                    .disabled(license.isLoading || magicCodeInput.count < 4)
                                    
                                    Button(action: {
                                        isCodeSent = false
                                        magicCodeInput = ""
                                    }) {
                                        Text("Change Email / Resend Code")
                                            .font(.caption)
                                            .foregroundColor(.blue)
                                    }
                                    .buttonStyle(.plain)
                                }
                            }
                            
                            if let err = license.errorMessage ?? statusAlert {
                                Text(err)
                                    .font(.caption)
                                    .foregroundColor(.red)
                            }
                        }
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 16)
                }
            } else {
                // LOGGED IN: Profile & Active Status
                SettingsCard(title: "Active Account & Plan", icon: "person.crop.circle") {
                    VStack(alignment: .leading, spacing: 20) {
                        HStack(spacing: 16) {
                            ZStack {
                                Circle()
                                    .fill(LinearGradient(colors: [.blue.opacity(0.3), .purple.opacity(0.3)], startPoint: .topLeading, endPoint: .bottomTrailing))
                                    .frame(width: 56, height: 56)
                                
                                Text(userInitials)
                                    .font(.system(size: 20, weight: .bold))
                                    .foregroundColor(.white)
                            }
                            
                            VStack(alignment: .leading, spacing: 4) {
                                Text(license.userEmail.isEmpty ? "Connected Account" : license.userEmail)
                                    .font(.headline)
                                    .foregroundColor(.white)
                                
                                HStack(spacing: 6) {
                                    Text(tierDisplayName)
                                        .font(.system(size: 11, weight: .bold))
                                        .foregroundColor(tierBadgeColor)
                                        .padding(.horizontal, 8)
                                        .padding(.vertical, 3)
                                        .background(tierBadgeColor.opacity(0.15))
                                        .cornerRadius(6)
                                    
                                    if license.tier.lowercased() == "free" {
                                        Text("60 min / week free")
                                            .font(.caption)
                                            .foregroundColor(.secondary)
                                    }
                                }
                            }
                            
                            Spacer()
                            
                            HStack(spacing: 10) {
                                Button(action: {
                                    Task { await license.fetchStatus() }
                                }) {
                                    Label("Sync Status", systemImage: "arrow.triangle.2.circlepath")
                                        .font(.caption.bold())
                                        .padding(.horizontal, 10)
                                        .padding(.vertical, 6)
                                        .background(Color.white.opacity(0.08))
                                        .cornerRadius(6)
                                }
                                .buttonStyle(.plain)
                                
                                Button(action: {
                                    license.logout()
                                    isCodeSent = false
                                    emailInput = ""
                                    magicCodeInput = ""
                                }) {
                                    Text("Sign Out")
                                        .font(.caption.bold())
                                        .foregroundColor(.secondary)
                                        .padding(.horizontal, 10)
                                        .padding(.vertical, 6)
                                        .background(Color.white.opacity(0.05))
                                        .cornerRadius(6)
                                }
                                .buttonStyle(.plain)
                            }
                        }
                        
                        Divider().opacity(0.1)
                        
                        // Usage Meter
                        VStack(alignment: .leading, spacing: 10) {
                            HStack {
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(license.tier.lowercased() == "free" ? "Weekly Free Cloud Usage" : "Cloud Transcription Quota")
                                        .font(.caption)
                                        .foregroundColor(.secondary)
                                    
                                    if license.tier.lowercased() == "free" {
                                        Text("\(String(format: "%.1f", license.weeklyUsageMinutes)) / \(Int(license.weeklyLimitMinutes)) minutes")
                                            .font(.system(size: 18, weight: .bold, design: .rounded))
                                            .foregroundColor(license.isOverQuota ? .red : .white)
                                    } else {
                                        Text("Unlimited High-Speed Minutes")
                                            .font(.system(size: 18, weight: .bold, design: .rounded))
                                            .foregroundColor(.green)
                                    }
                                }
                                
                                Spacer()
                                
                                if license.tier.lowercased() == "free" {
                                    Text("\(max(0, Int(license.weeklyLimitMinutes - license.weeklyUsageMinutes))) min left")
                                        .font(.caption.bold())
                                        .foregroundColor(license.isOverQuota ? .red : .green)
                                }
                            }
                            
                            if license.tier.lowercased() == "free" {
                                let progress = min(1.0, license.weeklyUsageMinutes / license.weeklyLimitMinutes)
                                GeometryReader { geo in
                                    ZStack(alignment: .leading) {
                                        RoundedRectangle(cornerRadius: 6)
                                            .fill(Color.white.opacity(0.08))
                                            .frame(height: 10)
                                        
                                        RoundedRectangle(cornerRadius: 6)
                                            .fill(
                                                LinearGradient(
                                                    colors: license.isOverQuota ? [.orange, .red] : [.blue, .purple],
                                                    startPoint: .leading,
                                                    endPoint: .trailing
                                                )
                                            )
                                            .frame(width: max(8, geo.size.width * CGFloat(progress)), height: 10)
                                    }
                                }
                                .frame(height: 10)
                                
                                Text("Your free 60 minutes renew every 7 days automatically. Local speech recognition is always 100% free and unlimited.")
                                    .font(.caption2)
                                    .foregroundColor(.secondary)
                            }
                        }
                    }
                }
            }
            
            // --- SECTION 2: MAC APP STORE IN-APP PURCHASES ---
            SettingsCard(title: "CosmoWhisper Pro Subscriptions", icon: "crown.fill") {
                VStack(alignment: .leading, spacing: 18) {
                    HStack(alignment: .top, spacing: 16) {
                        Image(systemName: "sparkles")
                            .font(.system(size: 28))
                            .foregroundColor(.yellow)
                            .padding(.top, 2)
                        
                        VStack(alignment: .leading, spacing: 4) {
                            Text("Unlock Unlimited Cloud Speed & Priority AI")
                                .font(.headline)
                                .foregroundColor(.white)
                            Text("Subscribe via Apple In-App Purchase to unlock unlimited cloud transcription, custom vocabulary syncing, and priority Groq AI models.")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                        }
                    }
                    
                    // Native StoreKit Subscription Options
                    if !storeKit.products.isEmpty {
                        VStack(spacing: 10) {
                            ForEach(storeKit.products) { product in
                                HStack {
                                    VStack(alignment: .leading, spacing: 2) {
                                        Text(product.displayName)
                                            .font(.subheadline.bold())
                                            .foregroundColor(.white)
                                        Text(product.description)
                                            .font(.caption)
                                            .foregroundColor(.secondary)
                                    }
                                    
                                    Spacer()
                                    
                                    Button(action: {
                                        Task {
                                            _ = await storeKit.purchase(product)
                                        }
                                    }) {
                                        Text(storeKit.purchasedProductIDs.contains(product.id) ? "Subscribed" : "Subscribe \(product.displayPrice)/mo")
                                            .font(.caption.bold())
                                            .padding(.horizontal, 14)
                                            .padding(.vertical, 8)
                                            .background(storeKit.purchasedProductIDs.contains(product.id) ? Color.green.opacity(0.8) : Color.blue)
                                            .foregroundColor(.white)
                                            .cornerRadius(8)
                                    }
                                    .buttonStyle(.plain)
                                    .disabled(storeKit.purchasedProductIDs.contains(product.id) || storeKit.isLoading)
                                }
                                .padding(12)
                                .background(Color.white.opacity(0.04))
                                .cornerRadius(10)
                            }
                        }
                    } else {
                        // Fallback Display / Store Preview
                        HStack {
                            VStack(alignment: .leading, spacing: 3) {
                                Text("CosmoWhisper Pro (Unlimited Cloud)")
                                    .font(.subheadline.bold())
                                    .foregroundColor(.white)
                                Text("Unlimited transcription, priority cloud speed, and multi-device sync.")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                            
                            Spacer()
                            
                            Button(action: {
                                Task { await storeKit.fetchProducts() }
                            }) {
                                Text("Check Store Plans")
                                    .font(.caption.bold())
                                    .padding(.horizontal, 14)
                                    .padding(.vertical, 8)
                                    .background(LinearGradient(colors: [.blue, .purple], startPoint: .leading, endPoint: .trailing))
                                    .foregroundColor(.white)
                                    .cornerRadius(8)
                            }
                            .buttonStyle(.plain)
                        }
                        .padding(12)
                        .background(Color.white.opacity(0.04))
                        .cornerRadius(10)
                    }
                    
                    // Restore & Legal Links (Mandatory for App Store Approval)
                    HStack(spacing: 16) {
                        Button(action: {
                            Task { await storeKit.restorePurchases() }
                        }) {
                            Text("Restore Apple Purchases")
                                .font(.caption.bold())
                                .foregroundColor(.blue)
                        }
                        .buttonStyle(.plain)
                        
                        Spacer()
                        
                        Button(action: {
                            if let url = URL(string: "https://cosmowhisper.com/privacy") { NSWorkspace.shared.open(url) }
                        }) {
                            Text("Privacy Policy")
                                .font(.caption2)
                                .foregroundColor(.secondary)
                        }
                        .buttonStyle(.plain)
                        
                        Text("•").font(.caption2).foregroundColor(.secondary.opacity(0.5))
                        
                        Button(action: {
                            if let url = URL(string: "https://cosmowhisper.com/terms") { NSWorkspace.shared.open(url) }
                        }) {
                            Text("Terms of Service")
                                .font(.caption2)
                                .foregroundColor(.secondary)
                        }
                        .buttonStyle(.plain)
                    }
                    .padding(.top, 4)
                    
                    if let msg = storeKit.statusMessage {
                        Text(msg)
                            .font(.caption)
                            .foregroundColor(.yellow)
                    }
                }
            }
            
            Spacer()
        }
        .onAppear {
            license.recalculateLocalWeeklyUsage()
            if license.isLoggedIn {
                Task { await license.fetchStatus() }
            }
            Task {
                await storeKit.fetchProducts()
                await storeKit.updatePurchasedProducts()
            }
        }
    }
    
    private func requestMagicCode() {
        statusAlert = nil
        Task { @MainActor in
            let ok = await license.requestMagicCode(email: emailInput)
            if ok {
                self.isCodeSent = true
            }
        }
    }
    
    private func verifyMagicCode() {
        statusAlert = nil
        Task { @MainActor in
            let ok = await license.verifyMagicCode(email: emailInput, code: magicCodeInput)
            if !ok {
                self.statusAlert = license.errorMessage ?? "Invalid or expired code"
            }
        }
    }
    
    private var userInitials: String {
        if !license.userEmail.isEmpty {
            return String(license.userEmail.prefix(2)).uppercased()
        }
        return "CW"
    }
    
    private var tierDisplayName: String {
        switch license.tier.lowercased() {
        case "unlimited": return "UNLIMITED ACCESS"
        case "personal": return "PERSONAL PLAN"
        case "professional", "pro": return "PRO PLAN"
        case "medical": return "MEDICAL PLAN"
        default: return "FREE TIER (60 MIN/WK)"
        }
    }
    
    private var tierBadgeColor: Color {
        switch license.tier.lowercased() {
        case "unlimited": return .green
        case "personal": return .blue
        case "professional", "pro": return .purple
        case "medical": return .green
        default: return .orange
        }
    }
}
