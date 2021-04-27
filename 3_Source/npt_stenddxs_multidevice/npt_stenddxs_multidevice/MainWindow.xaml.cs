using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace NptMultiSlot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Title += " ,ver. " + System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            AppManager.Worker.AttachUsbEvents();
            AppManager.Worker.LoadConfig();
            int i = -1, j = 0;

            WindowState = WindowState.Maximized;
            for (int index=0; index < AppManager.Worker.AppConfig.Slots.Count; index++)
            {
                Slot slot = AppManager.Worker.AppConfig.Slots[index];
                SlotControl sw = new SlotControl(slot) {
                    Margin = new Thickness(1,1,1,1), IsEnabled = true,
                    Height = 470,
                    Width = 270,
                    MinHeight = 470,
                    MinWidth = 270 };
                sw.ContentFrame.Content = new CalibratePage() { DataContext = slot };

                SlotsGrid.Children.Add(sw);

                
                if (index % 5 == 0  )
                {
                    SlotsGrid.RowDefinitions.Add(new RowDefinition());
                    i++;
                    j = 0;
                }
                if (index < 5)
                {
                    SlotsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }

                sw.SetValue(Grid.RowProperty, i);
                sw.SetValue(Grid.ColumnProperty, j);

                j++;
            }

            AppManager.Worker.CheckConnectedDevices();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppManager.Worker.StopDevices();
            AppManager.Worker.SaveConfig();
        }


        private void SlotConnectors_Click(object sender, RoutedEventArgs e)
        {
            AppManager.Worker.AppConfig.SlotSelected = null;
            SlotToConnectorWin w = new SlotToConnectorWin() { DataContext = AppManager.Worker.AppConfig };
            w.ShowDialog();
        }

        private void SlotDefaults_Click(object sender, RoutedEventArgs e)
        {
            DefaultSettingsWin w = new DefaultSettingsWin();
            w.ShowDialog();
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class RatioConverter : MarkupExtension, IValueConverter
    {
        private static RatioConverter _instance;

        public RatioConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        { // do not let the culture default to local to prevent variable outcome re decimal syntax
            double size = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return size.ToString("G0", CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { // read only converter...
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new RatioConverter());
        }

    }

}
