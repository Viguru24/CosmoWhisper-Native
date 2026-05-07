using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CosmoWhisper.Controls
{
    public partial class VocabularyView : System.Windows.Controls.UserControl
    {
        public VocabularyView()
        {
            InitializeComponent();
        }

        private DashboardWindow GetParentWindow() => Window.GetWindow(this) as DashboardWindow;

        private void BtnAddEntry_Click(object sender, RoutedEventArgs e) => GetParentWindow()?.BtnAddVocab_Click(sender, e);
        private void ToggleSignOff_Click(object sender, MouseButtonEventArgs e) => GetParentWindow()?.ToggleSignOff_Click(sender, e);
        
        private void DeleteVocabulary_Click(object sender, RoutedEventArgs e) 
        {
            if (sender is FrameworkElement el)
            {
                GetParentWindow()?.DeleteVocabulary_Click(el, e);
            }
        }

        private void TxtNewInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                GetParentWindow()?.TxtNewInput_TextChanged(tb, e);
            }
        }

        private void VocabListControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
                while (parent != null && !(parent is ScrollViewer))
                {
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent) as UIElement;
                }
                
                if (OuterScrollViewer != null)
                {
                    OuterScrollViewer.ScrollToVerticalOffset(OuterScrollViewer.VerticalOffset - e.Delta);
                }
            }
        }
    }
}
