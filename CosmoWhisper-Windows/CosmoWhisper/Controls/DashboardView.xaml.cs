using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
