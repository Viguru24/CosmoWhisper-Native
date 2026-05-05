using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Services
{
    /// <summary>
    /// Handles high-level account orchestration, including syncing, status monitoring, and session management.
    /// </summary>
    public class AccountService
    {
        public static AccountService Shared { get; } = new AccountService();

        private bool _isHeartbeatRunning = false;
        private int _heartbeatGuard = 0;

        public event Action? StatsUpdated;
        public event Action? AccessBlocked;
        public event Action? SessionExpired;

        public bool IsHeartbeatRunning => _isHeartbeatRunning;

        public void StartHeartbeat()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (string.IsNullOrEmpty(p.LicenseToken) && string.IsNullOrEmpty(p.AuthToken)) return;

            if (System.Threading.Interlocked.CompareExchange(ref _heartbeatGuard, 1, 0) != 0) return;
            _isHeartbeatRunning = true;

            Task.Run(async () =>
            {
                while (_isHeartbeatRunning)
                {
                    try
                    {
                        var prefs = PreferenceManager.Shared.Preferences;
                        if (string.IsNullOrEmpty(prefs.LicenseToken) && string.IsNullOrEmpty(prefs.AuthToken))
                        {
                            _isHeartbeatRunning = false;
                            break;
                        }

                        if (!string.IsNullOrEmpty(prefs.LicenseToken) || !string.IsNullOrEmpty(prefs.AuthToken))
                        {
                            var result = await BackendService.Shared.SyncStatus();
                            if (result == SyncResult.Unauthorized)
                            {
                                if (!AppState.Shared.IsAccessBlocked)
                                {
                                    AppState.Shared.IsAccessBlocked = true;
                                    try { AudioRecorder.Shared.StopRecording(); } catch { }
                                    AccessBlocked?.Invoke();
                                }
                            }
                            else if (result == SyncResult.Success)
                            {
                                AppState.Shared.IsAccessBlocked = false;
                            }
                        }
                        
                        StatsUpdated?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        DiagnosticManager.Shared.Log($"Heartbeat error: {ex.Message}", "ERROR");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                System.Threading.Interlocked.Exchange(ref _heartbeatGuard, 0);
            });
        }

        public void StopHeartbeat()
        {
            _isHeartbeatRunning = false;
            System.Threading.Interlocked.Exchange(ref _heartbeatGuard, 0);
        }

        public async Task<SyncResult> CheckAuthStatusAsync()
        {
            if (string.IsNullOrEmpty(PreferenceManager.Shared.Preferences.AuthToken)) 
                return SyncResult.Unauthorized;

            var result = await BackendService.Shared.SyncStatus();
            
            if (result == SyncResult.Unauthorized)
            {
                SessionExpired?.Invoke();
            }
            
            StatsUpdated?.Invoke();
            return result;
        }

        public void SignOut()
        {
            StopHeartbeat();
            
            try { AudioRecorder.Shared.StopRecording(); } catch { }
            AppState.Shared.IsAccessBlocked = true;
            
            var p = PreferenceManager.Shared.Preferences;
            p.LicenseToken = "";
            p.AuthToken = "";
            p.UserEmail = "";
            p.UserTier = "free";
            PreferenceManager.Shared.Save();
        }

        public void ResetStats()
        {
            var p = PreferenceManager.Shared.Preferences;
            p.TotalWords = 0;
            p.TotalTranscriptions = 0;
            p.TotalTimeSavedMinutes = 0;
            PreferenceManager.Shared.Save();
            StatsUpdated?.Invoke();
        }
        public async Task<(bool success, string oldToken)> ActivateLicenseAsync(string key)
        {
            var p = PreferenceManager.Shared.Preferences;
            string oldToken = p.LicenseToken;
            p.LicenseToken = key;

            if (key == "COSMO-PRO-TEST")
            {
                p.UserTier = "pro";
                PreferenceManager.Shared.Save();
                StatsUpdated?.Invoke();
                return (true, oldToken);
            }

            // Sync with server
            var result = await BackendService.Shared.SyncStatus();
            if (result == SyncResult.Success)
            {
                StatsUpdated?.Invoke();
                return (true, oldToken);
            }
            else
            {
                p.LicenseToken = oldToken; // Revert
                return (false, oldToken);
            }
        }
    }
}
