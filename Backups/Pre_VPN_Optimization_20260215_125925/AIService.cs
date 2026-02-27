using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using CosmoWhisper.Managers;
using CosmoWhisper;

namespace CosmoWhisper.Services
{
    public class AIService
    {
        public static AIService Shared { get; } = new AIService();
        private readonly HttpClient _httpClient;
        private const string DirectUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
        private const string DirectChatUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string FirebaseProxyUrl = "https://cosmowhisper.com/api";
        
        // Performance Watch
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        
        private string ChatUrl => $"{PreferenceManager.Shared.Preferences.BackendUrl.TrimEnd('/')}/api/ai/chat";
        private string ProxyUrl => $"{PreferenceManager.Shared.Preferences.BackendUrl.TrimEnd('/')}/api/ai/transcribe";
        private bool _useProxy = false; // "Sticky" proxy mode if direct fails
        private bool _useChatProxy = false;

        public AIService()
        {
            // Configure HttpClientHandler to use system proxy (including VPN)
            var handler = new HttpClientHandler
            {
                UseProxy = true,
                Proxy = System.Net.WebRequest.GetSystemWebProxy(),
                PreAuthenticate = true,
                UseDefaultCredentials = true
            };
            
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // Increase timeout for VPN scenarios
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
            RefreshConfig();

            PreferenceManager.Shared.PreferencesUpdated += () => RefreshConfig();
        }

        private void RefreshConfig()
        {
            var p = PreferenceManager.Shared.Preferences;
            string defaultKey = "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";
            
            // If unlocked, we can use the custom key. Otherwise, we use the shared default.
            // Configuration is now handled dynamically per request
        }

        private const string GroqDefaultKey = "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"; // Fixed Typo (Double U)

        public async Task<string> Transcribe(string filePath)
        {
            var p = PreferenceManager.Shared.Preferences;
            string model = string.IsNullOrWhiteSpace(p.AIModel) ? "whisper-large-v3" : p.AIModel;

            // Helper to create fresh content for each attempt
            MultipartFormDataContent CreateContent()
            {
                var form = new MultipartFormDataContent();
                var fileBytes = File.ReadAllBytes(filePath); // Sync read is fine for small audio
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

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
                return form;
            }

#if DEBUG
            // In DEBUG mode, force LOCAL PROXY ONLY to avoid the 2-second timeout on direct/VPN
            var debugProxyUrl = PreferenceManager.Shared.Preferences.BackendUrl;
            if (string.IsNullOrEmpty(debugProxyUrl)) debugProxyUrl = "http://127.0.0.1:5000";
            
            // OPTIMIZATION: Force 127.0.0.1 instead of localhost to skip Windows DNS lag (::1 vs 127.0.0.1)
            debugProxyUrl = debugProxyUrl.Replace("localhost", "127.0.0.1");

            if (debugProxyUrl.Contains("127.0.0.1"))
            {
                using (var devContent = CreateContent())
                {
                    LogEvent($"[transcribe] DEBUG MODE: Skipping Direct. Going straight to Local Proxy: {debugProxyUrl}");
                    string result = await ExecuteWithFallback(debugProxyUrl, devContent, false, 8); 
                    if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                    LogEvent($"[transcribe] Local Dev Proxy failed ({result}). Falling back to cloud...");
                }
            }
#endif

            // 1. Try Direct Groq (ONLY if not in Debug/Local mode, or if debug failed)
            // But wait! If we are blocked, we shouldn't even wait 4 seconds. 
            // effective immediately: if we have a proxy set, use it FIRST.
            
            /* 
               Legacy Logic Removed for Speed:
               - No longer trying Direct first if we know we are in a "blocked" environment.
            */
            // Try Direct (only if not in sticky proxy mode)
            if (!_useProxy)
            {
                using (var directContent = CreateContent())
                {
                    LogEvent($"[transcribe] Trying Direct Groq: {DirectUrl}");
                    // IMPORTANT: Use a very short timeout (4s) for direct calls. 
                    // If it's blocked by a VPN or ISP, it will hang. Fast fallback is better.
                    string result = await ExecuteWithFallback(DirectUrl, directContent, true, 4);
                    
                    if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                    
                    LogEvent($"[transcribe] Direct Groq failed ({result}). Switching to Sticky Proxy Mode...");
                    _useProxy = true; 
                }
            }

            // Fallback 1: Firebase Proxy (Production Ready / API v2)
            using (var firebaseContent = CreateContent())
            {
                // The correct Firebase URL is without the /ai/ subpath
                string firebaseTranscribeUrl = "https://cosmowhisper.com/api/transcribe"; 
                LogEvent($"[transcribe] Trying Firebase Proxy: {firebaseTranscribeUrl}");
                string result = await ExecuteWithFallback(firebaseTranscribeUrl, firebaseContent, false, 30);
                if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                LogEvent($"[transcribe] Firebase Proxy failed ({result}). Trying Render Proxy...");
            }

            // Fallback 2: Render Proxy (Original Backend)
            using (var proxyContent = CreateContent())
            {
                 LogEvent($"[transcribe] Trying Render Proxy: {ProxyUrl}");
                 string result = await ExecuteWithFallback(ProxyUrl, proxyContent, false, 45);
                 if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                 
                 string backupProxyUrl = ProxyUrl.Replace("/api/ai/transcribe", "/api/transcribe");
                 LogEvent($"[transcribe] Trying Backup Proxy: {backupProxyUrl}");
                 return await ExecuteWithFallback(backupProxyUrl, proxyContent, false, 45);
            }
        }

        private void LogEvent(string msg)
        {
            try
            {
                string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = System.IO.Path.Combine(logDir, "cosmo_debug.log");
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}\n");
            }
            catch { }
        }

        private async Task<string> ExecuteWithFallback(string url, HttpContent content, bool isDirect, int timeoutSeconds = 60)
        {
            try
            {
                var p = PreferenceManager.Shared.Preferences;
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;

                if (isDirect)
                {
                    bool bypassAllowed = SubscriptionManager.Shared.IsUnlimited;
                    string groqKey = (!bypassAllowed ? GroqDefaultKey : (string.IsNullOrEmpty(p.GroqApiKey) ? GroqDefaultKey : p.GroqApiKey)).Trim();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", groqKey);
                }
                else // Proxy (Local, Render, etc.)
                {
                    if (!string.IsNullOrEmpty(p.AuthToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p.AuthToken);
                }

                _stopwatch.Restart();
                var response = await _httpClient.SendAsync(request, cts.Token);
                _stopwatch.Stop();
                long elapsed = _stopwatch.ElapsedMilliseconds;
                
                var responseBody = await response.Content.ReadAsStringAsync();
                
                LogEvent($"[ExecuteWithFallback] Request to {url} took {elapsed}ms. Status: {response.StatusCode}");

                // DETECT BLOCKING: If we get HTML instead of JSON from an API endpoint, it's a block
                if (responseBody.TrimStart().StartsWith("<!DOCTYPE html") || responseBody.TrimStart().StartsWith("<html"))
                {
                    LogEvent($"[ExecuteWithFallback] Detected HTML response (Block?) from {url}");
                    return "Error: Request blocked by security layer (HTML response received)";
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"API Error ({response.StatusCode}): {responseBody}";
                    // Log to file for transcription errors
                    if (url.Contains("transcribe") || url.Contains("transcriptions"))
                    {
                        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                        Directory.CreateDirectory(logDir);
                        var logPath = Path.Combine(logDir, "groq_errors.log");
                        try { File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {url} - {errorMsg} - Limit: {response.Headers.RetryAfter?.ToString() ?? "N/A"}\n"); } catch { }
                    }
                    return $"Error: {response.StatusCode} - {responseBody}";
                }

                if (url.Contains("transcribe") || url.Contains("transcriptions"))
                {
                    var res = JsonSerializer.Deserialize<TranscriptionResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.text ?? "";
                }
                else // Assume chat response
                {
                    var res = JsonSerializer.Deserialize<ChatResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.choices[0]?.message?.content ?? "";
                }
            }
            catch (OperationCanceledException)
            {
                return "Error: Request timed out (Canceled)";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GenerateSpeech(string text, string voice, double speed, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return "Error: API Key Required";

            try
            {
                using var ttsClient = new HttpClient();
                ttsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                ttsClient.DefaultRequestHeaders.Add("User-Agent", "CosmoWhisper-Native");

                var request = new
                {
                    model = "tts-1",
                    input = text,
                    voice = voice.ToLower().Trim(),
                    speed = speed,
                    response_format = "mp3"
                };

                System.Diagnostics.Debug.WriteLine($"[TTS] Requesting voice: {voice} at speed {speed}");

                var response = await ttsClient.PostAsJsonAsync("https://api.openai.com/v1/audio/speech", request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[TTS Error] {response.StatusCode}: {err}");
                    return $"Error: {response.StatusCode} - {err}";
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var path = Path.Combine(Path.GetTempPath(), $"cosmo_speech_{Guid.NewGuid()}.mp3");
                await File.WriteAllBytesAsync(path, bytes);
                
                System.Diagnostics.Debug.WriteLine($"[TTS Success] Saved to {path} ({bytes.Length} bytes)");
                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS Exception] {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> ProcessCommand(string prompt, string context, bool usePersonality = true)
        {
            var p = PreferenceManager.Shared.Preferences;
            string langHint = GetLanguageHint(p.InterfaceLanguage);
            string personalityHint = usePersonality ? GetPersonalityHint(p.AIPersonality) : "";
            string finalPrompt = prompt + langHint + personalityHint;

            string modelName = "llama-3.3-70b-versatile"; 
            bool isFastTrack = prompt.Length < 100 && context.Length < 500;
            if (isFastTrack) modelName = "llama-3.1-8b-instant";

            var payload = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = finalPrompt },
                    new { role = "user", content = context }
                },
                temperature = isFastTrack ? 0.3 : 0.5
            };

            var content = JsonContent.Create(payload);

            System.Diagnostics.Debug.WriteLine($"[Chat] Sending to: {DirectChatUrl}");

            // Try Direct first (unless we are in sticky proxy mode)
            if (!_useProxy)
            {
                string result = await ExecuteWithFallback(DirectChatUrl, content, true);
                if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                
                System.Diagnostics.Debug.WriteLine("[AIService] Chat Direct failed. Switching to Sticky Proxy Mode...");
                _useProxy = true;
            }

            System.Diagnostics.Debug.WriteLine("[AIService] Using Proxies (Firebase/Render)...");
            string firebaseChatUrl = $"{FirebaseProxyUrl}/chat/completions";
            string fbResult = await ExecuteWithFallback(firebaseChatUrl, JsonContent.Create(payload), false);
            
            if (!fbResult.StartsWith("Error:") && !fbResult.Contains("Canceled")) return fbResult;

            System.Diagnostics.Debug.WriteLine("[AIService] Firebase Chat failed. Trying Render Proxy...");
            return await ExecuteWithFallback(ChatUrl, JsonContent.Create(payload), false);
        }

        private string GetPersonalityHint(string personality)
        {
            string prompt = "";

            // 1. Personality
            switch (personality)
            {
                case "Professional": prompt += " [SYSTEM: Tone: Formal, objective, business-like.]"; break;
                case "Friendly": prompt += " [SYSTEM: Tone: Warm, casual, helpful, use emojis.]"; break;
                case "Sassy": prompt += " [SYSTEM: Tone: Witty, playful, sarcastic, have attitude.]"; break;
                case "Guru": prompt += " [SYSTEM: Tone: Wise, philosophical, profound, metaphorical.]"; break;
                case "Pirate": prompt += " [SYSTEM: Tone: Pirate speech, nautical slang.]"; break;
            }

            // 2. Verbosity
            var verbosity = PreferenceManager.Shared.Preferences.AIVerbosity;
            switch (verbosity)
            {
                case "Concise": prompt += " [SYSTEM: Length: Extremely brief, bullet points, minimal words.]"; break;
                case "Detailed": prompt += " [SYSTEM: Length: Comprehensive, in-depth, cover nuances.]"; break;
                case "Balanced": prompt += " [SYSTEM: Length: Balanced, moderate detail.]"; break;
            }

            return prompt;
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
        public void WarmUp()
        {
            Task.Run(async () =>
            {
                try
                {
                    LogEvent("[WarmUp] 🚀 Warming up connection to Groq/Proxy...");
                    // Just hit the root or a lightweight endpoint to establish TCP/SSL handshake
                    // This eliminates the 1-2s "Cold Start" delay on the first request.
                    var url = PreferenceManager.Shared.Preferences.BackendUrl;
                    if (string.IsNullOrEmpty(url) || url.Contains("localhost")) url = "http://127.0.0.1:5000";
                    
                    using (var cts = new CancellationTokenSource(3000))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Head, url);
                         // Fire and forget, we just want the connection open
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    }
                    LogEvent("[WarmUp] ✅ Connection established and ready.");
                }
                catch (Exception ex)
                {
                    LogEvent($"[WarmUp] ⚠️ Warm-up ping failed (Non-fatal): {ex.Message}");
                }
            });
        }
    }
}



