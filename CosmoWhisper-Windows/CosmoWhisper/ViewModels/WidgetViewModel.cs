using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Threading.Tasks;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Managers;

namespace CosmoWhisper.ViewModels
{
    public class WidgetViewModel : INotifyPropertyChanged
    {
        private readonly IAIService _aiService;
        private readonly IPreferenceManager _preferenceManager;
        private readonly ISoundManager _soundManager;
        private readonly IVocabularyManager _vocabularyManager;
        private readonly ILogger _logger;

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            set 
            { 
                if (_isRecording != value)
                {
                    _isRecording = value; 
                    OnPropertyChanged();
                }
            }
        }

        private bool _isThinking;
        public bool IsThinking
        {
            get => _isThinking;
            set { _isThinking = value; OnPropertyChanged(); }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private string _hotkeyHint = "Hold F9";
        public string HotkeyHint
        {
            get => _hotkeyHint;
            set { _hotkeyHint = value; OnPropertyChanged(); }
        }

        private double _audioLevel;
        public double AudioLevel
        {
            get => _audioLevel;
            set { _audioLevel = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.Color _themeColor = System.Windows.Media.Color.FromRgb(0, 122, 255);
        public System.Windows.Media.Color ThemeColor
        {
            get => _themeColor;
            set { _themeColor = value; OnPropertyChanged(); }
        }

        public WidgetViewModel(
            IAIService aiService, 
            IPreferenceManager preferenceManager, 
            ISoundManager soundManager,
            IVocabularyManager vocabularyManager,
            ILogger logger)
        {
            _aiService = aiService;
            _preferenceManager = preferenceManager;
            _soundManager = soundManager;
            _vocabularyManager = vocabularyManager;
            _logger = logger;

            UpdateHotkeyHint();
            
            _preferenceManager.PreferencesUpdated += UpdateHotkeyHint;
        }

        private void UpdateHotkeyHint()
        {
            var p = _preferenceManager.Preferences;
            if (p.MouseButton != "None") HotkeyHint = $"Hold {p.MouseButton}";
            else HotkeyHint = $"Hold {p.ActivationKey}";
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void ToggleRecording()
        {
            if (IsRecording) AudioRecorder.Shared.StopRecording();
            else AudioRecorder.Shared.StartRecording();
        }

        public void SaveWidgetPosition(double left, double top)
        {
            // Guard against invalid values during startup
            if (double.IsNaN(left) || double.IsNaN(top) || left < -2000 || top < -2000) return;

            var p = _preferenceManager.Preferences;
            p.WidgetLeft = left;
            p.WidgetTop = top;
            _preferenceManager.Save();
        }
    }
}
