import Foundation

enum TranscriptionType: String, Codable {
    case transcription = "Transcription"
    case translation = "Translation"
    case extraction = "Extraction"
    case correction = "Correction"
}

struct TranscriptionItem: Identifiable, Codable {
    var id = UUID()
    let text: String
    let date: Date
    let type: TranscriptionType
}
