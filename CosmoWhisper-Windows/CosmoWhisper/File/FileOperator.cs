using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CosmoWhisper.FileSystem
{
    public interface IFileOperator
    {
        Task WriteFileAsync(string path, string content, bool append = false);
        Task<bool> ExistsAsync(string path);
        Task<(int returnCode, string stdout, string stderr)> RunCommandAsync(string cmd, int timeoutMilliseconds = 120000);
    }

    public class LocalFileOperator : IFileOperator
    {
        public async Task<string> ReadFileAsync(string path)
        {
            try
            {
                return await System.IO.File.ReadAllTextAsync(path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read {path}: {ex.Message}");
            }
        }

        public async Task WriteFileAsync(string path, string content, bool append = false)
        {
            try
            {
                if (append)
                    await System.IO.File.AppendAllTextAsync(path, content);
                else
                    await System.IO.File.WriteAllTextAsync(path, content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write to {path}: {ex.Message}");
            }
        }

        public Task<bool> IsDirectoryAsync(string path)
        {
            return Task.FromResult(Directory.Exists(path));
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(System.IO.File.Exists(path) || Directory.Exists(path));
        }

        public async Task<(int returnCode, string stdout, string stderr)> RunCommandAsync(string cmd, int timeoutMilliseconds = 120000)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new System.Text.StringBuilder();
            var error = new System.Text.StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (await Task.Run(() => process.WaitForExit(timeoutMilliseconds)))
            {
                return (process.ExitCode, output.ToString(), error.ToString());
            }
            else
            {
                process.Kill();
                throw new TimeoutException($"Command '{cmd}' timed out after {timeoutMilliseconds}ms");
            }
        }
    }
}
