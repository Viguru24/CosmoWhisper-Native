using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface ISpeechEngine
    {
        string EngineName { get; }
        bool IsReady { get; }
        Task InitializeAsync();
        Task<string> TranscribeAsync(string wavFilePath, string prompt = "", string language = "en");
        Task WarmUpAsync();
    }
}
