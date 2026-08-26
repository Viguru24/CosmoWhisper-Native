import Foundation
import Security

class KeychainManager {
    static let shared = KeychainManager()
    
    enum KeychainError: Error {
        case duplicateItem
        case itemNotFound
        case unexpectedStatus(OSStatus)
    }
    
    func save(_ data: Data, service: String, account: String) throws {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly,
            kSecUseDataProtectionKeychain as String: true
        ]
        
        // Delete any existing item first to avoid duplicates or prompt conflicts
        SecItemDelete(query as CFDictionary)
        
        let status = SecItemAdd(query as CFDictionary, nil)
        if status != errSecSuccess {
            // Fallback for sandboxed environments
            let fallbackKey = "keychain_fallback_\(service)_\(account)"
            UserDefaults.standard.set(data.base64EncodedString(), forKey: fallbackKey)
            LogManager.shared.log("KeychainManager: Stored with sandboxed fallback.")
        }
    }
    
    func read(service: String, account: String) -> Data? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne,
            kSecUseDataProtectionKeychain as String: true
        ]
        
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        
        if status == errSecSuccess, let data = result as? Data {
            return data
        }
        
        // Fallback read from sandboxed container
        let fallbackKey = "keychain_fallback_\(service)_\(account)"
        if let base64 = UserDefaults.standard.string(forKey: fallbackKey),
           let data = Data(base64Encoded: base64) {
            return data
        }
        
        return nil
    }
    
    func delete(service: String, account: String) {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecUseDataProtectionKeychain as String: true
        ]
        
        SecItemDelete(query as CFDictionary)
        let fallbackKey = "keychain_fallback_\(service)_\(account)"
        UserDefaults.standard.removeObject(forKey: fallbackKey)
    }
    
    // Convenience for String
    func saveString(_ string: String, service: String, account: String) -> Bool {
        if let data = string.data(using: .utf8) {
            do {
                try save(data, service: service, account: account)
                return true
            } catch {
                let fallbackKey = "keychain_fallback_\(service)_\(account)"
                UserDefaults.standard.set(string, forKey: fallbackKey)
                return true
            }
        }
        return false
    }
    
    func readString(service: String, account: String) -> String? {
        if let data = read(service: service, account: account) {
            return String(data: data, encoding: .utf8)
        }
        return nil
    }

    func hasKey(service: String, account: String) -> Bool {
        return read(service: service, account: account) != nil
    }
}
