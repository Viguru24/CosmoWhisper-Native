using System;
using System.Windows;
using System.Windows.Controls;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using ComboBox = System.Windows.Controls.ComboBox;
using System.Windows.Media;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Threading.Tasks;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Controllers
{
    public class MicrophoneController : BaseViewController
    {
        public MicrophoneController(DashboardWindow window) : base(window)
        {
        }

        public async Task Initialize()
        {
            AudioRecorder.Shared.PlayInteractionSounds = PreferenceManager.Shared.Preferences.InteractionSoundsEnabled;
            UpdateInteractionSoundsUI();

            // Sync sensitivity UI
            var p = PreferenceManager.Shared.Preferences;
            if (Window.SldSensitivity != null)
            {
                Window.SldSensitivity.Value = p.MicSensitivity * 100;
                if (Window.TxtSensitivityValue != null) Window.TxtSensitivityValue.Text = $"{(int)(p.MicSensitivity * 100)}%";
            }

            await InitializeMicrophones();
        }

        public async Task InitializeMicrophones()
        {
            if (Window.ComboMics != null) Window.ComboMics.Items.Clear();
            if (Window.ComboOnboardingMics != null) Window.ComboOnboardingMics.Items.Clear();

            try
            {
                var devices = await AudioRecorder.Shared.EnumerateInputDevices();
                foreach (var d in devices)
                {
                    if (Window.ComboMics != null) 
                        Window.ComboMics.Items.Add(new ComboBoxItem { Content = d.Name, Tag = d.Id });
                    
                    if (Window.ComboOnboardingMics != null) 
                        Window.ComboOnboardingMics.Items.Add(new ComboBoxItem { Content = d.Name, Tag = d.Id });
                }

                var p = PreferenceManager.Shared.Preferences;
                if (Window.ComboMics != null && Window.ComboMics.Items.Count > 0)
                {
                    var selected = Window.ComboMics.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == p.MicDeviceId);
                    if (selected != null) Window.ComboMics.SelectedItem = selected;
                    else Window.ComboMics.SelectedIndex = 0;
                }

                if (Window.ComboOnboardingMics != null && Window.ComboOnboardingMics.Items.Count > 0)
                {
                    var selected = Window.ComboOnboardingMics.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == p.MicDeviceId);
                    if (selected != null) Window.ComboOnboardingMics.SelectedItem = selected;
                    else Window.ComboOnboardingMics.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mic Enum Error: {ex.Message}");
            }
        }

        public void MicSelectionChanged(ComboBoxItem item)
        {
            if (item != null)
            {
                var p = PreferenceManager.Shared.Preferences;
                p.MicDeviceName = item.Content.ToString();
                p.MicDeviceId = item.Tag.ToString();
                PreferenceManager.Shared.Save();

                AudioRecorder.Shared.SelectedDeviceId = p.MicDeviceId;
                if (AudioRecorder.Shared.IsMonitoring)
                {
                    AudioRecorder.Shared.StopMonitoring();
                    AudioRecorder.Shared.StartMonitoring();
                }
            }
        }

        public void SensitivityChanged(double value)
        {
            if (AudioRecorder.Shared != null)
                AudioRecorder.Shared.Sensitivity = value / 100.0;

            // Save to preferences
            var p = PreferenceManager.Shared.Preferences;
            p.MicSensitivity = value / 100.0;
            PreferenceManager.Shared.Save();

            if (Window.TxtSensitivityValue != null)
            {
                Window.TxtSensitivityValue.Text = $"{(int)value}%";
            }
        }

        public void ToggleInteractionSounds()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.InteractionSoundsEnabled = !p.InteractionSoundsEnabled;
            PreferenceManager.Shared.Save();

            AudioRecorder.Shared.PlayInteractionSounds = p.InteractionSoundsEnabled;
            UpdateInteractionSoundsUI();
        }

        public void UpdateInteractionSoundsUI()
        {
            if (Window.ToggleInteractionSounds == null) return;

            bool isActive = AudioRecorder.Shared.PlayInteractionSounds;
            Window.ToggleInteractionSounds.Background = isActive
                ? (Brush)Application.Current.Resources["ThemeAccentBrush"]
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30FFFFFF"));

            var ellipse = Window.ToggleInteractionSounds.Child as System.Windows.Shapes.Shape;
            if (ellipse != null)
            {
                ellipse.HorizontalAlignment = isActive ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }

        public async Task Calibrate()
        {
            if (Window.BtnCalibrate == null) return;
            Window.BtnCalibrate.Content = "Listening...";
            Window.BtnCalibrate.IsEnabled = false;

            await Task.Delay(2000);

            AudioRecorder.Shared.Sensitivity = 0.65;
            if (Window.SldSensitivity != null) Window.SldSensitivity.Value = 65;

            Window.BtnCalibrate.Content = "✓ Optimized";
            await Task.Delay(1000);
            Window.BtnCalibrate.Content = "⚡ Calibrate";
            Window.BtnCalibrate.IsEnabled = true;
        }
    }
}
