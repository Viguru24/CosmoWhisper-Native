import SwiftUI

struct HistoryView: View {
    @State private var searchText = ""
    
    @State private var history: [TranscriptionItem] = []
    
    @ObservedObject var theme = ThemeManager.shared
    
    private func loadHistory() {
        if let data = UserDefaults.standard.data(forKey: "recentTranscriptions"),
           let decoded = try? JSONDecoder().decode([TranscriptionItem].self, from: data) {
            self.history = decoded.sorted(by: { $0.date > $1.date })
        }
    }
    
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Interaction History")
                        .font(.system(size: 32, weight: .black))
                    Text("Revisit your past transcriptions and AI magic.")
                        .foregroundColor(.secondary)
                }
                Spacer()
            }
            .padding(.bottom, 10)
            
            // Search Bar
            HStack {
                Image(systemName: "magnifyingglass")
                    .foregroundColor(.secondary)
                TextField("Search history...", text: $searchText)
                    .textFieldStyle(.plain)
            }
            .padding(12)
            .background(Color.white.opacity(0.05))
            .cornerRadius(10)
            .overlay(RoundedRectangle(cornerRadius: 10).stroke(Color.white.opacity(0.1), lineWidth: 1))
            
            ScrollView {
                VStack(spacing: 12) {
                    if history.isEmpty {
                        VStack(spacing: 20) {
                            Image(systemName: "clock.arrow.circlepath")
                                .font(.system(size: 40))
                                .foregroundColor(.secondary.opacity(0.5))
                            Text("No history yet. Start talking!")
                                .foregroundColor(.secondary)
                        }
                        .frame(maxWidth: .infinity, minHeight: 300)
                    } else {
                        ForEach(history) { item in
                            HistoryRow(item: item)
                        }
                    }
                }
            }
        }
        .onAppear(perform: loadHistory)
        .onReceive(NotificationCenter.default.publisher(for: NSNotification.Name("RecentActivityChanged"))) { _ in
            loadHistory()
        }
        .onReceive(NotificationCenter.default.publisher(for: UserDefaults.didChangeNotification)) { _ in
            loadHistory()
        }
    }
}

struct HistoryRow: View {
    let item: TranscriptionItem
    @ObservedObject var theme = ThemeManager.shared
    @State private var isHovered = false
    
    var body: some View {
        HStack(spacing: 16) {
            ZStack {
                RoundedRectangle(cornerRadius: 10)
                    .fill(theme.currentTheme.accent.opacity(0.1))
                    .frame(width: 44, height: 44)
                
                Image(systemName: iconForType(item.type))
                    .foregroundColor(theme.currentTheme.accent)
            }
            
            VStack(alignment: .leading, spacing: 4) {
                Text(item.text)
                    .lineLimit(2)
                    .font(.system(size: 14, weight: .medium))
                    .foregroundColor(.white)
                
                Text(item.date.formatted(date: .abbreviated, time: .shortened))
                    .font(.system(size: 11))
                    .foregroundColor(.secondary)
            }
            
            Spacer()
            
            Button(action: {
                NSPasteboard.general.clearContents()
                NSPasteboard.general.setString(item.text, forType: .string)
            }) {
                Image(systemName: "doc.on.doc")
                    .foregroundColor(isHovered ? .white : .secondary)
            }
            .buttonStyle(.plain)
        }
        .padding(12)
        .background(Color.white.opacity(isHovered ? 0.08 : 0.03))
        .cornerRadius(12)
        .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.white.opacity(isHovered ? 0.2 : 0.05), lineWidth: 1))
        .onHover { hovering in
            withAnimation(.easeInOut(duration: 0.2)) {
                isHovered = hovering
            }
        }
    }
    
    func iconForType(_ type: TranscriptionType) -> String {
        switch type {
        case .transcription: return "mic.fill"
        case .translation: return "globe"
        case .extraction: return "list.bullet.indent"
        case .correction: return "wand.and.stars"
        }
    }
}
