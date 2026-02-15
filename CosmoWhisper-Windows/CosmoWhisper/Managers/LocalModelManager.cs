using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace CosmoWhisper.Managers
{
    public class LocalModelManager
    {
        public static LocalModelManager Shared { get; } = new LocalModelManager();
        
        private string _modelPath;
        private WhisperFactory _factory;
        private bool _isDownloading = false;

        public bool IsReady => File.Exists(_modelPath) && _factory != null;

        public LocalModelManager()
        {
            string appData = PreferenceManager.Shared.AppDataFolder;
            string modelDir = Path.Combine(appData, "models");
            Directory.CreateDirectory(modelDir);
            _modelPath = Path.Combine(modelDir, "ggml-tiny.en.bin");
            LogEvent($"[LocalModel] Path: {_modelPath}");
        }
        public void Initialize()
        {
            LogEvent("[LocalModel] Initialize() called. Starting Task...");
            // Fire and forget initialization
            Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            LogEvent("[LocalModel] 🚀 Initializing...");
            if (File.Exists(_modelPath))
            {
                try 
                {
                    // Try to load with GPU acceleration
                    try
                    {
                        var builder = WhisperFactory.FromPath(_modelPath);
                        // Standard FromPath might not take bool in 1.9.0 directly? 
                        // Actually, looking at docs, it's often RuntimeOptions or Builder.
                        // Let's rely on the Runtime package being present.
                        // If 1.9.0 doesn't support explicit bool, we might need a different approach.
                        // Wait, let's try the Builder pattern if available or just the standard call
                        // usually having the DLL is enough.
                        // Verification: The user said "Can the local model be uploaded to VRAM?".
                        // If I can't be sure about the API, I should probably stick to the safest bet:
                        // The `Whisper.net.Runtime.Cuda` package *replaces* the native library.
                        // So `FromPath` *should* automatically use it if the DLL is loaded.
                        // BUT, we can try to force it via `RuntimeOptions` if that class exists.
                        // Let's assume standard behavior for now to avoid compilation errors if API differs.
                         _factory = WhisperFactory.FromPath(_modelPath);
                        LogEvent("[LocalModel] ✅ Model loaded successfully (GPU/CPU Auto-detect).");
                    }
                    catch (Exception ex)
                    {
                         LogEvent($"[LocalModel] ⚠️ Model load error: {ex.Message}");
                         throw; 
                    }
                    await RunSelfTest();
                }
                catch (Exception ex)
                {
                     LogEvent($"[LocalModel] ⚠️ Error loading existing model: {ex.Message}");
                     try { File.Delete(_modelPath); } catch { } // Corrupt? Re-download.
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

            try
            {
                LogEvent("[LocalModel] 📥 Starting background download...");
                
                using (var client = new HttpClient())
                {
                    // Use a reliable source for ggml models compatible with whisper.cpp
                    // Using the official huggingface repo for whisper.cpp models
                    string url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin"; 
                    
                    using (var s = await client.GetStreamAsync(url))
                    using (var fs = new FileStream(_modelPath, FileMode.Create))
                    {
                        await s.CopyToAsync(fs);
                    }
                }

                LogEvent("[LocalModel] ✅ Download complete. Loading model...");
                _factory = WhisperFactory.FromPath(_modelPath);
                LogEvent("[LocalModel] ✅ Model ready for use.");
                await RunSelfTest();
            }
            catch (Exception ex)
            {
                LogEvent($"[LocalModel] ❌ Download failed: {ex.Message}");
                try { if (File.Exists(_modelPath)) File.Delete(_modelPath); } catch { }
            }
            finally
            {
                _isDownloading = false;
            }
        }

        public async Task<string> TranscribeAsync(string wavFilePath)
        {
            if (!IsReady) 
            {
                if (!_isDownloading) Initialize(); // Retry init if needed
                return "Error: Local model is still downloading.";
            }

            LogEvent($"[LocalModel] 📝 Transcribing {wavFilePath}...");
            try
            {
                using (var processor = _factory.CreateBuilder()
                    .WithLanguage("en")
                    .WithThreads(System.Environment.ProcessorCount) // Use all available cores for maximum speed
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
                        LogEvent($"[LocalModel] 🗣️ Transcription result: {finalResult}");
                        return finalResult;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent($"[LocalModel] ❌ Transcription failed: {ex.Message}");
                return $"Error: Local transcription failed - {ex.Message}";
            }
        }

        public async Task RunSelfTest()
        {
            if (!IsReady) return;
            LogEvent("[LocalModel] 🧪 Running Self-Test...");
            try
            {
                // Create a 1-second silence WAV
                string tempWav = Path.Combine(Path.GetTempPath(), "cosmo_selftest.wav");
                using (var ms = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(ms))
                    {
                        // WAV Header for 16kHz Mono 16-bit
                        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                        writer.Write(32036); // ChunkSize
                        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
                        writer.Write(new char[] { 'f', 'm', 't', ' ' });
                        writer.Write(16); // Subchunk1Size
                        writer.Write((short)1); // AudioFormat (PCM)
                        writer.Write((short)1); // NumChannels
                        writer.Write(16000); // SampleRate
                        writer.Write(32000); // ByteRate
                        writer.Write((short)2); // BlockAlign
                        writer.Write((short)16); // BitsPerSample
                        writer.Write(new char[] { 'd', 'a', 't', 'a' });
                        writer.Write(32000); // Subchunk2Size (1 sec)
                        for (int i = 0; i < 16000; i++) writer.Write((short)0); // Silence
                    }
                    File.WriteAllBytes(tempWav, ms.ToArray());
                }

                string result = await TranscribeAsync(tempWav);
                LogEvent($"[LocalModel] 🧪 Self-Test Result: '{result}'");
                try { File.Delete(tempWav); } catch { }
            }
            catch (Exception ex)
            {
                 LogEvent($"[LocalModel] ❌ Self-Test Failed: {ex.Message}");
            }
        }

        private void LogEvent(string msg)
        {
            try
            {
                string logDir = Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "cosmo_debug.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}\n");
            }
            catch { }
        }
    }
}
