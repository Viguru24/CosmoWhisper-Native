using System.Configuration;
using System.Data;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using CosmoWhisper.Managers;
using CosmoWhisper.Services;

namespace CosmoWhisper;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static bool IsShuttingDown { get; set; } = false;
    private TrayManager? _trayManager;
    protected override void OnStartup(StartupEventArgs e)
    {

        base.OnStartup(e);

        // 0. Initialize Diagnostic Flight Recorder
        _ = DiagnosticManager.Shared;

        // 1. Register the protocol handler (idempotent)
        Managers.ProtocolHandler.Register();
        Managers.SingleInstanceProvider.StartListening();

        // Start background tasks
        // Initialize services in order
        _ = Task.Run(() =>
        {
            try 
            { 
                // Initialize Prefs first
                _ = PreferenceManager.Shared;
                
                // Then Local Model
                LocalModelManager.Shared.Initialize();
                
                // Then AI Service
                AIService.Shared.Initialize();
                AIService.Shared.WarmUp();
                
                _ = SoundManager.Shared;
                _ = CommandController.Shared;

                // Start Heartbeat immediately if logged in
                AccountService.Shared.StartHeartbeat();
            } 
            catch { }
        });

        // 2. Handle protocol URLs
        if (e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                if (arg.StartsWith("cosmowhisper://", StringComparison.OrdinalIgnoreCase))
                {
                    Managers.ProtocolHandler.Handle(arg);
                }
            }
        }

        if (e.Args.Contains("--list-devices"))
        {
            Task.Run(async () =>
            {
                var devices = await global::Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(global::Windows.Devices.Enumeration.DeviceClass.AudioCapture);
                var list = string.Join("\n", devices.Select(d => $"{d.Name} | ID: {d.Id}"));
                System.IO.File.WriteAllText("mic_devices_windows.txt", list);
                System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
            });
            return;
        }

        // 3. Initialize System Tray Manager
        _trayManager = new TrayManager();
        _trayManager.Initialize();

        // 4. Start Support Ticket Monitoring (Gravity Claw)
        new SupportMonitorManager().Initialize();

        _trayManager.ShowDashboardRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                var widget = Windows.OfType<WidgetWindow>().FirstOrDefault();
                if (widget != null)
                {
                    widget.ToggleDashboard();
                }
                else
                {
                    // Fallback if widget window is closed (shouldn't happen)
                    var dashboard = Windows.OfType<DashboardWindow>().FirstOrDefault();
                    if (dashboard == null)
                    {
                        dashboard = new DashboardWindow();
                        dashboard.Show();
                    }
                    else
                    {
                        dashboard.Activate();
                        dashboard.WindowState = WindowState.Normal;
                    }
                }
            });
        };

        _trayManager.ToggleCapsuleRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                var widget = Windows.OfType<WidgetWindow>().FirstOrDefault();
                if (widget != null)
                {
                    widget.Visibility = widget.Visibility == Visibility.Visible
                        ? Visibility.Hidden
                        : Visibility.Visible;
                }
            });
        };

        _trayManager.ExitRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                IsShuttingDown = true;
                _trayManager?.Dispose();
                Shutdown();
            });
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Managers.SingleInstanceProvider.StopListening();
        _trayManager?.Dispose();
        base.OnExit(e);
    }
}



