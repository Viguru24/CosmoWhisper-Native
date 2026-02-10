using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CosmoWhisper.Managers
{
    public enum BackdropType
    {
        Auto = 0,
        None = 1,
        Mica = 2,
        Acrylic = 3,
        Tabbed = 4
    }

    public class WindowEffectManager
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void ApplyEffect(Window window, BackdropType type)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;

            // Enable Dark Mode (Matches your premium aesthetic)
            int darkMode = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Apply Backdrop
            int backdropValue = (int)type;
            DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int));
            
            // Set background to transparent to let Mica show through
            window.Background = System.Windows.Media.Brushes.Transparent;
        }
    }
}
