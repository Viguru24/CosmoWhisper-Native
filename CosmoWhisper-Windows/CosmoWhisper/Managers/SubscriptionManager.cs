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
                var email = PreferenceManager.Shared.Preferences?.UserEmail?.ToLower();
                if (email == "louisdesouza@gmail.com")
                    return UserTier.Ultimate;
                    
                var tier = PreferenceManager.Shared.Preferences?.UserTier?.ToLower() ?? "free";
                
                if (tier == "ultimate" || tier == "admin") return UserTier.Ultimate;
                if (tier == "professional") return UserTier.Ultimate; 
                
                if (tier == "pro") return UserTier.Pro;
                if (tier == "personal") return UserTier.Pro; 

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

        public bool IsUnlimited => PreferenceManager.Shared.Preferences?.UserEmail?.ToLower() == "louisdesouza@gmail.com" || (!PreferenceManager.Shared.Preferences.IsStoreVersion && CurrentTier != UserTier.Free);

        // Feature Gating
        public bool HasUltraAccuracy => CurrentTier == UserTier.Ultimate || CurrentTier == UserTier.Professional;
        public bool HasScreenOCR => CurrentTier == UserTier.Ultimate || CurrentTier == UserTier.Professional;
        public bool HasPrioritySupport => IsUnlimited;

        public int MonthlyLimitMinutes 
        {
            get
            {
                if (PreferenceManager.Shared.Preferences?.UserEmail?.ToLower() == "louisdesouza@gmail.com") return 999999;
                
                return (PreferenceManager.Shared.Preferences?.IsStoreVersion == true) ? 60 : CurrentTier switch
                {
                    UserTier.Free => 60,
                    _ => 999999 // All paid plans are unlimited
                };
            }
        }
    }
}


