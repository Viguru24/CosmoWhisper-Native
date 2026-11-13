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

        private readonly System.Threading.SemaphoreSlim _heartbeatLock = new System.Threading.SemaphoreSlim(1, 1);
        private System.Threading.CancellationTokenSource? _heartbeatCts;

        public event Action? StatsUpdated;
        public event Action? AccessBlocked;
        public event Action? SessionExpired;

        public bool IsHeartbeatRunning => _isHeartbeatRunning;

        public async void StartHeartbeat()
        {
            var p = PreferenceManager.Shared.Preferences;
            bool isDevBypass = PreferenceManager.Shared.IsDevMode && string.IsNullOrEmpty(p.AuthToken);
            if (string.IsNullOrEmpty(p.LicenseToken) && string.IsNullOrEmpty(p.AuthToken) && !isDevBypass) return;

            if (!await _heartbeatLock.WaitAsync(0)) return;

            try
            {
                if (_isHeartbeatRunning) return;
                
                _isHeartbeatRunning = true;
                _heartbeatCts?.Dispose();
                _heartbeatCts = new System.Threading.CancellationTokenSource();

                StartStatusListener(_heartbeatCts.Token); // Launch instant push listener

                BackendService.Shared.LogEvent("[Account] Running startup sync status check...");
                _ = Task.Run(async () =>
                {
                    await TriggerSync();
                });
            }
            finally
            {
                _heartbeatLock.Release();
            }
        }

        public async Task TriggerSync()
        {
            try
            {
                var prefs = PreferenceManager.Shared.Preferences;
                bool isDevLoopBypass = PreferenceManager.Shared.IsDevMode && string.IsNullOrEmpty(prefs.AuthToken);
                if (string.IsNullOrEmpty(prefs.LicenseToken) && string.IsNullOrEmpty(prefs.AuthToken) && !isDevLoopBypass)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(prefs.LicenseToken) || !string.IsNullOrEmpty(prefs.AuthToken))
                {
                    var result = await BackendService.Shared.SyncStatus();
                    if (result == SyncResult.Unauthorized)
                    {
                        BackendService.Shared.LogEvent("[Account] SyncStatus: Unauthorized. Blocking access.");
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
                    else if (result == SyncResult.NetworkError)
                    {
                        BackendService.Shared.LogEvent("[Account] SyncStatus: Network Error.");
                    }
                }
                
                StatsUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                DiagnosticManager.Shared.Log($"SyncStatus exception: {ex.Message}", "ERROR");
            }
        }

        private void StartStatusListener(System.Threading.CancellationToken token)
        {
            if (_isStatusListenerRunning) return;
            _isStatusListenerRunning = true;

            Task.Run(async () =>
            {
                BackendService.Shared.LogEvent("[SSE] Status listener (SSE) started.");
                while (!token.IsCancellationRequested && _isStatusListenerRunning && _isHeartbeatRunning)
                {
                    try
                    {
                        using (var stream = await BackendService.Shared.GetStatusStream(token))
                        {
                            if (stream == null)
                            {
                                await Task.Delay(5000, token); // Wait before retry
                                continue;
                            }

                            using (var reader = new System.IO.StreamReader(stream))
                            {
                                while (!token.IsCancellationRequested && !reader.EndOfStream && _isStatusListenerRunning)
                                {
                                    var line = await reader.ReadLineAsync(token);
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
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        DiagnosticManager.Shared.Log($"Status listener exception: {ex.Message}", "ERROR");
                        try { await Task.Delay(5000, token); }
                        catch (OperationCanceledException) { break; }
                    }
                }
                _isStatusListenerRunning = false;
                BackendService.Shared.LogEvent("[SSE] Status listener (SSE) stopped.");
            });
        }

        public void StopHeartbeat()
        {
            _heartbeatLock.Wait();
            try
            {
                _isHeartbeatRunning = false;
                _isStatusListenerRunning = false;
                _heartbeatCts?.Cancel();
            }
            finally
            {
                _heartbeatLock.Release();
            }
        }

        public async Task<SyncResult> CheckAuthStatusAsync()
        {
            var p = PreferenceManager.Shared.Preferences;
            bool isDevBypass = PreferenceManager.Shared.IsDevMode && string.IsNullOrEmpty(p.AuthToken);

            if (string.IsNullOrEmpty(p.AuthToken) && !isDevBypass) 
                return SyncResult.Unauthorized;

            var result = await BackendService.Shared.SyncStatus();
            
            if (result == SyncResult.Unauthorized)
            {
                AppState.Shared.IsAccessBlocked = true;
                SessionExpired?.Invoke();
            }
            else if (result == SyncResult.Success)
            {
                AppState.Shared.IsAccessBlocked = false;
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

#if DEV_BUILD
            if (key == "COSMO-PRO-TEST")
            {
                p.UserTier = "pro";
                PreferenceManager.Shared.Save();
                StatsUpdated?.Invoke();
                return (true, oldToken);
            }
#endif

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
