using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Models;

namespace CosmoWhisper.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IPreferenceManager _preferenceManager;
        private readonly ILogger _logger;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DashboardViewModel(IPreferenceManager preferenceManager, ILogger logger)
        {
            _preferenceManager = preferenceManager;
            _logger = logger;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add core properties here as needed during Phase 3
    }
}
