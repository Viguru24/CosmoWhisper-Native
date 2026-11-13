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
                        if (_lastTicketId != null && !string.IsNullOrEmpty(activity.LatestTicketId) && activity.LatestTicketId != _lastTicketId)
                        {
                            string message = activity.LatestTicketMessage ?? string.Empty;
                            string preview = message.Substring(0, Math.Min(60, message.Length));
                            TrayManager.Shared?.ShowBalloon(
                                "🆕 New Support Ticket", 
                                $"Message: {preview}...", 
                                ToolTipIcon.Info
                            );
                        }
                        
                        _lastOpenCount = activity.OpenCount;
                        _lastTicketId = activity.LatestTicketId;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Support Monitor Loop Error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(60000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
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
