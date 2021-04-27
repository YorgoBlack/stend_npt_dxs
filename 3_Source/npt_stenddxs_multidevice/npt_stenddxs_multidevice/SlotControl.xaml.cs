using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NptMultiSlot
{
    /// <summary>
    /// Interaction logic for SlotControl.xaml
    /// </summary>
    public partial class SlotControl : UserControl
    {
        
        Slot slot;
        public SlotControl(Slot slot)
        {
            this.slot = slot;
            InitializeComponent();
        }

        private void BtnParams_click(object sender, RoutedEventArgs e)
        {
            if( slot.ModeCalibSettings )
            {
                ContentFrame.Content = new ParamsPage() { DataContext = slot };
                ((ToggleButton)sender).Content = "Калибровка";
            }
            else
            {
                ContentFrame.Content = new CalibratePage() { DataContext = slot };
                ((ToggleButton)sender).Content = "Настройка";
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = slot;
        }

    }

    public class ColoredText
    {
        public string TheText { get; set; }
        public SolidColorBrush TheColour { get; set; } = Brushes.Black;
    }

    public static class WpfHelper
    {
        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(WpfHelper), new PropertyMetadata(false, AutoScrollPropertyChanged));

        private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ScrollViewer;

            if (scrollViewer != null && (bool)e.NewValue)
            {
                scrollViewer.ScrollToBottom();
            }
        }
    }

}
