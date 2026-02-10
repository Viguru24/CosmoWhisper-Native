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

        public MacroManager()
        {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper", "Macros.json");
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
            string cmd = text.ToLower().Trim();
            var macro = _macros.FirstOrDefault(m => m.Trigger.ToLower() == cmd);
            if (macro != null)
            {
                foreach (var step in macro.Sequence)
                {
                    await CommandController.Shared.Handle(step);
                    await Task.Delay(200); // Small delay between steps
                }
                return true;
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
