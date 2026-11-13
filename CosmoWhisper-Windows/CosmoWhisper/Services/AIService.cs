using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using CosmoWhisper.Managers;
using CosmoWhisper;
using CosmoWhisper.Interfaces;
using System.Linq;

namespace CosmoWhisper.Services
{
    public class AIService : IAIService
    {
        public static AIService Shared => (AIService)ServiceLocator.GetService<IAIService>();
        private readonly IPreferenceManager _preferenceManager;

        private const string DirectUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
        private const string DirectChatUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string FirebaseProxyUrl = "https://CosmoWhisper.com/api";

        // Performance Watch
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _raceStopwatch = new System.Diagnostics.Stopwatch();

        public string LastTranscriptionSource { get; private set; } = "Unknown";
        public long LastTranscriptionTime { get; private set; } = 0;
        public bool IsAccessBlocked { get; set; } = false;

        private string ChatUrl => $"{_preferenceManager.Preferences.BackendUrl.TrimEnd('/')}/api/ai/chat";
        private string ProxyUrl => $"{_preferenceManager.Preferences.BackendUrl.TrimEnd('/')}/api/ai/transcribe";
        private bool _useProxy = false; // "Sticky" proxy mode if direct fails
        private bool _useChatProxy = false;

        public AIService(IPreferenceManager preferenceManager)
        {
            _preferenceManager = preferenceManager;
            RefreshConfig();
        }

        public void Initialize()
        {
            _preferenceManager.PreferencesUpdated += () => RefreshConfig();
        }

        private void RefreshConfig()
        {
            // Configuration is now handled dynamically per request
        }

        private string GroqDefaultKey => Environment.GetEnvironmentVariable("COSMOWHISPER_GROQ_API_KEY") ?? "";

        public async Task<string> Transcribe(string filePath)
        {
            if (AppState.Shared.IsAccessBlocked)
            {
                LogEvent("[transcribe] 🛑 ACCESS BLOCKED: Account is flagged in dashboard. Triggering re-sync...");
                _ = Task.Run(() => AccountService.Shared.CheckAuthStatusAsync());
                return "Error: Your account has been blocked. Please contact support or check your dashboard.";
            }

            var p = _preferenceManager.Preferences;
            string model = string.IsNullOrWhiteSpace(p.AIModel) ? "whisper-large-v3" : p.AIModel;

            // 1. Unified Prompt & Language Calculation
            string langCode = p.InterfaceLanguage.ToLower().Split('-')[0];
            string safeLangCode = (langCode == "auto") ? "en" : langCode;
            
            string appContext = AudioRecorder.Shared.GetCurrentFocusedApp();
            if (appContext.Contains("Cosmo", StringComparison.OrdinalIgnoreCase)) appContext = "";
            
            string hints = VocabularyManager.Shared.GetActiveHints();
            string hintsText = string.IsNullOrWhiteSpace(hints) ? "" : $" {hints}.";

            string basePrompt = $"Transcribe accurately. Ignore non-speech sounds like coughs or background noise. New line, new paragraph. {appContext}{hintsText}".Trim();
            if (langCode == "en")
            {
                string variant = p.InterfaceLanguage == "en-GB" ? "British English (colour, realise)" : "American English (color, realize)";
                basePrompt = $"Transcribe accurately in {variant}. Ignore non-speech sounds like coughs, laughter, or background noise. New line, new paragraph. {appContext}{hintsText}".Trim();
            }

            // 🛑 LOCAL MODEL MODE (User Priority)
            // Local Whisper uses a SHORT prompt (vocabulary/spelling hints only).
            // Whisper's initial_prompt is a style guide, NOT an instruction prompt.
            // Long instructions cause small models (distil-small) to hallucinate and repeat.
            if (!p.ForceDirectApi && (p.UseLocalWhisper || p.UseLocalWhisperOnly))
            {
                string spellingHints = (langCode == "en" && p.InterfaceLanguage == "en-GB")
                    ? "colour, realise, organise, centre"
                    : "color, realize, organize, center";
                
                string cleanHints = "";
                if (!string.IsNullOrWhiteSpace(hints))
                {
                    // Normalize spaces around commas
                    cleanHints = Regex.Replace(hints, @"\s*,\s*", ", ").Trim().TrimEnd(',', '.', ' ');
                    
                    // Allow up to 450 characters of vocabulary hints (Whisper models support ~224 tokens)
                    if (cleanHints.Length > 450)
                    {
                        cleanHints = cleanHints.Substring(0, 450);
                        int lastComma = cleanHints.LastIndexOf(',');
                        if (lastComma > 0)
                        {
                            cleanHints = cleanHints.Substring(0, lastComma);
                        }
                    }
                }

                string localPrompt = string.IsNullOrWhiteSpace(cleanHints)
                    ? $"{spellingHints}."
                    : $"{spellingHints}, {cleanHints}.".Trim();

                return await HandleLocalTranscription(filePath, localPrompt, safeLangCode);
            }

            // Normal Cloud Execution
            _stopwatch.Restart();
            string cloudResult = await RunCloudTranscription(filePath, model, basePrompt, langCode);
            _stopwatch.Stop();

            if (!cloudResult.StartsWith("Error:"))
            {
                LastTranscriptionSource = "Cloud API";
                LastTranscriptionTime = _stopwatch.ElapsedMilliseconds;
                return cloudResult;
            }

            // 🔄 FALLBACK TO LOCAL MODEL (If Cloud fails or Internet is down)
            LogEvent("[transcribe] ⚠️ Cloud transcription failed. Checking local model fallback...");
            if (LocalModelManager.Shared.IsReady)
            {
                LogEvent("[transcribe] 🔒 Local model is ready. Falling back to local transcription.");
                string spellingHints = (langCode == "en" && p.InterfaceLanguage == "en-GB")
                    ? "colour, realise, organise, centre."
                    : "color, realize, organize, center.";
                string localPrompt = string.IsNullOrWhiteSpace(hints)
                    ? spellingHints
                    : $"{spellingHints} {hints}.".Trim();
                return await HandleLocalTranscription(filePath, localPrompt, safeLangCode);
            }

            return "Error: Could not transcribe audio (Cloud API failed and Local model is not ready).";
        }

        private async Task<string> HandleLocalTranscription(string filePath, string basePrompt, string safeLangCode)
        {
            LogEvent("[transcribe] 🔒 LOCAL MODE ACTIVE: Skipping cloud as per user preference...");
            if (LocalModelManager.Shared.IsReady)
            {
                _stopwatch.Restart();
                string localResult = await LocalModelManager.Shared.TranscribeAsync(filePath, basePrompt, safeLangCode);
                _stopwatch.Stop();

                LastTranscriptionSource = $"Local ({LocalModelManager.Shared.ActiveEngineName})";
                LastTranscriptionTime = _stopwatch.ElapsedMilliseconds;
                return localResult;
            }
            else
            {
                LogEvent("[transcribe] ⚠️ Local model not ready. User requested Local-Only.");
                return "Error: Local Model is still initializing or not downloaded. Please wait a moment.";
            }
        }

        private async Task<string> RunCloudTranscription(string filePath, string model, string basePrompt, string langCode)
        {
            // ⚡ QUICK OFFLINE CHECK
            bool isOffline = !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (isOffline) return "Error: Network unreachable (Offline Check)";

            var p = _preferenceManager.Preferences;
            
            // 1. Try Direct Groq first if we have a direct API key and are not forced to proxy
            string groqKey = (!string.IsNullOrEmpty(p.GroqApiKey) ? p.GroqApiKey : GroqDefaultKey).Trim();
            bool hasDirectKey = !string.IsNullOrWhiteSpace(groqKey);
            bool hasCustomKey = !string.IsNullOrWhiteSpace(p.GroqApiKey);

            if (p.ForceDirectApi && !p.ForceCloudProxy)
            {
                if (!hasDirectKey)
                {
                    return "Error: Force Direct API is enabled, but no Groq API Key is configured.";
                }
                using (var directContent = CreateContent(filePath, model, basePrompt, langCode))
                {
                    return await ExecuteWithFallback(DirectUrl, directContent, true, 30);
                }
            }

            if (!_useProxy && !p.ForceCloudProxy && hasDirectKey)
            {
                using (var directContent = CreateContent(filePath, model, basePrompt, langCode))
                {
                    string result = await ExecuteWithFallback(DirectUrl, directContent, true, 30);
                    if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                    
                    LogEvent($"[transcribe] Direct Groq failed ({result}). Switching to Sticky Proxy.");
                    _useProxy = true; // Stay in proxy mode for the rest of the session
                }
            }

            // 2. Fallback to Proxy (Local Backend / Firebase)
            using (var proxyContent = CreateContent(filePath, model, basePrompt, langCode))
            {
                 LogEvent($"[transcribe] Sending to API Proxy: {ProxyUrl}");
                 string result = await ExecuteWithFallback(ProxyUrl, proxyContent, false, 30);

                 if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;

                 string backupProxyUrl = ProxyUrl.Replace("/api/ai/transcribe", "/api/transcribe");
                 return await ExecuteWithFallback(backupProxyUrl, proxyContent, false, 30);
            }
        }

        private MultipartFormDataContent CreateContent(string filePath, string model, string basePrompt, string langCode)
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

        public void LogEvent(string msg)
        {
            try
            {
                string logDir = Path.Combine(_preferenceManager.AppDataFolder, "logs");
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
                var p = _preferenceManager.Preferences;
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;

                if (isDirect)
                {
                    string groqKey = (!string.IsNullOrEmpty(p.GroqApiKey) ? p.GroqApiKey : GroqDefaultKey).Trim();
                    LogEvent($"[ExecuteWithFallback] Sending Direct request to {url} (Key Prefix: '{(groqKey.Length > 8 ? groqKey.Substring(0, 8) : "none")}', Length: {groqKey.Length})");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", groqKey);
                }
                else // Proxy (Local, Render, etc.)
                {
                    if (!string.IsNullOrEmpty(p.AuthToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p.AuthToken);
                }

                _stopwatch.Restart();
                var response = await SharedHttpClient.Instance.SendAsync(request, cts.Token);
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
                        if (isDirect)
                        {
                            return "Error: Invalid or expired Groq API Key. Please verify your custom key in Settings.";
                        }
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
                    if (res?.choices == null || res.choices.Length == 0)
                    {
                         LogEvent($"[ExecuteWithFallback] ⚠️ Empty choices array from {url}. Response: {responseBody}");
                         return "Error: The AI returned an empty response.";
                    }
                    return res.choices[0]?.message?.content ?? "";
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
                var request = new
                {
                    model = "tts-1",
                    input = text,
                    voice = voice.ToLower().Trim(),
                    speed = speed,
                    response_format = "mp3"
                };

                System.Diagnostics.Debug.WriteLine($"[TTS] Requesting voice: {voice} at speed {speed}");

                // Use SharedHttpClient to prevent socket exhaustion
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/speech");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                httpRequest.Headers.Add("User-Agent", "CosmoWhisper-Native");
                httpRequest.Content = JsonContent.Create(request);

                var response = await SharedHttpClient.Instance.SendAsync(httpRequest);

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

            var p = _preferenceManager.Preferences;
            string langHint = useInterfaceLanguage ? GetLanguageHint(p.InterfaceLanguage) : "";
            string personalityHint = usePersonality ? GetPersonalityHint(p.AIPersonality) : "";
            
            // 🛡️ TOKEN BALANCING: Prevent context window overflow
            string fullPrompt = $"{prompt}\n\nContext:\n{context}\n{langHint}{personalityHint}";
            var (balancedPrompt, maxOutput) = BalanceContext(fullPrompt, p.MaxOutputTokens);

            string modelName = "llama-3.3-70b-versatile"; 

            var payload = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant. Provide concise, direct, accurate answers." },
                    new { role = "user", content = balancedPrompt }
                },
                max_tokens = maxOutput
            };

            string groqKey = (!string.IsNullOrEmpty(p.GroqApiKey) ? p.GroqApiKey : GroqDefaultKey).Trim();
            string openAiKey = (!string.IsNullOrEmpty(p.OpenAIApiKey) ? p.OpenAIApiKey : "").Trim();

            // Auto-detect key format swaps (e.g. if user pasted sk-... into Groq field or gsk_ into OpenAI field)
            if (groqKey.StartsWith("sk-") && !groqKey.StartsWith("gsk_") && string.IsNullOrEmpty(openAiKey))
            {
                openAiKey = groqKey;
                groqKey = "";
            }
            else if (openAiKey.StartsWith("gsk_") && string.IsNullOrEmpty(groqKey))
            {
                groqKey = openAiKey;
                openAiKey = "";
            }

            bool hasGroqKey = !string.IsNullOrWhiteSpace(groqKey);
            bool hasOpenAIKey = !string.IsNullOrWhiteSpace(openAiKey);

            // 1. Direct Groq with Candidate Model Cascade
            if (hasGroqKey)
            {
                string[] candidateModels = new[] { "openai/gpt-oss-20b", "groq/compound-mini", "openai/gpt-oss-120b", "qwen/qwen3.6-27b", "llama-3.3-70b-versatile" };

                foreach (var model in candidateModels)
                {
                    var modelPayload = new
                    {
                        model = model,
                        messages = payload.messages,
                        max_tokens = payload.max_tokens
                    };

                    using var content = JsonContent.Create(modelPayload);
                    string res = await ExecuteWithFallback(DirectChatUrl, content, true, 30);
                    if (!res.StartsWith("Error:") && !res.Contains("Canceled"))
                    {
                        // Clean reasoning / think tags if emitted by reasoning models
                        res = Regex.Replace(res, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();
                        return res;
                    }

                    LogEvent($"[ProcessCommand] Groq model '{model}' failed: {res}");
                    if (!res.Contains("model_not_found") && !res.Contains("does not exist") && !res.Contains("decommissioned"))
                    {
                        // If it's an auth error or rate limit, no need to retry other models
                        break;
                    }
                }
            }

            // 2. Direct OpenAI (If user supplied OpenAI Key or auto-detected)
            if (hasOpenAIKey)
            {
                var openAiPayload = new
                {
                    model = "gpt-4o-mini",
                    messages = payload.messages,
                    max_tokens = payload.max_tokens
                };

                try
                {
                    var openAiRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                    openAiRequest.Content = JsonContent.Create(openAiPayload);
                    openAiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

                    var openAiRes = await SharedHttpClient.Instance.SendAsync(openAiRequest);
                    var openAiBody = await openAiRes.Content.ReadAsStringAsync();
                    if (openAiRes.IsSuccessStatusCode)
                    {
                        var res = JsonSerializer.Deserialize<ChatResponse>(openAiBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (res?.choices?.Length > 0)
                        {
                            return res.choices[0]?.message?.content ?? "";
                        }
                    }
                    LogEvent($"[ProcessCommand] Direct OpenAI Chat failed: {openAiBody}");
                }
                catch (Exception oex)
                {
                    LogEvent($"[ProcessCommand] Direct OpenAI Chat exception: {oex.Message}");
                }
            }

            // 3. Fallback: Cloud Server Proxy
            using (var proxyContent = JsonContent.Create(payload))
            {
                string result = await ExecuteWithFallback(ChatUrl, proxyContent, false, 30);
                if (!result.StartsWith("Error:") && !result.Contains("Canceled")) return result;
                
                string backupProxyUrl = ChatUrl.Replace("/api/ai/chat", "/api/chat");
                return await ExecuteWithFallback(backupProxyUrl, proxyContent, false, 30);
            }
        }

        private (string trimmedPrompt, int maxOutputTokens) BalanceContext(string input, int requestedOutput)
        {
            const int MAX_TOKENS = 128000; // Safe limit for most modern models
            const double TOKENS_PER_CHAR = 4.0;
            
            int inputTokens = (int)Math.Ceiling(input.Length / TOKENS_PER_CHAR);
            
            if (requestedOutput <= 0) requestedOutput = 4096;
            
            if (inputTokens + requestedOutput <= MAX_TOKENS)
                return (input, requestedOutput);
            
            // Calculate adjusted output
            int maxOutput = Math.Max(1000, MAX_TOKENS - inputTokens - 1000); // 1k safety margin
            
            // Optionally truncate input if it's too large
            if (inputTokens > MAX_TOKENS * 0.8)
            {
                int maxInputChars = (int)((MAX_TOKENS - maxOutput - 2000) * TOKENS_PER_CHAR);
                LogEvent($"[BalanceContext] 🛡️ Truncating input from {input.Length} to {maxInputChars} chars to prevent overflow.");
                return (input.Substring(0, maxInputChars), maxOutput);
            }
            
            return (input, maxOutput);
        }

        private async Task<string> ExecuteWithRetry(Func<Task<string>> operation, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try { return await operation(); }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    LogEvent($"[Retry] 🔄 Attempt {i + 1} failed: {ex.Message}. Retrying...");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
                }
            }
            return "Error: Max retries exceeded";
        }

        private string GetPersonalityHint(string personality)
        {
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
                    var url = _preferenceManager.Preferences.BackendUrl;
                    if (string.IsNullOrEmpty(url)) url = "https://CosmoWhisper.com";
                    
                    using (var cts = new System.Threading.CancellationTokenSource(3000))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Head, url);
                        await SharedHttpClient.Instance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    }

                    // Background Ping: Test Direct Route (for VPN Firewall logic)
                    if (!_preferenceManager.Preferences.ForceCloudProxy)
                    {
                        try
                        {
                            using (var cts = new System.Threading.CancellationTokenSource(2500))
                            {
                                var pingReq = new HttpRequestMessage(HttpMethod.Head, DirectUrl);
                                await SharedHttpClient.Instance.SendAsync(pingReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                            }
                        }
                        catch (Exception directEx)
                        {
                            LogEvent($"[WarmUp] Direct route blocked/timed out: {directEx.Message}. Flipping to Sticky Proxy.");
                            _useProxy = true; // Pre-activate proxy before first transcription
                        }
                    }
                    
                    // 2. Warm up local model (trigger load/compile)
                    if (_preferenceManager.Preferences.UseLocalWhisper)
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
