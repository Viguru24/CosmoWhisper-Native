using System;
using System.IO;
using CosmoWhisper.Managers;

namespace CosmoWhisper
{
    public static class CrashLogger
    {
        public static void Log(string message)
        {
            try
            {
                string logDir = Path.Combine(PreferenceManager.Shared.AppDataFolder, "logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "crash.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            }
            catch { }
        }
    }
}
