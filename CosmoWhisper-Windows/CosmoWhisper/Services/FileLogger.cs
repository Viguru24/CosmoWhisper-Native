using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using CosmoWhisper.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CosmoWhisper.Services
{
    /// <summary>
    /// Thread-safe, bounded rolling file logger shared across DI and static subsystems.
    /// </summary>
    public class FileLogger : ILogger
    {
        public const long DefaultMaxBytes = 5 * 1024 * 1024;
        public const int DefaultMaxFiles = 3;

        private readonly string _logDir;
        private readonly string _logPath;
        private readonly long _maxBytes;
        private readonly int _maxFiles;

        private static readonly ConcurrentDictionary<string, object> _pathLocks =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lazy<FileLogger> _shared =
            new(() => new FileLogger());

        public string LogFilePath => _logPath;

        [ActivatorUtilitiesConstructor]
        public FileLogger()
            : this(ResolveDefaultAppDataFolder())
        {
        }

        public FileLogger(
            string appDataFolder,
            long maxBytes = DefaultMaxBytes,
            int maxFiles = DefaultMaxFiles,
            string fileName = "cosmo_debug.log")
        {
            string baseFolder = string.IsNullOrWhiteSpace(appDataFolder)
                ? ResolveDefaultAppDataFolder()
                : appDataFolder;

            _logDir = Path.Combine(baseFolder, "logs");
            Directory.CreateDirectory(_logDir);
            _logPath = Path.Combine(_logDir, string.IsNullOrWhiteSpace(fileName) ? "cosmo_debug.log" : fileName);
            _maxBytes = Math.Max(1024, maxBytes);
            _maxFiles = Math.Max(1, maxFiles);
        }

        public static string ResolveDefaultAppDataFolder()
        {
            try
            {
                if (ServiceLocator.Provider?.GetService(typeof(IPreferenceManager)) is IPreferenceManager pref &&
                    !string.IsNullOrWhiteSpace(pref.AppDataFolder))
                {
                    return pref.AppDataFolder;
                }
            }
            catch
            {
            }

            string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            bool isDev =
                string.Equals(Environment.GetEnvironmentVariable("COSMO_DEV"), "true", StringComparison.OrdinalIgnoreCase) ||
                Environment.GetCommandLineArgs().Any(arg =>
                    arg.Equals("--dev", StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith("cosmowhisper-dev", StringComparison.OrdinalIgnoreCase));
#if DEBUG
            isDev = true;
#endif

            return Path.Combine(appDataRoot, isDev ? "CosmoWhisper_Dev" : "CosmoWhisper_Personal");
        }

        public static void WriteShared(string level, string message)
        {
            _shared.Value.WriteLog(level, message);
        }

        public static void WriteBoundedLine(
            string fullPath,
            string formattedLine,
            long maxBytes = DefaultMaxBytes,
            int maxFiles = DefaultMaxFiles)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            try
            {
                string normalizedPath = Path.GetFullPath(fullPath);
                string? dir = Path.GetDirectoryName(normalizedPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                long boundedMaxBytes = Math.Max(1024, maxBytes);
                int boundedMaxFiles = Math.Max(1, maxFiles);
                string payload = formattedLine.EndsWith('\n')
                    ? formattedLine
                    : formattedLine + Environment.NewLine;
                int incomingBytes = Encoding.UTF8.GetByteCount(payload);

                object gate = _pathLocks.GetOrAdd(normalizedPath, _ => new object());
                lock (gate)
                {
                    RotateIfNeeded(normalizedPath, boundedMaxBytes, boundedMaxFiles, incomingBytes);
                    File.AppendAllText(normalizedPath, payload, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never throw into callers.
            }
        }

        public void LogInformation(string message) => WriteLog("INFO", message);

        public void LogWarning(string message) => WriteLog("WARN", message);

        public void LogError(string message) => WriteLog("ERROR", message);

        public void LogError(Exception ex, string message) => WriteLog("ERROR", $"{message} - {ex.Message}");

        private void WriteLog(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            WriteBoundedLine(_logPath, line, _maxBytes, _maxFiles);
        }

        private static void RotateIfNeeded(string logPath, long maxBytes, int maxFiles, int incomingBytes)
        {
            if (!File.Exists(logPath)) return;

            var info = new FileInfo(logPath);
            if (info.Length <= 0 || info.Length + incomingBytes <= maxBytes) return;

            string oldestBackup = $"{logPath}.{maxFiles}";
            if (File.Exists(oldestBackup))
            {
                File.Delete(oldestBackup);
            }

            for (int i = maxFiles - 1; i >= 1; i--)
            {
                string src = $"{logPath}.{i}";
                string dst = $"{logPath}.{i + 1}";
                if (File.Exists(src))
                {
                    File.Move(src, dst, overwrite: true);
                }
            }

            File.Move(logPath, $"{logPath}.1", overwrite: true);
        }
    }
}
