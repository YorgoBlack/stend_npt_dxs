using ModbusHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NptMultiSlot
{
    [DataContract]
    [Serializable]
    public class Slot : INotifyPropertyChanged
    {
        public static SolidColorBrush BtnNothingColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3EAD5"));

        [DataMember] public string Name { get; set; }
        [DataMember] public string ConnectorId { get; set; }
        [DataMember] public Dictionary<string, NptParam> FactorySettings { get; set; }
        [DataMember] public Dictionary<string, CalibrateParam> CalibrateSettings { get; set; }
        [DataMember] public bool UseWarmUp { get; set; }
        [DataMember] public bool UseWriteFactorySettings { get; set; }
        [DataMember] public bool CleanLogBeforeStart { get; set; }
        public bool ModeCalibSettings { get; set; }

        public NptWorker NptWorker;

        string _DevParamKey;
        public string DevParamKey
        { get { return _DevParamKey ?? AppManager.Worker.DevTypesList.ElementAt(0).Value.Code; }
            set {
                _DevParamKey = value;
                OnPropertychanged("SensorsAll");
                OnPropertychanged("NptParam");
                OnPropertychanged("NptParamSensor");

                OnPropertychanged("ListSensorsForDXS");
                OnPropertychanged("CalibrateParam");
                OnPropertychanged("CalibrateParamSensor");

                OnPropertychanged("DisplayModeOut");
                OnPropertychanged("DisplayColdSold");

                OnPropertychanged("MaxScale");
                OnPropertychanged("MinScale");
                OnPropertychanged("ErrState");
                OnPropertychanged("Ftime");
                OnPropertychanged("ModeTermResNdx");
                OnPropertychanged("ModeOut");
                OnPropertychanged("ColdSold");

                OnPropertychanged("MaxCalibrate");
                OnPropertychanged("MinCalibrate");

                OnPropertychanged("TimeForDXS");
                OnPropertychanged("TempDXS");
                OnPropertychanged("DeltaTempDXS");
            }
        }

        public ObservableCollection<Sensor> SensorsAll {
            get {
                return new ObservableCollection<Sensor>( AppManager.Worker.GetSensorsByNpt(DevParamKey) );
            }
        }

        public ObservableCollection<Sensor> ListSensorsForDXS
        {
            get
            {
                return AppManager.Worker.GetSensorsForCalibrate(DevParamKey);
            }
        }

        public NptParam NptParam
        {
            get
            {
                return FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
            }
            set
            {
                FactorySettings[DevParamKey] = value ?? AppManager.Worker.GetDefaultParam(DevParamKey);
            }
        }

        public Sensor NptParamSensor
        {
            get
            {
                return NptParam.Sensor ?? AppManager.Worker.GetDefaultSensor(DevParamKey);
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].Sensor = value ?? AppManager.Worker.GetDefaultSensor(DevParamKey);
                FactorySettings[DevParamKey].MaxValue = FactorySettings[DevParamKey].Sensor.MaxScale;
                FactorySettings[DevParamKey].MinValue = FactorySettings[DevParamKey].Sensor.MinScale;

                OnPropertychanged("MaxScale");
                OnPropertychanged("MinScale");
                OnPropertychanged("ErrState");
                OnPropertychanged("Ftime");
                OnPropertychanged("ModeTermResNdx");
                OnPropertychanged("ModeOut");
                OnPropertychanged("ColdSold");

                OnPropertychanged("DisplayColdSold");
                OnPropertychanged("EnableModeTermRes");
            }
        }

        public float MaxScale
        {
            get
            {
                return NptParam.MaxValue;
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].MaxValue = value;
            }
        }
        public float MinScale
        {
            get
            {
                return NptParam.MinValue;
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].MinValue = value;
            }
        }
        public float Ftime
        {
            get
            {
                return NptParam.Ftime;
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].Ftime = value;
            }
        }

        public float ErrState
        {
            get
            {
                return NptParam.GetErrorState();
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].SetErrorState(value);
            }
        }

        public int ModeTermResNdx
        {
            get
            {
                return NptParam.ModeTermResNdx;
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].ModeTermResNdx = value;
            }
        }

        public int ModeOut
        {
            get
            {
                return (NptParam is NptParam_1KEX) ? ((NptParam_1KEX) NptParam).ModeOut : 0;
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                if (FactorySettings[DevParamKey] is NptParam_1KEX)
                {
                    ((NptParam_1KEX) FactorySettings[DevParamKey]).ModeOut = value;
                }
                
                OnPropertychanged("ErrState");
            }
        }

        public bool ColdSold
        {
            get
            {
                return NptParam.ColdSold;
            }
            set
            {
                FactorySettings[DevParamKey] = FactorySettings[DevParamKey] ?? AppManager.Worker.GetDefaultParam(DevParamKey);
                FactorySettings[DevParamKey].ColdSold = value;
            }
        }

        public CalibrateParam CalibrateParam
        {
            get
            {
                return CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
            }
            set
            {
                CalibrateSettings[DevParamKey] = value ?? AppManager.Worker.GetDefaultCalibrateParam();
            }
        }
        public Sensor CalibrateParamSensor
        {
            get
            {
                return CalibrateParam.Sensor ?? AppManager.Worker.GetDefaultSensorForDXS();
            }
            set
            {
                CalibrateSettings[DevParamKey] = CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
                CalibrateSettings[DevParamKey].Sensor = value ?? AppManager.Worker.GetDefaultSensorForDXS();
                CalibrateSettings[DevParamKey].MaxValue = CalibrateSettings[DevParamKey].Sensor.MaxCalibrate;
                CalibrateSettings[DevParamKey].MinValue = CalibrateSettings[DevParamKey].Sensor.MinCalibrate;

                OnPropertychanged("MaxCalibrate");
                OnPropertychanged("MinCalibrate");
                OnPropertychanged("TimeForDXS");
                OnPropertychanged("TempDXS");
                OnPropertychanged("DeltaTempDXS");
            }
        }
        public float MaxCalibrate
        {
            get
            {
                return CalibrateParam.MaxValue;
            }
            set
            {
                CalibrateSettings[DevParamKey] = CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
                CalibrateSettings[DevParamKey].MaxValue = value;
            }
        }
        public float MinCalibrate
        {
            get
            {
                return CalibrateParam.MinValue;
            }
            set
            {
                CalibrateSettings[DevParamKey] = CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
                CalibrateSettings[DevParamKey].MinValue = value;
            }
        }
        public int TimeForDXS
        {
            get
            {
                return CalibrateParam.TimeForDXS;
            }
            set
            {
                CalibrateSettings[DevParamKey] = CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
                CalibrateSettings[DevParamKey].TimeForDXS = value;
            }
        }
        public float TempDXS
        {
            get
            {
                return CalibrateParam.TempDXS;
            }
            set
            {
                CalibrateSettings[DevParamKey] = CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
                CalibrateSettings[DevParamKey].TempDXS = value;
            }
        }
        public float DeltaTempDXS
        {
            get
            {
                return CalibrateParam.DeltaTempDXS;
            }
            set
            {
                CalibrateSettings[DevParamKey] = CalibrateSettings[DevParamKey] ?? AppManager.Worker.GetDefaultCalibrateParam();
                CalibrateSettings[DevParamKey].DeltaTempDXS = value;
            }
        }
        public bool EnableModeTermRes { get { return NptParamSensor.SensorUID.Class == 1; } }
        public Visibility DisplayColdSold { get { return NptParamSensor.SensorUID.Class == 0 ? Visibility.Visible : Visibility.Hidden; } }
        public Visibility DisplayModeOut { get { return DevParamKey == "1KEX" ? Visibility.Visible : Visibility.Hidden; } }

        public int ProgressPos
        {
            get
            {
                OnPropertychanged("TimerTotalCurrent");
                OnPropertychanged("TimerCurrent");
                OnPropertychanged("WarmUpTimer");
                OnPropertychanged("WarmUpTimerVisi");

                bool f = (NptWorker == null) || (WorkFlowState != WFState.Operation);
                if( f && (NptWorker != null) )
                {
                    f = NptWorker.WorkFlow.Count() == 0;
                }

                if ( f ) return 0;
                else
                {
                    long ct = NptWorker.PrevOperationMs + NptWorker.stopWatch.ElapsedMilliseconds;
                    return (int) (NptWorker.TotalTimes == 0 ? 0 : 100 + (int) 100 * (ct - NptWorker.TotalTimes) / NptWorker.TotalTimes);
                }
            }
        }


        public string TimerCurrent {
            get
            {
                if (NptWorker != null)
                {
                    if (WorkFlowState == WFState.Operation)
                    {
                        var timespan = TimeSpan.FromSeconds(NptWorker.stopWatch.ElapsedMilliseconds / 1000);
                        _TimerCurrent = timespan.ToString(@"mm\:ss");
                        return _TimerCurrent;
                    }
                    else
                    {
                        return _TimerCurrent;
                    }
                }
                else
                    return "";
            }
        }
        string _TimerCurrent;
        
        public string TimerTotalCurrent
        {
            get
            {
                if (NptWorker != null)
                {
                    if (WorkFlowState == WFState.Operation)
                    {
                        var timespan = TimeSpan.FromSeconds((NptWorker.PrevOperationMs + NptWorker.stopWatch.ElapsedMilliseconds) / 1000);
                        _TimerTotalCurrent = timespan.ToString(@"mm\:ss");
                        return _TimerTotalCurrent;
                    }
                    else
                    {
                        return _TimerTotalCurrent;
                    }
                }
                else
                    return "";
            }
        }
        public string _TimerTotalCurrent;
        public string WarmUpTimer { get {
                if (NptWorker != null)
                {
                    var timespan = TimeSpan.FromSeconds(CalibrateParam.TimeForDXS - NptWorker.stopWatch.ElapsedMilliseconds/1000);
                    return timespan.ToString(@"mm\:ss");
                }
                else return "";
                
            } }
        public Visibility WarmUpTimerVisi
        {
            get
            {
                return NptWorker == null ? Visibility.Hidden : (NptWorker.Command == NptCommand.WaitWarmingUp ? Visibility.Visible : Visibility.Hidden);
            }
        }

        bool _WorkFlowBtnChecked;
        public bool WorkFlowBtnChecked { get { return _WorkFlowBtnChecked; }
            set {
                _WorkFlowBtnChecked = value;
                _EnableControls = !value;
                OnPropertychanged("EnableControls");
                OnPropertychanged("WorkFlowBtnText");
            } }

        public string WorkFlowBtnText { get { return _WorkFlowBtnChecked ? "СТОП" : "ПУСК"; } }

        public void StopDevice()
        {
            if (NptWorker != null)
            {
                NptWorker.Stop();
            }
        }

        public SolidColorBrush SlotDeviceStateAsColor
        {
            get
            {
                if (string.IsNullOrEmpty(ConnectorId)) return Brushes.LightGray;
                else if (NptWorker == null) return Brushes.Red;
                else return Brushes.LightGreen;
            }
        }

        
        public SolidColorBrush HeartColor { get { return _HeartColor; }
            set { _HeartColor = value;
                if (NptWorker != null) { if (NptWorker.WorkFlow.Count == 0) { _CalibColor = BtnNothingColor; _WriteFactorySettingsColor = BtnNothingColor; _CheckUpColor = BtnNothingColor; } }
                OnPropertychanged("HeartColor"); OnPropertychanged("CalibColor"); OnPropertychanged("WriteFactorySettingsColor"); OnPropertychanged("CheckUpColor");
            } }
        SolidColorBrush _HeartColor;
        
        public SolidColorBrush CalibColor { get { return _CalibColor; }
            set { _CalibColor = value;
                if (NptWorker != null) { if (NptWorker.WorkFlow.Count == 0) { _HeartColor = BtnNothingColor; _WriteFactorySettingsColor = BtnNothingColor; _CheckUpColor = BtnNothingColor; } }
                OnPropertychanged("HeartColor"); OnPropertychanged("CalibColor"); OnPropertychanged("WriteFactorySettingsColor"); OnPropertychanged("CheckUpColor");
            } }
        SolidColorBrush _CalibColor;
        public SolidColorBrush CheckUpColor { get { return _CheckUpColor; }
            set { _CheckUpColor = value;
                if (NptWorker != null) { if (NptWorker.WorkFlow.Count == 0) { _CalibColor = BtnNothingColor; _WriteFactorySettingsColor = BtnNothingColor; _HeartColor = BtnNothingColor; } }
                OnPropertychanged("HeartColor"); OnPropertychanged("CalibColor"); OnPropertychanged("WriteFactorySettingsColor"); OnPropertychanged("CheckUpColor");
            } }
        SolidColorBrush _CheckUpColor;
        public SolidColorBrush WriteFactorySettingsColor { get { return _WriteFactorySettingsColor; }
            set { _WriteFactorySettingsColor = value;
                if (NptWorker != null) { if (NptWorker.WorkFlow.Count == 0) { _CalibColor = BtnNothingColor; _HeartColor = BtnNothingColor; _CheckUpColor = BtnNothingColor; } }
                OnPropertychanged("HeartColor"); OnPropertychanged("CalibColor"); OnPropertychanged("WriteFactorySettingsColor"); OnPropertychanged("CheckUpColor");
            } }
        SolidColorBrush _WriteFactorySettingsColor;

        public ObservableCollection<ColoredText> ProcessLogLines { get; set;}
        public bool ProcessLogScroll { get { return true; } }
        public void Log(string msg, SolidColorBrush Fore)
        {
            var sav = new ObservableCollection<ColoredText>(ProcessLogLines) { new ColoredText() { TheColour = Fore, TheText = msg } };
            ProcessLogLines = sav;
            OnPropertychanged("ProcessLogLines");
            OnPropertychanged("ProcessLogScroll");
        }
        public void CleanLog()
        {
            ProcessLogLines = new ObservableCollection<ColoredText>();
            OnPropertychanged("ProcessLogLines");
        }

        public bool CalibSettingsEnabled { get { return WorkFlowState != WFState.Operation; } }

        WFState _WorkFlowState;
        public WFState WorkFlowState { get { return _WorkFlowState; } 
            set
            {
                _WorkFlowState = value;
                switch(value)
                {
                    case WFState.Nothing:
                        WorkFlowStateMsg = "Ожидание";
                        WorkFlowStateBack = Brushes.LightGray;
                        WorkFlowStateFore = Brushes.Black;
                        WorkFlowBtnChecked = false;
                        CheckUpColor = Slot.BtnNothingColor;
                        HeartColor = Slot.BtnNothingColor;
                        CalibColor = Slot.BtnNothingColor;
                        WriteFactorySettingsColor = Slot.BtnNothingColor;
                        OnPropertychanged("CheckUpColor");
                        OnPropertychanged("HeartColor");
                        OnPropertychanged("CalibColor");
                        OnPropertychanged("WriteFactorySettingsColor");
                        OnPropertychanged("Tempr");
                        break;

                    case WFState.Oki:
                        if (NptWorker.WorkFlow.Count() < 2)
                        {
                            WorkFlowStateMsg = "ОК";
                            WorkFlowStateBack = Brushes.Green;
                            WorkFlowStateFore = Brushes.White;
                        }
                        break;

                    case WFState.Operation:
                        WorkFlowStateMsg = "Операция";
                        WorkFlowStateBack = Brushes.Yellow;
                        WorkFlowStateFore = Brushes.White;
                        break;

                    case WFState.Error:
                        WorkFlowStateMsg = "БРАК";
                        WorkFlowStateBack = Brushes.Red;
                        WorkFlowStateFore = Brushes.White;
                        NptWorker.WorkFlow.Clear();
                        break;
                }
                OnPropertychanged("WorkFlowStateMsg");
                OnPropertychanged("WorkFlowStateBack");
                OnPropertychanged("WorkFlowStateFore");
                OnPropertychanged("CalibSettingsEnabled");
            }
        }
        public string WorkFlowStateMsg { get; set; }
        public SolidColorBrush WorkFlowStateBack { get; set; }
        public SolidColorBrush WorkFlowStateFore { get; set; }

        bool _EnableControls;
        public bool EnableControls { get { return NptWorker == null ? false : _EnableControls; } set {
                _EnableControls = value;
                OnPropertychanged("EnableControls");
                OnPropertychanged("WorkFlowBtnEnabled");
            } }
        public bool WorkFlowBtnEnabled { get { return NptWorker == null ? false : true; } }
        public string DevName { get {
                return NptWorker == null ? "" : NptWorker.GetRegisters().DevName();
            } }
        public string DevSensorName
        { get { return NptWorker == null ? "" : 
                    NptWorker.GetRegisters().GetCurrentParams().Sensor.ToString() + Environment.NewLine; } }
        public string Tempr {
            get {
                OnPropertychanged("DevSensorName");
                OnPropertychanged("TemprDeviation");
                OnPropertychanged("ShowDeviceError");
                OnPropertychanged("DeviceErrorAsText");
                string v = NptWorker == null ? "" : NptWorker.GetRegisters().Tempr.ToString("n4");
                return v == "" ? "" : (ShowDeviceError == Visibility.Visible ? "------" : v);
            }
        }
        public string TemprDeviation
        {
            get
            {
                string v = NptWorker == null ? "" : (CalibrateParam.TempDXS - NptWorker.GetRegisters().Tempr).ToString("n4");
                return v == "" ? "" : (ShowDeviceError == Visibility.Visible ? "------" : v);
            }
        }
        public Visibility ShowDeviceError
        {
            get
            {
                return NptWorker == null ? Visibility.Hidden : (NptWorker.GetRegisters().ShowDeviceError ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public string DeviceErrorAsText
        {
            get
            {
                return NptWorker == null ? "" : NptWorker.GetRegisters().DeviceErrorAsText;
            }
        }
        public void OnDeviceAttached(DevEventArgs e)
        {

            if (!string.IsNullOrEmpty(ConnectorId))
            {
                if( ConnectorId == e.ConnectorId)
                {
                    if(!AppManager.Worker.DevTypesList.ContainsKey(e.NptType) )
                    {
                        throw new Exception("Npt type not supported");
                    }
                    NptWorker = AppManager.Worker.DevTypesList[e.NptType].Device;
                    NptWorker.Slot = this;
                    NptWorker.Connect(e.PortName);
                    NptWorker.ExecuteRead(NptRegisters.Registers._DEVTYPE);
                    NptWorker.ExecuteRead(NptRegisters.Registers._PARAMS);
                    NptWorker.GetRegisters().OnDeviceRegistersRead("PARAMS");
                    NptWorker.Start();
                    OnPropertychanged("SlotDeviceStateAsColor");
                    OnPropertychanged("DevName");
                    OnPropertychanged("WarmUpTimer");
                    EnableControls = true;
                    WorkFlowState = WFState.Nothing;
                    
                }
            } 
        }
        public void OnDeviceRemoving(DevEventArgs e)
        {
            if (!string.IsNullOrEmpty(ConnectorId))
            {
                if (ConnectorId == e.ConnectorId)
                {
                    if (NptWorker != null)
                    {
                        NptWorker.Command = NptCommand.AbortOperation;
                        NptWorker.Stop();

                        EnableControls = false;
                        WorkFlowState = WFState.Nothing;
                        NptWorker = null;

                        if( e.CleanConnectorId )
                        {
                            ConnectorId = null;
                        }

                        OnPropertychanged("Tempr");
                        OnPropertychanged("WorkFlowBtnEnabled");
                        OnPropertychanged("EnableControls");
                        OnPropertychanged("SlotDeviceStateAsColor");
                        OnPropertychanged("DevName");
                        OnPropertychanged("WarmUpTimer");
                    }
                }
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertychanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return "Панель " + Name +   ", разъём " + (!string.IsNullOrEmpty(ConnectorId) ? ConnectorId : " не назначен");
        }

        public override bool Equals(object obj)
        {
            Slot other = obj as Slot;
            if (other == null)
            {
                return false;
            }

            return (Name == other.Name);
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static bool operator ==(Slot me, Slot other)
        {
            return Equals(me, other);
        }

        public static bool operator !=(Slot me, Slot other)
        {
            return Equals(me, other);
        }

    }
}
