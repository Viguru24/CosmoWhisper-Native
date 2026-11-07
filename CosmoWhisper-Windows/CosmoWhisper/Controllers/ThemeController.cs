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

        public void ApplyOcean() => ApplyTheme("#10B981", "#27272A", "#A1A1AA", "#09090B", "#121215", "#09090B", "#18181B");
        public void ApplySunset() => ApplyTheme("#F59E0B", "#27272A", "#D4D4D8", "#09090B", "#121215", "#09090B", "#18181B");
        public void ApplyForest() => ApplyTheme("#10B981", "#1E2922", "#34D399", "#09090B", "#0F1612", "#09090B", "#141C17");
        public void ApplyPurple() => ApplyTheme("#FFFFFF", "#27272A", "#D4D4D8", "#09090B", "#121215", "#09090B", "#18181B");

        public void ApplyTheme(string accent, string glow, string secondary, string bgStart, string bgMid, string bgEnd, string surface)
        {
            Application.Current.Resources["ThemeAccentColor"] = ColorConverter.ConvertFromString(accent);
            Application.Current.Resources["ThemeGlowColor"] = ColorConverter.ConvertFromString(glow);
            Application.Current.Resources["ThemeSecondaryColor"] = ColorConverter.ConvertFromString(secondary);
            Application.Current.Resources["ThemeBgStartColor"] = ColorConverter.ConvertFromString(bgStart);
            Application.Current.Resources["ThemeBgMidColor"] = ColorConverter.ConvertFromString(bgMid);
            Application.Current.Resources["ThemeBgEndColor"] = ColorConverter.ConvertFromString(bgEnd);
            Application.Current.Resources["ThemeSurfaceColor"] = ColorConverter.ConvertFromString(surface);
            Application.Current.Resources["ThemeForegroundColor"] = Colors.White;

            // Re-create brushes
            Application.Current.Resources["ThemeAccentBrush"] = CreateFrozenBrush(accent);
            Application.Current.Resources["ThemeGlowBrush"] = CreateFrozenBrush(glow);
            Application.Current.Resources["ThemeSecondaryBrush"] = CreateFrozenBrush(secondary);
            Application.Current.Resources["ThemeBgStartBrush"] = CreateFrozenBrush(bgStart);
            Application.Current.Resources["ThemeBgMidBrush"] = CreateFrozenBrush(bgMid);
            Application.Current.Resources["ThemeBgEndBrush"] = CreateFrozenBrush(bgEnd);
            Application.Current.Resources["ThemeSurfaceBrush"] = CreateFrozenBrush(surface);
            Application.Current.Resources["ThemeForegroundBrush"] = Brushes.White;

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


