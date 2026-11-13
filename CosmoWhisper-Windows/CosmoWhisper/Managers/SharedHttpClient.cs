using System;
using System.Net.Http;
using System.Threading;

namespace CosmoWhisper.Managers
{
    public static class SharedHttpClient
    {
        private static readonly Lazy<HttpClient> _client = new Lazy<HttpClient>(() => CreateClient(TimeSpan.FromSeconds(30)));
        private static readonly Lazy<HttpClient> _streamClient = new Lazy<HttpClient>(() => CreateClient(Timeout.InfiniteTimeSpan));

        private static HttpClient CreateClient(TimeSpan timeout)
        {
            var handler = new HttpClientHandler
            {
                UseProxy = true,
                PreAuthenticate = true,
                UseDefaultCredentials = true
            };
            var client = new HttpClient(handler)
            {
                Timeout = timeout
            };
            client.DefaultRequestHeaders.Add("User-Agent", "CosmoWhisper/1.0");
            return client;
        }

        public static HttpClient Instance => _client.Value;
        public static HttpClient StreamInstance => _streamClient.Value;
    }
}