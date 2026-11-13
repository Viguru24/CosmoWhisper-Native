using System;
using System.IO;
using CosmoWhisper.Services;

namespace CosmoWhisper
{
    public static class CrashLogger
    {
        public static void Log(string message)
        {
            try
            {
                string logPath = Path.Combine(FileLogger.ResolveDefaultAppDataFolder(), "logs", "crash.log");
                FileLogger.WriteBoundedLine(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                FileLogger.WriteShared("ERROR", $"[Crash] {message}");
            }
            catch { }
        }
    }
}
