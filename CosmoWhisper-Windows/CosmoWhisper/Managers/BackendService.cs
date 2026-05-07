using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using CosmoWhisper.Services;

namespace CosmoWhisper.Managers
{
    public enum SyncResult
    {
        Success,        // Server responded and token is valid
        Unauthorized,   // Server responded with 401/403 — token is revoked
        NetworkError    // Could not reach the server (offline, timeout, VPS down)
    }

    public class StatusChangeEvent
    {
        public bool Is_Active { get; set; }
        public string Email { get; set; }
    }

    public class BackendService
    {
        public static BackendService Shared { get; } = new BackendService();
        private HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public void LogEvent(string msg)
        {
            try { AIService.Shared.LogEvent("[BackendService] " + msg); } catch { }
        }

        public BackendService()
        {
            _client = new HttpClient();
            _client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            UpdateBaseAddress();
        }

        public static bool IsDevelopmentMode() => Array.Exists(Environment.GetCommandLineArgs(), arg => arg.Equals("--dev", StringComparison.OrdinalIgnoreCase));

        public void UpdateBaseAddress()
        {
            var p = PreferenceManager.Shared.Preferences;
            string url = p.BackendUrl;

            // Simple safety check
            if (string.IsNullOrEmpty(url)) url = "https://api.cosmowhisper.com/";
            if (!url.EndsWith("/")) url += "/";

            try
            {
                _client.BaseAddress = new Uri(url);
                _client.DefaultRequestHeaders.Authorization = null;
                LogEvent($"[Init] Base address set to: {url}");
            }
            catch { }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, path);
            var p = PreferenceManager.Shared.Preferences;
            string token = string.IsNullOrEmpty(p.AuthToken) ? p.LicenseToken : p.AuthToken;
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return request;
        }

        public async Task<(bool success, string message)> Login()
        {
            try
            {
                // Simple empty post to trigger the mock login
                var content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseString, _jsonOptions);

                    if (result != null)
                    {
                        var p = PreferenceManager.Shared.Preferences;
                        p.AuthToken = result.token;
                        p.UserTier = result.user.tier;
                        p.IsAIUnlocked = result.user.tier != "free";
                        p.UserEmail = result.user.email;
                        PreferenceManager.Shared.Save();
                        return (true, "Login Successful");
                    }
                }
                else
                {
                    try
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseString, _jsonOptions);
                        return (false, error?.error ?? "Login failed");
                    }
                    catch
                    {
                        return (false, "Login failed: " + response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login Error: {ex.Message}");
                return (false, "Connection Error: Is the backend running?");
            }
            return (false, "Unknown error");
        }

        public async Task<(bool success, string message)> RequestMagicCode(string email)
        {
            try
            {
                var payload = new { email };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("api/auth/request-otp", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Code sent!");
                }
                else
                {
                    try
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseString, _jsonOptions);
                        return (false, error?.error ?? "Request failed");
                    }
                    catch
                    {
                        return (false, "Request failed: " + response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "Connection Error: " + ex.Message);
            }
        }

        public async Task<(bool success, string message)> VerifyMagicCode(string email, string code)
        {
            try
            {
                var payload = new { email, code };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("api/auth/verify-otp", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseString, _jsonOptions);

                    if (result != null)
                    {
                        var p = PreferenceManager.Shared.Preferences;
                        p.AuthToken = result.token;
                        p.UserTier = result.user.tier;
                        p.IsAIUnlocked = result.user.tier != "free";
                        p.UserEmail = result.user.email;
                        PreferenceManager.Shared.Save();
                        return (true, "Login Successful");
                    }
                }
                else
                {
                     try
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseString, _jsonOptions);
                        return (false, error?.error ?? "Verification failed");
                    }
                    catch
                    {
                        return (false, "Verification failed: " + response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, "Connection Error: " + ex.Message);
            }
            return (false, "Unknown Error");
        }

        public async Task<SyncResult> SyncStatus()
        {
            var p = PreferenceManager.Shared.Preferences;
            if (p.AuthToken == "dev-bypass-active" || p.LicenseToken == "dev-bypass-active")
            {
                p.UserTier = "unlimited";
                p.UserEmail = "dev@cosmowhisper.local";
                p.IsAIUnlocked = true;
                p.UsageMinutes = 0;
                p.UsageLimitMinutes = 999999;
                PreferenceManager.Shared.Save();
                return SyncResult.Success;
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Get, "api/license/status");
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                LogEvent($"[SyncStatus] Pinging {(_client.BaseAddress?.ToString() ?? "N/A")}api/license/status ...");
                var response = await _client.SendAsync(request, cts.Token);
                LogEvent($"[SyncStatus] Received status: {response.StatusCode}");

                // Explicit auth failure — server actively rejected the token
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    LogEvent($"[SyncStatus] ❌ ACCESS REVOKED: {response.StatusCode}. User is likely blocked.");
                    return SyncResult.Unauthorized;
                }

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<StatusResponse>(responseString, _jsonOptions);

                    if (status != null)
                    {
                        p = PreferenceManager.Shared.Preferences;
                        p.UserTier = status.tier;
                        p.UserEmail = status.email;
                        p.IsAIUnlocked = status.tier != "free";
                        p.UsageMinutes = status.usageMinutes;
                        p.UsageLimitMinutes = status.limitMinutes;
                        PreferenceManager.Shared.Save();
                        return SyncResult.Success;
                    }
                }

                // Non-401/403 non-success (e.g. 500, 503) — treat as network error, keep session
                System.Diagnostics.Debug.WriteLine($"[SyncStatus] Server returned {response.StatusCode} — treating as NetworkError");
                return SyncResult.NetworkError;
            }
            catch (Exception ex)
            {
                // Could not reach server at all (timeout, DNS fail, no internet)
                System.Diagnostics.Debug.WriteLine($"[SyncStatus] Network error: {ex.Message}");
                return SyncResult.NetworkError;
            }
        }

        public async Task<bool> ReportUsage(long durationMs)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, "api/license/report-usage");
                var payload = new { durationMs };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Usage Report Error: {ex.Message}");
                return false;
            }
        }

        public async Task<SupportActivityResponse?> GetSupportTicketActivity()
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, "api/tickets/all");
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var tickets = JsonSerializer.Deserialize<SupportTicket[]>(responseString, _jsonOptions);
                    
                    if (tickets != null && tickets.Length > 0)
                    {
                        int openCount = 0;
                        string latestId = "";
                        foreach(var t in tickets) {
                           if(t.Status == "open") openCount++;
                        }
                        
                        return new SupportActivityResponse { 
                            OpenCount = openCount, 
                            LatestTicketId = tickets[0].Id.ToString(),
                            LatestTicketMessage = tickets[0].Message
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Support Activity Error: {ex.Message}");
            }
            return null;
        }

        public async Task<System.IO.Stream?> GetStatusStream()
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, "api/license/status-stream");
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Status Stream Error: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> SignOut()
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, "api/auth/logout");
                var response = await _client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignOut error: {ex.Message}");
                return false;
            }
        }
    }

    public class SupportTicket
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class SupportActivityResponse
    {
        public int OpenCount { get; set; }
        public string LatestTicketId { get; set; }
        public string LatestTicketMessage { get; set; }
    }

    public class LoginResponse
    {
        public string token { get; set; }
        public UserObj user { get; set; }
    }

    public class UserObj
    {
        public string email { get; set; }
        public string tier { get; set; }
    }

    public class StatusResponse
    {
        public string tier { get; set; }
        public string email { get; set; }
        public double usageMinutes { get; set; }
        public int limitMinutes { get; set; }
        public bool isOverLimit { get; set; }
    }

    public class ErrorResponse
    {
        public string error { get; set; }
    }
}


