using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Logger;
using Whisper.net.LibraryLoader;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers.Engines
{
    public class WhisperSpeechEngine : ISpeechEngine
    {
        private readonly string _engineName;
        public string EngineName => _engineName;

        private string _modelPath;
        private WhisperFactory? _factory;
        private bool _isDownloading = false;
        private static IDisposable? _whisperLogger = null;
        private readonly ILogger? _logger;

        public bool IsReady => File.Exists(_modelPath) && _factory != null;

        private readonly bool _useGpu;

        public WhisperSpeechEngine(string appDataFolder, ILogger? logger = null, bool useGpu = true)
        {
            _logger = logger;
            _useGpu = useGpu;
            string modelDir = Path.Combine(appDataFolder, "models");
            Directory.CreateDirectory(modelDir);

            // Dynamic Auto-Detection: prefer highest-quality local model found first
            string[] preferredModels = {
                "ggml-large-v3.bin",
                "ggml-medium.en.bin",
                "ggml-small.en.bin",
                "ggml-distil-small.en.bin",
                "ggml-base.en.bin",
                "ggml-tiny.en.bin"
            };
            string selectedModel = "ggml-tiny.en.bin";
            foreach (var m in preferredModels)
            {
                if (File.Exists(Path.Combine(modelDir, m)))
                {
                    selectedModel = m;
                    break;
                }
            }

            _engineName = selectedModel switch
            {
                "ggml-large-v3.bin" => "Whisper (Large-v3)",
                "ggml-medium.en.bin" => "Whisper (Medium)",
                "ggml-small.en.bin" => "Whisper (Small)",
                "ggml-distil-small.en.bin" => "Whisper (Distil-Small)",
                "ggml-base.en.bin" => "Whisper (Base)",
                _ => "Whisper (77MB Tiny)"
            };

            _modelPath = Path.Combine(modelDir, selectedModel);
            LogEvent($"[WhisperEngine] Resolved Model Path: {_modelPath}");
        }

        public async Task InitializeAsync()
        {
            if (_whisperLogger == null)
            {
                try
                {
                    _whisperLogger = LogProvider.AddLogger((level, message) => {
                        string msg = (message ?? "").Trim();
                        if (msg.StartsWith("whisper_init_state") ||
                            msg.StartsWith("whisper_backend_init_gpu") ||
                            msg.StartsWith("whisper_model_load") ||
                            msg.Contains("kv self size") ||
                            msg.Contains("kv cross size") ||
                            msg.Contains("compute buffer"))
                        {
                            return;
                        }

                        LogEvent($"[Whisper.net] {level}: {msg}");
                    });
                }
                catch (Exception ex)
                {
                    LogEvent($"[WhisperEngine] Failed to register Whisper.net logger: {ex.Message}");
                }
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // GPU first (CUDA), automatic CPU fallback if the CUDA runtime/driver is unavailable.
            RuntimeOptions.RuntimeLibraryOrder = _useGpu
                ? new List<RuntimeLibrary> { RuntimeLibrary.Cuda, RuntimeLibrary.Cpu }
                : new List<RuntimeLibrary> { RuntimeLibrary.Cpu };
            LogEvent($"[WhisperEngine] 🚀 Initializing (GPU acceleration: {(_useGpu ? "requested" : "disabled")})...");

            if (File.Exists(_modelPath))
            {
                try 
                {
                    _factory = WhisperFactory.FromPath(_modelPath);
                    sw.Stop();
                    LogEvent($"[WhisperEngine] ✅ Model loaded on '{RuntimeOptions.LoadedLibrary}' in {sw.ElapsedMilliseconds}ms.");
                }
                catch (Exception ex)
                {
                    LogEvent($"[WhisperEngine] ⚠️ Error loading existing model: {ex.Message}");
                    try { File.Delete(_modelPath); } catch { }
                    await DownloadModelAsync();
                }
            }
            else
            {
                await DownloadModelAsync();
            }
        }

        private async Task DownloadModelAsync()
        {
            if (_isDownloading) return;
            _isDownloading = true;
            string tempPath = _modelPath + ".tmp";

            try
            {
                LogEvent("[WhisperEngine] 📥 Starting background download of ggml-tiny.en.bin...");
                string url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin";
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                using (var response = await SharedHttpClient.StreamInstance.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    await using (var s = await response.Content.ReadAsStreamAsync(cts.Token))
                    await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    {
                        await s.CopyToAsync(fs, cts.Token);
                    }
                }

                File.Move(tempPath, _modelPath, overwrite: true);
                LogEvent("[WhisperEngine] ✅ Download complete. Loading model...");
                _factory = WhisperFactory.FromPath(_modelPath);
                LogEvent("[WhisperEngine] ✅ Model ready for use.");
            }
            catch (Exception ex)
            {
                LogEvent($"[WhisperEngine] ❌ Download failed: {ex.Message}");
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
            finally
            {
                _isDownloading = false;
            }
        }

        public async Task<string> TranscribeAsync(string wavFilePath, string prompt = "", string language = "en")
        {
            if (!IsReady) 
            {
                if (!_isDownloading) _ = InitializeAsync();
                return "Error: Local Whisper model is still downloading.";
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            LogEvent($"[WhisperEngine] 📝 Transcribing {wavFilePath} (Language: {language}, Prompt Length: {prompt.Length})...");
            try
            {
                int threads = Math.Min(8, Math.Max(1, System.Environment.ProcessorCount - 2));
                using (var processor = _factory!.CreateBuilder()
                    .WithLanguage(language)
                    .WithPrompt(prompt)
                    .WithThreads(threads)
                    .WithNoContext()
                    .Build())
                {
                    using (var fileStream = File.OpenRead(wavFilePath))
                    {
                        string result = "";
                        await foreach (var segment in processor.ProcessAsync(fileStream))
                        {
                            result += segment.Text;
                        }
                        
                        var finalResult = result.Trim();
                        sw.Stop();
                        LogEvent($"[WhisperEngine] 🗣️ Transcription result in {sw.ElapsedMilliseconds}ms: {finalResult}");
                        return finalResult;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent($"[WhisperEngine] ❌ Transcription failed: {ex.Message}");
                return $"Error: Whisper transcription failed - {ex.Message}";
            }
        }

        public Task WarmUpAsync()
        {
            if (!IsReady) return Task.CompletedTask;
            try
            {
                LogEvent("[WhisperEngine] 🔥 Warming up model...");
                using (var builder = _factory!.CreateBuilder().WithLanguage("en").Build())
                {
                    // No-op
                }
                LogEvent("[WhisperEngine] 🔥 Warm-up complete.");
            }
            catch (Exception ex)
            {
                LogEvent($"[WhisperEngine] ⚠️ Warm-up error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void LogEvent(string msg)
        {
            if (_logger != null) _logger.LogInformation(msg);
            else FileLogger.WriteShared("INFO", msg);
        }
    }
}
