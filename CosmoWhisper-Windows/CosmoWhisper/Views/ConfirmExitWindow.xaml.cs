using System;
using System.Windows;
using System.Windows.Input;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Views
{
    public partial class ConfirmExitWindow : Window
    {
        public bool ConfirmedExit { get; private set; } = false;

        public ConfirmExitWindow()
        {
            InitializeComponent();
            LoadStats();
            LoadFunMessage();
        }

        private void LoadStats()
        {
            var p = PreferenceManager.Shared.Preferences;
            // Calculate a rough "hours saved" metric (e.g. 40 wpm typing speed assumption)
            double hoursSaved = p.TotalWords / 40.0 / 60.0;
            
            if (hoursSaved < 0.1)
            {
                TxtStats.Text = $"{p.TotalWords} words transcribed so far.";
            }
            else
            {
                TxtStats.Text = $"You've saved over {hoursSaved:F1} hours of typing!";
            }
        }

        private void LoadFunMessage()
        {
            string[] messages = {
                "See you later, space cowboy!",
                "Resting my vocal cords...",
                "Catch you on the flip side!",
                "Don't work too hard!",
                "I'll be here when you get back.",
                "Going into sleep mode...",
                "Leaving orbit. Safe travels!",
                "Signing off, Captain."
            };

            TxtFunMessage.Text = messages[new Random().Next(messages.Length)];
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ConfirmedExit = false;
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ConfirmedExit = true;
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
