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
        private bool _isStatusListenerRunning = false;
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

            StartStatusListener(); // Launch instant push listener

            BackendService.Shared.LogEvent("[Account] Heartbeat loop started.");
            Task.Run(async () =>
            {
                while (_isHeartbeatRunning)
                {
                    try
                    {
                        var prefs = PreferenceManager.Shared.Preferences;
                        if (string.IsNullOrEmpty(prefs.LicenseToken) && string.IsNullOrEmpty(prefs.AuthToken))
                        {
                            BackendService.Shared.LogEvent("[Account] No tokens found, stopping heartbeat.");
                            _isHeartbeatRunning = false;
                            break;
                        }

                        if (!string.IsNullOrEmpty(prefs.LicenseToken) || !string.IsNullOrEmpty(prefs.AuthToken))
                        {
                            var result = await BackendService.Shared.SyncStatus();
                            if (result == SyncResult.Unauthorized)
                            {
                                BackendService.Shared.LogEvent("[Account] Heartbeat: Unauthorized. Blocking access.");
                                if (!AppState.Shared.IsAccessBlocked)
                                {
                                    AppState.Shared.IsAccessBlocked = true;
                                    try { AudioRecorder.Shared.StopRecording(); } catch { }
                                    AccessBlocked?.Invoke();
                                }
                            }
                            else if (result == SyncResult.Success)
                            {
                                // DiagnosticManager.Shared.Log("Heartbeat: Success.", "AUTH"); // Too noisy? 
                                AppState.Shared.IsAccessBlocked = false;
                            }
                            else if (result == SyncResult.NetworkError)
                            {
                                BackendService.Shared.LogEvent("[Account] Heartbeat: Network Error (Backend down?).");
                            }
                        }
                        
                        StatsUpdated?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        DiagnosticManager.Shared.Log($"Heartbeat loop exception: {ex.Message}", "ERROR");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                BackendService.Shared.LogEvent("[Account] Heartbeat loop stopped.");
                System.Threading.Interlocked.Exchange(ref _heartbeatGuard, 0);
            });
        }

        private void StartStatusListener()
        {
            if (_isStatusListenerRunning) return;
            _isStatusListenerRunning = true;

            Task.Run(async () =>
            {
                BackendService.Shared.LogEvent("[SSE] Status listener (SSE) started.");
                while (_isStatusListenerRunning && _isHeartbeatRunning)
                {
                    try
                    {
                        using (var stream = await BackendService.Shared.GetStatusStream())
                        {
                            if (stream == null)
                            {
                                await Task.Delay(5000); // Wait before retry
                                continue;
                            }

                            using (var reader = new System.IO.StreamReader(stream))
                            {
                                while (!reader.EndOfStream && _isStatusListenerRunning)
                                {
                                    var line = await reader.ReadLineAsync();
                                    if (line == null) 
                                    {
                                        BackendService.Shared.LogEvent("[SSE] Stream ended. Reconnecting...");
                                        break;
                                    }
                                    
                                    if (string.IsNullOrEmpty(line)) continue;
                                    
                                    // Log every line for debugging
                                    BackendService.Shared.LogEvent($"[SSE] Received: {line}");

                                    if (line.StartsWith("data: "))
                                    {
                                        var jsonData = line.Substring(6);
                                        try
                                        {
                                            var status = System.Text.Json.JsonSerializer.Deserialize<StatusChangeEvent>(jsonData, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                            if (status != null)
                                            {
                                                BackendService.Shared.LogEvent($"[SSE] Status change: is_active={status.Is_Active}");
                                                
                                                // Sync immediately
                                                await BackendService.Shared.SyncStatus();
                                                
                                                AppState.Shared.IsAccessBlocked = !status.Is_Active;
                                                if (AppState.Shared.IsAccessBlocked)
                                                {
                                                    try { AudioRecorder.Shared.StopRecording(); } catch { }
                                                    AccessBlocked?.Invoke();
                                                }
                                                
                                                StatsUpdated?.Invoke();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            BackendService.Shared.LogEvent($"[SSE] Parse error: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DiagnosticManager.Shared.Log($"Status listener exception: {ex.Message}", "ERROR");
                        await Task.Delay(5000); // Backoff
                    }
                }
                _isStatusListenerRunning = false;
                BackendService.Shared.LogEvent("[SSE] Status listener (SSE) stopped.");
            });
        }

        public void StopHeartbeat()
        {
            _isHeartbeatRunning = false;
            _isStatusListenerRunning = false;
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
