using System.Windows.Media;

namespace CosmoWhisper.Interfaces
{
    public interface IColorProvider
    {
        SolidColorBrush GetBrush(string key);
        System.Windows.Media.Color GetColor(string key);
    }
}