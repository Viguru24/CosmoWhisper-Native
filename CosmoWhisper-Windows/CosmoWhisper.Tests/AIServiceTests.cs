using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CosmoWhisper.Services;
using Xunit;

namespace CosmoWhisper.Tests
{
    public class AIServiceTests
    {
        [Theory]
        [InlineData("professional", "Professional")]
        [InlineData("Professional", "Professional")]
        [InlineData("FRIENDLY", "Friendly")]
        [InlineData("coding", "Technical, precise, developer-focused")]
        [InlineData("Coding", "Technical, precise, developer-focused")]
        [InlineData("minimal", "Direct and stripped-back")]
        [InlineData("SARCASTIC", "Sarcastic")]
        [InlineData("pirate", "pirate")]
        public void GetPersonalityHint_MatchesCaseInsensitivelyForAllUiTags(string input, string expectedSubstring)
        {
            string hint = AIService.GetPersonalityHint(input);
            Assert.Contains(expectedSubstring, hint);
        }

        [Fact]
        public void GetPersonalityHint_ReturnsEmptyForNullOrUnknown()
        {
            Assert.Equal("", AIService.GetPersonalityHint(null));
            Assert.Equal("", AIService.GetPersonalityHint("unknown-mode"));
        }

        [Theory]
        [InlineData("concise", "Brief, concise")]
        [InlineData("CONCISE", "Brief, concise")]
        [InlineData("Balanced", "moderate detail")]
        [InlineData("detailed", "Comprehensive, in-depth")]
        public void GetVerbosityHint_MatchesCaseInsensitively(string input, string expectedSubstring)
        {
            string hint = AIService.GetVerbosityHint(input);
            Assert.Contains(expectedSubstring, hint);
        }

        [Fact]
        public void BuildFullPrompt_InjectsLanguagePersonalityAndVerbosity_UnlessPersonalityDisabled()
        {
            using var prefs = new TestPreferenceManager();
            prefs.Preferences.InterfaceLanguage = "en-GB";
            prefs.Preferences.AIPersonality = "coding";
            prefs.Preferences.AIVerbosity = "concise";

            var service = new AIService(prefs, new TestLogger(), null, null, null, null);

            string withPersonality = service.BuildFullPrompt("Refactor", "var x = 1;", usePersonality: true, useInterfaceLanguage: true);
            Assert.Contains("British English", withPersonality);
            Assert.Contains("developer-focused", withPersonality);
            Assert.Contains("Brief, concise", withPersonality);

            string withoutPersonality = service.BuildFullPrompt("Translate", "Hello", usePersonality: false, useInterfaceLanguage: true);
            Assert.Contains("British English", withoutPersonality);
            Assert.DoesNotContain("developer-focused", withoutPersonality);
            Assert.DoesNotContain("Brief, concise", withoutPersonality);
        }

        [Fact]
        public void StripThinkTags_RemovesCaseInsensitiveMultilineReasoningBlocks()
        {
            string raw = "<THINK>\nstep 1\nstep 2\n</THINK>\nFinal clean answer.";
            Assert.Equal("Final clean answer.", AIService.StripThinkTags(raw));
        }

        [Fact]
        public void BalanceContext_TruncatesExtremeInputAndPreservesPositiveMaxTokens()
        {
            using var prefs = new TestPreferenceManager();
            var service = new AIService(prefs, new TestLogger(), null, null, null, null);

            var (shortPrompt, shortMax) = service.BalanceContext("short input", 0);
            Assert.Equal("short input", shortPrompt);
            Assert.Equal(4096, shortMax);

            string oversized = new string('a', 600_000);
            var (trimmed, maxOut) = service.BalanceContext(oversized, 4096);
            Assert.True(trimmed.Length < oversized.Length);
            Assert.True(maxOut >= 1000);
        }

        [Fact]
        public async Task ProcessCommand_WhenAccessBlocked_ReturnsErrorWithoutNetworkCall()
        {
            using var prefs = new TestPreferenceManager();
            prefs.Preferences.GroqApiKey = "gsk_test_1234567890";
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var service = new AIService(prefs, new TestLogger(), null, null, new HttpClient(handler), () => "Code")
            {
                IsAccessBlocked = true
            };

            string result = await service.ProcessCommand("Fix", "code");
            Assert.StartsWith("Error: Your account has been blocked", result);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ProcessCommand_SendsPersonalityAndVerbosityHints_AndStripsThinkTags()
        {
            using var prefs = new TestPreferenceManager();
            prefs.Preferences.GroqApiKey = "gsk_test_1234567890";
            prefs.Preferences.AIPersonality = "coding";
            prefs.Preferences.AIVerbosity = "concise";

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            choices = new[]
                            {
                                new { message = new { content = "<think>drafting</think>Done cleanly." } }
                            }
                        }),
                        Encoding.UTF8,
                        "application/json")
                });

            var logger = new TestLogger();
            var service = new AIService(prefs, logger, null, null, new HttpClient(handler), () => "Code");

            string result = await service.ProcessCommand("Improve", "int a = 1;");
            Assert.Equal("Done cleanly.", result);
            Assert.Single(handler.Requests);
            Assert.Contains("developer-focused", handler.Requests[0].Body);
            Assert.Contains("Brief, concise", handler.Requests[0].Body);
        }

        [Fact]
        public async Task ProcessCommand_CascadesFromDecommissionedGroqModelsToOpenAIAndProxy()
        {
            using var prefs = new TestPreferenceManager();
            prefs.Preferences.GroqApiKey = "gsk_test_1234567890";
            prefs.Preferences.OpenAIApiKey = "sk-test-openai";
            prefs.Preferences.BackendUrl = "https://backend.local";

            var handler = new FakeHttpMessageHandler(req =>
            {
                if (req.Url.Contains("api.groq.com"))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("{\"error\":{\"code\":\"model_not_found\"}}")
                    };
                }
                if (req.Url.Contains("api.openai.com"))
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("openai fail")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            choices = new[] { new { message = new { content = "<think>ok</think>Proxy response" } } }
                        }),
                        Encoding.UTF8,
                        "application/json")
                };
            });

            var service = new AIService(prefs, new TestLogger(), null, null, new HttpClient(handler), () => "");
            string result = await service.ProcessCommand("Test", "Hello");

            Assert.Equal("Proxy response", result);
            Assert.Equal(5, handler.Requests.Count(r => r.Url.Contains("api.groq.com")));
            Assert.Equal(1, handler.Requests.Count(r => r.Url.Contains("api.openai.com")));
            Assert.Equal(1, handler.Requests.Count(r => r.Url == "https://backend.local/api/ai/chat"));
        }

        [Fact]
        public async Task Transcribe_UseLocalWhisperOnly_UsesLocalEngineAndSkipsCloud()
        {
            using var prefs = new TestPreferenceManager();
            prefs.Preferences.UseLocalWhisperOnly = true;
            prefs.Preferences.InterfaceLanguage = "en-GB";

            var fakeLocal = new FakeLocalModelManager
            {
                IsReady = true,
                ActiveEngineName = "FakeParakeet",
                TranscribeResult = "Hello from local"
            };
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var service = new AIService(prefs, new TestLogger(), null, fakeLocal, new HttpClient(handler), () => "Code");

            string result = await service.Transcribe("dummy.wav");

            Assert.Equal("Hello from local", result);
            Assert.Equal("Local (FakeParakeet)", service.LastTranscriptionSource);
            Assert.Single(fakeLocal.Calls);
            Assert.Contains("colour", fakeLocal.Calls[0].Prompt);
            Assert.Empty(handler.Requests);

            fakeLocal.IsReady = false;
            string notReady = await service.Transcribe("dummy.wav");
            Assert.StartsWith("Error: Local Model", notReady);
            Assert.Empty(handler.Requests);
        }
    }
}
