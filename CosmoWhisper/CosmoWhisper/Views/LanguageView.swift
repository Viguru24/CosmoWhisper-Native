import SwiftUI

struct LanguageView: View {
    @AppStorage("primaryLanguage") private var primaryLanguage = "en"
    @AppStorage("autoTranslation") private var autoTranslation = false
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Language")
                .font(.system(size: 32, weight: .bold))
            Text("Global transcription and UI language.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            SettingsCard(title: "Regional Settings", icon: "globe") {
                VStack(alignment: .leading, spacing: 20) {
                    HStack {
                        Text("Primary Language")
                        Spacer()
                        Picker("", selection: $primaryLanguage) {
                            Text("English (US)").tag("en-US")
                            Text("English (UK)").tag("en-GB")
                            Text("Spanish").tag("es")
                            Text("French").tag("fr")
                            Text("German").tag("de")
                            Text("Auto Detect").tag("auto")
                        }
                        .labelsHidden()
                        .frame(width: 150)
                    }
                    
                    if primaryLanguage == "en-GB" {
                        Text("Groq will prioritize British spelling (e.g. 'Colour', 'Organise').")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                    
                    Divider().opacity(0.1)
                    
                    Toggle("Automatic Translation", isOn: $autoTranslation)
                }
            }
            
            Spacer()
        }
    }
}
