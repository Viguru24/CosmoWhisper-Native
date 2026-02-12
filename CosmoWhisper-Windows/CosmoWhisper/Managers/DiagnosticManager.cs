using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace CosmoWhisper.Managers
{
    public class DiagnosticManager
    {
        public static DiagnosticManager Shared { get; } = new DiagnosticManager();

        private readonly string _logPath;
        private readonly string _snapshotPath;
        private readonly object _lock = new object();

        public DiagnosticManager()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "diagnostics");
            Directory.CreateDirectory(appData);

            _logPath = Path.Combine(appData, "live_flight_recorder.log");
            _snapshotPath = Path.Combine(appData, "last_error_snapshot.json");

            // Direct project root link for the AI to "tail"
            // Using a relative path to the repo root if possible, or just a known location
            try 
            {
                // Set up a TraceListener to catch all Debug/Trace.WriteLine calls
                Trace.Listeners.Clear();
                Trace.Listeners.Add(new TextWriterTraceListener(File.Create(_logPath)));
                Trace.AutoFlush = true;
                
                Log("Diagnostic Flight Recorder Initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to init diagnostics: {ex.Message}");
            }
        }

        public void Log(string message, string category = "INFO")
        {
            lock (_lock)
            {
                string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}";
                Debug.WriteLine(entry);
                // Also write to a "volatile" log in the project root for the AI to see easily
                try 
                {
                    File.AppendAllText("cosmo_terminal.log", entry + Environment.NewLine);
                }
                catch { }
            }
        }

        public void TakeSnapshot(string error, Dictionary<string, object> state)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\",");
                sb.AppendLine($"  \"error\": \"{error.Replace("\"", "\\\"")}\",");
                sb.AppendLine("  \"state\": {");
                
                int count = 0;
                foreach (var kvp in state)
                {
                    string val = kvp.Value?.ToString()?.Replace("\"", "\\\"")?.Replace("\n", "\\n")?.Replace("\r", "\\r") ?? "null";
                    sb.Append($"    \"{kvp.Key}\": \"{val}\"");
                    if (++count < state.Count) sb.AppendLine(",");
                    else sb.AppendLine("");
                }
                
                sb.AppendLine("  }");
                sb.AppendLine("}");

                File.WriteAllText(_snapshotPath, sb.ToString());
                // Also copy to root for easy access
                File.WriteAllText("last_diagnostic_snapshot.json", sb.ToString());
                
                Log($"SNAPSHOT TAKEN: Check last_diagnostic_snapshot.json", "CRITICAL");
            }
            catch (Exception ex)
            {
                Log($"Failed to take snapshot: {ex.Message}", "ERROR");
            }
        }
    }
}
