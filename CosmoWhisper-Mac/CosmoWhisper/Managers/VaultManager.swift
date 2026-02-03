import Foundation
import AppKit
import CryptoKit

class VaultManager {
    static let shared = VaultManager()
    
    struct VaultData: Codable {
        let timestamp: Date
        let appVersion: String
        let settingsPlist: Data
        let keychainItems: [String: String]? // Future proofing
    }
    
    // MARK: - Encryption Helpers
    private func getSymmetricKey(from password: String, salt: Data) -> SymmetricKey {
        // We use HKDF to derive a strong key from the user's password
        let pwdData = password.data(using: .utf8)!
        let key = SymmetricKey(data: SHA256.hash(data: pwdData)) // Simple hash for 256 bits, or use HKDF if specific salt storage needed.
        // For simplicity in this local implementation without separate salt storage complexity:
        // We will hash the password to get a 32-byte key.
        return key
    }
    
    // MARK: - Public API
    func createVault(at url: URL, password: String) -> Bool {
        let allDefaults = UserDefaults.standard.dictionaryRepresentation()
        var exportDict: [String: Any] = [:]
        
        // Filter out system keys
        for (key, val) in allDefaults {
            if key.starts(with: "NS") || key.starts(with: "Apple") || key.starts(with: "pbs_") || key == "WebKit" { continue }
            exportDict[key] = val
        }
        
        do {
            // 1. Serialize Settings
            let plistData = try PropertyListSerialization.data(fromPropertyList: exportDict, format: .binary, options: 0)
            
            // 2. Create Vault Struct
            let vault = VaultData(
                timestamp: Date(),
                appVersion: Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0",
                settingsPlist: plistData,
                keychainItems: [
                    "groq_api": KeychainManager.shared.readString(service: "com.cosmowhisper.api", account: "groq") ?? ""
                ]

            )
            
            // 3. Encode to JSON
            let jsonEncoder = JSONEncoder()
            jsonEncoder.dateEncodingStrategy = .iso8601
            let rawData = try jsonEncoder.encode(vault)
            
            // 4. Encrypt (AES-GCM 256)
            let key = getSymmetricKey(from: password, salt: Data())
            let sealedBox = try AES.GCM.seal(rawData, using: key)
            let encryptedData = sealedBox.combined! // sealedBox contains nonce + tag + ciphertext
            
            // 5. Write to File
            try encryptedData.write(to: url)
            
            LogManager.shared.log("Vault: Encrypted backup created at \(url.path) with \(exportDict.count) keys.")
            return true
            
        } catch {
            LogManager.shared.log("Vault Creation Error: \(error)")
            return false
        }
    }
    
    func restoreVault(from url: URL, password: String) -> Bool {
        do {
            // 1. Read Encrypted Data
            let encryptedData = try Data(contentsOf: url)
            
            // 2. Decrypt (AES-GCM 256)
            let key = getSymmetricKey(from: password, salt: Data())
            let sealedBox = try AES.GCM.SealedBox(combined: encryptedData)
            let decryptedData = try AES.GCM.open(sealedBox, using: key)
            
            // 3. Decode Vault Data
            let jsonDecoder = JSONDecoder()
            jsonDecoder.dateDecodingStrategy = .iso8601
            let vault = try jsonDecoder.decode(VaultData.self, from: decryptedData)
            
            // 4. Restore Settings
            let restoredSettings = try PropertyListSerialization.propertyList(from: vault.settingsPlist, options: [], format: nil) as? [String: Any]
            guard let settings = restoredSettings else { return false }
            
            for (key, val) in settings {
                UserDefaults.standard.set(val, forKey: key)
            }
            
            LogManager.shared.log("Vault: Restored \(settings.count) keys from backup dated \(vault.timestamp)")
            
            // Force synchronize and notify system
            UserDefaults.standard.synchronize()
            NotificationCenter.default.post(name: UserDefaults.didChangeNotification, object: nil)
            
            // Restore Keychain items if any
            if let keychain = vault.keychainItems {
                if let groqKey = keychain["groq_api"], !groqKey.isEmpty {
                    _ = KeychainManager.shared.saveString(groqKey, service: "com.cosmowhisper.api", account: "groq")
                    LogManager.shared.log("Vault: Restored Groq API Key to Keychain")
                }
            }

            return true
            
        } catch {
            LogManager.shared.log("Vault Restore Failed: \(error). Likely wrong password or corrupted file.")
            return false
        }
    }
}
