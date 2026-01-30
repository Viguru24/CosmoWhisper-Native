import SwiftUI

struct HeroCard: View {
    let emoji: String
    let title: String
    let desc: String
    
    @State private var isHovered = false
    
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text(emoji)
                .font(.system(size: 32))
                .padding(10)
                .background(Color.white.opacity(0.1))
                .cornerRadius(12)
            
            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.system(size: 16, weight: .bold))
                    .foregroundColor(.white)
                
                Text(desc)
                    .font(.system(size: 13))
                    .foregroundColor(.secondary)
                    .lineLimit(3)
                    .fixedSize(horizontal: false, vertical: true)
            }
        }
        .padding(20)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(
            ZStack {
                Color(red: 30/255, green: 41/255, blue: 59/255)
                
                if isHovered {
                    RoundedRectangle(cornerRadius: 16)
                        .stroke(
                            LinearGradient(
                                colors: [.blue.opacity(0.6), .purple.opacity(0.6)],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            ),
                            lineWidth: 2
                        )
                }
            }
        )
        .cornerRadius(16)
        .contentShape(Rectangle()) // Make the entire box clickable
        .shadow(color: Color.black.opacity(0.2), radius: 10, x: 0, y: 5)
        .onHover { hovering in
            withAnimation(.spring(response: 0.3)) {
                isHovered = hovering
            }
        }
    }
}
