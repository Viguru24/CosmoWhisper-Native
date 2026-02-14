using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers
{
    public class NarrationManager
    {
        public static NarrationManager Shared { get; } = new NarrationManager();
        public event Action? SpeechStarted;
        public event Action? SpeechEnded;

        private NarrationManager() { }
        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                SpeechStarted?.Invoke();
                
                var p = PreferenceManager.Shared.Preferences;
                string voice = p.SelectedVoice;
                string apiKey = p.OpenAIApiKey;
                
                // Identify if it's a neural voice (OpenAI)
                string[] neuralVoices = { "Alloy", "Echo", "Fable", "Onyx", "Nova", "Shimmer" };
                bool isNeural = neuralVoices.Any(nv => string.Equals(nv, voice, StringComparison.OrdinalIgnoreCase));
                
                LogToFile($"Narration Request: Voice='{voice}', isNeural={isNeural}, hasKey={!string.IsNullOrWhiteSpace(apiKey)}");

                string audioFile = "";

                if (isNeural)
                {
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        LogToFile("Error: Neural voice selected but OpenAI API Key is missing.");
                        audioFile = "Error: OpenAI API Key Required for Neural Voices. Please add one in Voice Studio.";
                    }
                    else
                    {
                        // Use OpenAI Neural Voice
                        LogToFile($"Generating neural speech via OpenAI: {voice}");
                        audioFile = await AIService.Shared.GenerateSpeech(text, voice, p.VoiceSpeed, apiKey);
                    }
                }
                else
                {
                    // Use Local Windows TTS
                    LogToFile($"Generating local speech: {voice}");
                    using var synth = new SpeechSynthesizer();
                    
                    // Match the selected voice with multi-stage fallback for 100% reliability
                    var voices = SpeechSynthesizer.AllVoices;
                    var selectedVoice = voices.FirstOrDefault(v => v.Id == voice)
                                     ?? voices.FirstOrDefault(v => string.Equals(v.DisplayName, voice, StringComparison.OrdinalIgnoreCase))
                                     ?? voices.FirstOrDefault(v => v.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase));
                    
                    // Explicit Fallback to George (Royal Majesty) if selection is invalid/empty
                    if (selectedVoice == null)
                    {
                        selectedVoice = voices.FirstOrDefault(v => v.DisplayName.Contains("George")) 
                                     ?? voices.FirstOrDefault();
                        LogToFile($"Selection '{voice}' invalid. Falling back to: {selectedVoice?.DisplayName ?? "System Default"}");
                    }
                    
                    if (selectedVoice != null) 
                    {
                        synth.Voice = selectedVoice;
                        LogToFile($"Successfully loaded local voice: {selectedVoice.DisplayName}");
                    }
                    
                    synth.Options.AudioVolume = 1.0; // AudioRecorder handles volume playback
                    synth.Options.SpeakingRate = p.VoiceSpeed;
                    
                    var stream = await synth.SynthesizeTextToStreamAsync(text);
                    audioFile = Path.Combine(Path.GetTempPath(), $"narration_{Guid.NewGuid()}.wav");
                    
                    using (var dr = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)))
                    {
                        await dr.LoadAsync((uint)stream.Size);
                        byte[] buf = new byte[(int)stream.Size];
                        dr.ReadBytes(buf);
                        await File.WriteAllBytesAsync(audioFile, buf);
                    }
                }

                if (!string.IsNullOrEmpty(audioFile) && !audioFile.StartsWith("Error:"))
                {
                    await AudioRecorder.Shared.PlayAudio(audioFile);
                }
                else if (audioFile.StartsWith("Error:"))
                {
                    LogToFile($"Synthesis Error: {audioFile}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Speak Error: {ex.Message}");
            }
            finally
            {
                SpeechEnded?.Invoke();
            }
        }

        public void CancelSpeech()
        {
            LogToFile("Stopping speech playback.");
            AudioRecorder.Shared.StopPlayback();
        }

        private void LogToFile(string msg)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                Directory.CreateDirectory(path);
                File.AppendAllText(Path.Combine(path, "narration_debug.txt"), $"{DateTime.Now}: {msg}\n");
            }
            catch { }
        }
    }
}
