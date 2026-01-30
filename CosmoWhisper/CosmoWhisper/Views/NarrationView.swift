import SwiftUI
import AppKit

struct NarrationView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Narration")
                .font(.system(size: 32, weight: .bold))
            Text("Test and configure your AI voice.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            SpeechPlayground()
            
            Spacer()
        }
    }
}

import AVFoundation

struct SpeechPlayground: View {
    @State private var synthesizer = AVSpeechSynthesizer()
    @State private var voices: [VoiceOption] = []
    
    @AppStorage("narrationVoiceID") private var selectedVoiceID = "com.apple.speech.synthesis.voice.Alex"
    @AppStorage("narrationRate") private var speechRate: Double = 0.5 // AV uses 0.0-1.0
    @AppStorage("narrationPitch") private var speechPitch: Double = 1.0 // AV uses 0.5-2.0
    
    @State private var testText = "Hello! I am your new Cosmo Whisperer assistant. How does my voice sound?"
    @State private var isSpeaking = false
    
    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
             SettingsCard(title: "Voice Design", icon: "person.wave.2.fill") {
                 VStack(alignment: .leading, spacing: 16) {
                     Picker("Voice", selection: $selectedVoiceID) {
                         ForEach(voices) { voice in
                             Text(voice.displayName).tag(voice.id)
                         }
                     }
                     .labelsHidden()
                     .pickerStyle(.menu)
                     .frame(maxWidth: 300)
                     
                     Divider().opacity(0.1)
                     
                     VStack(alignment: .leading, spacing: 4) {
                         HStack {
                             Text("Speaking Rate")
                             Spacer()
                             Text(String(format: "%.2f", speechRate)).font(.caption).foregroundColor(.secondary)
                         }
                         Slider(value: $speechRate, in: 0.25...0.75, step: 0.05)
                     }
                     
                     VStack(alignment: .leading, spacing: 4) {
                         HStack {
                             Text("Pitch")
                             Spacer()
                             Text(String(format: "%.2f", speechPitch)).font(.caption).foregroundColor(.secondary)
                         }
                         Slider(value: $speechPitch, in: 0.8...1.5, step: 0.1)
                     }
                 }
             }
             
             SettingsCard(title: "Playground", icon: "play.circle.fill") {
                 VStack(alignment: .leading, spacing: 16) {
                     TextEditor(text: $testText)
                         .font(.body)
                         .padding(12)
                         .background(Color.black.opacity(0.2))
                         .cornerRadius(8)
                         .frame(height: 100)
                         .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.1), lineWidth: 1))
                     
                     HStack {
                         Button(action: toggleSpeech) {
                             HStack {
                                 Image(systemName: isSpeaking ? "stop.fill" : "play.fill")
                                 Text(isSpeaking ? "Stop" : "Test Voice")
                             }
                             .padding(.horizontal, 16)
                             .padding(.vertical, 8)
                             .background(isSpeaking ? Color.red : Color.blue)
                             .foregroundColor(.white)
                             .cornerRadius(8)
                         }
                         .buttonStyle(.plain)
                     }
                 }
             }
        }
        .onAppear(perform: loadVoices)
        .onReceive(Timer.publish(every: 0.5, on: .main, in: .common).autoconnect()) { _ in
            isSpeaking = synthesizer.isSpeaking
        }
    }
    
    // Switch to modern AVFoundation API
    private func toggleSpeech() {
        if synthesizer.isSpeaking {
            synthesizer.stopSpeaking(at: .immediate)
            isSpeaking = false
        } else {
            let utterance = AVSpeechUtterance(string: testText)
            utterance.voice = AVSpeechSynthesisVoice(identifier: selectedVoiceID)
            utterance.rate = Float(speechRate)
            utterance.pitchMultiplier = Float(speechPitch)
            utterance.volume = 1.0
            
            synthesizer.speak(utterance)
            isSpeaking = true
        }
    }
    
    private func loadVoices() {
        let allVoices = AVSpeechSynthesisVoice.speechVoices()
        // Filter for high quality english voices primarily, but include others
        self.voices = allVoices
            .filter { $0.language.starts(with: "en") }
            .map { VoiceOption(id: $0.identifier, name: $0.name, country: $0.language, quality: $0.quality) }
            .sorted { $0.name < $1.name }
        
        // Ensure default selection is valid
        if !self.voices.contains(where: { $0.id == selectedVoiceID }) {
            if let first = self.voices.first { selectedVoiceID = first.id }
        }
    }
}

struct VoiceOption: Identifiable, Hashable {
    let id: String
    let name: String
    let country: String
    let quality: AVSpeechSynthesisVoiceQuality
    
    var displayName: String {
        let qualityBadge = (quality == .enhanced) ? "⭐️ " : ""
        return "\(qualityBadge)\(name) (\(country))"
    }
}
