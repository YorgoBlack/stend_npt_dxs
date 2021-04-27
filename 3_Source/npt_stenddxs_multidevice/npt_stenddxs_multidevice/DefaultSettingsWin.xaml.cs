using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class DefaultSettingsWin : Window
    {
        ViewModel panel = new ViewModel();
        public DefaultSettingsWin()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            string key_prev = AppManager.Worker.AppConfig.DefaulSlot.DevParamKey;

            for (int ndx=0; ndx < AppManager.Worker.AppConfig.Slots.Count; ndx++)
            {
                foreach(var key in AppManager.Worker.SensorsNamesByNpt.Keys)
                {
                    AppManager.Worker.AppConfig.DefaulSlot.DevParamKey = key;
                    System.Threading.Thread.Sleep(50);
                    AppManager.Worker.AppConfig.Slots[ndx].FactorySettings[key] = AppManager.Worker.AppConfig.DefaulSlot.NptParam.Clone();
                    AppManager.Worker.AppConfig.Slots[ndx].CalibrateSettings[key] = AppManager.Worker.AppConfig.DefaulSlot.CalibrateParam.Clone();
                }
            }
            AppManager.Worker.AppConfig.DefaulSlot.DevParamKey = key_prev;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = panel;
            SlotUI.Content = new ParamsPage() { DataContext = AppManager.Worker.AppConfig.DefaulSlot };
            panel.PanelEnabled = true;
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        bool _PanelEnabled = true;
        public bool PanelEnabled { get { return _PanelEnabled; } set {
                _PanelEnabled = value;
                OnPropertychanged("PanelEnabled");

            } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertychanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
