using System;
using System.IO;
using System.Threading.Tasks;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Managers.Engines;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers
{
    public class LocalModelManager : ILocalModelManager
    {
        public static LocalModelManager Shared => (LocalModelManager)ServiceLocator.GetService<ILocalModelManager>();

        private readonly WhisperSpeechEngine _whisperEngine;
        private readonly ParakeetSpeechEngine _parakeetEngine;
        private readonly ILogger? _logger;
        private ISpeechEngine _activeEngine;
        private Task? _initTask;

        public bool IsReady => ResolveActiveEngine().IsReady || _whisperEngine.IsReady;
        public string ActiveEngineName => ResolveActiveEngine().EngineName;

        public LocalModelManager(ILogger? logger = null)
        {
            _logger = logger;
            string appData = PreferenceManager.Shared.AppDataFolder;
            _whisperEngine = new WhisperSpeechEngine(appData, logger);
            _parakeetEngine = new ParakeetSpeechEngine(appData, logger);

            _activeEngine = ResolveActiveEngine();
            LogEvent($"[LocalModel] Initialized router. Preferred Engine: {PreferenceManager.Shared.Preferences.LocalVoiceEngine}, Active Engine: {_activeEngine.EngineName}");

            PreferenceManager.Shared.PreferencesUpdated += OnPreferencesUpdated;
        }

        private void OnPreferencesUpdated()
        {
            var resolved = ResolveActiveEngine();
            if (resolved != _activeEngine)
            {
                LogEvent($"[LocalModel] Active engine switched from '{_activeEngine.EngineName}' to '{resolved.EngineName}'");
                _activeEngine = resolved;
            }
        }

        private ISpeechEngine ResolveActiveEngine()
        {
            string pref = PreferenceManager.Shared.Preferences.LocalVoiceEngine;
            if (string.Equals(pref, "WhisperTiny", StringComparison.OrdinalIgnoreCase))
            {
                return _whisperEngine;
            }

            // Default or ParakeetRedux
            if (_parakeetEngine.AreModelFilesPresent())
            {
                return _parakeetEngine;
            }

            // Safe Fallback: if Parakeet files are missing, fall back to Whisper
            LogEvent("[LocalModel] Parakeet files not detected. Falling back to Whisper engine.");
            return _whisperEngine;
        }

        public void Initialize()
        {
            LogEvent("[LocalModel] Initialize() called. Initializing engines...");
            _ = EnsureInitializedAsync();
        }

        public Task EnsureInitializedAsync()
        {
            return _initTask ??= InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Initialize both so fallback is always instantaneous
            var whisperInit = _whisperEngine.InitializeAsync();
            var parakeetInit = _parakeetEngine.InitializeAsync();
            await Task.WhenAll(whisperInit, parakeetInit);

            _activeEngine = ResolveActiveEngine();
            LogEvent($"[LocalModel] Engines initialized. Ready: {IsReady} (Active: {_activeEngine.EngineName})");
        }

        public async Task<string> TranscribeAsync(string wavFilePath, string prompt = "", string language = "en")
        {
            _activeEngine = ResolveActiveEngine();

            if (_activeEngine.IsReady)
            {
                try
                {
                    return await _activeEngine.TranscribeAsync(wavFilePath, prompt, language);
                }
                catch (Exception ex)
                {
                    LogEvent($"[LocalModel] ⚠️ Primary engine '{_activeEngine.EngineName}' failed: {ex.Message}. Attempting Whisper fallback...");
                    if (_activeEngine != _whisperEngine && _whisperEngine.IsReady)
                    {
                        return await _whisperEngine.TranscribeAsync(wavFilePath, prompt, language);
                    }
                    return $"Error: Transcription failed - {ex.Message}";
                }
            }

            // Fallback to Whisper if active isn't ready
            if (_whisperEngine.IsReady)
            {
                LogEvent("[LocalModel] Primary engine not ready. Using Whisper fallback...");
                return await _whisperEngine.TranscribeAsync(wavFilePath, prompt, language);
            }

            return "Error: Local voice engines are still initializing or downloading.";
        }

        public async Task WarmUp()
        {
            await _activeEngine.WarmUpAsync();
        }

        public async Task RunSelfTest()
        {
            if (!IsReady) return;
            LogEvent("[LocalModel] 🧪 Running Self-Test on active engine...");
            try
            {
                string tempWav = Path.Combine(Path.GetTempPath(), "cosmo_selftest.wav");
                using (var ms = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                        writer.Write(32036);
                        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
                        writer.Write(new char[] { 'f', 'm', 't', ' ' });
                        writer.Write(16);
                        writer.Write((short)1);
                        writer.Write((short)1);
                        writer.Write(16000);
                        writer.Write(32000);
                        writer.Write((short)2);
                        writer.Write((short)16);
                        writer.Write(new char[] { 'd', 'a', 't', 'a' });
                        writer.Write(32000);
                        for (int i = 0; i < 16000; i++) writer.Write((short)0);
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
            if (_logger != null) _logger.LogInformation(msg);
            else FileLogger.WriteShared("INFO", msg);
        }
    }
}
