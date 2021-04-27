using System;
using System.Collections.Generic;
using System.Globalization;
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
    public partial class ParamsPage : Page
    {
        public ParamsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

    }

    public class TimerToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var timespan = TimeSpan.FromSeconds(int.Parse(value.ToString()));
            return timespan.ToString(@"mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] s0 = value.ToString().Split(':');
            
            var timespan = TimeSpan.FromSeconds(60 * int.Parse(s0[0].Replace("_","0"))  + int.Parse(s0[1].Replace("_", "0")));
            return timespan.TotalSeconds;
        }
    }

    public class RadioBoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }


    [ValueConversion(typeof(float), typeof(string))]
    public class FloatConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            string str = value.ToString();
            str.Replace(".", ",");
            return str;
        }

        // Convert from string to double 
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            float val = Helpers.Win32Helper.ParseFloat(value.ToString());

            if ( parameter != null )
            {
                string[] bnds = parameter.ToString().Split(':');
                if( bnds.Length > 1  )
                {
                    float lower = Helpers.Win32Helper.ParseFloat(bnds[0].Replace(",","."));
                    float upper = Helpers.Win32Helper.ParseFloat(bnds[1].Replace(",", "."));
                    if( val >= lower && val <= upper )
                    {
                        return val;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            
            return val;
        }

    }

}
