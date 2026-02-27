using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CosmoWhisper.Managers
{
    public class LicenseStatus
    {
        public string tier { get; set; } = "free";
        public string email { get; set; }
        public double usageMinutes { get; set; }
        public int limitMinutes { get; set; }
        public bool isOverLimit { get; set; }
    }

    public class LicenseManager
    {
        public static LicenseManager Shared { get; } = new LicenseManager();
        private readonly HttpClient _httpClient;

        public LicenseManager()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<bool> SyncStatusAsync()
        {
            var prefs = PreferenceManager.Shared.Preferences;
            if (string.IsNullOrEmpty(prefs.LicenseToken)) return false;

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", prefs.LicenseToken);
                string url = $"{prefs.BackendUrl.TrimEnd('/')}/api/license/status";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<LicenseStatus>(json);

                    if (status != null)
                    {
                        prefs.UserTier = prefs.IsStoreVersion ? "free" : status.tier;
                        if (!string.IsNullOrEmpty(status.email)) prefs.UserEmail = status.email;
                        prefs.UsageMinutes = status.usageMinutes;
                        prefs.UsageLimitMinutes = prefs.IsStoreVersion ? 60 : status.limitMinutes;
                        prefs.IsAIUnlocked = !status.isOverLimit;

                        PreferenceManager.Shared.Save();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSE] Sync failed: {ex.Message}");
            }

            return false;
        }

        public async Task ReportUsageAsync(double durationSeconds)
        {
            var prefs = PreferenceManager.Shared.Preferences;
            if (string.IsNullOrEmpty(prefs.LicenseToken))
            {
                // Unauthenticated Local Usage Tracking
                if (prefs.LastLocalResetMonth != DateTime.Now.Month)
                {
                    prefs.UsageMinutes = 0;
                    prefs.LastLocalResetMonth = DateTime.Now.Month;
                }
                
                prefs.UsageMinutes += (durationSeconds / 60.0);
                PreferenceManager.Shared.Save();
                return;
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", prefs.LicenseToken);
                string url = $"{prefs.BackendUrl.TrimEnd('/')}/api/license/report-usage";

                var content = new StringContent(
                    JsonSerializer.Serialize(new { durationMs = durationSeconds * 1000 }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    // Refresh status after reporting
                    await SyncStatusAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LICENSE] Report failed: {ex.Message}");
            }
        }
    }
}


