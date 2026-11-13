using System.IO;
using System.Text.Json;
using CosmoWhisper.Managers;
using Xunit;

namespace CosmoWhisper.Tests
{
    public class VocabularyManagerTests
    {
        [Fact]
        public void ApplyCorrections_AppliesLongestMatchFirstAndIgnoresCase()
        {
            using var prefs = new TestPreferenceManager();
            File.WriteAllText(Path.Combine(prefs.AppDataFolder, "vocabulary.json"), "{}");

            var vm = new VocabularyManager(prefs);
            vm.AddReplacement("cosmo", "CosmoApp");
            vm.AddReplacement("cosmo whisper", "CosmoWhisper");

            string output = vm.ApplyCorrections("Try cosmo whisper and cosmo today");
            Assert.Equal("Try CosmoWhisper and CosmoApp today", output);
        }

        [Fact]
        public void ApplyCorrections_UsesInjectedPreferencesForSignOffAndEmailCorrections()
        {
            using var prefs = new TestPreferenceManager();
            File.WriteAllText(Path.Combine(prefs.AppDataFolder, "vocabulary.json"), "{}");
            prefs.Preferences.EnableSmartEmailCorrections = true;
            prefs.Preferences.EnableSignOff = true;
            prefs.Preferences.SignOffSignature = "Regards,\\nLouis";

            var vm = new VocabularyManager(prefs);

            string output = vm.ApplyCorrections("Email dev at cosmowhisper.com then sign off");
            Assert.Equal("Email dev@cosmowhisper.com then Regards,\nLouis", output);

            prefs.Preferences.EnableSignOff = false;
            string withoutSignOff = vm.ApplyCorrections("Please sign off");
            Assert.Equal("Please sign off", withoutSignOff);
        }

        [Fact]
        public void SetContext_LoadsAppOverridesAndCombinesHints()
        {
            using var prefs = new TestPreferenceManager();
            File.WriteAllText(Path.Combine(prefs.AppDataFolder, "vocabulary.json"), "{}");
            string contextsDir = Path.Combine(prefs.AppDataFolder, "Contexts");
            Directory.CreateDirectory(contextsDir);
            File.WriteAllText(
                Path.Combine(contextsDir, "Code.json"),
                JsonSerializer.Serialize(new
                {
                    Replacements = new Dictionary<string, string> { { "api", "WebAPI" } },
                    TranscriptionHints = "xUnit, WPF"
                }));

            var vm = new VocabularyManager(prefs);
            vm.TranscriptionHints = "CosmoWhisper,";
            vm.AddReplacement("api", "REST API");
            vm.SetContext("Code");

            Assert.Equal("Call WebAPI in xUnit", vm.ApplyCorrections("Call api in xunit"));
            Assert.Equal("CosmoWhisper, xUnit, WPF", vm.GetActiveHints());
        }

        [Fact]
        public void IsSoloCorrection_MatchesGlobalOrAppReplacementWithoutExtraWords()
        {
            using var prefs = new TestPreferenceManager();
            File.WriteAllText(Path.Combine(prefs.AppDataFolder, "vocabulary.json"), "{}");
            var vm = new VocabularyManager(prefs);
            vm.AddReplacement("my key", "secret-token-42");

            Assert.True(vm.IsSoloCorrection("secret-token-42."));
            Assert.False(vm.IsSoloCorrection("Here is secret-token-42"));
            Assert.False(vm.IsSoloCorrection("   "));
        }
    }
}
