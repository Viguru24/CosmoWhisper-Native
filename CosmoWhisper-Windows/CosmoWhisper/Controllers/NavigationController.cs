using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Linq;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Controllers
{
    public class NavigationController : BaseViewController
    {
        public NavigationController(DashboardWindow window) : base(window) { }

        public void ShowDashboard()
        {
            AudioRecorder.Shared.StopMonitoring(); // Save resources on main dashboard
            SwitchToView(Window.DashboardHeader, Window.DashboardView, Window.BtnDashboard);
            _ = Task.Run(() => CosmoWhisper.Services.AccountService.Shared.TriggerSync());
        }

        private List<UIElement?>? _allViews;
        private List<UIElement?>? _allHeaders;

        public void ShowSmartCommands()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(Window.SmartCommandsHeader, Window.SmartCommandsView, Window.BtnSmartCommands);
        }

        private void SetActiveTab(Button activeTab)
        {
            if (Window.TabPrefGeneral != null) Window.TabPrefGeneral.Tag = null;
            if (Window.TabPrefMicrophone != null) Window.TabPrefMicrophone.Tag = null;
            if (Window.TabPrefVocabulary != null) Window.TabPrefVocabulary.Tag = null;
            if (Window.TabPrefNarration != null) Window.TabPrefNarration.Tag = null;
            if (Window.TabPrefIntelligence != null) Window.TabPrefIntelligence.Tag = null;
            if (activeTab != null) activeTab.Tag = "Active";
        }

        public void ShowMicrophone()
        {
            AudioRecorder.Shared.StartMonitoring();
            SetActiveTab(Window.TabPrefMicrophone);
            SwitchToView(Window.PreferencesHeader, Window.MicrophoneView, Window.BtnPreferences);
        }

        public void ShowNarration()
        {
            AudioRecorder.Shared.StopMonitoring();
            SetActiveTab(Window.TabPrefNarration);
            SwitchToView(Window.PreferencesHeader, Window.NarrationView, Window.BtnPreferences);
        }

        public void ShowLibrary()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(null, Window.CloudLibraryView, Window.BtnLibrary);
        }

        public void ShowAccount()
        {
            AudioRecorder.Shared.StopMonitoring();
            var p = PreferenceManager.Shared.Preferences;
            // Token is the ultimate source of truth for login. Email is just UI metadata.
            bool isLoggedIn = !string.IsNullOrEmpty(p.AuthToken) || !string.IsNullOrEmpty(p.UserEmail);
            
            Debug.WriteLine($"[Navigation] ShowAccount: isLoggedIn={isLoggedIn}, Token={!string.IsNullOrEmpty(p.AuthToken)}, Email={!string.IsNullOrEmpty(p.UserEmail)}");

            if (!isLoggedIn)
            {
                ShowLogin();
                return;
            }
            SwitchToView(Window.AccountHeader, Window.AccountView, Window.BtnAccount);
            _ = Task.Run(() => CosmoWhisper.Services.AccountService.Shared.TriggerSync());
        }

        public void ShowLogin()
        {
            AudioRecorder.Shared.StopMonitoring();
            SwitchToView(Window.LoginHeader, Window.LoginView, null);
        }

        public void ShowIntelligence()
        {
            AudioRecorder.Shared.StopMonitoring();
            SetActiveTab(Window.TabPrefIntelligence);
            SwitchToView(Window.PreferencesHeader, Window.IntelligenceView, Window.BtnPreferences);
        }

        public void ShowVocabulary()
        {
            AudioRecorder.Shared.StopMonitoring();
            SetActiveTab(Window.TabPrefVocabulary);
            SwitchToView(Window.PreferencesHeader, Window.VocabularyView, Window.BtnPreferences);
        }

        public void ShowPreferences()
        {
            AudioRecorder.Shared.StopMonitoring();
            SetActiveTab(Window.TabPrefGeneral);
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

                // Inactivate all buttons (Comprehensive)
                var allButtons = new List<Button?> {
                    Window.BtnDashboard, Window.BtnSmartCommands, 
                    Window.BtnPreferences, Window.BtnAccount, Window.BtnLibrary
                };

                foreach (var b in allButtons)
                {
                    if (b != null) SetButtonInactive(b);
                }

                // Show selected
                if (header != null) header.Visibility = Visibility.Visible;

                view.Visibility = Visibility.Visible;
                view.Opacity = 0;

                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400));
                var slideUp = new ThicknessAnimation(new Thickness(0, 20, 0, 0), new Thickness(0, 0, 0, 0), TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                // connected scale animation
                var scaleX = new DoubleAnimation(0.97, 1.0, TimeSpan.FromMilliseconds(450)) { EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut } };
                var scaleY = new DoubleAnimation(0.97, 1.0, TimeSpan.FromMilliseconds(450)) { EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut } };

                view.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                view.BeginAnimation(FrameworkElement.MarginProperty, slideUp);

                if (view.RenderTransform is ScaleTransform st)
                {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
                }
                else if (view.RenderTransform is TransformGroup tg)
                {
                    var stInGroup = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
                    if (stInGroup != null)
                    {
                        stInGroup.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                        stInGroup.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
                    }
                }

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


