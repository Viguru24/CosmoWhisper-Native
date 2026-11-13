using System;
using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface IPreferenceManager
    {
        Managers.UserPreferences Preferences { get; }
        bool IsDevMode { get; }
        string AppDataFolder { get; }
        event Action PreferencesUpdated;
        
        void Load();
        void Save();
        Task SaveAsync();
        void Restore(string backupFolderPath);
    }
}
