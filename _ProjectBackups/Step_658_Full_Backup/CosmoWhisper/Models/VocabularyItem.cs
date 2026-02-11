using System;
using System.ComponentModel;
using System.Windows;

namespace CosmoWhisper.Models
{
    public class VocabularyItem : INotifyPropertyChanged
    {
        private string _key;
        public string Key
        {
            get => _key;
            set
            {
                if (_key != value) { _key = value; OnPropertyChanged(nameof(Key)); }
            }
        }

        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value) { _value = value; OnPropertyChanged(nameof(Value)); }
            }
        }

        public string OriginalKey { get; set; }

        // --- Editing Support ---
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                    OnPropertyChanged(nameof(ReadModeVisibility));
                    OnPropertyChanged(nameof(EditModeVisibility));
                }
            }
        }

        // Temp storage to support Cancel
        public string TempKey { get; set; }
        public string TempValue { get; set; }

        public Visibility ReadModeVisibility => IsEditing ? Visibility.Collapsed : Visibility.Visible;
        public Visibility EditModeVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
