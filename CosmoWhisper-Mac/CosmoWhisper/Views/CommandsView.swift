import SwiftUI

struct CommandItem: Identifiable {
    let id = UUID()
    let title: String
    let icon: String
    let triggers: [String]
    let desc: String
}

struct CommandsView: View {
    @ObservedObject var theme = ThemeManager.shared
    
    let commands: [CommandItem] = [
        CommandItem(title: "Read Aloud", icon: "speaker.wave.2.fill", triggers: ["Read This", "Speak"], desc: "Reads selected text out loud using the best AI voice on your Mac."),
        CommandItem(title: "Professional Tone", icon: "briefcase.fill", triggers: ["Make Professional"], desc: "Transforms casual text into corporate-speak."),
        CommandItem(title: "Friendly Tone", icon: "face.smiling.fill", triggers: ["Make Friendly"], desc: "Loosens up stiff text. Perfect for Slack messages."),
        CommandItem(title: "Rewrite", icon: "arrow.triangle.2.circlepath", triggers: ["Rewrite This"], desc: "AI rephrases your text for clarity and impact."),
        CommandItem(title: "Translate", icon: "globe", triggers: ["Translate to Spanish", "Translate to French"], desc: "Instant AI translation of selected text."),
        CommandItem(title: "Action Items", icon: "checklist", triggers: ["Extract Action Items"], desc: "Pulls out a neat todo list from emails or notes."),
        CommandItem(title: "Explain", icon: "brain.head.profile", triggers: ["Explain This"], desc: "Breaks down complex jargon like you're 5."),
        CommandItem(title: "Ask AI", icon: "questionmark.circle", triggers: ["Ask [Question]"], desc: "Directly answers your questions."),
        CommandItem(title: "Write", icon: "pencil.and.outline", triggers: ["Write [Topic]"], desc: "Expands your short idea into a full paragraph."),
        CommandItem(title: "Reply", icon: "arrowshape.turn.up.left", triggers: ["Reply to this"], desc: "Drafts a professional reply to selected text."),
        CommandItem(title: "Select All", icon: "selection.pin.in.out", triggers: ["Select All"], desc: "Highlights everything instantly."),
        CommandItem(title: "Delete All", icon: "trash", triggers: ["Delete All"], desc: "Wipes the current text field clean."),
        CommandItem(title: "Copy All", icon: "doc.on.doc", triggers: ["Copy All"], desc: "Copies everything to clipboard."),
        CommandItem(title: "Insert Date", icon: "calendar", triggers: ["Insert Date"], desc: "Types today's date automatically."),
        CommandItem(title: "Insert Time", icon: "clock", triggers: ["Insert Time"], desc: "Types the current time automatically."),
        CommandItem(title: "Bold", icon: "bold", triggers: ["Bold That"], desc: "Formats text as bold."),
        CommandItem(title: "Italic", icon: "italic", triggers: ["Italicize"], desc: "Formats text as italic."),
        CommandItem(title: "Summarize", icon: "doc.text.below.ecg", triggers: ["Summarize This"], desc: "Shrinks long text using AI."),
        CommandItem(title: "Shorter", icon: "arrow.down.right.and.arrow.up.left", triggers: ["Shorter"], desc: "Makes text more concise."),
        CommandItem(title: "Expand", icon: "arrow.up.left.and.arrow.down.right", triggers: ["Expand"], desc: "Adds flow and detail to writing.")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            VStack(alignment: .leading, spacing: 8) {
                Text("Voice Commands")
                    .font(.system(size: 28, weight: .bold))
                    .foregroundColor(theme.textPrimary)
                Text("Say the magic words and CosmoWhisper handles the rest.")
                    .foregroundColor(theme.textSecondary)
            }

            LazyVGrid(columns: [GridItem(.adaptive(minimum: 280), spacing: 16)], spacing: 16) {
                ForEach(commands) { cmd in
                    CommandCard(title: cmd.title, icon: cmd.icon, triggers: cmd.triggers, desc: cmd.desc)
                }
            }
        }
    }
}
