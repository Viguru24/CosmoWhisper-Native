using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using SherpaOnnx;
using NAudio.Wave;
using CosmoWhisper.Interfaces;

namespace CosmoWhisper.Managers.Engines
{
    public class ParakeetSpeechEngine : ISpeechEngine, IDisposable
    {
        public string EngineName => "Parakeet Redux (High Accuracy)";

        private readonly string _modelDir;
        private OfflineRecognizer? _recognizer;
        private readonly object _lock = new();

        public bool IsReady
        {
            get
            {
                lock (_lock)
                {
                    return _recognizer != null && AreModelFilesPresent();
                }
            }
        }

        public ParakeetSpeechEngine(string appDataFolder)
        {
            _modelDir = Path.Combine(appDataFolder, "models", "parakeet-redux");
            Directory.CreateDirectory(_modelDir);
        }

        public bool AreModelFilesPresent()
        {
            return File.Exists(Path.Combine(_modelDir, "encoder.onnx")) &&
                   File.Exists(Path.Combine(_modelDir, "decoder.onnx")) &&
                   File.Exists(Path.Combine(_modelDir, "joiner.onnx")) &&
                   File.Exists(Path.Combine(_modelDir, "tokens.txt"));
        }

        public Task InitializeAsync()
        {
            return Task.Run(() =>
            {
                if (!AreModelFilesPresent())
                {
                    LogEvent($"[ParakeetEngine] Model files not present in {_modelDir}. Skipping initialization.");
                    return;
                }

                try
                {
                    var sw = Stopwatch.StartNew();
                    LogEvent("[ParakeetEngine] 🚀 Initializing OfflineRecognizer...");

                    var modelConfig = new OfflineModelConfig();
                    modelConfig.Transducer.Encoder = Path.Combine(_modelDir, "encoder.onnx");
                    modelConfig.Transducer.Decoder = Path.Combine(_modelDir, "decoder.onnx");
                    modelConfig.Transducer.Joiner = Path.Combine(_modelDir, "joiner.onnx");
                    modelConfig.Tokens = Path.Combine(_modelDir, "tokens.txt");
                    modelConfig.ModelType = "nemo_transducer";
                    modelConfig.NumThreads = Math.Min(8, Math.Max(1, Environment.ProcessorCount - 2));
                    modelConfig.Debug = 0;
                    modelConfig.Provider = "cpu";

                    var config = new OfflineRecognizerConfig();
                    config.ModelConfig = modelConfig;

                    lock (_lock)
                    {
                        _recognizer?.Dispose();
                        _recognizer = new OfflineRecognizer(config);
                    }

                    sw.Stop();
                    LogEvent($"[ParakeetEngine] ✅ Model loaded successfully in {sw.ElapsedMilliseconds}ms.");
                }
                catch (Exception ex)
                {
                    LogEvent($"[ParakeetEngine] ❌ Initialization failed: {ex.Message}");
                    lock (_lock)
                    {
                        _recognizer = null;
                    }
                }
            });
        }

        public Task<string> TranscribeAsync(string wavFilePath, string prompt = "", string language = "en")
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_recognizer == null)
                    {
                        return "Error: Parakeet engine is not ready.";
                    }

                    var sw = Stopwatch.StartNew();
                    LogEvent($"[ParakeetEngine] 📝 Transcribing {wavFilePath}...");

                    try
                    {
                        using var reader = new AudioFileReader(wavFilePath);
                        var samples = new float[reader.Length / (reader.WaveFormat.BitsPerSample / 8)];
                        int read = reader.Read(samples, 0, samples.Length);
                        if (read < samples.Length)
                        {
                            Array.Resize(ref samples, read);
                        }

                        using var stream = _recognizer.CreateStream();
                        stream.AcceptWaveform(reader.WaveFormat.SampleRate, samples);
                        _recognizer.Decode(stream);

                        sw.Stop();
                        string result = stream.Result.Text.Trim();
                        LogEvent($"[ParakeetEngine] 🗣️ Transcription result in {sw.ElapsedMilliseconds}ms: {result}");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        LogEvent($"[ParakeetEngine] ❌ Transcription error: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        public Task WarmUpAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _recognizer?.Dispose();
                _recognizer = null;
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
