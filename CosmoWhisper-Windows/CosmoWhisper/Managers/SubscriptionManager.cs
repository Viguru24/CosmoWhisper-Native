using System;

namespace CosmoWhisper.Managers
{
    public enum UserTier
    {
        Free,
        Personal,
        Professional
    }

    public class SubscriptionManager
    {
        public static SubscriptionManager Shared { get; } = new SubscriptionManager();

        public UserTier CurrentTier
        {
            get
            {
                var tier = PreferenceManager.Shared.Preferences.UserTier.ToLower();
                if (tier == "personal") return UserTier.Personal;
                if (tier == "professional" || tier == "pro") return UserTier.Professional;
                return UserTier.Free;
            }
        }

        public string TierDisplayName => CurrentTier switch
        {
            UserTier.Personal => "Personal Plan",
            UserTier.Professional => "Professional Plan",
            _ => "Free Tier"
        };

        public string TierIcon => CurrentTier switch
        {
            UserTier.Personal => "👤",
            UserTier.Professional => "👑",
            _ => "⚡"
        };

        public bool IsUnlimited => CurrentTier != UserTier.Free;

        // Feature Gating
        public bool HasUltraAccuracy => CurrentTier == UserTier.Professional;
        public bool HasScreenOCR => CurrentTier == UserTier.Professional;
        public bool HasPrioritySupport => CurrentTier != UserTier.Free;

        public int MonthlyLimitMinutes => CurrentTier switch
        {
            UserTier.Personal => 999999, // Essentially unlimited
            UserTier.Professional => 999999,
            _ => 20
        };
    }
}
