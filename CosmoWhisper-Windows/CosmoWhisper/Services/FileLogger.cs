using System;
using System.IO;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Services
{
    public class FileLogger : ILogger
    {
        private readonly string _logDir;
        private readonly string _logPath;
        private static readonly object _lock = new object();

        public FileLogger()
        {
            _logDir = Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
            Directory.CreateDirectory(_logDir);
            _logPath = Path.Combine(_logDir, "cosmo_debug.log");
        }

        public void LogInformation(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        public void LogError(Exception ex, string message)
        {
            WriteLog("ERROR", $"{message} - {ex.Message}");
        }

        private void WriteLog(string level, string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}\n");
            }
            catch
            {
                // Silent fail
            }
        }
    }
}