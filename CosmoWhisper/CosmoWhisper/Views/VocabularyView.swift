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
                    .help("Instantly clear all custom vocabulary and snippet replacements")
                }
                
                Text("Customize recognition for specialized industry jargon, names, acronyms, and auto-expanding snippets.")
                    .font(.system(size: 16))
                    .foregroundColor(.secondary)
            }
            
            ScrollView {
                VStack(alignment: .leading, spacing: 40) {
                    // --- SECTION 1: TRANSCRIPTION HINTS ---
                    VStack(alignment: .leading, spacing: 18) {
                        HStack {
                            Label("Transcription Hints (Bias Vocabulary)", systemImage: "text.bubble.fill")
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
                                .frame(height: 120)
                                .background(Color.black.opacity(0.4))
                                .cornerRadius(12)
                                .overlay(
                                    RoundedRectangle(cornerRadius: 12)
                                        .stroke(LinearGradient(colors: [.white.opacity(0.1), .clear], startPoint: .topLeading, endPoint: .bottomTrailing), lineWidth: 1)
                                )
                            
                            if transcriptionHints.isEmpty {
                                Text("e.g. Kubernetes, PostgreSQL, Sarah Jenkins, OpenAI, proprietary acronyms, client names...")
                                    .font(.system(size: 14))
                                    .foregroundColor(.white.opacity(0.2))
                                    .padding(16)
                                    .allowsHitTesting(false)
                            }
                        }
                        
                        // Suggestion Templates
                        VStack(alignment: .leading, spacing: 8) {
                            Text("Quick Add Industry Presets:")
                                .font(.caption.bold())
                                .foregroundColor(.secondary)
                            
                            HStack(spacing: 8) {
                                PresetChip(title: "+ Tech / Dev", color: .blue) {
                                    appendHints("Kubernetes, PostgreSQL, GraphQL, Docker, TypeScript")
                                }
                                PresetChip(title: "+ Medical", color: .green) {
                                    appendHints("Hypertension, Arrhythmia, Acetaminophen, MRI, Stethoscope")
                                }
                                PresetChip(title: "+ Legal / Business", color: .purple) {
                                    appendHints("Affidavit, Indemnification, NDA, EBITDA, Jurisdiction")
                                }
                            }
                        }
                        
                        Text("List terms separated by commas. These are sent directly to the AI model to bias recognition accuracy.")
                            .font(.system(size: 12))
                            .foregroundColor(.secondary.opacity(0.8))
                    }
                    .padding(24)
                    .background(
                        RoundedRectangle(cornerRadius: 20)
                            .fill(Color.white.opacity(0.03))
                            .overlay(RoundedRectangle(cornerRadius: 20).stroke(Color.white.opacity(0.05), lineWidth: 1))
                    )
                    
                    // --- SECTION 2: INSTANT CORRECTIONS & SNIPPETS ---
                    VStack(alignment: .leading, spacing: 24) {
                        HStack {
                            Label("Instant Snippets & Auto-Expansions", systemImage: "sparkles")
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
                        HStack(alignment: .top, spacing: 12) {
                            VStack(alignment: .leading, spacing: 6) {
                                TextField("When I say... (e.g. 'my address')", text: $newTrigger)
                                    .textFieldStyle(.plain)
                                    .padding(12)
                                    .background(Color.black.opacity(0.3))
                                    .cornerRadius(10)
                                    .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
                            }
                            .frame(maxWidth: 220)
                            
                            Image(systemName: "arrow.right")
                                .foregroundColor(.secondary.opacity(0.5))
                                .padding(.top, 14)
                            
                            VStack(alignment: .leading, spacing: 6) {
                                if isSecureMode {
                                    SecureField("Type this...", text: $newValue)
                                        .textFieldStyle(.plain)
                                        .padding(12)
                                        .background(Color.black.opacity(0.3))
                                        .cornerRadius(10)
                                        .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
                                } else {
                                    ZStack(alignment: .topLeading) {
                                        TextEditor(text: $newValue)
                                            .font(.system(size: 13))
                                            .scrollContentBackground(.hidden)
                                            .padding(8)
                                            .frame(minHeight: 48, maxHeight: 110)
                                            .background(Color.black.opacity(0.3))
                                            .cornerRadius(10)
                                            .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
                                        
                                        if newValue.isEmpty {
                                            Text("Type this... (Press Enter for new line)")
                                                .font(.system(size: 13))
                                                .foregroundColor(.white.opacity(0.25))
                                                .padding(.horizontal, 12)
                                                .padding(.vertical, 10)
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
                                VStack(spacing: 14) {
                                    Text("No custom snippet rules yet.")
                                        .font(.subheadline.bold())
                                        .foregroundColor(.secondary)
                                    
                                    Text("Try adding quick voice expansions like:")
                                        .font(.caption)
                                        .foregroundColor(.secondary.opacity(0.7))
                                    
                                    HStack(spacing: 10) {
                                        ExampleSnippetChip(trigger: "my meeting link", expansion: "https://zoom.us/j/123456") {
                                            newTrigger = "my meeting link"
                                            newValue = "https://zoom.us/j/1234567890"
                                        }
                                        ExampleSnippetChip(trigger: "sign off", expansion: "Kind regards,\n[Name]") {
                                            newTrigger = "sign off"
                                            newValue = "Kind regards,\n[Your Name]"
                                        }
                                        ExampleSnippetChip(trigger: "brb", expansion: "Be right back in 5 mins!") {
                                            newTrigger = "brb"
                                            newValue = "Be right back in 5 minutes!"
                                        }
                                    }
                                }
                                .frame(maxWidth: .infinity)
                                .padding(.vertical, 24)
                                .background(Color.white.opacity(0.01))
                                .cornerRadius(12)
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
                                            .padding(.top, 4)
                                        
                                        Group {
                                            if isSecureMode {
                                                SecureField("", text: $item.value)
                                                    .textFieldStyle(.plain)
                                            } else {
                                                TextEditor(text: $item.value)
                                                    .font(.system(size: 13, design: .monospaced))
                                                    .scrollContentBackground(.hidden)
                                                    .padding(6)
                                                    .frame(minHeight: 36, maxHeight: 100)
                                                    .background(Color.black.opacity(0.2))
                                                    .cornerRadius(8)
                                            }
                                        }
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
                                        .padding(.top, 2)
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
    
    private func appendHints(_ terms: String) {
        if transcriptionHints.isEmpty {
            transcriptionHints = terms
        } else {
            transcriptionHints += ", " + terms
        }
    }
    
    private func addReplacement() {
        let newItem = ReplacementItem(trigger: newTrigger.trimmingCharacters(in: .whitespaces), value: newValue.trimmingCharacters(in: .whitespaces))
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

// Preset Chip Component
struct PresetChip: View {
    let title: String
    let color: Color
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            Text(title)
                .font(.system(size: 11, weight: .bold))
                .foregroundColor(color)
                .padding(.horizontal, 10)
                .padding(.vertical, 5)
                .background(color.opacity(0.12))
                .cornerRadius(6)
        }
        .buttonStyle(.plain)
    }
}

// Example Snippet Chip
struct ExampleSnippetChip: View {
    let trigger: String
    let expansion: String
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            VStack(alignment: .leading, spacing: 3) {
                Text("\"\(trigger)\"")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.white)
                Text("→ \(expansion)")
                    .font(.system(size: 10))
                    .foregroundColor(.secondary)
                    .lineLimit(1)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(Color.white.opacity(0.05))
            .cornerRadius(8)
            .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.08), lineWidth: 1))
        }
        .buttonStyle(.plain)
    }
}
