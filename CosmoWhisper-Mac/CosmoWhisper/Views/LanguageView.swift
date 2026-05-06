import SwiftUI

struct LanguageView: View {
    @AppStorage("primaryLanguage") private var primaryLanguage = "en"
    @AppStorage("autoTranslation") private var autoTranslation = false

    @ObservedObject var theme = ThemeManager.shared

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            VStack(alignment: .leading, spacing: 8) {
                Text("Language")
                    .font(.system(size: 28, weight: .bold))
                    .foregroundColor(theme.textPrimary)
                Text("Global transcription and UI language.")
                    .foregroundColor(theme.textSecondary)
            }

            SettingsCard(title: "Regional Settings", icon: "globe") {
                VStack(alignment: .leading, spacing: 20) {
                    HStack {
                        Text("Primary Language")
                            .foregroundColor(theme.textPrimary)
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
                        Text("Groq will prioritize British spelling (e.g. Colour, Organise).")
                            .font(.system(size: 12))
                            .foregroundColor(theme.textSecondary)
                    }

                    Divider().opacity(0.1)

                    Toggle("Automatic Translation", isOn: $autoTranslation)
                        .foregroundColor(theme.textPrimary)
                }
            }

            Spacer()
        }
    }
}
