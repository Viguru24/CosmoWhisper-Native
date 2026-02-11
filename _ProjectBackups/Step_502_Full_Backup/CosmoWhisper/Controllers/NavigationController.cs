using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using System.Windows.Media.Animation;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Controllers
{
    public class NavigationController : BaseViewController
    {
        public NavigationController(DashboardWindow window) : base(window) { }

        public void ShowDashboard()
        {
            AudioRecorder.Shared.StartMonitoring();
            SwitchToView(Window.DashboardHeader, Window.DashboardView, Window.BtnDashboard);
        }

        private List<UIElement?>? _allViews;
        private List<UIElement?>? _allHeaders;

        public void ShowSmartCommands()
        {
            SwitchToView(Window.SmartCommandsHeader, Window.SmartCommandsView, Window.BtnSmartCommands);
        }

        public void ShowMicrophone()
        {
            AudioRecorder.Shared.StartMonitoring();
            SwitchToView(Window.MicrophoneHeader, Window.MicrophoneView, Window.BtnMicrophone);
        }

        public void ShowNarration()
        {
            SwitchToView(Window.NarrationHeader, Window.NarrationView, Window.BtnNarration);
        }

        public void ShowLibrary()
        {
            SwitchToView(null, Window.CloudLibraryView, Window.BtnLibrary);
        }

        public void ShowAccount()
        {
            SwitchToView(Window.AccountHeader, Window.AccountView, Window.BtnAccount);
        }

        public void ShowLogin()
        {
            SwitchToView(Window.LoginHeader, Window.LoginView, null);
        }

        public void ShowIntelligence()
        {
            SwitchToView(Window.IntelligenceHeader, Window.IntelligenceView, Window.BtnIntelligence);
        }

        public void ShowVocabulary()
        {
            SwitchToView(Window.VocabularyHeader, Window.VocabularyView, Window.BtnVocabulary);
        }

        public void ShowPreferences()
        {
            SwitchToView(Window.PreferencesHeader, Window.PreferencesView, Window.BtnPreferences);
        }



        public void OpenVault()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "http://localhost:8338",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening vault: {ex.Message}");
            }
        }

        public void SwitchToView(UIElement? header, UIElement? view, Button? activeBtn)
        {
            if (view == null) return;

            try
            {
                // 1. Force collapse ALL known views
                if (_allViews == null)
                {
                    _allViews = new List<UIElement?> {
                        Window.DashboardView, Window.SmartCommandsView, Window.MicrophoneView, Window.VocabularyView,
                        Window.NarrationView, Window.IntelligenceView, Window.PreferencesView,

                        Window.AccountView, Window.CloudLibraryView, Window.LoginView
                    };
                }

                foreach (var v in _allViews)
                {
                    if (v != null) v.Visibility = Visibility.Collapsed;
                }

                // 2. Force collapse Headers
                if (_allHeaders == null)
                {
                    _allHeaders = new List<UIElement?> {
                       Window.DashboardHeader, Window.SmartCommandsHeader, Window.MicrophoneHeader, Window.VocabularyHeader,
                       Window.NarrationHeader, Window.IntelligenceHeader, Window.PreferencesHeader,

                       Window.AccountHeader, Window.LoginHeader
                    };
                }

                foreach (var h in _allHeaders)
                {
                    if (h != null) h.Visibility = Visibility.Collapsed;
                }

                // Inactivate all buttons
                SetButtonInactive(Window.BtnDashboard);
                SetButtonInactive(Window.BtnSmartCommands);
                SetButtonInactive(Window.BtnMicrophone);
                SetButtonInactive(Window.BtnVocabulary);
                SetButtonInactive(Window.BtnNarration);
                SetButtonInactive(Window.BtnIntelligence);
                SetButtonInactive(Window.BtnPreferences);

                SetButtonInactive(Window.BtnAccount);
                SetButtonInactive(Window.BtnLibrary);

                // Show selected
                if (header != null) header.Visibility = Visibility.Visible;

                view.Visibility = Visibility.Visible;
                view.Opacity = 0;

                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400));
                var slideUp = new ThicknessAnimation(new Thickness(0, 20, 0, 0), new Thickness(0, 0, 0, 0), TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                view.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                view.BeginAnimation(FrameworkElement.MarginProperty, slideUp);

                // Ensure the view scrolls to the top when switched
                if (view is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(0);
                    sv.UpdateLayout();
                }

                if (activeBtn != null) SetButtonActive(activeBtn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SwitchToView Error: {ex.Message}");
                if (view != null)
                {
                    view.Visibility = Visibility.Visible;
                    view.Opacity = 1;
                }
            }
        }
    }
}
