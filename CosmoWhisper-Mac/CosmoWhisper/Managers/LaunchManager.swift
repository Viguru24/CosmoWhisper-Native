import Foundation
import ServiceManagement
import OSLog

class LaunchManager: ObservableObject {
    static let shared = LaunchManager()
    private let logger = Logger(subsystem: "com.cosmowhisper.CosmoWhisper", category: "LaunchManager")
    
    func setLaunchAtLogin(_ enabled: Bool) {
        do {
            if enabled {
                try SMAppService.mainApp.register()
                LogManager.shared.log("LaunchManager: Registered for launch at login.")
            } else {
                try SMAppService.mainApp.unregister()
                LogManager.shared.log("LaunchManager: Unregistered from launch at login.")
            }
        } catch {
            LogManager.shared.log("LaunchManager ERROR: Failed to update launch at login status: \(error.localizedDescription)")
        }
    }
    
    var isRegistered: Bool {
        return SMAppService.mainApp.status == .enabled
    }
}
