using System;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using System.Windows.Media;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace CosmoWhisper.Controllers
{
    public abstract class BaseViewController
    {
        protected readonly DashboardWindow Window;

        protected BaseViewController(DashboardWindow window)
        {
            Window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public void SetVisibility(UIElement? element, Visibility visibility)
        {
            if (element != null) element.Visibility = visibility;
        }

        private static readonly Brush ActiveButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FFFFFF"));

        static BaseViewController()
        {
            if (ActiveButtonBrush.CanFreeze) ActiveButtonBrush.Freeze();
        }

        protected void SetButtonActive(Button? btn)
        {
            if (btn != null)
                btn.Tag = "Active";
        }

        protected void SetButtonInactive(Button? btn)
        {
            if (btn != null)
                btn.Tag = null;
        }
    }
}
