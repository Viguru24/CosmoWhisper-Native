import Foundation
import AppKit

struct LicenseStatus: Codable {
    let tier: String
    let usageMinutes: Double
    let limitMinutes: Int
    let isOverLimit: Bool
}

class LicenseManager: ObservableObject {
    static let shared = LicenseManager()
    
    private let session = URLSession.shared
    
    func syncStatus() async -> Bool {
        let token = UserDefaults.standard.string(forKey: "licenseToken") ?? ""
        let backendUrl = UserDefaults.standard.string(forKey: "backendUrl") ?? "http://localhost:5000"
        
        guard !token.isEmpty else { return false }
        
        guard let url = URL(string: "\(backendUrl)/api/license/status") else { return false }
        
        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        
        do {
            let (data, response) = try await session.data(for: request)
            if let httpResponse = response as? HTTPURLResponse, httpResponse.statusCode == 200 {
                let status = try JSONDecoder().decode(LicenseStatus.self, from: data)
                
                DispatchQueue.main.async {
                    UserDefaults.standard.set(status.tier, forKey: "userTier")
                    UserDefaults.standard.set(status.usageMinutes, forKey: "usageMinutes")
                    UserDefaults.standard.set(status.limitMinutes, forKey: "usageLimitMinutes")
                    UserDefaults.standard.set(!status.isOverLimit, forKey: "isAIUnlocked")
                    LogManager.shared.log("LICENSE: Sync successful. Tier: \(status.tier)")
                }
                return true
            }
        } catch {
            LogManager.shared.log("LICENSE: Sync failed: \(error.localizedDescription)")
        }
        
        return false
    }
    
    func reportUsage(durationSeconds: Double) async {
        let token = UserDefaults.standard.string(forKey: "licenseToken") ?? ""
        let backendUrl = UserDefaults.standard.string(forKey: "backendUrl") ?? "http://localhost:5000"
        
        guard !token.isEmpty else { return }
        
        guard let url = URL(string: "\(backendUrl)/api/license/report-usage") else { return }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        
        let body = ["durationMs": durationSeconds * 1000]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)
        
        do {
            let (_, response) = try await session.data(for: request)
            if let httpResponse = response as? HTTPURLResponse, httpResponse.statusCode == 200 {
                let _ = await syncStatus()
            }
        } catch {
            LogManager.shared.log("LICENSE: Report failed: \(error.localizedDescription)")
        }
    }
}
