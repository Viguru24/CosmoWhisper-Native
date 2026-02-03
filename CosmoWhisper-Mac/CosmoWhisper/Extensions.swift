import AppKit

extension NSWorkspace {
    func urlForApplication(withName name: String) -> URL? {
        let appPath = "/Applications/\(name).app"
        if FileManager.default.fileExists(atPath: appPath) {
            return URL(fileURLWithPath: appPath)
        }
        return nil
    }
}
