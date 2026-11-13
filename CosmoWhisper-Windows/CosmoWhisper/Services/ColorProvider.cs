using System.Windows;
using System.Windows.Media;
using Media = System.Windows.Media;
using CosmoWhisper.Interfaces;

namespace CosmoWhisper.Services
{
    public class ColorProvider : IColorProvider
    {
        private readonly System.Windows.Application _app = System.Windows.Application.Current;

        public SolidColorBrush GetBrush(string key)
        {
            return (SolidColorBrush)_app.TryFindResource(key) ?? System.Windows.Media.Brushes.White;
        }

        public System.Windows.Media.Color GetColor(string key)
        {
            var brush = GetBrush(key);
            return brush.Color;
        }
    }
}