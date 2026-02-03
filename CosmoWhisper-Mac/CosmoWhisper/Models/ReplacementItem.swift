import Foundation

struct ReplacementItem: Identifiable, Codable, Equatable {
    var id = UUID()
    var trigger: String
    var value: String
}
