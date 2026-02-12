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
            PreferenceManager.Shared.PreferencesUpdated += UpdateDashboardStats;

            var p = PreferenceManager.Shared.Preferences;
            if (!string.IsNullOrEmpty(p.LicenseToken))
            {
                // Ensure we don't start multiple tasks if initialized multiple times
                // Ideally this should use a CancellationTokenSource, but for now we trust single initialization or restart.
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await LicenseManager.Shared.SyncStatusAsync();
                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }
                });
            }
        }

        public void Cleanup()
        {
            PreferenceManager.Shared.PreferencesUpdated -= UpdateDashboardStats;
        }

        public async Task CheckAuthStatus()
        {
            UpdateDashboardStats();
            if (!string.IsNullOrEmpty(PreferenceManager.Shared.Preferences.AuthToken))
            {
                await BackendService.Shared.SyncStatus();
                UpdateDashboardStats();
            }
        }

        public void UpdateDashboardStats()
        {
            Window.Dispatcher.Invoke(() =>
            {
                var p = PreferenceManager.Shared.Preferences;
                var sub = SubscriptionManager.Shared;
                
                string tierName = sub.TierDisplayName;
                string tierIcon = sub.TierIcon;
                bool isUnlimited = sub.IsUnlimited;

                if (Window.TxtTierStatus != null) Window.TxtTierStatus.Text = tierName;
                if (Window.TierIcon != null) Window.TierIcon.Text = tierIcon;
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
                if (Window.StoreLiteBanner != null)
                {
                    Window.StoreLiteBanner.Visibility = p.IsStoreVersion ? Visibility.Visible : Visibility.Collapsed;
                }

                // If Pro is active AND it's not the Store version, show normal Pro Banner
                if (Window.ProBanner != null)
                {
                    Window.ProBanner.Visibility = (!p.IsStoreVersion && sub.IsUnlimited) ? Visibility.Visible : Visibility.Collapsed;
                }

                // Account View
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
            });
        }

        public async void ResetStats()
        {
            var result = await CosmoMessage.Show("Reset Stats", "Are you sure you want to reset all performance stats? This cannot be undone.", "🧹", true);
            if (result)
            {
                var p = PreferenceManager.Shared.Preferences;
                p.TotalWords = 0;
                p.TotalTranscriptions = 0;
                p.TotalTimeSavedMinutes = 0;
                PreferenceManager.Shared.Save();
                UpdateDashboardStats();
            }
        }
        public async Task PerformLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _ = CosmoMessage.Show("Validation Error", "Please enter both email and password.", "📧");
                return;
            }

            var (success, message) = await BackendService.Shared.Login(email, password);

            if (success)
            {
                UpdateDashboardStats();
                Window._navigation.ShowAccount();
                await BackendService.Shared.SyncStatus();
                UpdateDashboardStats();
            }
            else
            {
                _ = CosmoMessage.Show("Login Failed", message, "❌");
            }
        }

        public void ActivateLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            if (key == "COSMO-PRO-TEST")
            {
                var p = PreferenceManager.Shared.Preferences;
                p.LicenseToken = key;
                p.UserTier = "pro";
                PreferenceManager.Shared.Save();

                _ = CosmoMessage.Show("Success", "License Activated Successfully!", "💎");
                UpdateDashboardStats();
            }
            else
            {
                _ = CosmoMessage.Show("Error", "Invalid License Key", "❌");
            }
        }

        public void SignOut()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.LicenseToken = "";
            p.AuthToken = "";
            p.UserEmail = "";
            p.UserTier = "free";
            PreferenceManager.Shared.Save();

            UpdateDashboardStats();
            Window._navigation.ShowLogin();
        }
    }
}
