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
using System.Windows.Shapes;

namespace NptMultiSlot
{
    /// <summary>
    /// Interaction logic for PopupWin.xaml
    /// </summary>
    public partial class CalibratePage : Page
    {
        
        public CalibratePage()
        {
            InitializeComponent();
        }

        private void CheckBtn_Click(object sender, RoutedEventArgs e)
        {
            NptWorker npt = ((Slot)DataContext).NptWorker;
            if ( npt != null )
            {
                npt.Command = NptCommand.СheckUp;
                npt.TotalTimes = 1000 * npt.TimesByOpearation[npt.Command];
                npt.PrevOperationMs = 0;
            }
        }

        private void WriteFactorySettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            NptWorker npt = ((Slot)DataContext).NptWorker;
            if (npt != null)
            {
                npt.Command = NptCommand.WriteFactorySettings;
                npt.TotalTimes = 1000 * npt.TimesByOpearation[npt.Command];
                npt.PrevOperationMs = 0;
            }
        }

        private void HeartBtn_Click(object sender, RoutedEventArgs e)
        {
            NptWorker npt = ((Slot)DataContext).NptWorker;
            if (npt != null)
            {
                npt.Command = NptCommand.WarmingUp;
                npt.TotalTimes = 1000 * npt.TimesByOpearation[npt.Command];
                npt.PrevOperationMs = 0;
            }
        }

        private void CalibBtn_Click(object sender, RoutedEventArgs e)
        {
            NptWorker npt = ((Slot)DataContext).NptWorker;
            if (npt != null)
            {
                npt.Command = NptCommand.Calibrate;
                npt.TotalTimes = 1000 * npt.TimesByOpearation[npt.Command];
                npt.PrevOperationMs = 0;
            }

        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            NptWorker npt = ((Slot)DataContext).NptWorker;
            if (npt != null)
            {
                if( npt.Command == NptCommand.ReadState )
                {
                    npt.Command = NptCommand.StartWorkFlow;
                }
                else
                {
                    npt.Command = NptCommand.AbortOperation;
                }
                
            }
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            ((Slot)DataContext).CleanLog();
        }
    }
}
