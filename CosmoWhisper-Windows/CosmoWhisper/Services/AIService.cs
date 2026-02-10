using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CosmoWhisper.Managers;
using CosmoWhisper;

namespace CosmoWhisper.Services
{
    public class AIService
    {
        public static AIService Shared { get; } = new AIService();

        private readonly HttpClient _httpClient;
        private const string TranscriptionUrl = "https://api.groq.com/openai/v1/audio/transcriptions";

        public AIService()
        {
            _httpClient = new HttpClient();
            RefreshConfig();

            PreferenceManager.Shared.PreferencesUpdated += () => RefreshConfig();
        }

        private void RefreshConfig()
        {
            var p = PreferenceManager.Shared.Preferences;
            string key = string.IsNullOrWhiteSpace(p.GroqApiKey)
                ? "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" // Default fallback
                : p.GroqApiKey;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        public async Task<string> Transcribe(string filePath)
        {
            try
            {
                var p = PreferenceManager.Shared.Preferences;
                string model = string.IsNullOrWhiteSpace(p.AIModel) ? "whisper-large-v3" : p.AIModel;

                using var form = new MultipartFormDataContent();
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mp4");

                form.Add(fileContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent(model), "model");
                form.Add(new StringContent("json"), "response_format");

                string langCode = p.InterfaceLanguage.ToLower().Split('-')[0];
                if (langCode != "auto")
                {
                    form.Add(new StringContent(langCode), "language");
                }

                string appContext = Managers.AudioRecorder.Shared.GetCurrentFocusedApp();
                string hints = Managers.VocabularyManager.Shared.GetActiveHints();
                string hintsText = string.IsNullOrWhiteSpace(hints) ? "" : $" Terms: {hints}.";

                string basePrompt = $"Transcribe verbatim. No repetition. No hallucination. {appContext}.{hintsText}";

                if (langCode == "en")
                {
                    string variant = p.InterfaceLanguage == "en-GB" ? "British English (colour, realise)" : "American English (color, realize)";
                    basePrompt = $"Transcribe verbatim in {variant}. No repetition. No hallucination. {appContext}.{hintsText}";
                }

                form.Add(new StringContent(basePrompt), "prompt");

                var response = await _httpClient.PostAsync(TranscriptionUrl, form);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var errorMsg = $"Groq API {response.StatusCode}: {errorBody}";
                    System.Diagnostics.Debug.WriteLine($"[Groq Error] {errorMsg}");

                    // Log to file with absolute path
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groq_errors.log");
                    try
                    {
                        File.AppendAllText(logPath,
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMsg}\n");
                    }
                    catch { }

                    // Show popup for critical errors
                    _ = CosmoMessage.Show("Groq API Error", $"API returned {response.StatusCode}. Check the logs for details.", "📡");

                    return $"Error: Groq API {response.StatusCode}";
                }

                var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>();
                return result?.text ?? "";
            }
            catch (Exception ex)
            {
                var errorMsg = $"Transcription exception: {ex.GetType().Name} - {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[Transcription] {errorMsg}");

                // Log to file with absolute path
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groq_errors.log");
                try
                {
                    File.AppendAllText(logPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - EXCEPTION: {errorMsg}\n");
                }
                catch { }

                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GenerateSpeech(string text, string voice, double speed, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return "Error: API Key Required";

            try
            {
                using var ttsClient = new HttpClient();
                ttsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var request = new
                {
                    model = "tts-1",
                    input = text,
                    voice = voice.ToLower(),
                    speed = speed
                };

                var response = await ttsClient.PostAsJsonAsync("https://api.openai.com/v1/audio/speech", request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {err}";
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var path = Path.Combine(Path.GetTempPath(), $"speech_{Guid.NewGuid()}.mp3");
                await File.WriteAllBytesAsync(path, bytes);
                return path;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> ProcessCommand(string prompt, string context)
        {
            try
            {
                var p = PreferenceManager.Shared.Preferences;
                string langHint = GetLanguageHint(p.InterfaceLanguage);
                string finalPrompt = prompt + langHint;

                // ITEM 4: Dual-Track Inference (Fast-track for short commands)
                // Use a smaller, faster model if the prompt or context is short
                string modelName = "llama-3.3-70b-versatile"; // Default "Cloud" track
                bool isFastTrack = prompt.Length < 100 && context.Length < 500;

                if (isFastTrack)
                {
                    modelName = "llama-3.1-8b-instant"; // Swift "Fast" track
                }

                var request = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "system", content = finalPrompt },
                        new { role = "user", content = context }
                    },
                    temperature = isFastTrack ? 0.3 : 0.5 // Lower temperature for fast commands to ensure precision
                };

                var response = await _httpClient.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Groq AI Error: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                return result?.choices[0]?.message?.content ?? "";
            }
            catch (Exception ex)
            {
                return $"Error processing command: {ex.Message}";
            }
        }

        private string GetLanguageHint(string langCode)
        {
            switch (langCode)
            {
                case "en-GB": return "\nIMPORTANT: Use British English spelling (e.g., colour, organise).";
                case "en-US": return "\nIMPORTANT: Use American English spelling (e.g., color, organize).";
                case "zh-CN": return "\nRespond in Chinese (Simplified).";
                case "es-ES": return "\nRespond in Spanish.";
                case "fr-FR": return "\nRespond in French.";
                case "de-DE": return "\nRespond in German.";
                case "ja-JP": return "\nRespond in Japanese.";
                case "pt-PT": return "\nRespond in Portuguese.";
                case "hi-IN": return "\nRespond in Hindi.";
                case "ar-SA": return "\nRespond in Arabic.";
                case "af-ZA": return "\nRespond in Afrikaans.";
                default: return "";
            }
        }

        private class ChatResponse
        {
            public Choice[] choices { get; set; } = Array.Empty<Choice>();
        }

        private class Choice
        {
            public Message message { get; set; } = new Message();
        }

        private class Message
        {
            public string content { get; set; } = "";
        }

        private class TranscriptionResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("text")]
            public string text { get; set; } = "";
        }
    }
}
