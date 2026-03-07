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
    public class BackendService
    {
        public static BackendService Shared { get; } = new BackendService();
        private HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private void LogEvent(string msg)
        {
            try { AIService.Shared.LogEvent("[BackendService] " + msg); } catch { }
        }

        public BackendService()
        {
            _client = new HttpClient();
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

        private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? payload = null)
        {
            var request = new HttpRequestMessage(method, path);
            var p = PreferenceManager.Shared.Preferences;
            string token = string.IsNullOrEmpty(p.AuthToken) ? p.LicenseToken : p.AuthToken;
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (payload != null)
            {
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private async Task<ApiResponse<T>> SendRequestAsync<T>(HttpMethod method, string path, object? payload = null, int timeoutSeconds = 15)
        {
            try
            {
                using var request = CreateRequest(method, path, payload);
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                var response = await _client.SendAsync(request, cts.Token);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    LogEvent($"[API] Access Denied (401/403) at {path}");
                    return ApiResponse<T>.Failed("Unauthorized", true);
                }

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<T>(responseString, _jsonOptions);
                    return ApiResponse<T>.Successful(data);
                }
                else
                {
                    string errorMsg = "Request Failed";
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(responseString, _jsonOptions);
                        errorMsg = errorObj?.error ?? response.ReasonPhrase;
                    }
                    catch { errorMsg = response.ReasonPhrase; }

                    return ApiResponse<T>.Failed(errorMsg);
                }
            }
            catch (Exception ex)
            {
                LogEvent($"[API] Network Error at {path}: {ex.Message}");
                return ApiResponse<T>.Failed("Network connection failed. Is the backend running?", false, true);
            }
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string Message { get; set; } = "";
            public bool IsUnauthorized { get; set; }
            public bool IsNetworkError { get; set; }

            public static ApiResponse<T> Successful(T? data) => new ApiResponse<T> { Success = true, Data = data };
            public static ApiResponse<T> Failed(string msg, bool unauthorized = false, bool network = false) 
                => new ApiResponse<T> { Success = false, Message = msg, IsUnauthorized = unauthorized, IsNetworkError = network };
        }



        public async Task<(bool success, string message)> RequestMagicCode(string email)
        {
            var res = await SendRequestAsync<object>(HttpMethod.Post, "api/auth/request-otp", new { email });
            return (res.Success, res.Success ? "Code sent!" : res.Message);
        }

        public async Task<(bool success, string message)> VerifyMagicCode(string email, string code)
        {
            var res = await SendRequestAsync<LoginResponse>(HttpMethod.Post, "api/auth/verify-otp", new { email, code });
            if (res.Success && res.Data != null)
            {
                var p = PreferenceManager.Shared.Preferences;
                p.AuthToken = res.Data.token;
                p.UserTier = res.Data.user.tier;
                p.IsAIUnlocked = res.Data.user.tier != "free";
                p.UserEmail = res.Data.user.email;
                PreferenceManager.Shared.Save();
                return (true, "Login Successful");
            }
            return (false, res.Message);
        }

        public async Task<SyncResult> SyncStatus()
        {
            var p = PreferenceManager.Shared.Preferences;
            var res = await SendRequestAsync<StatusResponse>(HttpMethod.Get, "api/license/status", timeoutSeconds: 10);
            
            if (res.IsUnauthorized) return SyncResult.Unauthorized;
            if (res.IsNetworkError) return SyncResult.NetworkError;

            if (res.Success && res.Data != null)
            {
                p = PreferenceManager.Shared.Preferences;
                p.UserTier = res.Data.tier;
                p.UserEmail = res.Data.email;
                p.UsageMinutes = res.Data.usageMinutes;
                p.UsageLimitMinutes = res.Data.limitMinutes;
                
                // Fallback for store/lite version
                if (p.UsageLimitMinutes <= 0) p.UsageLimitMinutes = 60;

                // Respect the server's over-limit flag or tier status
                p.IsAIUnlocked = !res.Data.isOverLimit && (res.Data.tier != "free");
                
                PreferenceManager.Shared.Save();
                return SyncResult.Success;
            }

            return SyncResult.NetworkError;
        }

        public async Task<bool> ReportUsage(long durationMs)
        {
            var p = PreferenceManager.Shared.Preferences;
            string token = string.IsNullOrEmpty(p.AuthToken) ? p.LicenseToken : p.AuthToken;

            if (string.IsNullOrEmpty(token))
            {
                // Local usage tracking for unauthenticated users
                if (p.LastLocalResetMonth != DateTime.Now.Month)
                {
                    p.UsageMinutes = 0;
                    p.LastLocalResetMonth = DateTime.Now.Month;
                }
                p.UsageMinutes += (durationMs / 60000.0);
                PreferenceManager.Shared.Save();
                return true;
            }

            var res = await SendRequestAsync<object>(HttpMethod.Post, "api/license/report-usage", new { durationMs });
            if (res.Success)
            {
                // Refresh status after reporting to get updated limits
                await SyncStatus();
            }
            return res.Success;
        }

        public async Task<SupportActivityResponse?> GetSupportTicketActivity()
        {
            var res = await SendRequestAsync<SupportTicket[]>(HttpMethod.Get, "api/tickets/all");
            
            if (res.Success && res.Data != null && res.Data.Length > 0)
            {
                int openCount = 0;
                foreach(var t in res.Data) {
                    if(t.Status == "open") openCount++;
                }
                
                return new SupportActivityResponse { 
                    OpenCount = openCount, 
                    LatestTicketId = res.Data[0].Id.ToString(),
                    LatestTicketMessage = res.Data[0].Message
                };
            }
            return null;
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


