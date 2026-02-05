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
using Windows.Media;
using Windows.Foundation;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Devices.Enumeration;

namespace CosmoWhisper.Managers
{
    // WinRT Interop for direct buffer access
    [ComImport]
    [Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMemoryBufferByteAccess
    {
        unsafe void GetBuffer(out byte* buffer, out uint capacity);
    }

    public class AudioRecorder
    {
        public static AudioRecorder Shared { get; } = new AudioRecorder();

        public event Action<bool>? IsRecordingChanged;
        public event Action<float>? AudioLevelChanged;
        public event Action<string>? TranscriptionReceived;
        public event Action<string>? ErrorOccurred;

        public AudioRecorder()
        {
            RefreshConfig();
            PreferenceManager.Shared.PreferencesUpdated += () => RefreshConfig();
            CleanupTempFiles();
        }

        private void RefreshConfig()
        {
            var p = PreferenceManager.Shared.Preferences;
            Sensitivity = p.MicSensitivity;
            SelectedDeviceId = p.MicDeviceId;
            ActiveDeviceName = p.MicDeviceName;
        }

        public double Sensitivity { get; set; } = 0.5;
        public bool PlayInteractionSounds { get; set; } = true;
        public string ActiveDeviceName { get; private set; } = "Default System Device";
        public string? SelectedDeviceId { get; set; }

        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                _isRecording = value;
                IsRecordingChanged?.Invoke(value);
            }
        }

        private bool _isMonitoring;
        public bool IsMonitoring => _isMonitoring;
        private AudioGraph? _audioGraph;
        private AudioDeviceInputNode? _deviceInputNode;
        private AudioFileOutputNode? _fileOutputNode;
        private AudioFrameOutputNode? _frameOutputNode;
        private string? _currentFilePath;
        private DateTime _lastToggleTime = DateTime.MinValue;
        private DateTime _recordingStartTime = DateTime.MinValue;
        private readonly object _cleanupLock = new object();

        // --- PRE-ROLL BUFFER (ITEM 5) ---
        private readonly System.Collections.Concurrent.ConcurrentQueue<AudioFrame> _preRollBuffer = new System.Collections.Concurrent.ConcurrentQueue<AudioFrame>();
        private const int PreRollDurationMs = 500;
        private AudioFrameInputNode? _preRollInputNode;

        // --- WIN32 INTEROP (ITEM 7) ---
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public string GetCurrentFocusedApp()
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return "Desktop";

                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                var process = Process.GetProcessById((int)pid);
                string procName = process.ProcessName;

                var title = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, title, 256);

                return $"Application: {procName}, Window: '{title}'";
            }
            catch { return "Unknown App"; }
        }

        private async Task EnsureGraphInitialized()
        {
            if (_audioGraph != null) return;

            try
            {
                var settings = new AudioGraphSettings(AudioRenderCategory.Speech);
                var graphResult = await AudioGraph.CreateAsync(settings).AsTask();
                if (graphResult.Status != AudioGraphCreationStatus.Success)
                    throw new Exception($"Graph Error: {graphResult.Status}");
                _audioGraph = graphResult.Graph;

                CreateAudioDeviceInputNodeResult? inputResult;
                if (!string.IsNullOrEmpty(SelectedDeviceId))
                {
                    var device = await DeviceInformation.CreateFromIdAsync(SelectedDeviceId);
                    inputResult = await _audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Other, _audioGraph.EncodingProperties, device).AsTask();
                }
                else
                {
                    inputResult = await _audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Other).AsTask();
                }

                if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
                    throw new Exception($"Input Node Error: {inputResult.Status}");
                _deviceInputNode = inputResult.DeviceInputNode;

                // ITEM 6: Enable Native Noise Suppression / Speech Optimization
                _deviceInputNode.OutgoingGain = 2.0;

                // Frame Output for Level Monitoring & Pre-roll
                _frameOutputNode = _audioGraph.CreateFrameOutputNode();
                _deviceInputNode.AddOutgoingConnection(_frameOutputNode);

                _audioGraph.QuantumStarted += AudioGraph_QuantumStarted;

                // If we are just initializing, we might want to start the graph immediately so monitoring works
                _audioGraph.Start();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Init Failed: {ex.Message}");
                _audioGraph = null;
            }
        }

        private unsafe void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            try
            {
                if (_frameOutputNode == null) return;

                var frame = _frameOutputNode.GetFrame();
                using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacityInBytes;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                    float* dataInFloat = (float*)dataInBytes;
                    uint samples = capacityInBytes / sizeof(float);
                    if (samples == 0) return;

                    float sum = 0;
                    for (int i = 0; i < samples; i++)
                    {
                        float val = dataInFloat[i];
                        sum += val * val;
                    }

                    float rms = (float)Math.Sqrt(sum / samples);
                    float db = rms > 0.000001f ? 20 * (float)Math.Log10(rms) : -100;

                    AudioLevelChanged?.Invoke(db);

                    // ITEM 5: Maintain Pre-roll Buffer (last 500ms)
                    if (!IsRecording)
                    {
                        _preRollBuffer.Enqueue(frame);
                        // 500ms at 10ms quantum is ~50 frames
                        while (_preRollBuffer.Count > 50)
                        {
                            if (_preRollBuffer.TryDequeue(out var oldFrame)) oldFrame.Dispose();
                        }
                    }
                }
            }
            catch
            {
                // Graph likely disposed or stopping. Ignore.
            }
        }

        // --- MONITORING ---

        public async void StartMonitoring()
        {
            if (_isMonitoring) return;
            await EnsureGraphInitialized();
            _isMonitoring = true;
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;
            _isMonitoring = false;

            // Only stop/dispose graph if we are NOT recording
            if (!IsRecording)
            {
                CleanupGraph();
            }
        }

        // --- PLAYBACK ---

        private MediaPlayer? _mediaPlayer;

        public async Task PlayAudio(string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                // Force mixer slider to 100%
                SoundManager.Shared.ForceProcessVolumeMax();

                if (_mediaPlayer == null)
                {
                    _mediaPlayer = new MediaPlayer();
                }

                _mediaPlayer.Volume = 1.0;
                _mediaPlayer.IsMuted = false;

                // For WinRT MediaPlayer, ensure we use a proper file URI for local files
                var uri = new Uri(filePath);
                _mediaPlayer.Source = MediaSource.CreateFromUri(uri);
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Playback Error: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        public void StopPlayback()
        {
            try
            {
                _mediaPlayer?.Pause();
                _mediaPlayer.Source = null;
            }
            catch { }
        }

        // --- RECORDING ---

        public async void StartRecording()
        {
            LogDebug("StartRecording() called.");
            if (IsRecording) return;

            try { SoundManager.Shared.PlayStartSound(); } catch { }

            await EnsureGraphInitialized();

            try
            {
                _currentFilePath = Path.Combine(Path.GetTempPath(), $"cosmo_{Guid.NewGuid()}.m4a");
                var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetTempPath()).AsTask();
                var storageFile = await folder.CreateFileAsync(Path.GetFileName(_currentFilePath), CreationCollisionOption.ReplaceExisting).AsTask();

                var profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High);
                profile.Audio.ChannelCount = 1;
                profile.Audio.SampleRate = 44100;

                var outputResult = await _audioGraph.CreateFileOutputNodeAsync(storageFile, profile).AsTask();
                if (outputResult.Status != AudioFileNodeCreationStatus.Success)
                    throw new Exception($"File Output Error: {outputResult.Status}");

                _fileOutputNode = outputResult.FileOutputNode;
                _deviceInputNode.AddOutgoingConnection(_fileOutputNode);

                // ITEM 5: Inject Pre-roll frames into the file output
                var frameInputResult = _audioGraph.CreateFrameInputNode();
                _preRollInputNode = frameInputResult;
                _preRollInputNode.AddOutgoingConnection(_fileOutputNode);
                _preRollInputNode.Start();

                while (_preRollBuffer.TryDequeue(out var preFrame))
                {
                    _preRollInputNode.AddFrame(preFrame);
                }

                _recordingStartTime = DateTime.Now;
                IsRecording = true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Start Recording Failed: {ex.Message}");
                CleanupGraph();
            }
        }

        public async void StopRecording()
        {
            LogDebug($"StopRecording() called. IsRecording={IsRecording}");
            if (!IsRecording || _audioGraph == null) return;
            IsRecording = false;

            try { SoundManager.Shared.PlayStopSound(); } catch (Exception ex) { LogDebug($"Sound Error: {ex.Message}"); }

            try
            {
                // Disconnect and Finalize File
                if (_fileOutputNode != null)
                {
                    try { _deviceInputNode?.RemoveOutgoingConnection(_fileOutputNode); } catch { }

                    // This is where "ObjectDisposed" usually happens if CleanupGraph runs
                    await _fileOutputNode.FinalizeAsync().AsTask();
                    _fileOutputNode.Dispose();
                    _fileOutputNode = null;
                }

                // If not monitoring, kill graph
                if (!_isMonitoring)
                {
                    CleanupGraph();
                }

                // Calculate duration and report usage
                if (_recordingStartTime != DateTime.MinValue)
                {
                    double durationSeconds = (DateTime.Now - _recordingStartTime).TotalSeconds;
                    _recordingStartTime = DateTime.MinValue;
                    _ = LicenseManager.Shared.ReportUsageAsync(durationSeconds);
                }
            }
            catch (ObjectDisposedException)
            {
                // Graph was disposed by StopMonitoring race - this is expected/safe.
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Dispose") || ex.Message.Contains("ObjectDisposed")) return;
                ErrorOccurred?.Invoke($"Stop Failed: {ex.Message}");
            }
            finally
            {
                // Always attempt to process the file, even if graph crashed
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    await ProcessAudioFile(_currentFilePath);
                    _currentFilePath = null;
                }
            }
        }

        private void CleanupGraph()
        {
            lock (_cleanupLock)
            {
                if (_audioGraph == null) return;
                try
                {
                    _audioGraph.Stop();
                    _audioGraph.QuantumStarted -= AudioGraph_QuantumStarted;
                    _audioGraph.Dispose();
                }
                catch { }
                finally
                {
                    _audioGraph = null;
                    _deviceInputNode = null;
                    _frameOutputNode = null;
                }
            }
        }

        public void ToggleRecording()
        {
            if ((DateTime.Now - _lastToggleTime).TotalMilliseconds < 300) return;
            _lastToggleTime = DateTime.Now;

            if (IsRecording) StopRecording();
            else StartRecording();
        }



        private void CleanupTempFiles()
        {
            Task.Run(() =>
            {
                try
                {
                    var tempPath = Path.GetTempPath();
                    var files = Directory.GetFiles(tempPath, "cosmo_*.m4a");
                    foreach (var f in files)
                    {
                        try 
                        { 
                            var fi = new FileInfo(f);
                            if (fi.CreationTime < DateTime.Now.AddMinutes(-10)) // Only delete old files
                                File.Delete(f); 
                        } 
                        catch { }
                    }
                }
                catch { }
            });
        }

        private async Task ProcessAudioFile(string filePath)
        {
            var info = new FileInfo(filePath);
            LogDebug($"Processing file: {filePath} ({info.Length} bytes)");

            if (info.Length < 1000)
            {
                LogDebug("File too small (<1KB), deleting.");
                try { File.Delete(filePath); } catch { }
                return;
            }

            try
            {
                TranscriptionReceived?.Invoke($"Thinking ({info.Length / 1024}KB)...");
                string text = await AIService.Shared.Transcribe(filePath);
                LogDebug($"API Response: '{text}'");

                if (text.StartsWith("Error:"))
                {
                    LogDebug("API Error detected.");
                    ErrorOccurred?.Invoke(text);
                }
                else
                {
                    string cleaned = TextProcessor.CleanText(text);
                    LogDebug($"Cleaned Text: '{cleaned}'");

                    if (!TextProcessor.IsGarbage(cleaned))
                    {
                        // Apply Regional Spelling & Custom Corrections
                        string regionFixed = RegionalSpellingManager.Shared.Apply(cleaned);
                        string corrected = VocabularyManager.Shared.ApplyCorrections(regionFixed);
                        LogDebug($"Corrected Text: '{corrected}'");

                        bool handled = await CommandController.Shared.Handle(corrected);
                        if (!handled)
                        {
                            TranscriptionReceived?.Invoke(corrected);
                            var prefs = PreferenceManager.Shared.Preferences;

                            // Centralized Performance Stats Tracking
                            int wordCount = corrected.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                            if (wordCount > 0)
                            {
                                prefs.TotalWords += wordCount;
                                prefs.TotalTranscriptions += 1;
                                // Heuristic: Speaking is ~3x faster than typing. Saves ~1 min per 65 words.
                                prefs.TotalTimeSavedMinutes += (wordCount / 65.0);
                                PreferenceManager.Shared.Save();
                                LogDebug($"[STATS] Updated: {wordCount} words. New Total: {prefs.TotalWords}");
                            }

                            if (prefs.AutoCopy)
                            {
                                try
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        System.Windows.Clipboard.SetText(corrected);
                                    });
                                }
                                catch { }
                            }

                            await InputController.Shared.PasteText(corrected + " ", prefs.AutoSubmit, prefs.RestoreClipboard);
                        }
                    }
                    else
                    {
                        LogDebug("Text classified as GARBAGE.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Exception: {ex.Message}");
                ErrorOccurred?.Invoke($"Worker Error: {ex.Message}");
            }
            finally
            {
                try { File.Delete(filePath); } catch { }
            }
        }

        public async Task<List<DeviceInformation>> EnumerateInputDevices()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture).AsTask();
            return devices.ToList();
        }

        private void LogDebug(string msg)
        {
            try
            {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "logs");
                Directory.CreateDirectory(logPath);
                File.AppendAllText(Path.Combine(logPath, "audio_debug.txt"), $"{DateTime.Now}: {msg}\n");
            }
            catch { }
        }
    }
}
