using System;

namespace CosmoWhisper.Managers
{
    public enum UserTier
    {
        Free,
        Personal, // Legacy
        Pro,      // New Standard
        Ultimate, // New Power User
        Professional // Legacy
    }

    public class SubscriptionManager
    {
        public static SubscriptionManager Shared { get; } = new SubscriptionManager();

        public UserTier CurrentTier
        {
            get
            {
                var tier = PreferenceManager.Shared.Preferences?.UserTier?.ToLower() ?? "free";
                
                if (tier == "ultimate") return UserTier.Ultimate;
                if (tier == "professional") return UserTier.Ultimate; // Legacy mapping
                
                if (tier == "pro") return UserTier.Pro;
                if (tier == "personal") return UserTier.Pro; // Legacy mapping

                return UserTier.Free;
            }
        }

        public string TierDisplayName 
        {
            get
            {
                if (PreferenceManager.Shared.Preferences.IsStoreVersion) return "Store Edition (Lite)";
                return CurrentTier switch
                {
                    UserTier.Ultimate => "Ultimate Plan",
                    UserTier.Professional => "Ultimate Plan",
                    UserTier.Pro => "Pro Plan",
                    UserTier.Personal => "Pro Plan",
                    _ => "Free Tier"
                };
            }
        }

        public string TierIcon => PreferenceManager.Shared.Preferences.IsStoreVersion ? "🛒" : CurrentTier switch
        {
            UserTier.Ultimate => "🚀",
            UserTier.Professional => "🚀",
            UserTier.Pro => "⚡",
            UserTier.Personal => "⚡",
            _ => "🌱"
        };

        public bool IsUnlimited => !PreferenceManager.Shared.Preferences.IsStoreVersion && CurrentTier != UserTier.Free;

        // Feature Gating
        public bool HasUltraAccuracy => CurrentTier == UserTier.Ultimate || CurrentTier == UserTier.Professional;
        public bool HasScreenOCR => CurrentTier == UserTier.Ultimate || CurrentTier == UserTier.Professional;
        public bool HasPrioritySupport => IsUnlimited;

        public int MonthlyLimitMinutes => (PreferenceManager.Shared.Preferences.IsStoreVersion) ? 60 : CurrentTier switch
        {
            UserTier.Free => 60,
            _ => 999999 // All paid plans are unlimited
        };
    }
}
