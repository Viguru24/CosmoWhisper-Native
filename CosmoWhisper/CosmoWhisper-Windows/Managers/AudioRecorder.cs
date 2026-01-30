using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

// --- CLASS FOR AUDIO HARDWARE (Conceptually similar to the Actor) ---
namespace CosmoWhisper.Managers
{
    public class AudioEngine
    {
        private AudioGraph audioGraph;
        private AudioFileInputNode fileInputNode;
        private AudioFileOutputNode fileOutputNode;
        private AudioDeviceInputNode deviceInputNode;
        private bool isRecording = false;

        public async Task<bool> CheckPermissionAsync()
        {
            LogManager.Shared.Log("AudioEngine: Checking microphone authorization status...");
            // UWP/WinUI permission request style
            try
            {
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                using (var capture = new MediaCapture())
                {
                    await capture.InitializeAsync(settings);
                }
                LogManager.Shared.Log("AudioEngine: Authorization Granted.");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Shared.Log($"AudioEngine: Access denied or restricted. {ex.Message}");
                return false;
            }
        }

        public async Task StartRecordingAsync(StorageFile file)
        {
            LogManager.Shared.Log($"AudioEngine: Preparing to start recording at {file.Path}...");

            // 1. Create AudioGraph
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                throw new Exception($"AudioGraph Creation Error: {result.Status}");
            }

            audioGraph = result.Graph;

            // 2. Create Input Node (Microphone)
            CreateAudioDeviceInputNodeResult deviceInputResult = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech);
            if (deviceInputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new Exception($"Device Input Error: {deviceInputResult.Status}");
            }
            deviceInputNode = deviceInputResult.DeviceInputNode;

            // 3. Create Output Node (File)
            MediaEncodingProfile profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High);
            CreateAudioFileOutputNodeResult fileOutputResult = await audioGraph.CreateFileOutputNodeAsync(file, profile);
             if (fileOutputResult.Status != AudioFileNodeCreationStatus.Success)
            {
                throw new Exception($"File Output Error: {fileOutputResult.Status}");
            }
            fileOutputNode = fileOutputResult.FileOutputNode;

            // 4. Connect and Start
            deviceInputNode.AddOutgoingConnection(fileOutputNode);
            audioGraph.Start();
            isRecording = true;
            LogManager.Shared.Log("AudioEngine: Recording Started [C#]");
        }

        public async Task StopRecordingAsync()
        {
            if (isRecording && audioGraph != null)
            {
                audioGraph.Stop();
                // Finalize file
                if (fileOutputNode != null)
                {
                    await fileOutputNode.FinalizeAsync();
                }
                audioGraph.Dispose(); // Cleanup
                LogManager.Shared.Log("AudioEngine: Recorder Stopped [C#]");
            }
            isRecording = false;
        }

        public double GetLevels()
        {
            // WinUI AudioGraph provides quantum data for levels, slightly different but similar concept.
            // Simplified for this prototype.
            return -160.0; 
        }
    }

    // --- MAIN VIEW MODEL ---
    public class AudioRecorder
    {
        public static AudioRecorder Shared { get; } = new AudioRecorder();
        private AudioEngine engine = new AudioEngine();

        // Published properties become Observable Properties (INotifyPropertyChanged)
        public bool IsRecording { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        public string ErrorMessage { get; private set; }

        public async void StartRecording()
        {
            if (IsRecording) return;
            LogManager.Shared.Log("AudioRecorder: Starting... (C#)");

            bool allowed = await engine.CheckPermissionAsync();
            if (!allowed)
            {
                ErrorMessage = "Mic Access Denied";
                return;
            }

            try
            {
                StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("cosmo_recording.m4a", CreationCollisionOption.ReplaceExisting);
                await engine.StartRecordingAsync(file);
                IsRecording = true;
            }
            catch (Exception ex)
            {
                 ErrorMessage = $"Mic Failed: {ex.Message}";
            }
        }

        public async void StopRecording()
        {
            if (!IsRecording) return;
            LogManager.Shared.Log("AudioRecorder: Stopping...");

            IsRecording = false;
            IsProcessing = true; // Updates UI spinner

            await engine.StopRecordingAsync();
            
            // Trigger processing loop equivalent
            await ProcessAudioFile();
        }

        private async Task ProcessAudioFile()
        {
            // Call AIService.cs here...
            await Task.Delay(100); // Simulate work
            IsProcessing = false;
        }
    }
}
