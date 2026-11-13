using System;

namespace CosmoWhisper.Interfaces
{
    public interface ILogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception ex, string message);
    }
}