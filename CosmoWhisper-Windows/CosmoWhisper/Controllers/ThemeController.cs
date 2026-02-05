using System;
using System.Windows;
using System.Windows.Media;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace CosmoWhisper.Controllers
{
    public class ThemeController : BaseViewController
    {
        public ThemeController(DashboardWindow window) : base(window)
        {
        }

        public void ApplyOcean() => ApplyTheme("#4f46e5", "#3b82f6", "#06b6d4", "#020617", "#0a0f1d", "#050A14", "#1e293b");
        public void ApplySunset() => ApplyTheme("#f59e0b", "#ec4899", "#e11d48", "#110005", "#1a000a", "#050002", "#450a1a");
        public void ApplyForest() => ApplyTheme("#10b981", "#34d399", "#a7f3d0", "#021102", "#0a1a0a", "#000500", "#0a3b1a");
        public void ApplyPurple() => ApplyTheme("#9333ea", "#a855f7", "#f472b6", "#0a0211", "#15021a", "#05000a", "#2e0a45");

        public void ApplyTheme(string accent, string glow, string secondary, string bgStart, string bgMid, string bgEnd, string surface)
        {
            Application.Current.Resources["ThemeAccentColor"] = ColorConverter.ConvertFromString(accent);
            Application.Current.Resources["ThemeGlowColor"] = ColorConverter.ConvertFromString(glow);
            Application.Current.Resources["ThemeSecondaryColor"] = ColorConverter.ConvertFromString(secondary);
            Application.Current.Resources["ThemeBgStartColor"] = ColorConverter.ConvertFromString(bgStart);
            Application.Current.Resources["ThemeBgMidColor"] = ColorConverter.ConvertFromString(bgMid);
            Application.Current.Resources["ThemeBgEndColor"] = ColorConverter.ConvertFromString(bgEnd);
            Application.Current.Resources["ThemeSurfaceColor"] = ColorConverter.ConvertFromString(surface);

            // Re-create brushes
            Application.Current.Resources["ThemeAccentBrush"] = CreateFrozenBrush(accent);
            Application.Current.Resources["ThemeGlowBrush"] = CreateFrozenBrush(glow);
            Application.Current.Resources["ThemeSecondaryBrush"] = CreateFrozenBrush(secondary);
            Application.Current.Resources["ThemeBgStartBrush"] = CreateFrozenBrush(bgStart);
            Application.Current.Resources["ThemeBgMidBrush"] = CreateFrozenBrush(bgMid);
            Application.Current.Resources["ThemeBgEndBrush"] = CreateFrozenBrush(bgEnd);
            Application.Current.Resources["ThemeSurfaceBrush"] = CreateFrozenBrush(surface);

            // Add slight transparency to surface for glass effect
            var surfaceColor = (Color)ColorConverter.ConvertFromString(surface);
            var glassBrush = new SolidColorBrush(Color.FromArgb(180, surfaceColor.R, surfaceColor.G, surfaceColor.B));
            glassBrush.Freeze();
            Application.Current.Resources["ThemeSurfaceGlassBrush"] = glassBrush;
        }

        private Brush CreateFrozenBrush(string colorHex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            brush.Freeze();
            return brush;
        }

    }
}
