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

            // Save currently selected tag if any
            string? currentTag = null;
            if (Window.ComboVoice.SelectedItem is ComboBoxItem selected)
            {
                currentTag = selected.Tag?.ToString();
            }

            Window.ComboVoice.Items.Clear();

            // 1. Add System Voices (Local) - Filtered to George and Hazel
            string[] allowedBasics = { "George", "Hazel" };
            var voices = SpeechSynthesizer.AllVoices
                .Where(v => allowedBasics.Any(name => v.DisplayName.Contains(name)))
                .OrderBy(v => v.DisplayName.Contains("George") ? 0 : 1)
                .ThenBy(v => v.DisplayName);

            foreach (var v in voices)
            {
                Window.ComboVoice.Items.Add(new ComboBoxItem 
                { 
                    Content = v.DisplayName.Contains("George") ? $"👑 {v.DisplayName} (Default)" : $"✨ {v.DisplayName}", 
                    Tag = v.Id
                });
            }

            var p = PreferenceManager.Shared.Preferences;
            string targetVoice = !string.IsNullOrEmpty(currentTag) ? currentTag : p.SelectedVoice;
            bool selectionMade = false;
            
            // Try to match the target preference
            if (!string.IsNullOrEmpty(targetVoice))
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    if (item.Tag?.ToString() == targetVoice)
                    {
                        Window.ComboVoice.SelectedItem = item;
                        selectionMade = true;
                        break;
                    }
                }
            }
            
            // Fallback: Default to George
            if (!selectionMade)
            {
                foreach (ComboBoxItem item in Window.ComboVoice.Items)
                {
                    string content = item.Content?.ToString() ?? "";
                    if (content.Contains("George") || content.Contains("👑"))
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
            if (Window.ComboOutput?.SelectedItem is ComboBoxItem item && item.Tag is int index)
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
            string audioFile = "";

            try
            {
                string text = !string.IsNullOrWhiteSpace(Window.TxtPlayground?.Text)
                    ? Window.TxtPlayground.Text
                    : "Hello, I am CosmoWhisper, your advanced AI assistant.";

                string voice = "George";

                if (Window.ComboVoice?.SelectedItem is ComboBoxItem item)
                {
                    voice = (item.Tag?.ToString() ?? "George").Trim();
                    string content = item.Content?.ToString() ?? "";
                    DiagnosticManager.Shared.Log($"PlaySample UI Selection: '{content}' (Tag='{voice}')", "VOICE");
                }

                Window.BtnPlaySample.Content = "Generating...";
                Window.BtnPlaySample.IsEnabled = false;

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
                        using var stream = await synth.SynthesizeTextToStreamAsync(text);
                        audioFile = await SaveStreamToTempFile(stream);
                    }
                    catch (Exception innerEx)
                    {
                        DiagnosticManager.Shared.Log($"Safe Fallback: Options failed, trying bare synthesis: {innerEx.Message}", "WARN");
                        
                        // RESET everything for the bare attempt
                        using (var safeSynth = new SpeechSynthesizer())
                        {
                            if (selectedVoice != null) safeSynth.Voice = selectedVoice;
                            using var safeStream = await safeSynth.SynthesizeTextToStreamAsync(text);
                            audioFile = await SaveStreamToTempFile(safeStream);
                        }
                    }
                }

                if (audioFile.StartsWith("Error:"))
                {
                    Window.BtnPlaySample.Content = "Failed";
                    _ = CosmoMessage.Show("Speech Error", audioFile, "\uE767");
                    await Task.Delay(2000);
                }
                else
                {
                    Window.BtnPlaySample.Content = "Playing...";
                    await AudioRecorder.Shared.PlayAudio(audioFile);
                }
            }
            catch (Exception ex)
            {
                var state = new Dictionary<string, object>
                {
                    { "Voice", Window.ComboVoice?.SelectedItem?.ToString() ?? "null" },
                    { "Text", Window.TxtPlayground?.Text ?? "" },
                    { "Pitch", PreferenceManager.Shared.Preferences.VoicePitch },
                    { "Speed", PreferenceManager.Shared.Preferences.VoiceSpeed },
                    { "Volume", PreferenceManager.Shared.Preferences.VoiceVolume }
                };
                DiagnosticManager.Shared.TakeSnapshot(ex.Message, state);
                
                _ = CosmoMessage.Show("Narration Error", ex.Message, "\uE783");
                System.Diagnostics.Debug.WriteLine($"Narration Error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(audioFile) && !audioFile.StartsWith("Error:") && File.Exists(audioFile))
                    {
                        File.Delete(audioFile);
                    }
                }
                catch { }
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


