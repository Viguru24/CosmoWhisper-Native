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
        private const string FirebaseProxyUrl = "https://CosmoWhisper.com/api";
        
        // Performance Watch
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _raceStopwatch = new System.Diagnostics.Stopwatch();
        
        public string LastTranscriptionSource { get; private set; } = "Unknown";
        public long LastTranscriptionTime { get; private set; } = 0;
        public bool IsAccessBlocked { get; set; } = false;
        
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

        }

        public void Initialize()
        {
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
            if (AppState.Shared.IsAccessBlocked)
            {
                LogEvent("[transcribe] 🛑 ACCESS BLOCKED: Account is flagged in dashboard. Triggering re-sync...");
                _ = Task.Run(() => AccountService.Shared.CheckAuthStatusAsync());
                return "Error: Your account has been blocked. Please contact support or check your dashboard.";
            }

            var p = PreferenceManager.Shared.Preferences;
            string model = string.IsNullOrWhiteSpace(p.AIModel) ? "whisper-large-v3" : p.AIModel;

            // 1. Unified Prompt & Language Calculation
            string langCode = p.InterfaceLanguage.ToLower().Split('-')[0];
            string safeLangCode = (langCode == "auto") ? "en" : langCode;
            
            string appContext = Managers.AudioRecorder.Shared.GetCurrentFocusedApp();
            if (appContext.Contains("Cosmo", StringComparison.OrdinalIgnoreCase)) appContext = "";
            
            string hints = Managers.VocabularyManager.Shared.GetActiveHints();
            string hintsText = string.IsNullOrWhiteSpace(hints) ? "" : $" {hints}.";

            string basePrompt = $"Transcribe accurately. Ignore non-speech sounds like coughs or background noise. New line, new paragraph. {appContext}{hintsText}".Trim();
            if (langCode == "en")
            {
                string variant = p.InterfaceLanguage == "en-GB" ? "British English (colour, realise)" : "American English (color, realize)";
                basePrompt = $"Transcribe accurately in {variant}. Ignore non-speech sounds like coughs, laughter, or background noise. New line, new paragraph. {appContext}{hintsText}".Trim();
            }

            // Helper to create fresh content for each attempt
            MultipartFormDataContent CreateContent()
            {
                var form = new MultipartFormDataContent();
                // We use shared read to allow multiple tasks to read if necessary
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

                form.Add(fileContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent(model, System.Text.Encoding.UTF8), "model");
                form.Add(new StringContent("json", System.Text.Encoding.UTF8), "response_format");

                if (langCode != "auto")
                {
                    form.Add(new StringContent(langCode, System.Text.Encoding.UTF8), "language");
                }

                form.Add(new StringContent(basePrompt, System.Text.Encoding.UTF8), "prompt");
                return form;
            }

            // 🛑 LOCAL ONLY MODE (Survival Switch)
            // If the user checked "Local Only", respect it immediately.
            if (p.UseLocalWhisperOnly)
            {
                LogEvent("[transcribe] 🔒 LOCAL ONLY MODE ACTIVE: Skipping cloud...");
                if (LocalModelManager.Shared.IsReady)
                {
                    return await LocalModelManager.Shared.TranscribeAsync(filePath, basePrompt, safeLangCode);
                }
                else
                {
                    LogEvent("[transcribe] ⚠️ Local model not ready for Local-Only mode.");
                    return "Error: Local Only Mode enabled but model not ready.";
                }
            }

            // 🏎️ RACE MODE: If Local is ready, run it in PARALLEL with Cloud.
            // The first one to return a valid result wins. 
            // This guarantees the fastest possible speed (Local ~0.5s vs Cloud ~2s+).
            bool canRace = p.UseLocalWhisper && LocalModelManager.Shared.IsReady;

            // Define Cloud Logic as a local function to allow parallel execution
            async Task<string> RunCloudTranscription()
            {
                // ⚡ QUICK OFFLINE CHECK
                bool isOffline = !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                if (isOffline) return "Error: Network unreachable (Offline Check)";

                // 1. Proxy: Render
                using (var proxyContent = CreateContent())
                {
                     LogEvent($"[transcribe] Sending to API Proxy: {ProxyUrl}");
                     int renderTimeout = canRace ? 2 : 30;
                     string result = await ExecuteWithFallback(ProxyUrl, proxyContent, false, renderTimeout);
                     
                     if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                     
                     string backupProxyUrl = ProxyUrl.Replace("/api/ai/transcribe", "/api/transcribe");
                     return await ExecuteWithFallback(backupProxyUrl, proxyContent, false, renderTimeout);
                }
            }

            // EXECUTE RACE
            if (canRace)
            {
                LogEvent("[transcribe] 🏎️ STARTING RACE: Local vs Cloud...");
                _raceStopwatch.Restart();
                
                var cloudTask = RunCloudTranscription();
                var localTask = LocalModelManager.Shared.TranscribeAsync(filePath, basePrompt, safeLangCode);

                // Wait for the FIRST task to complete
                var winner = await Task.WhenAny(cloudTask, localTask);
                long raceElapsed = _raceStopwatch.ElapsedMilliseconds;
                string result = await winner;

                // Validate Winner
                if (!string.IsNullOrWhiteSpace(result) && !result.StartsWith("Error:"))
                {
                    LastTranscriptionSource = (winner == localTask ? "Local Model" : "Cloud API");
                    LastTranscriptionTime = raceElapsed;
                    LogEvent($"[transcribe] 🏁 WINNER: {(winner == localTask ? "Local Model 🏠" : "Cloud API ☁️")} in {raceElapsed}ms");
                    return result;
                }

                // If winner failed, await the loser
                LogEvent($"[transcribe] ⚠️ Winner failed ({result}). Waiting for runner-up...");
                var loser = (winner == localTask) ? cloudTask : localTask;
                string loserResult = await loser;
                LastTranscriptionSource = (loser == localTask ? "Local Model" : "Cloud API");
                LastTranscriptionTime = _raceStopwatch.ElapsedMilliseconds;
                LogEvent($"[transcribe] 🏁 RUNNER-UP finished in {_raceStopwatch.ElapsedMilliseconds}ms result: {(string.IsNullOrEmpty(loserResult) ? "EMPTY" : "OK")}");
                return loserResult;
            }

            // Normal Execution (No Race)
            _stopwatch.Restart();
            string cloudResult = await RunCloudTranscription();
            _stopwatch.Stop();
            
            if (!cloudResult.StartsWith("Error:"))
            {
                LastTranscriptionSource = "Cloud API";
                LastTranscriptionTime = _stopwatch.ElapsedMilliseconds;
                return cloudResult;
            }

            // Final Fallback if not racing but cloud failed (and we didn't use local yet)
            if (p.UseLocalWhisper && LocalModelManager.Shared.IsReady)
            {
                _stopwatch.Restart();
                LogEvent("[transcribe] 🚨 All cloud services failed. Converting to local inference...");
                string localResult = await LocalModelManager.Shared.TranscribeAsync(filePath, basePrompt, safeLangCode);
                _stopwatch.Stop();
                LastTranscriptionSource = "Local Model";
                LastTranscriptionTime = _stopwatch.ElapsedMilliseconds;
                return localResult;
            }

            return "Error: Could not transcribe audio (Cloud failed, Local model not ready or disabled).";
        }

        public void LogEvent(string msg)
        {
            try
            {
                string logDir = Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "cosmo_debug.log");
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
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        LogEvent($"[ExecuteWithFallback] Access Denied ({response.StatusCode}). Token may be invalid or account paused.");
                        return "Error: Access Revoked"; // Special string caught by the UI
                    }

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
                LogEvent($"[ExecuteWithFallback] ⏳ Request to {url} timed out.");
                return "Error: Request timed out (Canceled)";
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null) msg += " -> " + ex.InnerException.Message;

                LogEvent($"[ExecuteWithFallback] ❌ Exception for {url}: {msg}");

                // FAST FAIL for offline/DNS/Timeout status
                // We use case-insensitive check to be safe
                msg = msg.ToLowerInvariant();
                if (msg.Contains("no such host") || msg.Contains("network is unreachable") || 
                    msg.Contains("connection refused") || msg.Contains("resolved") ||
                    msg.Contains("not be reached") || msg.Contains("timed out") ||
                    msg.Contains("connection failed") || msg.Contains("failed to respond") ||
                    msg.Contains("remote name could not be resolved"))
                {
                    return "Error: Network unreachable";
                }

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

        public async Task<string> ProcessCommand(string prompt, string context, bool usePersonality = true, bool useInterfaceLanguage = true)
        {
            if (AppState.Shared.IsAccessBlocked)
            {
                LogEvent("[command] 🛑 ACCESS BLOCKED: Account is flagged in dashboard. Triggering re-sync...");
                _ = Task.Run(() => AccountService.Shared.CheckAuthStatusAsync());
                return "Error: Your account has been blocked. Please contact support or check your dashboard.";
            }

            var p = PreferenceManager.Shared.Preferences;
            string langHint = useInterfaceLanguage ? GetLanguageHint(p.InterfaceLanguage) : "";
            string personalityHint = usePersonality ? GetPersonalityHint(p.AIPersonality) : "";
            
            string finalPrompt = $"{prompt}\n\nContext:\n{context}\n{langHint}{personalityHint}";

            string modelName = "llama-3.3-70b-versatile"; 
            bool isFastTrack = prompt.Length < 100 && context.Length < 500;
            if (isFastTrack) modelName = "llama-3.1-8b-instant";

            var payload = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = finalPrompt }
                }
            };

            if (!_useChatProxy && !PreferenceManager.Shared.Preferences.ForceCloudProxy)
            {
                using (var content = JsonContent.Create(payload))
                {
                    string res = await ExecuteWithFallback(DirectChatUrl, content, true, 30);
                    if (!res.StartsWith("Error:") && !res.Contains("Canceled")) return res;
                    LogEvent($"[ProcessCommand] Direct Groq Chat failed ({res}). Switching to Sticky Chat Proxy.");
                    _useChatProxy = true;
                }
            }

            // Fallback: Firebase / Local Proxy
            using (var proxyContent = JsonContent.Create(payload))
            {
                string result = await ExecuteWithFallback(ChatUrl, proxyContent, false, 30);
                if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                
                string backupProxyUrl = ChatUrl.Replace("/api/ai/chat", "/api/chat");
                return await ExecuteWithFallback(backupProxyUrl, proxyContent, false, 30);
            }
            
        }

        private string GetPersonalityHint(string personality)
        {
            // Reconstructing from memory/Step 500
            string prompt = "";
            switch (personality)
            {
                case "Professional": prompt += " [SYSTEM: Tone: Professional, concise, efficient.]"; break;
                case "Friendly": prompt += " [SYSTEM: Tone: Friendly, casual, warm.]"; break;
                case "Sarcastic": prompt += " [SYSTEM: Tone: Sarcastic, witty, dry humor.]"; break;
                case "Pirate": prompt += " [SYSTEM: Tone: Talk like a pirate! Arrr!]"; break;
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
                    LogEvent("[WarmUp] 🚀 Warming up connection and local model...");
                    
                    // 1. Warm up SSL/TCP connection
                    var url = PreferenceManager.Shared.Preferences.BackendUrl;
                    if (string.IsNullOrEmpty(url)) url = "https://CosmoWhisper.com";
                    
                    using (var cts = new System.Threading.CancellationTokenSource(3000))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Head, url);
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    }

                    // Background Ping: Test Direct Route (for VPN Firewall logic)
                    if (!PreferenceManager.Shared.Preferences.ForceCloudProxy)
                    {
                        try
                        {
                            using (var cts = new System.Threading.CancellationTokenSource(2500))
                            {
                                var pingReq = new HttpRequestMessage(HttpMethod.Head, DirectUrl);
                                await _httpClient.SendAsync(pingReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                            }
                        }
                        catch (Exception directEx)
                        {
                            LogEvent($"[WarmUp] Direct route blocked/timed out: {directEx.Message}. Flipping to Sticky Proxy.");
                            _useProxy = true; // Pre-activate proxy before first transcription
                        }
                    }
                    
                    // 2. Warm up local model (trigger load/compile)
                    if (PreferenceManager.Shared.Preferences.UseLocalWhisper)
                    {
                        await LocalModelManager.Shared.WarmUp();
                    }

                    LogEvent("[WarmUp] ✅ Environment pre-heated and ready.");
                }
                catch (Exception ex)
                {
                    LogEvent($"[WarmUp] ⚠️ Warm-up failed (Non-fatal): {ex.Message}");
                }
            });
        }
    }
}

