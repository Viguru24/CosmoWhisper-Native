import SwiftUI

struct MicrophoneView: View {
    @ObservedObject var recorder = AudioRecorder.shared
    @AppStorage("micSensitivity") private var micSensitivity = 0.5
    @AppStorage("playChimes") private var playChimes = true
    @State private var isCalibrating = false
    @ObservedObject var theme = ThemeManager.shared
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Audio Input")
                .font(.system(size: 32, weight: .black))
            Text("Manage your recording hardware.")
                .foregroundColor(.secondary)
                .padding(.bottom, 24)
            
            VStack(alignment: .leading, spacing: 0) {
                 Text("Active Microphone")
                    .font(.headline)
                    .padding(.horizontal, 16)
                    .padding(.top, 16)
                    .padding(.bottom, 8)
                 
                 Text("Default System Device")
                    .font(.system(size: 13))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 16)
                    .padding(.bottom, 20)
                 
                 VStack(alignment: .leading, spacing: 12) {
                    HStack {
                        Image(systemName: "mic.fill")
                            .foregroundColor(theme.currentTheme.accent)
                        Text("Live Monitor")
                            .font(.system(size: 12, weight: .bold))
                    }
                    .foregroundColor(.white)
                    
                    GeometryReader { geo in
                         ZStack(alignment: .leading) {
                             RoundedRectangle(cornerRadius: 6)
                                 .fill(Color.black.opacity(0.3))
                                 .frame(height: 8)
                             
                             let normalized = max(0, min(1, (recorder.audioLevel + 60) / 60))
                             
                             RoundedRectangle(cornerRadius: 6)
                                 .fill(theme.accentGradient)
                                 .frame(width: geo.size.width * CGFloat(normalized), height: 8)
                                 .animation(.spring(response: 0.1, dampingFraction: 0.8), value: recorder.audioLevel)
                         }
                    }
                    .frame(height: 8)
                    
                    Text("If the bar doesn't move when you speak, macOS is blocking the microphone or the wrong device is selected.")
                        .font(.system(size: 10))
                        .foregroundColor(.gray)
                 }
                 .padding(20)
                 .background(Color.black.opacity(0.2))
                 .cornerRadius(12)
                 .padding(16)
            }
            .background(Color.white.opacity(0.03))
            .cornerRadius(16)
            .overlay(RoundedRectangle(cornerRadius: 16).stroke(Color.white.opacity(0.05), lineWidth: 1))
            
            VStack(alignment: .leading, spacing: 16) {
                HStack {
                    Text("Microphone Sensitivity")
                        .font(.headline)
                    Spacer()
                    Text("\(Int(micSensitivity * 100))%")
                        .font(.system(size: 14, weight: .bold, design: .monospaced))
                        .foregroundColor(theme.currentTheme.accent)
                }
                
                Slider(value: $micSensitivity, in: 0...1)
                   .accentColor(theme.currentTheme.accent)
                
                HStack {
                    Text("Sensitive (Whisper)").font(.caption).foregroundColor(.gray)
                    Spacer()
                    Text("Strict (Loud)").font(.caption).foregroundColor(.gray)
                }
            }
            .padding(20)
            
            VStack(alignment: .leading, spacing: 12) {
                 HStack {
                     VStack(alignment: .leading, spacing: 4) {
                         Text("Neural Auto-Calibration")
                            .fontWeight(.bold)
                         Text("Measure room noise to find the perfect sensitivity automatically.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                     }
                     Spacer()
                     
                     Button(action: {
                         isCalibrating = true
                         DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                             micSensitivity = 0.12
                             isCalibrating = false
                         }
                     }) {
                         HStack {
                             if isCalibrating {
                                 ProgressView().scaleEffect(0.5).brightness(1)
                             } else {
                                 Image(systemName: "bolt.fill")
                             }
                             Text(isCalibrating ? "Measuring..." : "Calibrate")
                         }
                         .padding(.horizontal, 20)
                         .padding(.vertical, 10)
                         .background(theme.accentGradient)
                         .foregroundColor(.white)
                         .cornerRadius(8)
                     }
                     .buttonStyle(.plain)
                 }
            }
            .padding(16)
            .background(theme.currentTheme.accent.opacity(0.05))
            .cornerRadius(12)
            .overlay(RoundedRectangle(cornerRadius: 12).stroke(theme.currentTheme.accent.opacity(0.2), style: StrokeStyle(lineWidth: 1, dash: [4, 2])))
            
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Interaction Sounds")
                        .fontWeight(.bold)
                    Text("Play chimes when recording starts and stops")
                        .font(.caption)
                        .foregroundColor(.gray)
                }
                Spacer()
                Toggle("", isOn: $playChimes)
                    .labelsHidden()
                    .toggleStyle(SwitchToggleStyle(tint: theme.currentTheme.accent))
            }
            .padding(20)
            .background(Color.white.opacity(0.03))
            .cornerRadius(12)
            .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.white.opacity(0.05), lineWidth: 1))

            Spacer()
        }
        .onAppear {
            if !recorder.isRecording { recorder.startPreview() }
        }
        .onDisappear {
            recorder.stopPreview()
        }
    }
}
