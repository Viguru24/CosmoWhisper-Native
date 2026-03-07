using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

namespace CosmoWhisper
{
    public class Program
    {
        private const string MutexName = "CosmoWhisper_Personal_Mutex";
        private const string PipeName = "CosmoWhisper_Personal_Pipe";
        private static readonly string AuthTempFile = Path.Combine(Path.GetTempPath(), "cosmowhisper_personal_auth.tmp");

        [STAThread]
        public static void Main(string[] args)
        {
            // Grab ALL args including those passed via the OS protocol handler
            string[] allArgs = Environment.GetCommandLineArgs();
            string? protocolUrl = allArgs.FirstOrDefault(a =>
                a.StartsWith("cosmowhisper", StringComparison.OrdinalIgnoreCase));

            if (protocolUrl != null)
            {
                // ALWAYS write temp file — handles both first-launch and secondary launch
                try { File.WriteAllText(AuthTempFile, protocolUrl); } catch { }
            }

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

                    // Initialize and run transcription
                    Managers.PreferenceManager.Shared.Load();
                    var mgr = Managers.LocalModelManager.Shared;
                    
                    // Force initialization if not ready
                    if (!mgr.IsReady)
                    {
                        // We use the sync-over-async pattern for the CLI exit
                        Task.Run(async () => await mgr.TranscribeAsync(filePath, "", lang)).Wait();
                    }
                    
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
                // Another instance is already running — forward the URL via pipe as well and exit
                if (protocolUrl != null)
                {
                    // Pipe for instant delivery in running instance
                    try
                    {
                        using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                        client.Connect(1000);
                        using var writer = new System.IO.StreamWriter(client);
                        writer.Write(protocolUrl);
                        writer.Flush();
                    }
                    catch { }
                }
                return;
            }

            // We ARE the first instance — start WPF
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
