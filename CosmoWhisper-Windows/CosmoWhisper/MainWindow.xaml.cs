using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using CosmoWhisper.Managers;
using CosmoWhisper.Manus;

using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;

namespace CosmoWhisper;

public partial class MainWindow : Window
{
#pragma warning disable CS8618
    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to recorder events
        AudioRecorder.Shared.IsRecordingChanged += OnIsRecordingChanged;
        AudioRecorder.Shared.AudioLevelChanged += OnAudioLevelChanged;
        AudioRecorder.Shared.TranscriptionReceived += OnTranscriptionReceived;
        ManusAgent.Shared.ManusStatusChanged += OnManusStatusChanged;
        ManusAgent.Shared.ManusResponseReceived += OnManusResponseReceived;
        AudioRecorder.Shared.ErrorOccurred += OnErrorOccurred;
    }

    private void OnErrorOccurred(string error)
    {
        Dispatcher.Invoke(() =>
        {
            OutputTextBox.Foreground = Brushes.Red;
            OutputTextBox.Text = $"ERROR: {error}";
        });
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        OutputTextBox.Foreground = Brushes.Black;
        AudioRecorder.Shared.ToggleRecording();
    }

    private void OnManusStatusChanged(string status)
    {
        Dispatcher.Invoke(() =>
        {
            ManusTextBox.Text = status;
        });
    }

    private void OnManusResponseReceived(string response)
    {
        Dispatcher.Invoke(() =>
        {
            ManusTextBox.Text = response;
        });
    }

    private void OnIsRecordingChanged(bool isRecording)
    {
        Dispatcher.Invoke(() =>
        {
            RecordButton.Content = isRecording ? "Stop Recording" : "Start Recording";
        });
    }

    private void OnAudioLevelChanged(float level)
    {
        Dispatcher.Invoke(() =>
        {
            AudioLevelMeter.Value = level;
        });
    }

    private void OnTranscriptionReceived(string text)
    {
        Dispatcher.Invoke(() =>
        {
            OutputTextBox.Text = text;
        });
    }
}
