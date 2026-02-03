using System.Configuration;
using System.Data;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;

namespace CosmoWhisper;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Register the protocol handler (idempotent)
        Managers.ProtocolHandler.Register();

        // 2. Handle protocol URLs
        if (e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                if (arg.StartsWith("cosmowhisper://"))
                {
                    Managers.ProtocolHandler.Handle(arg);
                }
            }
        }

        if (e.Args.Contains("--list-devices"))
        {
            Task.Run(async () => {
                var devices = await global::Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(global::Windows.Devices.Enumeration.DeviceClass.AudioCapture);
                var list = string.Join("\n", devices.Select(d => $"{d.Name} | ID: {d.Id}"));
                System.IO.File.WriteAllText("mic_devices_windows.txt", list);
                System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
            });
            return;
        }
        base.OnStartup(e);
    }
}

