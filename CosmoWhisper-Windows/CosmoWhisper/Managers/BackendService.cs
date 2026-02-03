using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace CosmoWhisper.Managers
{
    public class BackendService
    {
        public static BackendService Shared { get; } = new BackendService();
        private HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public BackendService()
        {
            _client = new HttpClient();
            UpdateBaseAddress();
        }

        public void UpdateBaseAddress()
        {
             string url = PreferenceManager.Shared.Preferences.BackendUrl;
             if (string.IsNullOrEmpty(url)) url = "http://localhost:5000";
             if (!url.EndsWith("/")) url += "/";
             
             try {
                _client.BaseAddress = new Uri(url);
             } catch {}
        }

        private void SetAuthHeader()
        {
            string token = PreferenceManager.Shared.Preferences.AuthToken;
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _client.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<(bool success, string message)> Login(string email, string password)
        {
            try
            {
                var payload = new { email, password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
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
                        PreferenceManager.Shared.Save();
                        return (true, "Login Successful");
                    }
                }
                else
                {
                    try {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseString, _jsonOptions);
                        return (false, error?.error ?? "Login failed");
                    } catch {
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

        public async Task<bool> SyncStatus()
        {
            try
            {
                SetAuthHeader();
                var response = await _client.GetAsync("api/license/status");
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<StatusResponse>(responseString, _jsonOptions);
                    
                    if (status != null)
                    {
                        var p = PreferenceManager.Shared.Preferences;
                        p.UserTier = status.tier;
                        p.UsageMinutes = status.usageMinutes;
                        p.UsageLimitMinutes = status.limitMinutes;
                        PreferenceManager.Shared.Save();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Sync Error: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> ReportUsage(long durationMs)
        {
            try
            {
                SetAuthHeader();
                var payload = new { durationMs };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _client.PostAsync("api/license/report-usage", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Usage Report Error: {ex.Message}");
                return false;
            }
        }
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
        public double usageMinutes { get; set; }
        public int limitMinutes { get; set; }
        public bool isOverLimit { get; set; }
    }

    public class ErrorResponse
    {
        public string error { get; set; }
    }
}
