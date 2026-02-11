using System;
using System.IO;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Controls;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using ComboBox = System.Windows.Controls.ComboBox;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using CosmoWhisper.Managers;
using CosmoWhisper.Services;
using CosmoWhisper;

namespace CosmoWhisper.Controllers
{
    public class NarrationController : BaseViewController
    {
        public NarrationController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            InitializeVoices();
        }

        private void InitializeVoices()
        {
            if (Window.ComboVoice == null) return;
            Window.ComboVoice.Items.Clear();

            // 1. Add Neural Voices (OpenAI) - Always available as options
            string[] neuralVoices = { "Alloy", "Echo", "Fable", "Onyx", "Nova", "Shimmer" };
            foreach (var nv in neuralVoices)
            {
                Window.ComboVoice.Items.Add(new ComboBoxItem 
                { 
                    Content = $"🌐 {nv} (Neural AI)", 
                    Tag = nv,
                    FontWeight = FontWeights.Bold
                });
            }

            var languagePriority = new System.Collections.Generic.Dictionary<string, int>
            {
                { "en-GB", 1 }, { "en-US", 2 }, { "zh", 3 }, { "es", 4 },
                { "hi", 5 }, { "ar", 6 }, { "pt", 7 }, { "fr", 8 },
                { "de", 9 }, { "ja", 10 }
            };

            var voices = SpeechSynthesizer.AllVoices
                .Select(v => new
                {
                    Voice = v,
                    Priority = languagePriority.ContainsKey(v.Language) ? languagePriority[v.Language] :
                               languagePriority.Any(kv => v.Language.StartsWith(kv.Key + "-")) ?
                               languagePriority.First(kv => v.Language.StartsWith(kv.Key + "-")).Value : 999
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Voice.DisplayName);

            foreach (var item in voices)
            {
                var v = item.Voice;
                // Use the Star emoji (✨) for Premier local voices as per Pro Tip
                Window.ComboVoice.Items.Add(new ComboBoxItem 
                { 
                    Content = $"✨ {v.DisplayName}", 
                    Tag = v.DisplayName 
                });
            }

            // Restore selection from preferences
            var p = PreferenceManager.Shared.Preferences;
            if (!string.IsNullOrEmpty(p.SelectedVoice))
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedVoice)
                    {
                        Window.ComboVoice.SelectedItem = item;
                        break;
                    }
                }
            }
            
            if (Window.ComboVoice.SelectedIndex == -1 && Window.ComboVoice.Items.Count > 0) 
                Window.ComboVoice.SelectedIndex = 0;
        }

        public void VoiceChanged(object selectedItem)
        {
            if (selectedItem is ComboBoxItem item)
            {
                var p = PreferenceManager.Shared.Preferences;
                string voiceName = item.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(voiceName) && p.SelectedVoice != voiceName)
                {
                    p.SelectedVoice = voiceName;
                    PreferenceManager.Shared.Save();
                }
            }
        }

        public void SpeedChanged(double value)
        {
            if (Window.TxtSpeedValue != null) Window.TxtSpeedValue.Text = value.ToString("0.0");
        }

        public void VolumeChanged(double value)
        {
            if (Window.TxtVolumeValue != null) Window.TxtVolumeValue.Text = $"{(int)value}%";
        }

        public void PitchChanged(double value)
        {
            if (Window.TxtPitchValue != null) Window.TxtPitchValue.Text = value.ToString("0.0");
        }

        public async Task PlaySample()
        {
            if (Window.BtnPlaySample == null) return;
            string originalContent = "▷ Play Sample";

            string voice = "alloy";
            bool isLocal = false;

            if (Window.ComboVoice.SelectedItem is ComboBoxItem item)
            {
                voice = item.Tag?.ToString() ?? "alloy";
                isLocal = item.Content?.ToString().StartsWith("✨") ?? false;
            }

            string apiKey = Window.TxtApiKey.Password;

            try
            {
                Window.BtnPlaySample.Content = "Generating...";
                Window.BtnPlaySample.IsEnabled = false;

                string text = !string.IsNullOrWhiteSpace(Window.TxtPlayground.Text)
                    ? Window.TxtPlayground.Text
                    : "Hello, I am CosmoWhisper, your advanced AI assistant.";

                string audioFile = "";

                if (isLocal)
                {
                    using (var synth = new SpeechSynthesizer())
                    {
                        synth.Options.SpeakingRate = Window.SldSpeed.Value;
                        var voices = SpeechSynthesizer.AllVoices;
                        var selectedVoice = voices.FirstOrDefault(v => v.DisplayName == voice);
                        if (selectedVoice != null) synth.Voice = selectedVoice;

                        var stream = await synth.SynthesizeTextToStreamAsync(text);
                        audioFile = Path.Combine(Path.GetTempPath(), $"local_{Guid.NewGuid()}.mp3");
                        using (var dataReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)))
                        {
                            await dataReader.LoadAsync((uint)stream.Size);
                            byte[] buffer = new byte[(int)stream.Size];
                            dataReader.ReadBytes(buffer);
                            await File.WriteAllBytesAsync(audioFile, buffer);
                        }
                    }
                }
                else
                {
                    audioFile = await AIService.Shared.GenerateSpeech(text, voice, Window.SldSpeed.Value, apiKey);
                }

                if (audioFile.StartsWith("Error:"))
                {
                    Window.BtnPlaySample.Content = "❌ Failed";
                    _ = CosmoMessage.Show("Speech Error", audioFile, "🔊");
                    await Task.Delay(2000);
                }
                else
                {
                    Window.BtnPlaySample.Content = "🔊 Playing...";
                    await AudioRecorder.Shared.PlayAudio(audioFile);
                    await Task.Delay(3000);
                }
            }
            catch (Exception ex)
            {
                _ = CosmoMessage.Show("Error", ex.Message, "❌");
            }
            finally
            {
                Window.BtnPlaySample.Content = originalContent;
                Window.BtnPlaySample.IsEnabled = true;
            }
        }
    }
}
