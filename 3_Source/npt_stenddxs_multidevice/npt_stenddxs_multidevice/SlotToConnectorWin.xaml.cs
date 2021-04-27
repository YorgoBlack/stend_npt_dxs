using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NptMultiSlot
{
    /// <summary>
    /// Interaction logic for PopupWin.xaml
    /// </summary>
    public partial class SlotToConnectorWin : Window
    {
        public SlotToConnectorWin()
        {
            InitializeComponent();
        }

        private void BindConnector_Click(object sender, RoutedEventArgs e)
        {
            AppManager.Worker.AppConfig.BindSelectedSlot();
        }
        private void NextConnector_Click(object sender, RoutedEventArgs e)
        {
            AppManager.Worker.AppConfig.SlotSelected = null;
            AppManager.Worker.AppConfig.ConfigureMsg = "Подключите (переподключите) прибор";
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppManager.Worker.AppConfig.ConfigureMsg = "Подключите (переподключите) прибор";
        }
    }
}
