import SwiftUI

struct VocabularyView: View {
    @AppStorage("customVocabulary") private var transcriptionHints = ""
    @State private var replacements: [ReplacementItem] = []
    
    @State private var newTrigger = ""
    @State private var newValue = ""
    @State private var isSecureMode = false
    
    @ObservedObject var theme = ThemeManager.shared
    
    var body: some View {
        VStack(alignment: .leading, spacing: 32) {
            VStack(alignment: .leading, spacing: 12) {
                HStack(alignment: .bottom) {
                    Text("Your Secret Language")
                        .font(.system(size: 34, weight: .black))
                        .foregroundStyle(LinearGradient(colors: [.white, .white.opacity(0.7)], startPoint: .top, endPoint: .bottom))
                    
                    Spacer()
                    
                    Button(action: {
                        transcriptionHints = ""
                        replacements = []
                        saveReplacements()
                    }) {
                        Label("Emergency Wipe", systemImage: "shredder.fill")
                            .font(.system(size: 10, weight: .black))
                            .padding(.horizontal, 10)
                            .padding(.vertical, 6)
                            .background(Color.red.opacity(0.15))
                            .foregroundColor(.red)
                            .cornerRadius(8)
                    }
                    .buttonStyle(.plain)
                    .help("Instantly clear all names, addresses, and hints")
                }
                
                Text("Teach me your slang, your friends' names, and your bad habits.")
                    .font(.system(size: 16))
                    .foregroundColor(.secondary)
            }
            
            ScrollView {
                VStack(alignment: .leading, spacing: 40) {
                    // --- SECTION 1: TRANSCRIPTION HINTS ---
                    VStack(alignment: .leading, spacing: 20) {
                        HStack {
                            Label("Transcription Hints", systemImage: "text.bubble.fill")
                                .font(.system(size: 16, weight: .bold))
                                .foregroundColor(theme.currentTheme.accent)
                            
                            Spacer()
                            
                            if !transcriptionHints.isEmpty {
                                Button(action: { 
                                    withAnimation { transcriptionHints = "" }
                                }) {
                                    Image(systemName: "trash")
                                        .font(.system(size: 12))
                                        .foregroundColor(.red.opacity(0.7))
                                        .frame(width: 28, height: 28)
                                        .background(Color.red.opacity(0.1))
                                        .clipShape(Circle())
                                }
                                .buttonStyle(.plain)
                            }
                        }
                        
                        ZStack(alignment: .topLeading) {
                            TextEditor(text: $transcriptionHints)
                                .font(.system(size: 14, design: .monospaced))
                                .scrollContentBackground(.hidden)
                                .padding(12)
                                .frame(height: 140)
                                .background(Color.black.opacity(0.4))
                                .cornerRadius(12)
                                .overlay(
                                    RoundedRectangle(cornerRadius: 12)
                                        .stroke(LinearGradient(colors: [.white.opacity(0.1), .clear], startPoint: .topLeading, endPoint: .bottomTrailing), lineWidth: 1)
                                )
                            
                            if transcriptionHints.isEmpty {
                                Text("e.g. Louis de Souza, Groq API, specialized jargon, family names...")
                                    .font(.system(size: 14))
                                    .foregroundColor(.white.opacity(0.2))
                                    .padding(16)
                                    .allowsHitTesting(false)
                            }
                        }
                        
                        Text("List terms separated by commas. These are sent directly to the AI for better recognition.")
                            .font(.system(size: 12))
                            .foregroundColor(.secondary.opacity(0.8))
                    }
                    .padding(24)
                    .background(
                        RoundedRectangle(cornerRadius: 20)
                            .fill(Color.white.opacity(0.03))
                            .overlay(RoundedRectangle(cornerRadius: 20).stroke(Color.white.opacity(0.05), lineWidth: 1))
                    )
                    
                    // --- SECTION 2: INSTANT CORRECTIONS ---
                    VStack(alignment: .leading, spacing: 24) {
                        HStack {
                            Label("Instant Corrections", systemImage: "sparkles")
                                .font(.system(size: 16, weight: .bold))
                                .foregroundColor(theme.currentTheme.accent)
                            
                            Spacer()
                            
                            Button(action: { 
                                withAnimation(.spring(response: 0.3)) { isSecureMode.toggle() }
                            }) {
                                HStack(spacing: 8) {
                                    Image(systemName: isSecureMode ? "lock.fill" : "lock.open.fill")
                                    Text(isSecureMode ? "PROTECTED" : "SECURE MODE")
                                }
                                .font(.system(size: 10, weight: .black))
                                .padding(.horizontal, 12)
                                .padding(.vertical, 6)
                                .background(isSecureMode ? Color.green.opacity(0.2) : Color.white.opacity(0.1))
                                .foregroundColor(isSecureMode ? .green : .secondary)
                                .cornerRadius(20)
                            }
                            .buttonStyle(.plain)
                        }
                        
                        // Add New Form
                        HStack(spacing: 12) {
                            VStack(alignment: .leading, spacing: 6) {
                                TextField("When I say...", text: $newTrigger)
                                    .textFieldStyle(.plain)
                                    .padding(12)
                                    .background(Color.black.opacity(0.3))
                                    .cornerRadius(10)
                                    .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
                            }
                            
                            Image(systemName: "arrow.right")
                                .foregroundColor(.secondary.opacity(0.5))
                            
                            VStack(alignment: .leading, spacing: 6) {
                                if isSecureMode {
                                    SecureField("Type this...", text: $newValue)
                                        .textFieldStyle(.plain)
                                        .padding(12)
                                        .background(Color.black.opacity(0.3))
                                        .cornerRadius(10)
                                        .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
                                } else {
                                    // Use TextEditor to allow multiline input (e.g. addresses)
                                    ZStack(alignment: .topLeading) {
                                        TextEditor(text: $newValue)
                                            .font(.system(size: 13))
                                            .scrollContentBackground(.hidden)
                                            .padding(8)
                                            .frame(height: 80)
                                            .background(Color.black.opacity(0.3))
                                            .cornerRadius(10)
                                            .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
                                        
                                        if newValue.isEmpty {
                                            Text("Type this...")
                                                .foregroundColor(.white.opacity(0.2))
                                                .font(.system(size: 13))
                                                .padding(.leading, 12)
                                                .padding(.top, 14)
                                                .allowsHitTesting(false)
                                        }
                                    }
                                }
                            }
                            
                            Button(action: addReplacement) {
                                Text("Add")
                                    .font(.system(size: 13, weight: .black))
                                    .foregroundColor(.white)
                                    .padding(.horizontal, 24)
                                    .padding(.vertical, 12)
                                    .background(theme.accentGradient)
                                    .cornerRadius(10)
                                    .shadow(color: theme.currentTheme.accent.opacity(0.3), radius: 10, x: 0, y: 5)
                            }
                            .buttonStyle(.plain)
                            .disabled(newTrigger.isEmpty || newValue.isEmpty)
                        }
                        .padding(.bottom, 8)
                        
                        // List items
                        VStack(spacing: 12) {
                            if replacements.isEmpty {
                                Text("No custom rules defined.")
                                    .font(.subheadline)
                                    .foregroundColor(.secondary)
                                    .frame(maxWidth: .infinity)
                                    .padding(.vertical, 20)
                            } else {
                                ForEach($replacements) { $item in
                                    HStack(alignment: .top, spacing: 16) {
                                        TextField("", text: $item.trigger)
                                            .textFieldStyle(.plain)
                                            .font(.system(size: 14, weight: .bold))
                                            .frame(maxWidth: 160)
                                            .onChange(of: item.trigger) { _ in saveReplacements() }
                                        
                                        Image(systemName: "chevron.right")
                                            .font(.caption2)
                                            .foregroundColor(.secondary.opacity(0.3))
                                            .padding(.top, 8)
                                        
                                        Group {
                                            if isSecureMode {
                                                SecureField("", text: $item.value)
                                                    .textFieldStyle(.plain)
                                            } else {
                                                TextEditor(text: $item.value)
                                                    .scrollContentBackground(.hidden)
                                                    .frame(minHeight: 40, maxHeight: 120)
                                                    .padding(6)
                                                    .background(Color.black.opacity(0.2))
                                                    .cornerRadius(8)
                                            }
                                        }
                                        .font(.system(size: 14, design: .monospaced))
                                        .foregroundColor(theme.currentTheme.accent)
                                        .onChange(of: item.value) { _ in saveReplacements() }
                                        
                                        Spacer()
                                        
                                        Button(action: {
                                            withAnimation { deleteReplacement(item) }
                                        }) {
                                            Image(systemName: "xmark.circle.fill")
                                                .foregroundColor(.white.opacity(0.15))
                                                .font(.system(size: 18))
                                        }
                                        .buttonStyle(.plain)
                                        .padding(.top, 4)
                                    }
                                    .padding(.horizontal, 16)
                                    .padding(.vertical, 12)
                                    .background(Color.white.opacity(0.02))
                                    .cornerRadius(12)
                                    .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.white.opacity(0.05), lineWidth: 1))
                                }
                            }
                        }
                    }
                    .padding(24)
                    .background(
                        RoundedRectangle(cornerRadius: 20)
                            .fill(Color.white.opacity(0.03))
                            .overlay(RoundedRectangle(cornerRadius: 20).stroke(Color.white.opacity(0.05), lineWidth: 1))
                    )
                }
                .padding(.bottom, 60)
            }
        }
        .onAppear(perform: loadReplacements)
        .onReceive(NotificationCenter.default.publisher(for: UserDefaults.didChangeNotification)) { _ in
            loadReplacements()
        }
    }
    
    private func addReplacement() {
        let newItem = ReplacementItem(
            trigger: newTrigger.trimmingCharacters(in: .whitespacesAndNewlines),
            value: newValue.trimmingCharacters(in: .whitespaces) // preserves internal newlines, only trims outer space/newlines
        )
        replacements.insert(newItem, at: 0)
        saveReplacements()
        newTrigger = ""
        newValue = ""
    }
    
    private func deleteReplacement(_ item: ReplacementItem) {
        if let index = replacements.firstIndex(where: { $0.id == item.id }) {
            replacements.remove(at: index)
            saveReplacements()
        }
    }
    
    private func loadReplacements() {
        if let data = UserDefaults.standard.data(forKey: "replacementsJSON_v3") {
            if let decoded = try? JSONDecoder().decode([ReplacementItem].self, from: data) {
                self.replacements = decoded
            }
        } else {
            self.replacements = []
            saveReplacements()
        }
    }
    
    private func saveReplacements() {
        if let encoded = try? JSONEncoder().encode(replacements) {
            UserDefaults.standard.set(encoded, forKey: "replacementsJSON_v3")
        }
    }
}
