using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface IAIService
    {
        string LastTranscriptionSource { get; }
        long LastTranscriptionTime { get; }
        bool IsAccessBlocked { get; set; }

        void Initialize();
        void WarmUp();
        void LogEvent(string msg);
        Task<string> Transcribe(string filePath);
        Task<string> ProcessCommand(string prompt, string context, bool usePersonality = true, bool useInterfaceLanguage = true);
        Task<string> GenerateSpeech(string text, string voice, double speed, string apiKey);
    }
}
