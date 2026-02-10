import SwiftUI

struct LoginView: View {
    @Binding var isLoggedIn: Bool
    @State private var email = ""
    @State private var password = ""
    @State private var errorMessage = ""
    @State private var isLoading = false
    
    var body: some View {
        ZStack {
            Color(red: 10/255, green: 10/255, blue: 18/255).ignoresSafeArea()
            
            VStack(spacing: 30) {
                // Logo logic
                ZStack {
                    RoundedRectangle(cornerRadius: 20)
                        .fill(LinearGradient(colors: [.blue, .purple], startPoint: .topLeading, endPoint: .bottomTrailing))
                        .frame(width: 80, height: 80)
                    Image(systemName: "mic.fill")
                        .font(.system(size: 40))
                        .foregroundColor(.white)
                }
                .shadow(color: .blue.opacity(0.3), radius: 20, x: 0, y: 10)
                
                VStack(spacing: 8) {
                    Text("Welcome Back")
                        .font(.system(size: 28, weight: .bold))
                        .foregroundColor(.white)
                    Text("Log in to your CosmoWhisper account")
                        .foregroundColor(.secondary)
                }
                
                VStack(spacing: 16) {
                    TextField("Email", text: $email)
                        .textFieldStyle(PlainTextFieldStyle())
                        .padding(12)
                        .background(Color.black.opacity(0.3))
                        .cornerRadius(8)
                        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.1), lineWidth: 1))
                    
                    SecureField("Password", text: $password)
                        .textFieldStyle(PlainTextFieldStyle())
                        .padding(12)
                        .background(Color.black.opacity(0.3))
                        .cornerRadius(8)
                        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.1), lineWidth: 1))
                    
                    if !errorMessage.isEmpty {
                        Text(errorMessage)
                            .foregroundColor(.red)
                            .font(.caption)
                    }
                    
                    Button(action: login) {
                        HStack {
                            if isLoading {
                                ProgressView().scaleEffect(0.5)
                            }
                            Text("Sign In")
                        }
                        .fontWeight(.bold)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                        .background(Color.blue)
                        .cornerRadius(8)
                        .foregroundColor(.white)
                    }
                    .buttonStyle(.plain)
                    .disabled(isLoading || email.isEmpty || password.isEmpty)
                }
                .padding(30)
                .background(Color.white.opacity(0.05))
                .cornerRadius(16)
                .frame(maxWidth: 400)
                
                Button("Create Account") {
                    if let url = URL(string: "https://cosmowhisper-app.web.app/register") {
                        NSWorkspace.shared.open(url)
                    }
                }
                .buttonStyle(.link)
                .foregroundColor(.blue)
                
                Button("🛠️ Dev Skip") {
                    isLoggedIn = true
                    UserDefaults.standard.set(true, forKey: "userLoggedIn")
                }
                .buttonStyle(.link)
                .foregroundColor(.gray)
                .padding(.top, 10)
            }
            .padding()
        }
    }
    
    func login() {
        isLoading = true
        errorMessage = ""
        
        // Mock Login Delay
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.5) {
            isLoading = false
            if email.contains("@") && password.count > 4 {
                // Success
                isLoggedIn = true
                UserDefaults.standard.set(true, forKey: "userLoggedIn")
                UserDefaults.standard.set(email, forKey: "userEmail")
            } else {
                errorMessage = "Invalid email or password."
            }
        }
    }
}
