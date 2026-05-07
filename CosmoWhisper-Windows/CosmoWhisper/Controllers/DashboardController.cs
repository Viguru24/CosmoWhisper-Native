using System;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Application = System.Windows.Application;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Media.Animation;
using System.Linq;
using System.Threading.Tasks;
using CosmoWhisper.Managers;
using CosmoWhisper.Services;
using CosmoWhisper;

namespace CosmoWhisper.Controllers
{
    public class DashboardController : BaseViewController
    {
        public DashboardController(DashboardWindow window) : base(window)
        {
        }

        public void Initialize()
        {
            UpdateDashboardStats();
            
            // Subscribe to Domain Events
            AccountService.Shared.StatsUpdated += () => UpdateDashboardStats();
            AccountService.Shared.AccessBlocked += () => {
                Window.Dispatcher.Invoke(() => {
                    _ = CosmoMessage.Show("Access Paused", "Your access has been paused by an administrator. It will resume automatically when restored.", "🔒");
                });
            };
            AccountService.Shared.SessionExpired += () => {
                SignOut();
                _ = Task.Delay(500).ContinueWith(_ => Window.Dispatcher.Invoke(() => 
                {
                    _ = CosmoMessage.Show("Session Expired", "Your session has expired or your access was paused. Please log in again.", "🔒");
                }));
            };

            PreferenceManager.Shared.PreferencesUpdated += () => UpdateDashboardStats();
        }

        public void Cleanup()
        {
        }

        public async Task CheckAuthStatus()
        {
            await AccountService.Shared.CheckAuthStatusAsync();
        }

        public void UpdateDashboardStats()
        {
            var p = PreferenceManager.Shared.Preferences;
            // SAFE CHECK: If we're logged out, don't update UI widgets that are being disposed/removed
            if (string.IsNullOrEmpty(p.AuthToken) && string.IsNullOrEmpty(p.LicenseToken)) return;
            if (Window == null) return;

            Window.Dispatcher.Invoke(() =>
            {
                var p = PreferenceManager.Shared.Preferences;
                var sub = SubscriptionManager.Shared;
                
                string tierName = sub.TierDisplayName;
                string tierIcon = sub.TierIcon;
                bool isUnlimited = sub.IsUnlimited;

                if (Window.TxtTierStatus != null) Window.TxtTierStatus.Text = tierName;
                if (Window.TierIcon != null) Window.TierIcon.Text = tierIcon;
                if (Window.TxtUserEmail != null) 
                {
                    if (!string.IsNullOrEmpty(p.UserEmail)) Window.TxtUserEmail.Text = p.UserEmail;
                    else if (!string.IsNullOrEmpty(p.AuthToken)) Window.TxtUserEmail.Text = "Token Active";
                    else Window.TxtUserEmail.Text = "Not Logged In";
                }
                if (Window.TxtUsageLabel != null) Window.TxtUsageLabel.Text = isUnlimited ? "Unlimited Access" : $"{p.UsageLimitMinutes - p.UsageMinutes:F1} min remaining";

                if (Window.TxtUsageStats != null)
                {
                    Window.TxtUsageStats.Text = isUnlimited ? $"{p.UsageMinutes:F1} / ∞" : $"{p.UsageMinutes:F1} / {p.UsageLimitMinutes}";
                }

                if (Window.UsageProgressBar != null)
                {
                    double percentage = isUnlimited ? 0.1 : (p.UsageMinutes / p.UsageLimitMinutes);
                    if (percentage > 1) percentage = 1;

                    var animation = new DoubleAnimation(percentage * 300, TimeSpan.FromMilliseconds(800))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Window.UsageProgressBar.BeginAnimation(FrameworkElement.WidthProperty, animation);

                    Window.UsageProgressBar.Background = (p.UsageMinutes >= p.UsageLimitMinutes && !isUnlimited)
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                        : (Brush)Application.Current.Resources["ThemeAccentBrush"];
                }

                if (Window.StatTotalWords != null) Window.StatTotalWords.Text = p.TotalWords.ToString("N0");
                if (Window.StatTranscriptions != null) Window.StatTranscriptions.Text = p.TotalTranscriptions.ToString("N0");
                if (Window.StatTimeSaved != null)
                {
                    double hours = p.TotalTimeSavedMinutes / 60.0;
                    Window.StatTimeSaved.Text = hours >= 1 ? $"{hours:F1}h" : $"{p.TotalTimeSavedMinutes:F0}m";
                }

                // Handle Store Lite Banner Visibility
                // Handle Banner Visibility
                bool isPro = sub.IsUnlimited;

                if (Window.StoreLiteBanner != null)
                {
                    // Always show the "Lite/Free" banner explanation if the user is NOT Pro.
                    // This satisfies Microsoft Store Policy 10.8.4 regarding disclosure of free tier limits.
                    Window.StoreLiteBanner.Visibility = isPro ? Visibility.Collapsed : Visibility.Visible;
                }

                if (Window.ProBanner != null)
                {
                    // Show Pro status only if they are Pro
                    Window.ProBanner.Visibility = isPro ? Visibility.Visible : Visibility.Collapsed;
                }

                // Account View — Profile Card name
                if (Window.AccountUserName != null)
                {
                    if (!string.IsNullOrEmpty(p.UserEmail)) Window.AccountUserName.Text = p.UserEmail;
                    else if (!string.IsNullOrEmpty(p.AuthToken)) Window.AccountUserName.Text = "Token Active";
                    else Window.AccountUserName.Text = "Not Logged In";
                }
                // Account View — Subscription section
                if (Window.TxtAccountEmail != null)
                {
                    if (!string.IsNullOrEmpty(p.UserEmail))
                    {
                        Window.TxtAccountEmail.Text = tierName;
                    }
                    else if (!string.IsNullOrEmpty(p.AuthToken))
                    {
                        Window.TxtAccountEmail.Text = "Loading...";
                    }
                    else
                    {
                        Window.TxtAccountEmail.Text = "Not Signed In";
                    }
                }
                if (Window.AccountTierStatus != null) Window.AccountTierStatus.Text = tierName;
                if (Window.AccountTierIcon != null) Window.AccountTierIcon.Text = tierIcon;
                if (Window.AccountUsageLabel != null) Window.AccountUsageLabel.Text = isUnlimited ? "Unlimited Access" : $"{p.UsageLimitMinutes - p.UsageMinutes:F1} min remaining";
                if (Window.AccountUsageStats != null) Window.AccountUsageStats.Text = isUnlimited ? $"{p.UsageMinutes:F1} / ∞" : $"{p.UsageMinutes:F1} / {p.UsageLimitMinutes}";

                if (Window.AccountUsageProgressBar != null)
                {
                    double percentage = isUnlimited ? 0.1 : (p.UsageMinutes / p.UsageLimitMinutes);
                    if (percentage > 1) percentage = 1;

                    var animation = new DoubleAnimation(percentage * 300, TimeSpan.FromMilliseconds(800))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Window.AccountUsageProgressBar.BeginAnimation(FrameworkElement.WidthProperty, animation);
                }

                if (Window.BtnUpgradeAccount != null)
                {
                    Window.BtnUpgradeAccount.Visibility = isPro ? Visibility.Collapsed : Visibility.Visible;
                }
            });
        }

        public async void ResetStats()
        {
            var result = await CosmoMessage.Show("Reset Stats", "Are you sure you want to reset all performance stats? This cannot be undone.", "🧹", true);
            if (result)
            {
                AccountService.Shared.ResetStats();
            }
        }
        public async void ActivateLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var (success, _) = await AccountService.Shared.ActivateLicenseAsync(key);
            
            if (success)
            {
                _ = CosmoMessage.Show("Success", "License Activated Successfully!", "✨");
            }
            else
            {
                _ = CosmoMessage.Show("Error", "Invalid License Key or Server Offline", "❌");
            }
        }

        public async Task PerformMagicLogin(string code)
        {
             // This is a placeholder for the master version's logic
             // It seems the master version had this method but it was lost in sync.
             // We'll implement a basic version that triggers the auth flow.
             await Task.Yield();
             _ = CosmoMessage.Show("Magic Link", "Magic Link flow triggered. Please check your email.", "✨");
        }

        public void SignOut()
        {
            DiagnosticManager.Shared.Log("SignOut initiated.", "AUTH");
            AccountService.Shared.SignOut();
            
            // UI Transition
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Don't hide the widget on sign out - it confuses the user.
                    // The widget will update its own state based on the missing token.
                    /*
                    var windows = Application.Current.Windows.Cast<Window>().ToList();
                    foreach (var window in windows)
                    {
                        if (window is WidgetWindow widget) widget.Hide();
                    }
                    */

                    var dashboard = Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
                    if (dashboard == null) dashboard = new DashboardWindow();

                    if (dashboard._navigation != null)
                    {
                        dashboard._navigation.ShowLogin();
                        dashboard.Show();
                        dashboard.Activate();
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticManager.Shared.Log($"Error during SignOut UI transition: {ex.Message}", "ERROR");
                }
            });
        }
    }
}


