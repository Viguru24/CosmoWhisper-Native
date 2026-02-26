using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace CosmoWhisper.Managers
{
    /// <summary>
    /// Monitors the support ticket queue and provides Windows notifications (Gravity Claw).
    /// </summary>
    public class SupportMonitorManager : IDisposable
    {
        public static SupportMonitorManager? Shared { get; private set; }

        private CancellationTokenSource? _monitorCts;
        private int _lastOpenCount = -1;
        private string? _lastTicketId;

        public void Initialize()
        {
            if (Shared != null) return;
            Shared = this;
            
            StartMonitoring();
        }

        public void StartMonitoring()
        {
            _monitorCts?.Cancel();
            _monitorCts = new CancellationTokenSource();
            
            _ = MonitorLoop(_monitorCts.Token);
            Debug.WriteLine("Gravity Claw: Support Monitoring Started.");
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var activity = await BackendService.Shared.GetSupportTicketActivity();
                    if (activity != null)
                    {
                        // New Ticket Detection
                        if (_lastTicketId != null && activity.LatestTicketId != _lastTicketId)
                        {
                            // Gravity Claw Notification
                            TrayManager.Shared?.ShowBalloon(
                                "🆕 New Support Ticket", 
                                $"Message: {activity.LatestTicketMessage.Substring(0, Math.Min(60, activity.LatestTicketMessage.Length))}...", 
                                ToolTipIcon.Info
                            );
                        }
                        
                        _lastOpenCount = activity.OpenCount;
                        _lastTicketId = activity.LatestTicketId;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Support Monitor Loop Error: {ex.Message}");
                }

                // Poll every 60 seconds (conservative to not spam Netlify)
                await Task.Delay(60000, token);
            }
        }

        public void Dispose()
        {
            _monitorCts?.Cancel();
            _monitorCts?.Dispose();
            Shared = null;
        }
    }
}
