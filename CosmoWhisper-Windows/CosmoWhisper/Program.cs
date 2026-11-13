using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Services;
using CosmoWhisper.Managers;
using CosmoWhisper.ViewModels;


namespace CosmoWhisper
{
    public class Program
    {
        private static string MutexName => Environment.GetEnvironmentVariable("COSMO_DEV") == "true" ? "CosmoWhisper_Dev_Mutex" : "CosmoWhisper_Personal_Mutex";
        private static string PipeName => Environment.GetEnvironmentVariable("COSMO_DEV") == "true" ? "CosmoWhisper_Dev_Pipe" : "CosmoWhisper_Personal_Pipe";
        private static string AuthTempFile => Path.Combine(Path.GetTempPath(), Environment.GetEnvironmentVariable("COSMO_DEV") == "true" ? "cosmowhisper_dev_auth.tmp" : "cosmowhisper_personal_auth.tmp");

        [STAThread]
        public static void Main(string[] args)
        {
            // Grab ALL args including those passed via the OS protocol handler
            string[] allArgs = Environment.GetCommandLineArgs();
            
            // Check for dev mode FIRST so PreferenceManager knows
            if (allArgs.Contains("--dev") || allArgs.Any(a => a.StartsWith("cosmowhisper-dev", StringComparison.OrdinalIgnoreCase))) {
                Environment.SetEnvironmentVariable("COSMO_DEV", "true");
            }

            string? protocolUrl = allArgs.FirstOrDefault(a =>
                a.StartsWith("cosmowhisper", StringComparison.OrdinalIgnoreCase));

            // --- CLI TRANSCRIPTION BRIDGE ---
            if (args.Contains("--transcribe"))
            {
                int fileIdx = Array.IndexOf(args, "--transcribe") + 1;
                if (fileIdx < args.Length)
                {
                    string filePath = args[fileIdx];
                    string lang = "en";
                    int langIdx = Array.IndexOf(args, "--lang") + 1;
                    if (langIdx > 0 && langIdx < args.Length) lang = args[langIdx];

                    ServiceLocator.Provider = BuildServiceProvider();
                    Managers.PreferenceManager.Shared.Load();
                    var mgr = Managers.LocalModelManager.Shared;
                    mgr.EnsureInitializedAsync().GetAwaiter().GetResult();

                    var result = mgr.TranscribeAsync(filePath, "", lang).GetAwaiter().GetResult();
                    Console.WriteLine("TRANSCRIPTION_START");
                    Console.WriteLine(result);
                    Console.WriteLine("TRANSCRIPTION_END");
                    return;
                }
            }

            using var mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance is already running — forward the URL via pipe (fallback to temp file only if pipe fails)
                if (protocolUrl != null)
                {
                    bool sentViaPipe = false;
                    try
                    {
                        using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                        client.Connect(2000);
                        using var writer = new System.IO.StreamWriter(client);
                        writer.Write(protocolUrl);
                        writer.Flush();
                        sentViaPipe = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Program] Error sending protocol URL to pipe: {ex.Message}");
                    }

                    if (!sentViaPipe)
                    {
                        try { File.WriteAllText(AuthTempFile, protocolUrl); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Program] Error writing auth temp file: {ex.Message}"); }
                    }
                }
                return;
            }

            // Configure DI before starting WPF
            ServiceLocator.Provider = BuildServiceProvider();

            // We ARE the first instance — start WPF
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger, FileLogger>();
            serviceCollection.AddSingleton<IColorProvider, ColorProvider>();
            serviceCollection.AddSingleton<Services.AnimationService>();

            // Core Managers (Register by type so DI handles instantiation order)
            serviceCollection.AddSingleton<IPreferenceManager, PreferenceManager>();
            serviceCollection.AddSingleton<IDiagnosticManager, DiagnosticManager>();
            serviceCollection.AddSingleton<ILocalModelManager, LocalModelManager>();
            serviceCollection.AddSingleton<IBackendService, BackendService>();
            serviceCollection.AddSingleton<IInputController, InputController>();
            serviceCollection.AddSingleton<ICommandController, CommandController>();
            serviceCollection.AddSingleton<IAIService, AIService>();
            serviceCollection.AddSingleton<ISoundManager, SoundManager>();
            serviceCollection.AddSingleton<IVocabularyManager, VocabularyManager>();
            serviceCollection.AddSingleton<IAudioRecorder, AudioRecorder>();

            // ViewModels
            serviceCollection.AddSingleton<WidgetViewModel>();
            serviceCollection.AddSingleton<DashboardViewModel>();

            // Windows
            serviceCollection.AddSingleton<WidgetWindow>();
            serviceCollection.AddSingleton<DashboardWindow>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
