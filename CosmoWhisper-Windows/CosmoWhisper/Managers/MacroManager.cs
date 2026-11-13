using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CosmoWhisper.Managers
{
    public class Macro
    {
        public string Trigger { get; set; } = "";
        public List<string> Sequence { get; set; } = new();
    }

    public class MacroManager
    {
        public static MacroManager Shared { get; } = new MacroManager();
        private List<Macro> _macros = new();
        private readonly string _path;
        private readonly HashSet<string> _activeTriggers = new(StringComparer.OrdinalIgnoreCase);
        private int _executionDepth = 0;
        private const int MaxMacroDepth = 3;

        public MacroManager()
        {
            _path = Path.Combine(PreferenceManager.Shared.AppDataFolder, "Macros.json");
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path);
                    _macros = JsonSerializer.Deserialize<List<Macro>>(json) ?? new();
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                string json = JsonSerializer.Serialize(_macros, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, json);
            }
            catch { }
        }

        public async Task<bool> TryExecuteMacro(string text)
        {
            if (_executionDepth >= MaxMacroDepth) return false;

            string cmd = text.ToLower().Trim();
            var macro = _macros.FirstOrDefault(m => m.Trigger.Equals(cmd, StringComparison.OrdinalIgnoreCase));
            if (macro != null && _activeTriggers.Add(cmd))
            {
                _executionDepth++;
                try
                {
                    foreach (var step in macro.Sequence)
                    {
                        await CommandController.Shared.Handle(step);
                        await Task.Delay(200); // Small delay between steps
                    }
                    return true;
                }
                finally
                {
                    _executionDepth--;
                    _activeTriggers.Remove(cmd);
                }
            }
            return false;
        }

        public void AddMacro(string trigger, List<string> sequence)
        {
            _macros.RemoveAll(m => m.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase));
            _macros.Add(new Macro { Trigger = trigger, Sequence = sequence });
            Save();
        }

        public List<Macro> GetMacros() => _macros;
    }
}


