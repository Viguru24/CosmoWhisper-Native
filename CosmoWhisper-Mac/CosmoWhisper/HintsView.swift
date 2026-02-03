import SwiftUI

struct HintsView: View {
    let hints = [
        Hint(title: "The Power of Right Option", icon: "keyboard", text: "Hold your hotkey (Right Option or Mouse Button) to talk, release it to instantly paste. No clicking required."),
        Hint(title: "System Control", icon: "command", text: "Say 'Select all', 'Copy all', or 'Delete all' to manage text without touching the keyboard."),
        Hint(title: "Instant Translation", icon: "globe", text: "Select clear text and say 'Translate to Spanish' (or French) to instantly replace it."),
        Hint(title: "Smart Commands", icon: "bolt.fill", text: "Try saying 'new line', 'bold that', or 'comma' during transcription to format your text on the fly.")
    ]
    
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            Text("Pro Tips & Hints")
                .font(.headline)
            
            VStack(spacing: 12) {
                ForEach(hints) { hint in
                    HStack(spacing: 16) {
                        Image(systemName: hint.icon)
                            .font(.system(size: 18))
                            .foregroundColor(.blue)
                            .frame(width: 32, height: 32)
                            .background(Color.blue.opacity(0.1))
                            .cornerRadius(8)
                        
                        VStack(alignment: .leading, spacing: 2) {
                            Text(hint.title)
                                .font(.system(size: 14, weight: .bold))
                            Text(hint.text)
                                .font(.system(size: 13))
                                .foregroundColor(.secondary)
                        }
                        Spacer()
                    }
                    .padding(12)
                    .background(Color.white.opacity(0.03))
                    .cornerRadius(12)
                }
            }
        }
        .padding(24)
        .background(Color(red: 30/255, green: 40/255, blue: 60/255).opacity(0.3))
        .cornerRadius(16)
        .overlay(RoundedRectangle(cornerRadius: 16).stroke(Color.white.opacity(0.05), lineWidth: 1))
    }
}

struct Hint: Identifiable {
    let id = UUID()
    let title: String
    let icon: String
    let text: String
}
