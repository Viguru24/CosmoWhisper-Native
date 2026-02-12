using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using CosmoWhisper.Managers;
using CosmoWhisper.Services;

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
            InitializeOutputDevices();
            
            // Sync UI Sliders with Preferences - Removed (Section Deleted)
        }

        private void InitializeVoices()
        {
            if (Window.ComboVoice == null) return;
            Window.ComboVoice.Items.Clear();

            // 1. Add Neural Voices (OpenAI)
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

            var voices = SpeechSynthesizer.AllVoices
                .OrderBy(v => v.DisplayName.Contains("George") ? 0 : 1)
                .ThenBy(v => v.DisplayName);

            foreach (var v in voices)
            {
                Window.ComboVoice.Items.Add(new ComboBoxItem 
                { 
                    Content = v.DisplayName.Contains("George") ? $"👑 {v.DisplayName} (Default)" : $"✨ {v.DisplayName}", 
                    Tag = v.DisplayName 
                });
            }

            var p = PreferenceManager.Shared.Preferences;
            bool selectionMade = false;
            
            if (!string.IsNullOrEmpty(p.SelectedVoice))
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedVoice)
                    {
                        Window.ComboVoice.SelectedItem = item;
                        selectionMade = true;
                        break;
                    }
                }
            }
            
            if (!selectionMade)
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    if (item.Tag?.ToString().Contains("George") == true)
                    {
                        Window.ComboVoice.SelectedItem = item;
                        selectionMade = true;
                        break;
                    }
                }
            }

            if (!selectionMade && Window.ComboVoice.Items.Count > 0) 
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

        private void InitializeOutputDevices()
        {
            if (Window.ComboOutput == null) return;
            Window.ComboOutput.Items.Clear();

            var devices = AudioRecorder.Shared.EnumerateOutputDevices();
            foreach (var dev in devices)
            {
                Window.ComboOutput.Items.Add(new ComboBoxItem 
                { 
                    Content = $"🔈 {dev.Name}", 
                    Tag = dev.Index 
                });
            }

            var p = PreferenceManager.Shared.Preferences;
            bool selected = false;
            foreach (ComboBoxItem item in Window.ComboOutput.Items)
            {
                if (item.Tag is int idx && idx == p.OutputDeviceIndex)
                {
                    Window.ComboOutput.SelectedItem = item;
                    selected = true;
                    break;
                }
            }
            if (!selected && Window.ComboOutput.Items.Count > 0) Window.ComboOutput.SelectedIndex = 0;
        }

        public void OutputDeviceChanged()
        {
            if (Window.ComboOutput.SelectedItem is ComboBoxItem item && item.Tag is int index)
            {
                var p = PreferenceManager.Shared.Preferences;
                p.OutputDeviceIndex = index;
                p.OutputDeviceName = item.Content?.ToString() ?? "Unknown";
                PreferenceManager.Shared.Save();
            }
        }



        public async Task PlaySample()
        {
            if (Window.BtnPlaySample == null) return;
            string originalContent = "▷ Play Sample";

            try
            {
                string text = !string.IsNullOrWhiteSpace(Window.TxtPlayground.Text)
                    ? Window.TxtPlayground.Text
                    : "Hello, I am CosmoWhisper, your advanced AI assistant.";

                string voice = "alloy";
                bool isLocal = false;

                if (Window.ComboVoice.SelectedItem is ComboBoxItem item)
                {
                    voice = item.Tag?.ToString() ?? "alloy";
                    string content = item.Content?.ToString() ?? "";
                    isLocal = content.Contains("✨") || content.Contains("👑");
                }

                DiagnosticManager.Shared.Log($"PlaySample: Voice={voice}, Local={isLocal}, Text='{text.Substring(0, Math.Min(text.Length, 30))}...'", "VOICE");

                // Use the key from Preferences directly to ensure sync between views
                string apiKey = PreferenceManager.Shared.Preferences.OpenAIApiKey;

                if (!isLocal && string.IsNullOrWhiteSpace(apiKey))
                {
                    _ = CosmoMessage.Show("OpenAI Key Required", "Neural voices (like Alloy) require an OpenAI API Key. Please enter one in the Intelligence settings or select a local voice (with \u2728 or \uD83D\uDC51).", "\uD83D\uDD11");
                    return;
                }

                Window.BtnPlaySample.Content = "Generating...";
                Window.BtnPlaySample.IsEnabled = false;

                string audioFile = "";

                if (isLocal)
                {
                    using (var synth = new SpeechSynthesizer())
                    {
                        var voices = SpeechSynthesizer.AllVoices;
                        var selectedVoice = voices.FirstOrDefault(v => v.DisplayName == voice || v.Id == voice);
                        if (selectedVoice != null) synth.Voice = selectedVoice;

                        try
                        {
                            synth.Options.SpeakingRate = 1.0;
                            synth.Options.AudioVolume = 1.0; // Max volume

                            DiagnosticManager.Shared.Log($"Plain Text Synthesis (Default Settings)", "VOICE");
                            var stream = await synth.SynthesizeTextToStreamAsync(text);
                            audioFile = await SaveStreamToTempFile(stream);
                        }
                        catch (Exception innerEx)
                        {
                            DiagnosticManager.Shared.Log($"Safe Fallback: Options failed, trying bare synthesis: {innerEx.Message}", "WARN");
                            
                            // RESET everything for the bare attempt
                            using (var safeSynth = new SpeechSynthesizer())
                            {
                                if (selectedVoice != null) safeSynth.Voice = selectedVoice;
                                var safeStream = await safeSynth.SynthesizeTextToStreamAsync(text);
                                audioFile = await SaveStreamToTempFile(safeStream);
                            }
                        }
                    }
                }
                else
                {
                    // Always use 1.0 speed for AI voices
                    audioFile = await AIService.Shared.GenerateSpeech(text, voice, 1.0, apiKey);
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
                    await Task.Delay(4000); // Increased wait for playback
                }
            }
            catch (Exception ex)
            {
                var state = new Dictionary<string, object>
                {
                    { "Voice", Window.ComboVoice.SelectedItem?.ToString() ?? "null" },
                    { "Text", Window.TxtPlayground.Text },
                    { "Pitch", PreferenceManager.Shared.Preferences.VoicePitch },
                    { "Speed", PreferenceManager.Shared.Preferences.VoiceSpeed },
                    { "Volume", PreferenceManager.Shared.Preferences.VoiceVolume }
                };
                DiagnosticManager.Shared.TakeSnapshot(ex.Message, state);
                
                _ = CosmoMessage.Show("Narration Error", ex.Message, "❌");
                System.Diagnostics.Debug.WriteLine($"Narration Error: {ex.Message}");
            }
            finally
            {
                Window.BtnPlaySample.Content = originalContent;
                Window.BtnPlaySample.IsEnabled = true;
            }
        }

        private async Task<string> SaveStreamToTempFile(SpeechSynthesisStream stream)
        {
            string path = Path.Combine(Path.GetTempPath(), $"local_{Guid.NewGuid()}.wav");
            using (var outputStream = File.Create(path))
            {
                using (var inputStream = System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(stream))
                {
                    await inputStream.CopyToAsync(outputStream);
                }
            }
            return path;
        }
    }
}
