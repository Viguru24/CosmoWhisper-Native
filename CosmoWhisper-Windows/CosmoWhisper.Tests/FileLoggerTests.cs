using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CosmoWhisper.Services;
using Xunit;

namespace CosmoWhisper.Tests
{
    public class FileLoggerTests
    {
        [Fact]
        public void FileLogger_RotatesWithinMaxFilesAndRespectsMaxBytes()
        {
            using var prefs = new TestPreferenceManager();
            var logger = new FileLogger(prefs.AppDataFolder, maxBytes: 1024, maxFiles: 3);

            string payload = new string('X', 220);
            for (int i = 0; i < 40; i++)
            {
                logger.LogInformation($"entry-{i:D2}-{payload}");
            }

            string logDir = Path.Combine(prefs.AppDataFolder, "logs");
            Assert.True(File.Exists(Path.Combine(logDir, "cosmo_debug.log")));
            Assert.True(File.Exists(Path.Combine(logDir, "cosmo_debug.log.1")));
            Assert.True(File.Exists(Path.Combine(logDir, "cosmo_debug.log.2")));
            Assert.True(File.Exists(Path.Combine(logDir, "cosmo_debug.log.3")));
            Assert.False(File.Exists(Path.Combine(logDir, "cosmo_debug.log.4")));

            long activeSize = new FileInfo(Path.Combine(logDir, "cosmo_debug.log")).Length;
            Assert.True(activeSize <= 1024, $"Expected active log size <= 1024 bytes, got {activeSize}");
        }

        [Fact]
        public async Task FileLogger_ConcurrentWritesPreserveIntegrity()
        {
            using var prefs = new TestPreferenceManager();
            var logger = new FileLogger(prefs.AppDataFolder, maxBytes: 5 * 1024 * 1024, maxFiles: 3);

            var tasks = Enumerable.Range(0, 12)
                .Select(worker => Task.Run(() =>
                {
                    for (int i = 0; i < 25; i++)
                    {
                        logger.LogWarning($"w{worker:D2}-msg{i:D2}");
                    }
                }));

            await Task.WhenAll(tasks);

            string[] lines = File.ReadAllLines(logger.LogFilePath);
            Assert.Equal(300, lines.Length);
            Assert.All(lines, line => Assert.Contains("[WARN] w", line));
        }
    }
}
