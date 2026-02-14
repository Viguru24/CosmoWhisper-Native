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
                    Tag = v.Id // Use unique ID for technical precision
                });
            }

            var p = PreferenceManager.Shared.Preferences;
            bool selectionMade = false;
            
            // Try to restore user preference
            if (!string.IsNullOrEmpty(p.SelectedVoice))
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    if (item.Tag?.ToString() == p.SelectedVoice)
                    {
                        // If it's a neural voice but we have no key, don't auto-select it
                        bool isNeural = !item.Content.ToString().Contains("✨") && !item.Content.ToString().Contains("👑");
                        if (isNeural && string.IsNullOrWhiteSpace(p.OpenAIApiKey)) continue;

                        Window.ComboVoice.SelectedItem = item;
                        selectionMade = true;
                        break;
                    }
                }
            }
            
            // If No Preference OR Neural Voice failed (no key), Force Fallback to George
            if (!selectionMade)
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    string content = item.Content?.ToString() ?? "";
                    if (content.Contains("George") || content.Contains("👑"))
                    {
                        Window.ComboVoice.SelectedItem = item;
                        p.SelectedVoice = item.Tag?.ToString() ?? ""; // Sync preference
                        selectionMade = true;
                        break;
                    }
                }
            }

            // Absolute fallback to first item
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
                    voice = (item.Tag?.ToString() ?? "alloy").Trim();
                    string content = item.Content?.ToString() ?? "";
                    isLocal = content.Contains("✨") || content.Contains("👑");
                    DiagnosticManager.Shared.Log($"PlaySample UI Selection: '{content}' (Tag='{voice}'), isLocal={isLocal}", "VOICE");
                }

                // Reference preferences directly to ensure we have the latest Saved key
                var p = PreferenceManager.Shared.Preferences;
                string apiKey = p.OpenAIApiKey?.Trim() ?? "";

                if (!isLocal && string.IsNullOrWhiteSpace(apiKey))
                {
                    _ = CosmoMessage.Show("OpenAI Key Required", "Neural voices (like Alloy or Nova) require an OpenAI API Key.\n\nPlease enter your key and click the 💾 Save button first.", "\uD83D\uDD11");
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
                        // Prioritize ID match, then DisplayName, then partial name
                        var selectedVoice = voices.FirstOrDefault(v => v.Id == voice) 
                                         ?? voices.FirstOrDefault(v => v.DisplayName == voice)
                                         ?? voices.FirstOrDefault(v => v.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase));
                                         
                        if (selectedVoice != null) 
                        {
                            synth.Voice = selectedVoice;
                            DiagnosticManager.Shared.Log($"Voice Success: Loaded {selectedVoice.DisplayName}", "VOICE");
                        }
                        else
                        {
                            DiagnosticManager.Shared.Log($"Voice Warning: Could not find '{voice}', using system default", "WARN");
                        }

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
                    
                    if (audioFile.Contains("insufficient_quota"))
                    {
                        _ = CosmoMessage.Show("Credits Required", 
                            "It looks like your OpenAI account has run out of credits.\n\nTo use neural high-fidelity voices, please add a small balance (e.g. $5) to your OpenAI Billing Dashboard.", 
                            "\uD83D\uDCA2");
                    }
                    else if (audioFile.Contains("invalid_api_key"))
                    {
                        _ = CosmoMessage.Show("Invalid API Key", 
                            "The OpenAI API key you entered is not being recognized.\n\nPlease double-check the key in Voice Studio and click Save again.", 
                            "\u26A0\uFE0F");
                    }
                    else
                    {
                        _ = CosmoMessage.Show("Speech Error", audioFile, "🔊");
                    }
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
