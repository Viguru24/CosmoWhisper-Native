using System.Threading.Tasks;
using System.Windows;

namespace CosmoWhisper
{
    public static class CosmoMessage
    {
        public static async Task<bool> Show(string title, string message, string icon = "\uE946", bool showCancel = false, string confirmButtonText = "Confirm")
        {
            if (DashboardWindow.Instance != null)
            {
                return await DashboardWindow.Instance.ShowDialogAsync(title, message, icon, showCancel, confirmButtonText);
            }
            
            // Fallback for cases where main window isn't loaded
            var res = System.Windows.MessageBox.Show(message, title, showCancel ? MessageBoxButton.YesNo : MessageBoxButton.OK);
            return res == System.Windows.MessageBoxResult.Yes || res == System.Windows.MessageBoxResult.OK;
        }
    }
}
