import Foundation
import SwiftUI

class LogManager: ObservableObject {
    static let shared = LogManager()
    
    @Published var logs: [String] = []
    private let formatter = DateFormatter()
    private var logFileURL: URL?
    private let queue = DispatchQueue(label: "com.cosmowhisper.logmanager", qos: .background)
    
    init() {
        formatter.dateFormat = "HH:mm:ss.SSS"
        setupFileLogging()
    }
    
    private func setupFileLogging() {
        do {
            let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
            let logsDir = appSupport.appendingPathComponent("CosmoWhisper/Logs", isDirectory: true)
            try FileManager.default.createDirectory(at: logsDir, withIntermediateDirectories: true)
            logFileURL = logsDir.appendingPathComponent("app.log")
            
            let startLine = "\n--- SESSION STARTED: \(Date()) ---\n"
            if let data = startLine.data(using: .utf8) {
                if FileManager.default.fileExists(atPath: logFileURL!.path) {
                    if let fileHandle = try? FileHandle(forWritingTo: logFileURL!) {
                        fileHandle.seekToEndOfFile()
                        fileHandle.write(data)
                        fileHandle.closeFile()
                    }
                } else {
                    try data.write(to: logFileURL!)
                }
            }
        } catch {
            print("LogManager: Failed to setup file logging: \(error)")
        }
    }
    
    func log(_ message: String) {
        let timestamp = formatter.string(from: Date())
        let line = "[\(timestamp)] \(message)"
        print(line)
        
        if let url = logFileURL {
            queue.async {
                if let data = (line + "\n").data(using: .utf8) {
                    if let fileHandle = try? FileHandle(forWritingTo: url) {
                        defer { try? fileHandle.close() }
                        _ = try? fileHandle.seekToEnd()
                        _ = try? fileHandle.write(contentsOf: data)
                    } else {
                        try? data.write(to: url)
                    }
                }
            }
        }
        
        DispatchQueue.main.async {
            self.logs.insert(line, at: 0)
            if self.logs.count > 100 {
                self.logs.removeLast()
            }
        }
    }
    
    func clear() {
        DispatchQueue.main.async {
            self.logs.removeAll()
            if let url = self.logFileURL {
                try? "".write(to: url, atomically: true, encoding: .utf8)
            }
        }
    }
}
