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
        private const string PipeName = "CosmoWhisper_Personal_Pipe";
        private static CancellationTokenSource? _cts;

        public static void StartListening()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ListenLoop(_cts.Token));
        }

        public static void StopListening()
        {
            _cts?.Cancel();
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
