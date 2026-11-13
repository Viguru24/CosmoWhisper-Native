using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface IAudioRecorder
    {
        event Action<bool>? IsRecordingChanged;
        event Action<float>? AudioLevelChanged;
        event Action<string>? TranscriptionReceived;
        event Action<string, long>? TranscriptionSourceReceived;
        event Action<string>? ErrorOccurred;

        bool IsRecording { get; }
        bool IsMonitoring { get; }
        double Sensitivity { get; set; }
        string ActiveDeviceName { get; }
        string? SelectedDeviceId { get; set; }

        void StartRecording();
        void StopRecording();
        void ToggleRecording();
        void StartMonitoring();
        void StopMonitoring();
        Task PlayAudio(string filePath);
        void StopPlayback();
        Task<List<global::Windows.Devices.Enumeration.DeviceInformation>> EnumerateInputDevices();
        List<(int Index, string Name)> EnumerateOutputDevices();
    }
}
