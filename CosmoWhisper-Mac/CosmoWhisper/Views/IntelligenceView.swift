import SwiftUI

struct IntelligenceView: View {
    @State private var groqApiKey = ""
    @AppStorage("aiModel") private var aiModel = "llama-3.3-70b-versatile"
    @AppStorage("aiPersonality") private var aiPersonality = "balanced"
    @AppStorage("isApiKeyLocked") private var isApiKeyLocked = true
    @State private var unlockCode = ""
    @State private var errorMessage: String?

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("The Big Brain")
                .font(.system(size: 32, weight: .bold))
            Text("Customize the voice inside your machine.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            VStack(alignment: .leading, spacing: 24) {
                apiServiceProviderSection
                apiKeySection
                aiPersonalitySection

            }
            .padding(24)
            .background(Color(red: 10/255, green: 15/255, blue: 30/255))
            .cornerRadius(16)
            .overlay(RoundedRectangle(cornerRadius: 16).stroke(Color.blue.opacity(0.1), lineWidth: 1))
            
            Spacer()
        }
        .onAppear {
            groqApiKey = KeychainManager.shared.readString(service: "com.cosmowhisper.api", account: "groq") ?? ""
        }
    }
    
    private var apiServiceProviderSection: some View {
        VStack(alignment: .leading, spacing: 16) {
            VStack(alignment: .leading, spacing: 6) {
                Text("AI Service Provider")
                    .font(.headline)
                Text("Groq (Free & Fast)")
                    .font(.system(size: 14, weight: .medium))
                    .padding(.horizontal, 12)
                    .padding(.vertical, 8)
                    .background(Color.white.opacity(0.05))
                    .cornerRadius(8)
            }
            
            VStack(alignment: .leading, spacing: 6) {
                Text("Model Name")
                    .font(.headline)
                Text(aiModel)
                    .font(.system(size: 14, weight: .medium))
                    .padding(.horizontal, 12)
                    .padding(.vertical, 8)
                    .background(Color.white.opacity(0.05))
                    .cornerRadius(8)
            }
        }
    }
    
    private var apiKeySection: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: "key.fill")
                    .font(.caption)
                Text("Groq API Key")
                    .font(.headline)
                
                if !isApiKeyLocked {
                    Spacer()
                    Button(action: { isApiKeyLocked = true }) {
                        HStack(spacing: 4) {
                            Image(systemName: "lock.fill")
                            Text("Lock")
                        }
                        .font(.caption).bold()
                        .foregroundColor(.orange)
                    }
                    .buttonStyle(.plain)
                }
            }
            
            if isApiKeyLocked {
                VStack(alignment: .leading, spacing: 12) {
                    HStack {
                        Text("Locked")
                            .foregroundColor(.gray)
                        Image(systemName: "lock.fill")
                            .font(.caption)
                            .foregroundColor(.gray)
                        Spacer()
                    }
                    .padding(12)
                    .background(Color.black.opacity(0.3))
                    .cornerRadius(8)
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.1), lineWidth: 1))
                    
                    VStack(alignment: .leading, spacing: 6) {
                        Text("Enterprise Unlock Code")
                            .font(.caption).bold()
                            .foregroundColor(.blue)
                        
                        TextField("Enter Enterprise Unlock Code", text: $unlockCode)
                            .textFieldStyle(PlainTextFieldStyle())
                            .padding(10)
                            .background(Color.white.opacity(0.05))
                            .cornerRadius(6)
                            .onChange(of: unlockCode) { newValue in
                                if newValue == "10810" {
                                    isApiKeyLocked = false
                                    unlockCode = ""
                                }
                            }
                        
                        Text("Need a code? Contact enterprise support.")
                            .font(.caption2)
                            .foregroundColor(.secondary)
                    }
                    
                    HStack(spacing: 6) {
                        Image(systemName: "exclamationmark.circle")
                            .foregroundColor(.yellow)
                        Text("Custom API Keys are restricted to Enterprise Tier")
                            .foregroundColor(.yellow)
                            .font(.caption)
                    }
                }
            } else {
                VStack(alignment: .leading, spacing: 8) {
                    TextField("Enter your Groq API Key", text: $groqApiKey)
                        .textFieldStyle(PlainTextFieldStyle())
                        .padding(12)
                        .background(Color.blue.opacity(0.1))
                        .cornerRadius(8)
                        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.blue.opacity(0.3), lineWidth: 1))
                        .onChange(of: groqApiKey) { newValue in
                            let success = KeychainManager.shared.saveString(newValue, service: "com.cosmowhisper.api", account: "groq")
                            if !success {
                                self.errorMessage = "Failed to save to Keychain"
                            } else {
                                self.errorMessage = nil
                            }
                        }
                    
                    if let error = errorMessage {
                        Text(error)
                            .font(.caption)
                            .foregroundColor(.red)
                    } else {
                        Text("API Key Unlocked. Changes save automatically.")
                            .font(.caption)
                            .foregroundColor(.green)
                    }
                }
            }
        }
    }
    
    private var aiPersonalitySection: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack {
                Image(systemName: "message.fill")
                    .font(.caption)
                Text("Vibe Check (Personality Settings)")
                    .font(.headline)
            }
            
            HStack(spacing: 8) {
                personalityButton(title: "All Raw", subtitle: "Literal. No fixes.", type: "literal")
                personalityButton(title: "Straight Talk", subtitle: "No fluff, concise", type: "concise")
                personalityButton(title: "The Sweet Spot", subtitle: "Verbatim with fixes", type: "balanced")
                personalityButton(title: "Storyteller", subtitle: "Professional polish", type: "detailed")
            }
        }
        .padding(16)
        .background(Color.white.opacity(0.03))
        .cornerRadius(12)
    }
    
    private func personalityButton(title: String, subtitle: String, type: String) -> some View {
        let isSelected = aiPersonality == type
        let color: Color = (type == "balanced") ? .purple : .blue
        
        return Button(action: { aiPersonality = type }) {
            VStack(spacing: 4) {
                Text(title)
                    .fontWeight(.bold)
                    .foregroundColor(isSelected ? .white : .gray)
                Text(subtitle)
                    .font(.caption)
                    .foregroundColor(isSelected ? .white.opacity(0.7) : .gray)
            }
            .frame(maxWidth: .infinity)
            .padding(.vertical, 12)
            .background(isSelected ? color.opacity(0.3) : Color.white.opacity(0.05))
            .cornerRadius(8)
            .overlay(RoundedRectangle(cornerRadius: 8).stroke(isSelected ? color : Color.clear, lineWidth: 1))
        }
        .buttonStyle(.plain)
    }
    

}
