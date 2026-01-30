import SwiftUI

struct AccountView: View {
    let accentColor = Color(red: 59/255, green: 130/255, blue: 246/255)
    
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Account")
                .font(.system(size: 32, weight: .bold))
            Text("Manage your subscription and profile.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            SettingsCard(title: "Profile Info", icon: "person.crop.circle") {
                VStack(alignment: .leading, spacing: 24) {
                    HStack(spacing: 16) {
                        Circle()
                            .fill(accentColor.opacity(0.2))
                            .frame(width: 60, height: 60)
                            .overlay(Text("LD").font(.system(size: 20, weight: .bold)).foregroundColor(accentColor))
                        
                        VStack(alignment: .leading, spacing: 4) {
                            Text("Louis DeSouza")
                                .font(.headline)
                            Text("Pro Plan Subscriber")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                        }
                        Spacer()
                        
                        Button("Edit Profile") {}
                            .buttonStyle(.bordered)
                    }
                    
                    Divider().opacity(0.1)
                    
                    HStack {
                        VStack(alignment: .leading, spacing: 4) {
                            Text("Monthly Usage")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            Text("Unlimited")
                                .font(.headline)
                        }
                        Spacer()
                        ProgressView(value: 0.24)
                            .progressViewStyle(.linear)
                            .frame(width: 200)
                    }
                }
            }
        }
    }
}
