using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using CosmoWhisper.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Devices.Enumeration;
using CosmoWhisper.Interfaces;


namespace CosmoWhisper.Managers
{
    public class AudioRecorder : IAudioRecorder
    {
        public static AudioRecorder Shared => (AudioRecorder)ServiceLocator.GetService<IAudioRecorder>();

        private readonly IPreferenceManager _preferenceManager;
        private readonly IAIService _aiService;
        private readonly IBackendService _backendService;
        private readonly IVocabularyManager _vocabularyManager;
        private readonly ISoundManager _soundManager;
        private readonly ILogger _logger;

        public event Action<bool>? IsRecordingChanged;
        public event Action<float>? AudioLevelChanged;
        public event Action<string>? TranscriptionReceived;
        public event Action<string, long>? TranscriptionSourceReceived;
        public event Action<string>? ErrorOccurred;

        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private string? _currentFilePath;
        private bool _isRecording;
        private bool _isStartingRecording;
        private bool _isMonitoring;
        private DateTime _recordingStartTime = DateTime.MinValue;
        private DateTime _lastToggleTime = DateTime.MinValue;
        private DateTime _lastLevelUpdateTime = DateTime.MinValue;

        private bool _isWaveInRecording = false;
        private readonly object _waveInLock = new object();
        private TaskCompletionSource<bool>? _waveInStoppingTask;
        private bool _needsLeadingSpace = false;
        private bool _overrideAutoSubmit = false;
        private string _lastFocusedApp = "";
        private DateTime _lastTranscriptionTime = DateTime.MinValue;

        public AudioRecorder(
            IPreferenceManager preferenceManager,
            IAIService aiService,
            IBackendService backendService,
            IVocabularyManager vocabularyManager,
            ISoundManager soundManager,
            ILogger logger)
        {
            _preferenceManager = preferenceManager;
            _aiService = aiService;
            _backendService = backendService;
            _vocabularyManager = vocabularyManager;
            _soundManager = soundManager;
            _logger = logger;

            RefreshConfig();
            _preferenceManager.PreferencesUpdated += () => RefreshConfig();
            CleanupTempFiles();
        }

        private void RefreshConfig()
        {
            var p = _preferenceManager.Preferences;
            Sensitivity = p.MicSensitivity;
            SelectedDeviceId = p.MicDeviceId;
            ActiveDeviceName = p.MicDeviceName;
        }

        public double Sensitivity { get; set; } = 0.5;
        public bool PlayInteractionSounds { get; set; } = true;
        public string ActiveDeviceName { get; private set; } = "Default System Device";
        public string? SelectedDeviceId { get; set; }

        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                _isRecording = value;
                IsRecordingChanged?.Invoke(value);
            }
        }

        public bool IsMonitoring => _isMonitoring;

        public void ResetSpacingState()
        {
            _needsLeadingSpace = false;
            _lastFocusedApp = "";
            _lastTranscriptionTime = DateTime.MinValue;
            DiagnosticManager.Shared.Log("Spacing state reset manually/via context.", "AUDIO");
        }

        // --- MONITORING & CAPTURE ---

        private void InitializeWaveIn()
        {
            if (_waveIn != null) return;

            try
            {
                _waveIn = new WaveInEvent();
                _waveIn.BufferMilliseconds = 150; // Optimized for performance
                _waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz Mono (Optimized for Whisper/VPN)
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;

                // Select device if we have one
                if (!string.IsNullOrEmpty(SelectedDeviceId))
                {
                    // NAudio DeviceNumber is just an index (0, 1, 2...)
                    // We need to map the WinRT DeviceId back to an index
                    int devIndex = GetDeviceIndex(SelectedDeviceId);
                    if (devIndex >= 0) _waveIn.DeviceNumber = devIndex;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Mic Init Error: {ex.Message}");
                _waveIn = null;
            }
        }

        private int GetDeviceIndex(string deviceId)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var caps = WaveIn.GetCapabilities(i);
                    // Match by name starts with (WaveIn names are truncated to 31 chars)
                    foreach (var device in devices)
                    {
                        using (device)
                        {
                            if (device.ID == deviceId && device.FriendlyName.StartsWith(caps.ProductName))
                            {
                                return i;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Device mapping error: {ex.Message}");
            }
            return -1; // Fallback to default
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // 1. Write to file if recording (synchronized with _writer lifecycle in StopRecording/CleanupResources)
            lock (_waveInLock)
            {
                if (IsRecording && _writer != null)
                {
                    _writer.Write(e.Buffer, 0, e.BytesRecorded);
                }
            }

            // 2. Throttled Audio Level Calculation (Idle Efficiency)
            if ((DateTime.Now - _lastLevelUpdateTime).TotalMilliseconds < 50) return;
            _lastLevelUpdateTime = DateTime.Now;

            float max = 0;
            // High-Performance Bitwise Amplitude Extraction (16-bit PCM)
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                float sample32 = sample / 32768f;
                if (System.Math.Abs(sample32) > max) max = System.Math.Abs(sample32);
            }

            // Skip event if level is negligible to save UI cycles
            if (max < 0.001f && !IsRecording) return;

            float db = max > 0.000001f ? 20 * (float)System.Math.Log10(max) : -100;
            AudioLevelChanged?.Invoke(db);
        }

        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            lock (_waveInLock)
            {
                _isWaveInRecording = false;
                _waveInStoppingTask?.TrySetResult(true);
                _waveInStoppingTask = null;
                DiagnosticManager.Shared.Log("Mic WaveIn Hardware Released.", "AUDIO");
            }

            if (e.Exception != null)
            {
                LogDebug($"Mic Error Event: {e.Exception.Message}");
                ErrorOccurred?.Invoke($"Mic Error: {e.Exception.Message}");
            }
        }

        private readonly System.Threading.SemaphoreSlim _monitoringLock = new System.Threading.SemaphoreSlim(1, 1);

        public async void StartMonitoring()
        {
            if (_isMonitoring) return;

            await _monitoringLock.WaitAsync();
            try
            {
                if (_isMonitoring) return;

                if (_waveInStoppingTask != null) 
                {
                    DiagnosticManager.Shared.Log("Waiting for mic hardware to let go...", "AUDIO");
                    await _waveInStoppingTask.Task;
                }

                _isMonitoring = true; // Set this BEFORE StartRecording so StopRecording knows to keep it open
                InitializeWaveIn();
                if (_waveIn != null && !_isWaveInRecording)
                {
                    _waveIn.StartRecording();
                    _isWaveInRecording = true;
                    DiagnosticManager.Shared.Log("Mic WaveIn Physical Start (Monitoring)", "AUDIO");
                }
                LogDebug("Monitoring started.");
            }
            catch (Exception ex)
            {
                DiagnosticManager.Shared.Log($"Start Monitoring Fail: {ex.Message}", "ERROR");
                ErrorOccurred?.Invoke($"Start Monitoring Fail: {ex.Message}");
            }
            finally
            {
                _monitoringLock.Release();
            }
        }

        public async void StopMonitoring()
        {
            await _monitoringLock.WaitAsync();
            try
            {
                if (!_isMonitoring) return;
                _isMonitoring = false;
                
                if (!IsRecording && _isWaveInRecording)
                {
                    _waveInStoppingTask = new TaskCompletionSource<bool>();
                    _waveIn?.StopRecording();
                    DiagnosticManager.Shared.Log("Mic WaveIn Physical Stop Initiated (Monitoring)", "AUDIO");
                }
                LogDebug("Monitoring stopped.");
            }
            finally
            {
                _monitoringLock.Release();
            }
        }

        // --- PLAYBACK ---

        private WaveOutEvent? _outputDevice;
        private AudioFileReader? _audioFile;
        public bool IsPlaying => _outputDevice != null && _outputDevice.PlaybackState == PlaybackState.Playing;

        public async Task PlayAudio(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ErrorOccurred?.Invoke($"File not found: {filePath}");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    StopPlayback();
                    SoundManager.Shared.ForceProcessVolumeMax();

                    DiagnosticManager.Shared.Log($"PlayAudio: Initializing NAudio for {filePath}", "AUDIO");

                    _audioFile = new AudioFileReader(filePath);
                    _audioFile.Volume = (float)(_preferenceManager.Preferences.VoiceVolume / 100.0);


                    _outputDevice = new WaveOutEvent();
                    try
                    {
                        int devIndex = _preferenceManager.Preferences.OutputDeviceIndex;
                        _outputDevice.DeviceNumber = devIndex;
                        DiagnosticManager.Shared.Log($"PlayAudio: Using Output Device index {devIndex}", "AUDIO");
                    }
                    catch
                    {
                        _outputDevice.DeviceNumber = -1; // Fallback
                    }

                    _outputDevice.Init(_audioFile);
                    _outputDevice.Play();

                    var startTime = DateTime.Now;
                    while (_outputDevice != null && _outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(50);
                        if ((DateTime.Now - startTime).TotalSeconds > 20) break;
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"Playback Error: {ex.Message}");
                    LogDebug($"Playback Fail: {ex.Message}");
                }
                finally
                {
                    StopPlayback();
                }
            });
        }

        public void StopPlayback()
        {
            try
            {
                if (_outputDevice != null)
                {
                    _outputDevice.Stop();
                    _outputDevice.Dispose();
                    _outputDevice = null;
                }
                if (_audioFile != null)
                {
                    _audioFile.Dispose();
                    _audioFile = null;
                }
            }
            catch { }
        }

        // --- RECORDING ---

        public async void StartRecording()
        {
            if (IsRecording || _isStartingRecording) return;
            _isStartingRecording = true;

            try
            {
                // 0. BLOCK CHECK (On-Demand re-verification)
                if (_aiService.IsAccessBlocked)
                {
                    // If we are marked as blocked, don't just give up. 
                    // Ask the server RIGHT NOW if the admin unblocked us.
                    var result = await _backendService.SyncStatus();
                    if (result == SyncResult.Success)
                    {
                        _aiService.IsAccessBlocked = false;
                        DiagnosticManager.Shared.Log("Access restored via on-demand check.", "AUTH");
                    }
                    else
                    {
                        ErrorOccurred?.Invoke("Access Revoked: Please contact support or check your account status.");
                        return;
                    }
                }

                // Wait for any pending stop (e.g. from monitoring)
                if (_waveInStoppingTask != null) await _waveInStoppingTask.Task;

                // Usage Limit
                var p = _preferenceManager.Preferences;
                var sub = SubscriptionManager.Shared;
                
                // Check if user is over limit AND not on an unlimited plan
                if (!sub.IsUnlimited && p.UsageMinutes >= sub.MonthlyLimitMinutes)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        bool upgrade = await CosmoMessage.Show("Limit Reached", 
                            "You've reached your monthly transcription limit. Upgrade to a Pro plan for unlimited usage and elite features.", 
                            "🚀", true, "Upgrade Now");
                        
                        if (upgrade)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://cosmowhisper.com/pricing") { UseShellExecute = true });
                            }
                            catch { }
                        }
                    });
                    return;
                }

                try { _soundManager.PlayStartSound(); } catch { }

                try
                {
                    lock (_waveInLock)
                    {
                        InitializeWaveIn();
                        if (_waveIn == null) return;

                        _currentFilePath = Path.Combine(Path.GetTempPath(), $"cosmo_{Guid.NewGuid()}.wav");
                        _writer = new WaveFileWriter(_currentFilePath, _waveIn.WaveFormat);

                        if (!_isWaveInRecording)
                        {
                            _waveIn.StartRecording();
                            _isWaveInRecording = true;
                            DiagnosticManager.Shared.Log("Mic WaveIn Physical Start (Capture)", "AUDIO");
                        }
                    }

                    _recordingStartTime = DateTime.Now;
                    IsRecording = true;
                    LogDebug($"Recording started: {_currentFilePath}");
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"Start Rec Fail: {ex.Message}");
                    IsRecording = false;
                }
            }
            finally
            {
                _isStartingRecording = false;
            }
        }

        public async void StopRecording()
        {
            if (!IsRecording) return;

            try 
            { 
                _soundManager.PlayStopSound();
                // Capture Shift state IMMEDIATELY when recording stops to override AutoSubmit
                _overrideAutoSubmit = (GetAsyncKeyState(0x10) & 0x8000) != 0; // 0x10 is VK_SHIFT
                if (_overrideAutoSubmit) DiagnosticManager.Shared.Log("Shift held: AutoSubmit will be overridden.", "INPUT");
            } 
            catch { }

            try
            {
                // Add padding so Whisper doesn't clip the final syllable while keeping IsRecording=true during tail
                await Task.Delay(300);

                string? fileToProcess = null;
                lock (_waveInLock)
                {
                    IsRecording = false;
                    _recordingStartTime = DateTime.MinValue;

                    _writer?.Flush();
                    _writer?.Close();
                    _writer?.Dispose();
                    _writer = null;

                    fileToProcess = _currentFilePath;
                    _currentFilePath = null;

                    if (!_isMonitoring && _isWaveInRecording)
                    {
                        _waveInStoppingTask = new TaskCompletionSource<bool>();
                        _waveIn?.StopRecording();
                        DiagnosticManager.Shared.Log("Mic WaveIn Physical Stop Initiated (Capture)", "AUDIO");
                    }
                }

                if (!string.IsNullOrEmpty(fileToProcess))
                {
                    _ = ProcessAudioFile(fileToProcess);
                }
            }
            catch (Exception ex)
            {
                IsRecording = false;
                ErrorOccurred?.Invoke($"Stop Rec Fail: {ex.Message}");
            }
        }

        public void CleanupResources()
        {
            try
            {
                StopPlayback();
                lock (_waveInLock)
                {
                    IsRecording = false;
                    _writer?.Dispose();
                    _writer = null;
                    _waveIn?.Dispose();
                    _waveIn = null;
                }
            }
            catch { }
        }

        public void ToggleRecording()
        {
            if ((DateTime.Now - _lastToggleTime).TotalMilliseconds < 400) return;
            _lastToggleTime = DateTime.Now;

            if (IsRecording) StopRecording();
            else StartRecording();
        }

        private async Task ProcessAudioFile(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (info.Length < 1000) { File.Delete(filePath); return; }
                
                // --- SILENCE CHECK (VAD Light) ---
                if (IsSilentWav(filePath))
                {
                    DiagnosticManager.Shared.Log("Recording discarded: Silence detected.", "AUDIO");
                    File.Delete(filePath);
                    return;
                }

                string app = GetCurrentFocusedApp();
                _vocabularyManager.SetContext(app.Split(' ')[0]);

                TranscriptionReceived?.Invoke($"Thinking...");
                
                // Track Usage Duration (16kHz, 16-bit, Mono = 32000 bytes/sec)
                // We subtract the 44-byte WAV header
                double durationSeconds = (info.Length - 44) / 32000.0;
                long durationMs = (long)(durationSeconds * 1000);
                
                // Report to backend (Fire-and-forget, but tracked)
                _ = _backendService.ReportUsage(durationMs);

                // Update local stats for immediate UI feedback
                var p = _preferenceManager.Preferences;
                p.UsageMinutes += (durationSeconds / 60.0);
                _preferenceManager.Save();

                string text = await _aiService.Transcribe(filePath);

                // Notify UI of source and timing
                TranscriptionSourceReceived?.Invoke(_aiService.LastTranscriptionSource, _aiService.LastTranscriptionTime);

                if (text.StartsWith("Error:"))
                {
                    DiagnosticManager.Shared.Log($"Transcription API Error: {text}", "ERROR");
                    ErrorOccurred?.Invoke($"Transcription Failed: {text.Replace("Error:", "").Trim()}");
                }
                else
                {
                    string cleaned = TextProcessor.CleanText(text);
                    if (!TextProcessor.IsGarbage(cleaned))
                    {
                        string polished = CosmoWhisper.Services.SpeechPolishService.Shared.Polish(cleaned);
                        string corrected = _vocabularyManager.ApplyCorrections(RegionalSpellingManager.Shared.Apply(polished));
                        TranscriptionReceived?.Invoke(corrected);

                        bool handled = await CommandController.Shared.Handle(corrected);
                        if (!handled)
                        {
                            var prefs = _preferenceManager.Preferences;
                            // Stats
                            int words = corrected.Split(' ').Length;
                            prefs.TotalWords += words;
                            prefs.TotalTranscriptions += 1;
                            prefs.TotalTimeSavedMinutes += (words / 65.0);
                            _preferenceManager.Save();

                            if (prefs.AutoCopy) System.Windows.Application.Current.Dispatcher.Invoke(() => InputController.Shared.SafeSetClipboardText(corrected));
                            
                            // --- SMART LEADING SPACE LOGIC ---
                            string currentApp = GetCurrentFocusedApp();
                            TimeSpan timeSinceLast = DateTime.Now - _lastTranscriptionTime;

                            // Reset leading space if context changed (new app or 60s pause)
                            // We ignore empty application context (e.g. focusing CosmoWhisper widget/window)
                            if (!string.IsNullOrEmpty(currentApp) && currentApp != _lastFocusedApp)
                            {
                                _needsLeadingSpace = false;
                            }

                            if (timeSinceLast.TotalSeconds > 60)
                            {
                                _needsLeadingSpace = false;
                            }

                            bool startsWithPunctuation = Regex.IsMatch(corrected, @"^[.,!?;:]");
                            bool containsFormatting = corrected.Contains('\n') || corrected.Contains('\r') || corrected.Contains('\v');
                            bool isSoloCorrection = _vocabularyManager.IsSoloCorrection(corrected);

                            string finalOutput = corrected;
                            if (!startsWithPunctuation && !containsFormatting && !isSoloCorrection && _needsLeadingSpace)
                            {
                                finalOutput = " " + corrected;
                            }

                            // Update state for NEXT transcription
                            if (!string.IsNullOrEmpty(currentApp))
                            {
                                _lastFocusedApp = currentApp;
                            }
                            _lastTranscriptionTime = DateTime.Now;
                            _needsLeadingSpace = !containsFormatting && !isSoloCorrection;
                            // -------------------------------

                            bool shouldSubmit = prefs.AutoSubmit && !_overrideAutoSubmit;
                            DiagnosticManager.Shared.Log($"Injecting transcription using {(prefs.UsePasteMode ? "PASTE" : "TYPE")} mode: '{finalOutput}' (AutoSubmit: {prefs.AutoSubmit}, ShiftOverride: {_overrideAutoSubmit})", "INPUT");

                            // Force Type Mode for formatting-only commands (newlines/paragraphs)
                            // This ensures Shift+Enter (Soft Return) is physically executed via keyboard, which is more reliable in Word than clipboard \v
                            bool isFormattingOnly = !string.IsNullOrEmpty(finalOutput) && string.IsNullOrWhiteSpace(finalOutput);

                            if (prefs.UsePasteMode && !isFormattingOnly)
                            {
                                await InputController.Shared.PasteText(finalOutput, shouldSubmit, prefs.RestoreClipboard);
                            }
                            else
                            {
                                await InputController.Shared.TypeText(finalOutput, shouldSubmit);
                            }
                        }
                    }
                    else
                    {
                        DiagnosticManager.Shared.Log($"Transcription filtered as garbage/silence: '{cleaned}' (raw: '{text}')", "INFO");
                        TranscriptionReceived?.Invoke(""); // Clear thinking state
                    }
                }
            }
            catch (Exception ex) 
            { 
                DiagnosticManager.Shared.Log($"Audio Processing Exception: {ex.Message}", "ERROR");
                ErrorOccurred?.Invoke($"Process Error: {ex.Message}"); 
            }
            finally 
            { 
                try { File.Delete(filePath); } catch { } 
            }
        }
        
        private bool IsSilentWav(string path)
        {
            try
            {
                using var reader = new WaveFileReader(path);
                float max = 0;
                var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i += 2)
                    {
                        short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                        float sample32 = Math.Abs(sample / 32768f);
                        if (sample32 > max) max = sample32;
                        if (max > Sensitivity * 0.1) return false; // Early exit if we find sound above threshold
                    }
                }
                return max < (Sensitivity * 0.02); // Very low threshold for absolute silence
            }
            catch { return false; } // If we can't read it, assume it's not silent to be safe
        }

        public async Task<List<DeviceInformation>> EnumerateInputDevices()
        {
            // We use WinRT for enumeration because it gives better names/IDs
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture).AsTask();
            return devices.ToList();
        }

        public List<(int Index, string Name)> EnumerateOutputDevices()
        {
            var list = new List<(int Index, string Name)>();
            list.Add((-1, "Default System Device"));
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                list.Add((i, caps.ProductName));
            }
            return list;
        }

        private void CleanupTempFiles()
        {
            Task.Run(() => {
                try {
                    foreach (var f in Directory.GetFiles(Path.GetTempPath(), "cosmo_*.wav"))
                        if (new FileInfo(f).CreationTime < DateTime.Now.AddMinutes(-10)) File.Delete(f);
                } catch { }
            });
        }

        private void LogDebug(string msg)
        {
            DiagnosticManager.Shared.Log(msg, "AUDIO");
        }

        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
        public string GetCurrentFocusedApp()
        {
            try {
                IntPtr h = GetForegroundWindow();
                if (h == IntPtr.Zero) return "Desktop";
                uint p; GetWindowThreadProcessId(h, out p);
                string name = Process.GetProcessById((int)p).ProcessName;
                if (name.Contains("CosmoWhisperNative", StringComparison.OrdinalIgnoreCase)) return "";
                return name;
            }
            catch { return ""; }
        }
    }
}
