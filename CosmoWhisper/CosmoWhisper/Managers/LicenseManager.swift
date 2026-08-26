import Foundation
import SwiftUI

@MainActor
class LicenseManager: ObservableObject {
    static let shared = LicenseManager()
    
    @Published var isLoggedIn: Bool = false
    @Published var userEmail: String = ""
    @Published var tier: String = "free" // "free", "personal", "professional", "medical"
    @Published var monthlyUsageMinutes: Double = 0.0
    @Published var monthlyLimitMinutes: Double = 60.0
    
    // Backward compatibility aliases
    var weeklyUsageMinutes: Double { monthlyUsageMinutes }
    var weeklyLimitMinutes: Double { monthlyLimitMinutes }
    
    @Published var isOverQuota: Bool = false
    @Published var isLoading: Bool = false
    @Published var errorMessage: String? = nil
    
    // VPS Backend Base URL (Configurable via UserDefaults or defaults to production VPS)
    var baseURL: String {
        UserDefaults.standard.string(forKey: "vps_backend_url") ?? "https://api.cosmowhisper.com"
    }
    
    private let tokenService = "com.cosmowhisper.auth"
    private let tokenAccount = "jwt_token"
    
    init() {
        self.isLoggedIn = UserDefaults.standard.bool(forKey: "userLoggedIn")
        self.userEmail = UserDefaults.standard.string(forKey: "userEmail") ?? ""
        self.tier = UserDefaults.standard.string(forKey: "subscriptionTier") ?? "free"
        self.monthlyLimitMinutes = (self.tier == "free") ? 60.0 : 999999.0
        
        recalculateLocalMonthlyUsage()
        
        if isLoggedIn {
            Task { await fetchStatus() }
        }
    }
    
    /// Recalculates usage accumulated in the last 30 rolling days (1 Month)
    func recalculateLocalMonthlyUsage() {
        let thirtyDaysAgo = Date().addingTimeInterval(-30 * 24 * 3600).timeIntervalSince1970
        let records = UserDefaults.standard.array(forKey: "usage_records_v1") as? [[String: Any]] ?? []
        
        // Filter records from the last 30 days
        var totalMs: Double = 0.0
        var validRecords: [[String: Any]] = []
        
        for record in records {
            if let timestamp = record["timestamp"] as? Double, timestamp >= thirtyDaysAgo,
               let ms = record["duration_ms"] as? Double {
                totalMs += ms
                validRecords.append(record)
            }
        }
        
        // Save cleaned records back
        UserDefaults.standard.set(validRecords, forKey: "usage_records_v1")
        
        let minutes = totalMs / 60000.0
        self.monthlyUsageMinutes = (minutes * 10).rounded() / 10
        self.isOverQuota = (self.tier == "free") && (self.monthlyUsageMinutes >= self.monthlyLimitMinutes)
        
        LogManager.shared.log("LicenseManager: Monthly Usage: \(self.monthlyUsageMinutes)/\(self.monthlyLimitMinutes) min (Tier: \(self.tier), OverQuota: \(self.isOverQuota))")
    }
    
    func recalculateLocalWeeklyUsage() {
        recalculateLocalMonthlyUsage()
    }
    
    /// Reports usage after a transcription session
    func reportUsage(durationMs: Int) {
        guard durationMs > 0 else { return }
        
        // 1. Record locally with timestamp
        var records = UserDefaults.standard.array(forKey: "usage_records_v1") as? [[String: Any]] ?? []
        records.append([
            "timestamp": Date().timeIntervalSince1970,
            "duration_ms": Double(durationMs)
        ])
        UserDefaults.standard.set(records, forKey: "usage_records_v1")
        
        recalculateLocalWeeklyUsage()
        
        // 2. Report to VPS Backend if token is available
        guard let token = KeychainManager.shared.readString(service: tokenService, account: tokenAccount) else {
            return
        }
        
        Task {
            guard let url = URL(string: "\(baseURL)/api/license/report-usage") else { return }
            var request = URLRequest(url: url)
            request.httpMethod = "POST"
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
            request.timeoutInterval = 10
            
            let body: [String: Any] = ["durationMs": durationMs]
            request.httpBody = try? JSONSerialization.data(withJSONObject: body)
            
            do {
                let (_, response) = try await URLSession.shared.data(for: request)
                if let httpRes = response as? HTTPURLResponse, httpRes.statusCode == 200 {
                    LogManager.shared.log("LicenseManager: Reported \(durationMs)ms to VPS backend successfully.")
                }
            } catch {
                LogManager.shared.log("LicenseManager: VPS report warning: \(error.localizedDescription)")
            }
        }
    }
    
    /// Fetches live license & subscription status from VPS backend
    func fetchStatus() async {
        guard let token = KeychainManager.shared.readString(service: tokenService, account: tokenAccount) else {
            return
        }
        
        guard let url = URL(string: "\(baseURL)/api/license/status") else { return }
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.timeoutInterval = 8
        
        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            if let httpRes = response as? HTTPURLResponse, httpRes.statusCode == 200 {
                if let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
                    if let remoteEmail = json["email"] as? String, !remoteEmail.isEmpty {
                        self.userEmail = remoteEmail
                        UserDefaults.standard.set(remoteEmail, forKey: "userEmail")
                    }
                    if let remoteTier = json["tier"] as? String {
                        self.tier = remoteTier
                        UserDefaults.standard.set(remoteTier, forKey: "subscriptionTier")
                        self.monthlyLimitMinutes = (remoteTier.lowercased() == "free") ? 60.0 : 999999.0
                    }
                    if let isOver = json["isOverLimit"] as? Bool {
                        self.isOverQuota = isOver
                    }
                    recalculateLocalMonthlyUsage()
                    LogManager.shared.log("LicenseManager: Synced status with VPS -> Email: \(self.userEmail), Tier: \(self.tier), OverLimit: \(self.isOverQuota)")
                }
            }
        } catch {
            LogManager.shared.log("LicenseManager: Could not reach VPS status endpoint (\(error.localizedDescription)). Using local quotas.")
        }
    }
    
    private func decodeJWTPayload(_ token: String) -> [String: Any]? {
        let parts = token.components(separatedBy: ".")
        guard parts.count >= 2 else { return nil }
        var base64 = parts[1]
            .replacingOccurrences(of: "-", with: "+")
            .replacingOccurrences(of: "_", with: "/")
        while base64.count % 4 != 0 {
            base64.append("=")
        }
        guard let data = Data(base64Encoded: base64),
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            return nil
        }
        return json
    }
    
    func handleAuthDeepLink(url: URL) {
        LogManager.shared.log("LicenseManager: Received Auth Deep Link: \(url.absoluteString)")
        guard let components = URLComponents(url: url, resolvingAgainstBaseURL: false),
              let queryItems = components.queryItems else { return }
        
        var token: String?
        for item in queryItems {
            if item.name == "token" { token = item.value }
        }
        
        if let token = token, !token.isEmpty {
            _ = KeychainManager.shared.saveString(token, service: tokenService, account: tokenAccount)
            self.isLoggedIn = true
            
            // Decode payload for immediate UI update
            if let payload = decodeJWTPayload(token) {
                if let jwtEmail = payload["email"] as? String {
                    self.userEmail = jwtEmail
                    UserDefaults.standard.set(jwtEmail, forKey: "userEmail")
                }
                if let jwtTier = payload["tier"] as? String {
                    self.tier = jwtTier
                    UserDefaults.standard.set(jwtTier, forKey: "subscriptionTier")
                    self.monthlyLimitMinutes = (jwtTier.lowercased() == "free") ? 60.0 : 999999.0
                }
            }
            
            UserDefaults.standard.set(true, forKey: "userLoggedIn")
            recalculateLocalMonthlyUsage()
            LogManager.shared.log("LicenseManager: Authenticated as \(self.userEmail) (Tier: \(self.tier))")
            
            // Sync live status with VPS
            Task { @MainActor in
                await self.fetchStatus()
            }
        }
    }
    
    /// Requests a 6-digit magic code sent to the user's email
    func requestMagicCode(email: String) async -> Bool {
        self.isLoading = true
        self.errorMessage = nil
        
        guard let url = URL(string: "\(baseURL)/api/auth/request-otp") else {
            self.errorMessage = "Invalid backend URL"
            self.isLoading = false
            return false
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = 10
        
        let body: [String: Any] = ["email": email.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)
        
        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            self.isLoading = false
            
            guard let httpRes = response as? HTTPURLResponse else {
                self.errorMessage = "Invalid server response"
                return false
            }
            
            if httpRes.statusCode == 200 {
                LogManager.shared.log("LicenseManager: Magic code requested successfully for \(email)")
                return true
            } else {
                if let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                   let err = json["error"] as? String {
                    self.errorMessage = err
                } else {
                    self.errorMessage = "Failed to send code (Error \(httpRes.statusCode))"
                }
            }
        } catch {
            self.isLoading = false
            self.errorMessage = "Network error: \(error.localizedDescription)"
        }
        return false
    }
    
    /// Verifies the 6-digit magic code and logs in
    func verifyMagicCode(email: String, code: String) async -> Bool {
        self.isLoading = true
        self.errorMessage = nil
        
        guard let url = URL(string: "\(baseURL)/api/auth/verify-otp") else {
            self.errorMessage = "Invalid backend URL"
            self.isLoading = false
            return false
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = 10
        
        let cleanEmail = email.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        let cleanCode = code.trimmingCharacters(in: .whitespacesAndNewlines)
        let body: [String: Any] = ["email": cleanEmail, "code": cleanCode]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)
        
        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            self.isLoading = false
            
            guard let httpRes = response as? HTTPURLResponse else {
                self.errorMessage = "Invalid server response"
                return false
            }
            
            if httpRes.statusCode == 200 {
                if let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                   let token = json["token"] as? String {
                    
                    _ = KeychainManager.shared.saveString(token, service: tokenService, account: tokenAccount)
                    self.isLoggedIn = true
                    self.userEmail = cleanEmail
                    UserDefaults.standard.set(true, forKey: "userLoggedIn")
                    UserDefaults.standard.set(cleanEmail, forKey: "userEmail")
                    
                    if let user = json["user"] as? [String: Any], let userTier = user["tier"] as? String {
                        self.tier = userTier
                        UserDefaults.standard.set(userTier, forKey: "subscriptionTier")
                        self.monthlyLimitMinutes = (userTier.lowercased() == "free") ? 60.0 : 999999.0
                    }
                    
                    recalculateLocalMonthlyUsage()
                    LogManager.shared.log("LicenseManager: Magic code verified successfully for \(cleanEmail) (Tier: \(self.tier))")
                    
                    Task { @MainActor in
                        await self.fetchStatus()
                    }
                    return true
                }
            } else {
                if let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                   let err = json["error"] as? String {
                    self.errorMessage = err
                } else {
                    self.errorMessage = "Verification failed (Error \(httpRes.statusCode))"
                }
            }
        } catch {
            self.isLoading = false
            self.errorMessage = "Network error: \(error.localizedDescription)"
        }
        return false
    }
    
    /// Logs in against the VPS backend
    func login(email: String, password: String) async -> Bool {
        self.isLoading = true
        self.errorMessage = nil
        
        guard let url = URL(string: "\(baseURL)/api/auth/login") else {
            self.errorMessage = "Invalid backend URL"
            self.isLoading = false
            return false
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = 10
        
        let body: [String: Any] = ["email": email, "password": password]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)
        
        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            self.isLoading = false
            
            guard let httpRes = response as? HTTPURLResponse else {
                self.errorMessage = "Invalid server response"
                return false
            }
            
            if httpRes.statusCode == 200 {
                if let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                   let token = json["token"] as? String {
                    
                    // Save token
                    _ = KeychainManager.shared.saveString(token, service: tokenService, account: tokenAccount)
                    
                    self.isLoggedIn = true
                    self.userEmail = email
                    UserDefaults.standard.set(true, forKey: "userLoggedIn")
                    UserDefaults.standard.set(email, forKey: "userEmail")
                    
                    if let user = json["user"] as? [String: Any], let userTier = user["tier"] as? String {
                        self.tier = userTier
                        UserDefaults.standard.set(userTier, forKey: "subscriptionTier")
                    }
                    
                    recalculateLocalWeeklyUsage()
                    LogManager.shared.log("LicenseManager: Logged in successfully as \(email) (Tier: \(self.tier))")
                    return true
                }
            } else {
                if let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                   let err = json["error"] as? String {
                    self.errorMessage = err
                } else {
                    self.errorMessage = "Login failed (Error \(httpRes.statusCode))"
                }
            }
        } catch {
            self.isLoading = false
            // Fallback for offline / demo mode
            LogManager.shared.log("LicenseManager: Backend unreachable, enabling local demo mode.")
            if email.contains("@") && password.count >= 4 {
                self.isLoggedIn = true
                self.userEmail = email
                UserDefaults.standard.set(true, forKey: "userLoggedIn")
                UserDefaults.standard.set(email, forKey: "userEmail")
                return true
            } else {
                self.errorMessage = "Could not connect to VPS server. Check connection."
            }
        }
        
        return false
    }
    
    func logout() {
        KeychainManager.shared.delete(service: tokenService, account: tokenAccount)
        self.isLoggedIn = false
        self.userEmail = ""
        self.tier = "free"
        UserDefaults.standard.set(false, forKey: "userLoggedIn")
        UserDefaults.standard.removeObject(forKey: "userEmail")
        UserDefaults.standard.set("free", forKey: "subscriptionTier")
        LogManager.shared.log("LicenseManager: Logged out.")
    }
    
    func startGoogleAuth() {
        LogManager.shared.log("LicenseManager: Starting Google OAuth flow...")
        if let url = URL(string: "\(baseURL)/api/auth/google?platform=mac") {
            NSWorkspace.shared.open(url)
        }
    }
    
    func openPricingWebsite() {
        if let url = URL(string: "https://cosmowhisper.com/pricing") {
            NSWorkspace.shared.open(url)
        }
    }
}
