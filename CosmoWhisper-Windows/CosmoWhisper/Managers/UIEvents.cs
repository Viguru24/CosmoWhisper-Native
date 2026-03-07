using System;
using System.Threading.Tasks;

namespace CosmoWhisper.Managers
{
    public class UIEvents
    {
        public static UIEvents Shared { get; } = new UIEvents();

        public delegate Task<bool> MessageBoxRequest(string title, string message, string icon, bool showCancel = false, string confirmText = "OK");
        public event MessageBoxRequest? RequestMessageBox;

        public event Action<string>? RequestOpenUrl;

        public async Task<bool> ShowMessageAsync(string title, string message, string icon = "✨", bool showCancel = false, string confirmText = "OK")
        {
            if (RequestMessageBox != null)
            {
                return await RequestMessageBox.Invoke(title, message, icon, showCancel, confirmText);
            }
            return false;
        }

        public void OpenUrl(string url)
        {
            RequestOpenUrl?.Invoke(url);
        }
    }
}
