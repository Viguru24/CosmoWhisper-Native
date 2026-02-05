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

                        // 1. Try local bundled professional sounds (F9 on/off)
                        string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", isStart ? "chime_on.wav" : "chime_off.wav");
                        string path = assetsPath;

                        // 2. Fallback to Windows official dictation sounds
                        if (!File.Exists(path))
                        {
                            path = isStart ? @"C:\Windows\Media\Speech On.wav" : @"C:\Windows\Media\Speech Off.wav";
                        }

                        // 3. Fallback to generic notify sound
                        if (!File.Exists(path)) path = @"C:\Windows\Media\Windows Notify.wav";

                        if (!File.Exists(path))
                        {
                            System.Media.SystemSounds.Exclamation.Play();
                            return;
                        }

                        // Use WaveOutEvent with DeviceNumber = -1 (Dynamic Mapping)
                        using (var reader = new AudioFileReader(path))
                        using (var output = new WaveOutEvent())
                        {
                            output.DeviceNumber = -1;
                            output.Init(reader);
                            output.Play();

                            // We play the full duration (usually ~1s) because these are 
                            // professionally mastered melodic chirps that shouldn't be cut off.
                            while (output.PlaybackState == PlaybackState.Playing)
                            {
                                Thread.Sleep(20);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SoundManager] Dictation Play Fail: {ex.Message}");
                    }
                }
            });
        }
    }
}
