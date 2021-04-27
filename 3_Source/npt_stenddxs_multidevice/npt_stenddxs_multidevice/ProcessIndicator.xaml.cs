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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NptMultiSlot
{
    /// <summary>
    /// Interaction logic for ProcessIndicator.xaml
    /// </summary>
    public partial class ProcessIndicator : UserControl
    {
        public ProcessIndicator()
        {
            InitializeComponent();
        }

        public object ProcessColor
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("ProcessColor", typeof(object),
              typeof(ProcessIndicator), new PropertyMetadata(null));

    }
}
