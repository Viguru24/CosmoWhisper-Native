using System.Configuration;
using System.Data;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using CosmoWhisper.Managers;
using CosmoWhisper.Services;
using CosmoWhisper.Interfaces;
using System.IO;

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
        // Setup global exception handling immediately
        SetupGlobalExceptionHandling();

        base.OnStartup(e);

        // 1. Register the protocol handler (idempotent)
        Managers.ProtocolHandler.Register();
        Managers.SingleInstanceProvider.StartListening();

        // 2. Handle protocol URLs (before window creation)
        if (e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                if (arg.StartsWith($"{ProtocolHandler.ProtocolName}:", StringComparison.OrdinalIgnoreCase))
                {
                    Managers.ProtocolHandler.Handle(arg);
                }
            }
        }

        if (e.Args.Contains("--list-devices"))
        {
            _ = Task.Run(async () =>
            {
                var devices = await global::Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(global::Windows.Devices.Enumeration.DeviceClass.AudioCapture);
                var list = string.Join("\n", devices.Select(d => $"{d.Name} | ID: {d.Id}"));
                System.IO.File.WriteAllText("mic_devices_windows.txt", list);
                await Dispatcher.InvokeAsync(() => Shutdown());
            });
            return;
        }

        // 3. Initialize System Tray Manager FIRST (lightweight — shows sign of life)
        _trayManager = new TrayManager();
        _trayManager.Initialize();
        WireTrayEvents();

        // 4. Create and show WidgetWindow EXPLICITLY
        // (Removed StartupUri from App.xaml to prevent WPF auto-creating it
        //  before OnStartup completes — which caused initialization race conditions)
        var widgetWindow = ServiceLocator.GetService<WidgetWindow>();
        widgetWindow.Show();
        widgetWindow.UpdateTopmostState();

        // Instantiate DashboardWindow in background to register it
        var dashboardWindow = ServiceLocator.GetService<DashboardWindow>();
        WidgetWindow._dashboard = dashboardWindow;

        // Auto-show Dashboard on First-Run / Unauthenticated boot, OR in Dev Mode
        // This ensures new users are welcomed with full setup, microphone calibration, and hotkey tips
        bool isFirstRunOrUnauth = string.IsNullOrEmpty(PreferenceManager.Shared.Preferences.AuthToken);
        bool isDevMode = e.Args.Contains("--dev") || Environment.GetEnvironmentVariable("COSMO_DEV") == "true";

        if (isFirstRunOrUnauth || isDevMode)
        {
            dashboardWindow.Show();
            dashboardWindow.Activate();
            dashboardWindow.WindowState = WindowState.Normal;
        }

        // 5. Start Support Ticket Monitoring (Dev Mode only — avoid polling admin endpoints on customer machines)
        if (PreferenceManager.Shared.IsDevMode)
        {
            new SupportMonitorManager().Initialize();
        }

        // 6. DEFERRED HEAVY INITIALIZATION — runs AFTER widget is visible
        _ = WarmUpServicesAsync();
    }

    private async Task WarmUpServicesAsync()
    {
        try 
        { 
            // Initialize Diagnostic Flight Recorder (no longer hijacks Trace listeners)
            _ = DiagnosticManager.Shared;
            DiagnosticManager.Shared.Log("Starting background services...");
            
            // Warm up Preferences synchronously in background thread
            _ = PreferenceManager.Shared;

            // Initialize Local Model
            LocalModelManager.Shared.Initialize();

            // Initialize AI Service
            AIService.Shared.Initialize();
            
            // WarmUp internally starts a Task.Run, so we don't need to await it 
            // unless we want to wait for it to finish (which we don't during boot)
            AIService.Shared.WarmUp();

            // Initialize other managers
            _ = SoundManager.Shared;
            _ = CommandController.Shared;

            // Start Heartbeat immediately if logged in
            AccountService.Shared.StartHeartbeat();
            
            DiagnosticManager.Shared.Log("Background services ready.");
            await Task.CompletedTask; // Keep it async to satisfy the return type for now, or change to Task
        }
        catch (Exception ex)
        {
            DiagnosticManager.Shared.Log($"Background service warm-up failed: {ex.Message}", "ERROR");
        }
    }

    private void WireTrayEvents()
    {
        if (_trayManager == null) return;

        _trayManager.ShowDashboardRequested += () =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var widget = Windows.OfType<WidgetWindow>().FirstOrDefault();
                if (widget != null)
                {
                    widget.ToggleDashboard();
                }
                else
                {
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
            }));
        };

        _trayManager.ToggleCapsuleRequested += () =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var widget = Windows.OfType<WidgetWindow>().FirstOrDefault();
                if (widget != null)
                {
                    if (widget.Visibility == Visibility.Visible)
                    {
                        widget.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        widget.Visibility = Visibility.Visible;
                        widget.UpdateTopmostState();
                    }
                }
            }));
        };

        _trayManager.ExitRequested += () =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                IsShuttingDown = true;
                _trayManager?.Dispose();
                Shutdown();
            }));
        };
    }

    private void SetupGlobalExceptionHandling()
    {
        // 1. Dispatcher Unhandled Exception (UI Thread)
        this.DispatcherUnhandledException += (s, e) =>
        {
            LogFatalError(e.Exception, "DispatcherUnhandledException");
            e.Handled = true; // Attempt to stay alive, though it might be unstable
        };

        // 2. AppDomain Unhandled Exception (Background Threads)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogFatalError(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
        };

        // 3. Task Unobserved Exception
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogFatalError(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private void LogFatalError(Exception? ex, string source)
    {
        try
        {
            string logPath = Path.Combine(FileLogger.ResolveDefaultAppDataFolder(), "logs", "fatal_crash.log");
            string errorInfo = $"\n--- FATAL ERROR [{DateTime.Now}] ---\nSource: {source}\nMessage: {ex?.Message}\nStack: {ex?.StackTrace}\n";
            if (ex?.InnerException != null)
            {
                errorInfo += $"Inner Exception: {ex.InnerException.Message}\nInner Stack: {ex.InnerException.StackTrace}\n";
            }
            errorInfo += "----------------------------------------\n";

            FileLogger.WriteBoundedLine(logPath, errorInfo);
            FileLogger.WriteShared("FATAL", $"{source}: {ex?.Message}");
        }
        catch { /* Last resort - nowhere to log */ }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AudioRecorder.Shared.CleanupResources();
        SupportMonitorManager.Shared?.Dispose();
        AccountService.Shared.StopHeartbeat();
        Managers.SingleInstanceProvider.StopListening();
        _trayManager?.Dispose();
        base.OnExit(e);
    }
}



