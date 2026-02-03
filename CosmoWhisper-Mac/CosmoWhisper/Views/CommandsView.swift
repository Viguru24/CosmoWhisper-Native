import SwiftUI

struct CommandItem: Identifiable {
    let id = UUID()
    let title: String
    let icon: String
    let triggers: [String]
    let desc: String
}

struct CommandsView: View {
    let commands: [CommandItem] = [
        CommandItem(title: "The Reader", icon: "speaker.wave.2.fill", triggers: ["Read This", "Speak"], desc: "Reads the selected text out loud using the best AI voice on your Mac. Stop it by saying \"Shush\"."),
        CommandItem(title: "Tone Shift: Pro", icon: "briefcase.fill", triggers: ["Make Professional"], desc: "Transforms casual slang into corporate-speak. \"Sup bro\" becomes \"Greetings, colleague\"."),
        CommandItem(title: "Tone Shift: Chill", icon: "face.smiling.fill", triggers: ["Make Friendly"], desc: "Loosens up stiff text. Perfect for Slack messages. \"Per our discussion\" becomes \"Like we chatted about\"."),
        CommandItem(title: "The Rewriter", icon: "arrow.triangle.2.circlepath", triggers: ["Rewrite This"], desc: "Stuck on a sentence? Let AI rephrase it for clarity and impact. It's like having a built-in copilot."),
        CommandItem(title: "Translator", icon: "globe", triggers: ["Translate to Spanish", "Translate to French"], desc: "Instant translation of selected text. Great for learning or just showing off."),
        CommandItem(title: "Task Master", icon: "checklist", triggers: ["Extract Action Items"], desc: "Scans a long email or meeting note and pulls out a neat Todo list of tasks."),
        CommandItem(title: "The Explainer", icon: "brain.head.profile", triggers: ["Explain This"], desc: "Confused by jargon? Select it and say \"Explain This\". AI will break it down like you're 5."),
        CommandItem(title: "Ask AI", icon: "questionmark.circle", triggers: ["Ask [Question]"], desc: "Directly answers your question instead of typing it. Try: \"Ask what is the airspeed velocity of an unladen swallow?\""),
        CommandItem(title: "Writer Mode", icon: "pencil.and.outline", triggers: ["Write [Topic]"], desc: "Uses AI to expand your short idea into a full paragraph. Try: \"Write a polite excuse for missing the morning meeting.\""),
        CommandItem(title: "Reply Mate", icon: "arrowshape.turn.up.left", triggers: ["Reply to this"], desc: "Reads the recent text and drafts a professional reply. Try: Select an angry email and say \"Reply to this\". Watch the magic."),
        CommandItem(title: "System Access", icon: "macwindow", triggers: ["System Control", "Execute"], desc: "Converts voice to PowerShell commands. Try: \"System Control list all files in this directory.\""),
        CommandItem(title: "The Magnet", icon: "selection.pin.in.out", triggers: ["Select All"], desc: "Instantly highlights everything. Perfect for big moves. Use this before \"The Nuke\" for maximum efficiency."),
        CommandItem(title: "The Nuke", icon: "trash", triggers: ["Delete All", "Clear Field"], desc: "Wipes the slate clean. Warning: Very satisfying. When your draft is absolute garbage, just say \"Delete All\"."),
        CommandItem(title: "The Snip", icon: "scissors", triggers: ["Cut All", "Cut Everything"], desc: "Nabs everything and puts it in your clipboard. Highlight a messy block of text and say \"Cut Everything\"."),
        CommandItem(title: "Copy Cat", icon: "doc.on.doc", triggers: ["Copy That", "Copy All"], desc: "Snaps everything into your clipboard. Highlight a name, say \"Copy That\". Done."),
        CommandItem(title: "Time Machine", icon: "arrow.uturn.backward", triggers: ["Undo", "Undo That"], desc: "Didn't mean that? Step back in time instantly. It undoes your last typo (but sadly not your last relationship choice)."),
        CommandItem(title: "Fast Paste", icon: "doc.on.clipboard", triggers: ["Paste That", "Paste Here"], desc: "Dumps your clipboard contents exactly where you want them. Say \"Paste That\" to drop a link faster than a hot potato."),
        CommandItem(title: "Time Traveler", icon: "calendar", triggers: ["Insert Date"], desc: "Types today's full date (e.g., Monday, Jan 5th) so you don't have to. Instead of checking your phone, just say \"Insert Date\"."),
        CommandItem(title: "Clock Watcher", icon: "clock", triggers: ["Insert Time"], desc: "Types the current time (e.g., 9:14 PM). Check and type the time without looking up: \"Insert Time\"."),
        CommandItem(title: "Shout It", icon: "textformat.size", triggers: ["All Caps"], desc: "MAKES EVERYTHING UPPERCASE. USE RESPONSIBLY. For when you need to politely scream at someone."),
        CommandItem(title: "Whisper Mode", icon: "textformat", triggers: ["Lower Case"], desc: "Converts everything to lowercase. Stealthy. Cool down a loud sentence with \"Lower Case\"."),
        CommandItem(title: "Omni Search", icon: "magnifyingglass", triggers: ["Google [Topic]", "Search..."], desc: "Don't touch the keyboard. Just say \"Google Space X\". Try: \"Google why do cats knock things off tables?\""),
        CommandItem(title: "Tube Jumper", icon: "play.rectangle.fill", triggers: ["YouTube [Topic]"], desc: "Jumps straight to results on YouTube. Procrastination made easy. Try: \"YouTube lo-fi beats to relax and code to\"."),
        CommandItem(title: "The Editor", icon: "wand.and.stars", triggers: ["Fix Everything"], desc: "Uses AI to fix grammar and spelling. Your personal editor. Select your messy draft and say \"Fix Everything\"."),
        CommandItem(title: "The Digest", icon: "doc.text.below.ecg", triggers: ["Summarize This", "Summarize"], desc: "Too long; didn't read? Let AI shrink it for you. Select a giant legal contract and say \"Summarize This\"."),
        CommandItem(title: "The Squeezer", icon: "arrow.down.right.and.arrow.up.left", triggers: ["Shorter", "Condense"], desc: "Make your text concise and punchy. Turn a rambling email into a 2-sentence gem: \"Shorter\"."),
        CommandItem(title: "The Architect", icon: "list.bullet.rectangle", triggers: ["Flesh Out"], desc: "Turns your tiny notes into a detailed masterpiece. Say \"Flesh Out\" on your bullet points to write the report."),
        CommandItem(title: "The Bloom", icon: "arrow.up.left.and.arrow.down.right", triggers: ["Expand", "Lengthen"], desc: "Add flow and detail to your writing. Naturally adds flow and detail to your writing. Make a dry sentence more engaging with \"Expand\"."),
        CommandItem(title: "Digitizer", icon: "number.square", triggers: ["Numeral", "Digits"], desc: "Instantly turns 'ten' into '10' and 'forty two' into '42'."),
        CommandItem(title: "Word Smith", icon: "text.quote", triggers: ["Words", "Spelled Out"], desc: "Turns digits back into words. elegance in numbers."),
        CommandItem(title: "Strong Style", icon: "bold", triggers: ["Bold That", "Bold Text"], desc: "Makes your message stand out with bold formatting."),
        CommandItem(title: "The Lean", icon: "italic", triggers: ["Italicize", "Make Italic"], desc: "Adds emphasis with a stylish slant."),
        CommandItem(title: "Underliner", icon: "underline", triggers: ["Underline That"], desc: "Draws a line under your point. Literally."),
        CommandItem(title: "humpCase", icon: "textformat", triggers: ["Camel Case"], desc: "perfectForDevelopers (converts to camelCase)."),
        CommandItem(title: "snake_case", icon: "textformat", triggers: ["Snake Case"], desc: "for_the_python_lovers (converts to snake_case).")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HStack(alignment: .lastTextBaseline) {
                VStack(alignment: .leading, spacing: 8) {
                    Text("Magic Spells")
                        .font(.system(size: 32, weight: .bold))
                        .foregroundColor(.white)
                    Text("Say the magic words and I'll do the rest.")
                        .font(.body)
                        .foregroundColor(.gray)
                }
                
                Spacer()
                
                Button(action: {
                    if let url = URL(string: "https://cosmowhisper.com/smart-commands") {
                        NSWorkspace.shared.open(url)
                    }
                }) {
                    HStack {
                        Image(systemName: "globe")
                        Text("Cloud Library")
                    }
                    .font(.system(size: 13, weight: .bold))
                    .foregroundColor(.white)
                    .padding(.horizontal, 16)
                    .padding(.vertical, 10)
                    .background(theme.currentTheme.accent.opacity(0.1))
                    .cornerRadius(8)
                }
                .buttonStyle(.plain)
            }
            .padding(.bottom, 10)
            
            ScrollView {
                VStack(spacing: 24) {
                    ScrollView(.horizontal, showsIndicators: false) {
                        HStack(spacing: 16) {
                            HeroCard(emoji: "👄", title: "Read it to me", desc: "Tired of reading? Select text and say \"Read this\". It's like an audiobook for your emails.")
                            HeroCard(emoji: "🤪", title: "Change the Vibe", desc: "Sounding too stiff? Select your text and say \"Make Friendly\". Or go full corporate with \"Make Professional\".")
                            HeroCard(emoji: "🌍", title: "Polyglot Mode", desc: "Instant AI translation. Select text and say \"Translate to Spanish\". Hola!")
                        }
                        .padding(.horizontal, 1)
                    }
                    
                    LazyVGrid(columns: [GridItem(.adaptive(minimum: 280), spacing: 16)], spacing: 16) {
                        ForEach(commands) { cmd in
                            CommandCard(title: cmd.title, icon: cmd.icon, triggers: cmd.triggers, desc: cmd.desc)
                        }
                    }
                    .padding(.bottom, 40)
                }
            }
        }
    }
}
