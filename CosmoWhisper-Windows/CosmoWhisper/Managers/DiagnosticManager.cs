using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Services;

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

        private string? _logPath;
        private string? _snapshotPath;
        private readonly IPreferenceManager _preferenceManager;
        private readonly ILogger? _logger;

        public DiagnosticManager(IPreferenceManager preferenceManager, ILogger? logger = null)
        {
            _preferenceManager = preferenceManager;
            _logger = logger;
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
            if (string.IsNullOrEmpty(_logPath)) return;

            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}";
            FileLogger.WriteBoundedLine(_logPath, entry);

            string formatted = $"[{category}] {message}";
            if (_logger != null)
            {
                if (category.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                    category.Equals("CRITICAL", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(formatted);
                }
                else if (category.Equals("WARN", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(formatted);
                }
                else
                {
                    _logger.LogInformation(formatted);
                }
            }
            else
            {
                FileLogger.WriteShared(category, message);
            }
        }

        public void TakeSnapshot(string error, Dictionary<string, object> state)
        {
            try
            {
                var safeState = new Dictionary<string, string>();
                foreach (var kvp in state)
                {
                    safeState[kvp.Key] = kvp.Value?.ToString() ?? "null";
                }

                var payload = new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    error,
                    state = safeState
                };

                string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

                if (!string.IsNullOrEmpty(_snapshotPath))
                {
                    File.WriteAllText(_snapshotPath, json);
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
