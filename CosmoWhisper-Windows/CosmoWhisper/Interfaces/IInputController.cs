using System.Threading.Tasks;

namespace CosmoWhisper.Interfaces
{
    public interface IInputController
    {
        Task PasteText(string text, bool autoSubmit, bool restoreClipboard);
        void ExecuteKeystroke(string key, bool ctrl = false, bool shift = false, bool alt = false, bool win = false);
        Task<string> CaptureSelectedText();
        void SendKey(byte virtualKey, bool keyUp);
        void SendModifierKey(byte virtualKey, bool keyUp);
        void SafeSetClipboardText(string text);
    }
}
