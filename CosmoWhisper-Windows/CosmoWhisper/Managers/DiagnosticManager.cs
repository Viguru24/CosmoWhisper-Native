using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using CosmoWhisper.Interfaces;

namespace CosmoWhisper.Managers
{
    public interface IDiagnosticManager
    {
        void Log(string message, string category = "INFO");
        void TakeSnapshot(string error, Dictionary<string, object> state);
    }

    public class DiagnosticManager : IDiagnosticManager
    {
        public static DiagnosticManager Shared => (DiagnosticManager)ServiceLocator.GetService<IDiagnosticManager>();

        private string _logPath;
        private string _snapshotPath;
        private readonly object _lock = new object();
        private readonly IPreferenceManager _preferenceManager;

        public DiagnosticManager(IPreferenceManager preferenceManager)
        {
            _preferenceManager = preferenceManager;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                string appData = _preferenceManager.AppDataFolder;
                string diagDir = Path.Combine(appData, "diagnostics");
                Directory.CreateDirectory(diagDir);

                _logPath = Path.Combine(diagDir, "live_flight_recorder.log");
                _snapshotPath = Path.Combine(diagDir, "last_error_snapshot.json");

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
                try
                {
                    if (string.IsNullOrEmpty(_logPath)) return;
                    string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}";
                    File.AppendAllText(_logPath, entry + Environment.NewLine);
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

                if (!string.IsNullOrEmpty(_snapshotPath))
                {
                    File.WriteAllText(_snapshotPath, sb.ToString());
                    Log($"SNAPSHOT TAKEN: Check {_snapshotPath}", "CRITICAL");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to take snapshot: {ex.Message}", "ERROR");
            }
        }
    }
}
