using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CosmoWhisper.Interfaces;
using CosmoWhisper.Managers;

namespace CosmoWhisper.Tests
{
    internal sealed class TestPreferenceManager : IPreferenceManager, IDisposable
    {
        public UserPreferences Preferences { get; } = new UserPreferences();
        public bool IsDevMode => true;
        public string AppDataFolder { get; }

        public event Action? PreferencesUpdated;

        public TestPreferenceManager()
        {
            AppDataFolder = Path.Combine(Path.GetTempPath(), $"CosmoWhisperTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(AppDataFolder);
        }

        public void Load() { }

        public void Save() => PreferencesUpdated?.Invoke();

        public Task SaveAsync()
        {
            PreferencesUpdated?.Invoke();
            return Task.CompletedTask;
        }

        public void Restore(string backupFolderPath) { }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(AppDataFolder))
                {
                    Directory.Delete(AppDataFolder, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    internal sealed class TestLogger : ILogger
    {
        public ConcurrentQueue<(string Level, string Message)> Entries { get; } = new();

        public void LogInformation(string message) => Entries.Enqueue(("INFO", message));
        public void LogWarning(string message) => Entries.Enqueue(("WARN", message));
        public void LogError(string message) => Entries.Enqueue(("ERROR", message));
        public void LogError(Exception ex, string message) => Entries.Enqueue(("ERROR", $"{message} - {ex.Message}"));
    }

    internal sealed class FakeLocalModelManager : ILocalModelManager
    {
        public bool IsReady { get; set; }
        public string ActiveEngineName { get; set; } = "FakeLocalEngine";
        public string TranscribeResult { get; set; } = "local transcript";
        public List<(string FilePath, string Prompt, string Language)> Calls { get; } = new();
        public bool WarmUpCalled { get; private set; }

        public void Initialize() { }

        public Task WarmUp()
        {
            WarmUpCalled = true;
            return Task.CompletedTask;
        }

        public Task<string> TranscribeAsync(string wavFilePath, string prompt = "", string language = "en")
        {
            Calls.Add((wavFilePath, prompt, language));
            return Task.FromResult(TranscribeResult);
        }

        public Task RunSelfTest() => Task.CompletedTask;
    }

    internal sealed record RecordedHttpRequest(string Url, HttpMethod Method, string? AuthScheme, string? AuthParameter, string Body);

    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<RecordedHttpRequest, HttpResponseMessage> _responder;
        public List<RecordedHttpRequest> Requests { get; } = new();

        public FakeHttpMessageHandler(Func<RecordedHttpRequest, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : "";

            var recorded = new RecordedHttpRequest(
                request.RequestUri?.ToString() ?? "",
                request.Method,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                body);

            Requests.Add(recorded);
            return _responder(recorded);
        }
    }
}
