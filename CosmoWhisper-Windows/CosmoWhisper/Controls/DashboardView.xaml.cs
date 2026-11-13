using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System;

namespace CosmoWhisper.Controls
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        // Exposing internal elements for the legacy controller
        public Border StoreLiteBannerControl => StoreLiteBanner;
        public Border ProBannerControl => ProBanner;
        public TextBlock TxtTierStatusControl => TxtTierStatus;
        public TextBlock TierIconControl => TierIcon;
        public TextBlock TxtUsageLabelControl => TxtUsageLabel;
        public TextBlock TxtUsageStatsControl => TxtUsageStats;
        public Border UsageProgressBarControl => UsageProgressBar;
        public TextBlock StatTotalWordsControl => StatTotalWords;
        public TextBlock StatTimeSavedControl => StatTimeSaved;
        public TextBlock StatTranscriptionsControl => StatTranscriptions;
        public TextBlock TxtLocalWhisperStatusControl => TxtLocalWhisperStatus;
        public TextBlock TxtAutoSubmitStatusControl => TxtAutoSubmitStatus;
        public TextBlock LocalWhisperIconControl => LocalWhisperIcon;
        public TextBlock AutoSubmitIconControl => AutoSubmitIcon;
        public Ellipse LocalWhisperGlowControl => LocalWhisperGlow;
        public Ellipse AutoSubmitGlowControl => AutoSubmitGlow;

        private void ToggleLocalWhisper_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DashboardWindow.Instance?.ToggleLocalWhisper_Click(sender, e);
        }

        private void ToggleAutoSubmit_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DashboardWindow.Instance?.ToggleAutoSubmit_Click(sender, e);
        }

        private void ResetStats_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.ResetStats_Click(sender, e);
        }

        private void GetFullAccess_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow.Instance?.GetFullAccess_Click(sender, e);
        }
    }
}
