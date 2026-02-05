using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace CosmoWhisper.Managers
{
    public class NarrationManager
    {
        public static NarrationManager Shared { get; } = new NarrationManager();
        public event Action? SpeechStarted;
        public event Action? SpeechEnded;

        private NarrationManager() { }
        public async Task SpeakAsync(string text)
        {
            try
            {
                SpeechStarted?.Invoke();
                LogToFile($"Speaking text: {text.Length} chars");
                using var synth = new SpeechSynthesizer();
                synth.Options.SpeakingRate = PreferenceManager.Shared.Preferences.VoiceSpeed;
                var stream = await synth.SynthesizeTextToStreamAsync(text);
                string tempFile = Path.Combine(Path.GetTempPath(), $"manus_{Guid.NewGuid()}.mp3");
                using var dr = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));
                await dr.LoadAsync((uint)stream.Size);
                byte[] buf = new byte[(int)stream.Size];
                dr.ReadBytes(buf);
                await File.WriteAllBytesAsync(tempFile, buf);
                await AudioRecorder.Shared.PlayAudio(tempFile);
            }
            catch (Exception ex)
            {
                LogToFile($"Speak Error: {ex.Message}");
            }
            finally
            {
                SpeechEnded?.Invoke();
            }
        }

        public void CancelSpeech()
        {
            LogToFile("Stopping speech playback.");
            AudioRecorder.Shared.StopPlayback();
        }

        private void LogToFile(string msg)
        {
            try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "cosmo_debug.txt"), $"{DateTime.Now}: [Narration] {msg}\n"); } catch { }
        }
    }
}
