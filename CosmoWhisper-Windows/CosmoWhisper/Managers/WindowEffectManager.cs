using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

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

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWCP_ROUND = 2;

        private static readonly SolidColorBrush FallbackWindowBrush = CreateFrozenFallbackBrush();

        public static bool IsMicaSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);

        private static SolidColorBrush CreateFrozenFallbackBrush()
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 13, 16));
            if (brush.CanFreeze) brush.Freeze();
            return brush;
        }

        public static void ApplyEffect(Window window, BackdropType type)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;

            int darkMode = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Request native Windows 11 rounded corners when supported
            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            bool micaApplied = false;
            if (type != BackdropType.None && IsMicaSupported)
            {
                int backdropValue = (int)type;
                micaApplied = DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int)) == 0;
            }

            // Let Win11 Mica show through on supported builds; keep opaque dark Obsidian on Win10/fallback
            window.Background = micaApplied
                ? System.Windows.Media.Brushes.Transparent
                : FallbackWindowBrush;
        }
    }
}


