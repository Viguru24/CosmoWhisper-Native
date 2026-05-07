using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace CosmoWhisper.Managers
{
    public class SoundManager
    {
        public static SoundManager Shared { get; } = new SoundManager();
        private readonly object _playLock = new object();

        private SoundManager()
        {
            try
            {
                // Ensure Cosmo keeps its volume at 100% across all devices
                _ = Task.Run(async () =>
                {
                    // Wait for system to settle (prevent startup race conditions)
                    await Task.Delay(10000);

                    while (true)
                    {
                        try { ForceProcessVolumeMax(); } catch { }
                        await Task.Delay(5000);
                    }
                });
            }
            catch { }
        }

        public void ForceProcessVolumeMax()
        {
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    uint myPid = (uint)Process.GetCurrentProcess().Id;

                    foreach (var device in devices)
                    {
                        try
                        {
                            var sessionManager = device.AudioSessionManager;
                            if (sessionManager == null) continue;

                            var sessions = sessionManager.Sessions;
                            for (int i = 0; i < sessions.Count; i++)
                            {
                                using (var session = sessions[i])
                                {
                                    if (session.GetProcessID == myPid || session.DisplayName.Contains("CosmoWhisper", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (session.SimpleAudioVolume.Volume < 1.0f || session.SimpleAudioVolume.Mute)
                                        {
                                            session.SimpleAudioVolume.Volume = 1.0f;
                                            session.SimpleAudioVolume.Mute = false;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public void PlayStartSound() { PlayDictationChime(true); }
        public void PlayStopSound() { PlayDictationChime(false); }

        private void PlayDictationChime(bool isStart)
        {
            Task.Run(() =>
            {
                lock (_playLock)
                {
                    try
                    {
                        if (!PreferenceManager.Shared.Preferences.InteractionSoundsEnabled) return;

                        // Premium Asset Path
                        string fileName = isStart ? "mic_start_new.wav" : "mic_stop_new.wav";
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);

                        if (!File.Exists(filePath))
                        {
                            // Legacy Fallback
                            fileName = isStart ? "mic_start.wav" : "mic_stop.wav";
                            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
                        }

                        if (File.Exists(filePath))
                        {
                            using (var audioFile = new AudioFileReader(filePath))
                            using (var outputDevice = new WaveOutEvent())
                            {
                                outputDevice.Init(audioFile);
                                outputDevice.Play();
                                while (outputDevice.PlaybackState == PlaybackState.Playing)
                                {
                                    Thread.Sleep(10);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SoundManager] Chime Playback Fail: {ex.Message}");
                    }
                }
            });
        }
    }
}


