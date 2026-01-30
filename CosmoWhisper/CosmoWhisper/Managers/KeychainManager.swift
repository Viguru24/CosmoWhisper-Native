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
            kSecValueData as String: data
        ]
        
        let status = SecItemAdd(query as CFDictionary, nil)
        
        if status == errSecDuplicateItem {
            let updateQuery: [String: Any] = [
                kSecClass as String: kSecClassGenericPassword,
                kSecAttrService as String: service,
                kSecAttrAccount as String: account
            ]
            
            let attributesToUpdate: [String: Any] = [
                kSecValueData as String: data
            ]
            
            let updateStatus = SecItemUpdate(updateQuery as CFDictionary, attributesToUpdate as CFDictionary)
            if updateStatus != errSecSuccess {
                throw KeychainError.unexpectedStatus(updateStatus)
            }
        } else if status != errSecSuccess {
            throw KeychainError.unexpectedStatus(status)
        }
    }
    
    func read(service: String, account: String) -> Data? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ]
        
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        
        if status == errSecSuccess {
            return result as? Data
        }
        
        return nil
    }
    
    func delete(service: String, account: String) {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account
        ]
        
        SecItemDelete(query as CFDictionary)
    }
    
    // Convenience for String
    func saveString(_ string: String, service: String, account: String) -> Bool {
        if let data = string.data(using: .utf8) {
            do {
                try save(data, service: service, account: account)
                return true
            } catch {
        LogManager.shared.log("KeychainManager: Failed to save string for \(service)/\(account): \(error)")
                LogManager.shared.log("Keychain ERROR: Failed to save credential - \(error.localizedDescription)")
                return false
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
