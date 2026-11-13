using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface ILocalModelManager
    {
        bool IsReady { get; }
        string ActiveEngineName { get; }
        void Initialize();
        Task WarmUp();
        Task<string> TranscribeAsync(string wavFilePath, string prompt = "", string language = "en");
        Task RunSelfTest();
    }
}
