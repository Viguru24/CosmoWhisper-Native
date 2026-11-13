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
        public void ApplyForest() => ApplyTheme("#22C55E", "#1E2922", "#9CA3AF", "#09090B", "#0F1612", "#09090B", "#141C17");
        public void ApplyPurple() => ApplyTheme("#A855F7", "#27272A", "#D4D4D8", "#09090B", "#121215", "#09090B", "#18181B");

        public void ApplyTheme(string accent, string glow, string secondary, string bgStart, string bgMid, string bgEnd, string surface)
        {
            var res = Application.Current.Resources;
            var accentColor = (Color)ColorConverter.ConvertFromString(accent);
            var surfaceColor = (Color)ColorConverter.ConvertFromString(surface);
            var bgStartColor = (Color)ColorConverter.ConvertFromString(bgStart);

            res["ThemeAccentColor"] = accentColor;
            res["ThemeGlowColor"] = ColorConverter.ConvertFromString(glow);
            res["ThemeSecondaryColor"] = ColorConverter.ConvertFromString(secondary);
            res["ThemeBgStartColor"] = bgStartColor;
            res["ThemeBgMidColor"] = ColorConverter.ConvertFromString(bgMid);
            res["ThemeBgEndColor"] = ColorConverter.ConvertFromString(bgEnd);
            res["ThemeSurfaceColor"] = surfaceColor;
            res["ThemeForegroundColor"] = (Color)ColorConverter.ConvertFromString("#F4F4F5");

            var accentBrush = CreateFrozenBrush(accentColor);
            res["ThemeAccentBrush"] = accentBrush;
            res["AccentBrush"] = accentBrush;
            res["ThemeGlowBrush"] = CreateFrozenBrush(glow);
            res["ThemeSecondaryBrush"] = CreateFrozenBrush(secondary);
            res["ThemeBgStartBrush"] = CreateFrozenBrush(bgStartColor);
            res["ThemeBgMidBrush"] = CreateFrozenBrush(bgMid);
            res["ThemeBgEndBrush"] = CreateFrozenBrush(bgEnd);
            res["ThemeSurfaceBrush"] = CreateFrozenBrush(surfaceColor);
            res["ThemeForegroundBrush"] = CreateFrozenBrush("#F4F4F5");

            // Subtle & hover semantic tokens derived from current theme
            res["ThemeAccentSubtleBrush"] = CreateFrozenBrush(Color.FromArgb(32, accentColor.R, accentColor.G, accentColor.B));
            res["ThemeAccentHoverBrush"] = CreateFrozenBrush(Color.FromArgb(255,
                (byte)Math.Min(255, accentColor.R + 24),
                (byte)Math.Min(255, accentColor.G + 24),
                (byte)Math.Min(255, accentColor.B + 24)));

            // Choose dark vs white foreground on solid accent fills by perceptual luminance
            double luminance = (0.2126 * accentColor.R + 0.7152 * accentColor.G + 0.0722 * accentColor.B) / 255.0;
            res["ThemeAccentForegroundBrush"] = CreateFrozenBrush(luminance > 0.55 ? "#09090B" : "#FFFFFF");

            // Slightly elevated border / surface variants
            res["ThemeSubtleSurfaceBrush"] = CreateFrozenBrush(Color.FromRgb(
                (byte)Math.Min(255, surfaceColor.R + 8),
                (byte)Math.Min(255, surfaceColor.G + 8),
                (byte)Math.Min(255, surfaceColor.B + 10)));
            res["ThemeHoverSurfaceBrush"] = CreateFrozenBrush(Color.FromRgb(
                (byte)Math.Min(255, surfaceColor.R + 16),
                (byte)Math.Min(255, surfaceColor.G + 16),
                (byte)Math.Min(255, surfaceColor.B + 20)));
            res["ThemeWindowBackdropBrush"] = CreateFrozenBrush(Color.FromArgb(214, bgStartColor.R, bgStartColor.G, bgStartColor.B));
            res["ThemeSidebarBrush"] = CreateFrozenBrush(Color.FromArgb(200, bgStartColor.R, bgStartColor.G, bgStartColor.B));
            res["ThemeSurfaceGlassBrush"] = CreateFrozenBrush(Color.FromArgb(230, surfaceColor.R, surfaceColor.G, surfaceColor.B));
        }

        private Brush CreateFrozenBrush(string colorHex) => CreateFrozenBrush((Color)ColorConverter.ConvertFromString(colorHex));

        private Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

    }
}


