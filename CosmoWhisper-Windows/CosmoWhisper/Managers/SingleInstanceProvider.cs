using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CosmoWhisper.Managers
{
    public class SingleInstanceProvider
    {
        private static string PipeName => Environment.GetEnvironmentVariable("COSMO_DEV") == "true" ? "CosmoWhisper_Dev_Pipe" : "CosmoWhisper_Personal_Pipe";
        private static CancellationTokenSource? _cts;
        private static FileSystemWatcher? _tempFileWatcher;

        public static void StartListening()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ListenLoop(_cts.Token));
            StartTempFileWatcher();
        }

        public static void StopListening()
        {
            _cts?.Cancel();
            StopTempFileWatcher();
        }

        private static void StartTempFileWatcher()
        {
            try
            {
                string tempDir = Path.GetTempPath();
                string filter = Environment.GetEnvironmentVariable("COSMO_DEV") == "true" ? "cosmowhisper_dev_auth.tmp" : "cosmowhisper_personal_auth.tmp";

                // Check initially if file exists (e.g. from previous run or startup race)
                ProcessTempFile(Path.Combine(tempDir, filter));

                _tempFileWatcher = new FileSystemWatcher(tempDir, filter)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                _tempFileWatcher.Created += (s, e) => ProcessTempFile(e.FullPath);
                _tempFileWatcher.Changed += (s, e) => ProcessTempFile(e.FullPath);
            }
            catch (Exception ex)
            {
                DiagnosticManager.Shared.Log($"Failed to start temp file watcher: {ex.Message}", "ERROR");
            }
        }

        private static void StopTempFileWatcher()
        {
            try
            {
                if (_tempFileWatcher != null)
                {
                    _tempFileWatcher.EnableRaisingEvents = false;
                    _tempFileWatcher.Dispose();
                    _tempFileWatcher = null;
                }
            }
            catch { }
        }

        private static void ProcessTempFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Add a tiny delay to ensure the other process has finished writing and closed the file
                    Thread.Sleep(100);
                    string content = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        // Delete the file to consume it
                        File.Delete(filePath);

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ProtocolHandler.Handle(content);
                        });
                    }
                }
            }
            catch (IOException)
            {
                // File might still be locked, we'll try again on the next event
            }
            catch (Exception ex)
            {
                DiagnosticManager.Shared.Log($"Error processing temp file: {ex.Message}", "ERROR");
            }
        }

        private static async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync(token);
                        using (var reader = new StreamReader(server))
                        {
                            string message = await reader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(message))
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ProtocolHandler.Handle(message);
                                });
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // Log error but keep listening
                    DiagnosticManager.Shared.Log($"Pipe Server Error: {ex.Message}", "ERROR");
                    await Task.Delay(1000, token);
                }
            }
        }
        public static void SendMessage(string message)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(2000); // 2 second timeout
                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // If the first instance isn't listening or pipe fails, we just log it.
                System.Diagnostics.Debug.WriteLine($"Failed to send message via pipe: {ex.Message}");
            }
        }
    }
}
