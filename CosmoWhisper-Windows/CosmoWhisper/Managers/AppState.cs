using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CosmoWhisper.Managers
{
    public class AppState : INotifyPropertyChanged
    {
        public static AppState Shared { get; } = new AppState();

        private bool _isRecording = false;
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

        private bool _isAccessBlocked = false;
        public bool IsAccessBlocked
        {
            get => _isAccessBlocked;
            set
            {
                if (_isAccessBlocked != value)
                {
                    _isAccessBlocked = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
