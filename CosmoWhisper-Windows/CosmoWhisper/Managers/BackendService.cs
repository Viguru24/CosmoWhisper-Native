using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using CosmoWhisper.Services;
using CosmoWhisper.Interfaces;


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
        public string Tier { get; set; }
    }

    public class BackendService : IBackendService

    {
        public static BackendService Shared => (BackendService)ServiceLocator.GetService<IBackendService>();
        private readonly IPreferenceManager _preferenceManager;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private volatile Uri _baseUri = new Uri("https://api.cosmowhisper.com/");

        public void LogEvent(string msg)
        {
            try { AIService.Shared.LogEvent("[BackendService] " + msg); } catch { }
        }

        public BackendService(IPreferenceManager preferenceManager)
        {
            _preferenceManager = preferenceManager;
            UpdateBaseAddress();
        }

        public static bool IsDevelopmentMode() => Array.Exists(Environment.GetCommandLineArgs(), arg => arg.Equals("--dev", StringComparison.OrdinalIgnoreCase));

        public void UpdateBaseAddress()
        {
            var p = _preferenceManager.Preferences;
            string url = p.BackendUrl;

            // Simple safety check
            if (string.IsNullOrEmpty(url)) url = "https://api.cosmowhisper.com/";
            if (!url.EndsWith("/")) url += "/";

            try
            {
                _baseUri = new Uri(url);
                LogEvent($"[Init] Base address set to: {url}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BackendService] Error updating base address to {url}: {ex.Message}");
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var requestUri = new Uri(_baseUri, path.TrimStart('/'));
            var request = new HttpRequestMessage(method, requestUri);
            var p = _preferenceManager.Preferences;
            string token = string.IsNullOrEmpty(p.AuthToken) ? p.LicenseToken : p.AuthToken;

            // Basic validation to prevent malformed or empty headers
            if (!string.IsNullOrWhiteSpace(token) && token.Length > 10) 
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
            }
            return request;
        }

        private HttpClient GetClient() => SharedHttpClient.Instance;

        public async Task<(bool success, string message)> Login()
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, "api/auth/login");
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                using var response = await GetClient().SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseString, _jsonOptions);

                    if (result != null)
                    {
                        var p = _preferenceManager.Preferences;
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
                using var request = CreateRequest(HttpMethod.Post, "api/auth/request-otp");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await GetClient().SendAsync(request);

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
                using var request = CreateRequest(HttpMethod.Post, "api/auth/verify-otp");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await GetClient().SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseString, _jsonOptions);

                    if (result != null)
                    {
                        var p = _preferenceManager.Preferences;
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
            var p = _preferenceManager.Preferences;
            // FORCE BYPASS: Only allow explicit dev bypass token or unauthenticated local dev when IsDevMode is active
            bool isExplicitBypass = PreferenceManager.Shared.IsDevMode && (p.AuthToken == "dev-bypass-active" || p.LicenseToken == "dev-bypass-active");
            bool isDevModeUnauthenticated = PreferenceManager.Shared.IsDevMode && string.IsNullOrEmpty(p.AuthToken);

            if (isExplicitBypass || isDevModeUnauthenticated)
            {
                // Only overwrite if we aren't already logged in with a real account (or if explicit)
                if (isExplicitBypass || string.IsNullOrEmpty(p.UserEmail) || p.UserEmail == "dev@cosmowhisper.local")
                {
                    p.UserTier = "unlimited";
                    p.UserEmail = "dev@cosmowhisper.local";
                    p.IsAIUnlocked = true;
                    p.UsageMinutes = 0;
                    p.UsageLimitMinutes = 999999;
                    PreferenceManager.Shared.Save();
                }
                return SyncResult.Success;
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Get, "api/license/status");
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                LogEvent($"[SyncStatus] Pinging {_baseUri}api/license/status ...");
                using var response = await GetClient().SendAsync(request, cts.Token);
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

                using var response = await GetClient().SendAsync(request);
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
                using var response = await GetClient().SendAsync(request);
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

        public async Task<System.IO.Stream?> GetStatusStream(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = CreateRequest(HttpMethod.Get, "api/license/status-stream");
                response = await SharedHttpClient.StreamInstance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    return new HttpResponseStream(response, stream);
                }
                response.Dispose();
            }
            catch (OperationCanceledException)
            {
                response?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                response?.Dispose();
                System.Diagnostics.Debug.WriteLine($"Status Stream Error: {ex.Message}");
            }
            return null;
        }

        private sealed class HttpResponseStream : System.IO.Stream
        {
            private readonly HttpResponseMessage _response;
            private readonly System.IO.Stream _inner;

            public HttpResponseStream(HttpResponseMessage response, System.IO.Stream inner)
            {
                _response = response;
                _inner = inner;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
            public override long Seek(long offset, System.IO.SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                    _response.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        public async Task<bool> SignOut()
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, "api/auth/logout");
                using var response = await GetClient().SendAsync(request);
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


