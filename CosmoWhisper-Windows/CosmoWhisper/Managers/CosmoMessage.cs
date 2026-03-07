using System;
using System.Threading.Tasks;
using System.Windows;

namespace CosmoWhisper.Managers
{
    public static class CosmoMessage
    {
        public static async Task<bool> Show(string title, string message, string icon = "✨", bool showCancel = false)
        {
            return await UIEvents.Shared.ShowMessageAsync(title, message, icon, showCancel);
        }
    }
}
