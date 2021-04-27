using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Cet.IO;
using Cet.IO.Protocols;
using Cet.IO.Serial;
using ModbusHelpers;

namespace NptMultiSlot
{

    public class NptRegisters : INotifyPropertyChanged, INotifiedRegisters
    {
        public class Registers
        {
            public static int _DEVTYPE = 0x1000;
            public static int _VERSION = 0x1002;

            public static int _STATE = 0x400;
            public static int _PARAMS = 0x100;
            public static int _CALIB_REZULT = 0x0200;
            public static int _CALIB_CMD = 0x0;
        }

        public UInt16[] _PARAMS;
        public UInt16[] _PARAMS_PREV;
        public UInt16[] _STATE;
        public UInt16[] _CALIB_REZULT;
        public UInt16[] _CALIB_CMD = new UInt16[4];
        public UInt16[] _DEVTYPE = new UInt16[2];
        public int CurrentCalibCommand;
        public string DEVTYPE { get { return Helpers.Win32Helper.ToAsciiString(_DEVTYPE) == "1K  " ? "1KEX" : Helpers.Win32Helper.ToAsciiString(_DEVTYPE); } }

        public UInt16[] _VERSION = new UInt16[2];
        public string VERSION { get { return Helpers.Win32Helper.ToAsciiString(_VERSION); } }

        public virtual NptParam GetCurrentParams() { throw new Exception("Not implemented yet"); }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertychanged(string propertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        public virtual int GetCalibRezult() { throw new Exception("Not implemented yet"); }
        public virtual void SetParams(NptParam nptParam) { throw new Exception("Not implemented yet"); }
        public virtual void SetCalibCmd(ushort cmd, float param) { throw new Exception("Not implemented yet"); }
        public virtual void SetCalibEnterCmd() { throw new Exception("Not implemented yet"); }
        public virtual string DevName() { return AppManager.Worker.DevTypesList.ContainsKey(DEVTYPE) ? AppManager.Worker.DevTypesList[DEVTYPE].DevName : ""; }
        public float Tempr { get; protected set; }
        public float OutCurrent { get; protected set; }
        public ushort DeviceError { get; protected set; }
        public bool ShowDeviceError { get; protected set; }
        public virtual void OnDeviceRegistersRead(string Name) { }

        public virtual void SetCalibParams(CalibrateParam input)
        {
            NptParam param = new NptParam()
            {
                Sensor = new Sensor() { SensorUID = input.Sensor.SensorUID },
                MinValue = input.MinValue,
                MaxValue = input.MaxValue
            };
            SetParams(param);
        }

        public void RestoreParams()
        {
            _PARAMS = (UInt16[])_PARAMS_PREV.Clone();
        }
        public string DeviceErrorAsText
        {
            get
            {
                string DeviceErrMsg = "";
                if ((DeviceError & 1) == 1) DeviceErrMsg += "Датчик не найден, ";
                if ((DeviceError & 2) == 2) DeviceErrMsg += "Ошибка диапазона датчика, ";
                if ((DeviceError & 4) == 4) DeviceErrMsg += "Ошибка инициализации датчика, ";
                if ((DeviceError & 8) == 8) DeviceErrMsg += "Ошибка измерения температуры холодного спая, ";
                if ((DeviceError & 16) == 16) DeviceErrMsg += "Ошибка измерения температуры, ";
                if ((DeviceError & 32) == 32) DeviceErrMsg += "Обрыв термопары, ";
                if ((DeviceError & 64) == 64) DeviceErrMsg += "Ошибка измерения АЦП, ";
                if ((DeviceError & 128) == 128) DeviceErrMsg += "Отказ флэш, ";
                return DeviceErrMsg;
            }
        }
    }

    public class Npt1K_Registers : NptRegisters
    {
        NptParam_1KEX CurrentParams;

        public Npt1K_Registers()
        {
            _PARAMS = new UInt16[24];
            _CALIB_REZULT = new UInt16[6];
            _STATE = new UInt16[0x1D];
        }

        public override NptParam GetCurrentParams()
        {
            return CurrentParams;
        }
        public override void OnDeviceRegistersRead(string Name)
        {
            base.OnDeviceRegistersRead(Name);
            switch(Name)
            {
                case "PARAMS":
                    _PARAMS_PREV = (UInt16[])_PARAMS.Clone();
                    CurrentParams = new NptParam_1KEX(); 
                    CurrentParams.ErrOut5 = ByteArrayHelpers.ReadSingle(_PARAMS, 0);
                    CurrentParams.ErrOut4 = ByteArrayHelpers.ReadSingle(_PARAMS, 2);
                    CurrentParams.ErrOut3 = ByteArrayHelpers.ReadSingle(_PARAMS, 4);
                    CurrentParams.ErrOut2 = ByteArrayHelpers.ReadSingle(_PARAMS, 6);
                    CurrentParams.ErrOut1 = ByteArrayHelpers.ReadSingle(_PARAMS, 8);
                    CurrentParams.ErrOut0 = ByteArrayHelpers.ReadSingle(_PARAMS, 10);
                    CurrentParams.ModeOut = _PARAMS[12];
                    CurrentParams.Ftime = ByteArrayHelpers.ReadSingle(_PARAMS, 13);
                    CurrentParams.ModeTermRes = (byte)_PARAMS[20];
                    CurrentParams.ColdSold = _PARAMS[21] == 1;
                    int sclass = _PARAMS[23], stype = _PARAMS[22];
                    CurrentParams.Sensor = AppManager.Worker.FindSensor(DEVTYPE, new SensorUID() { Class = sclass, Senstype = stype });
                    CurrentParams.MaxValue = ByteArrayHelpers.ReadSingle(_PARAMS, 15);
                    CurrentParams.MinValue = ByteArrayHelpers.ReadSingle(_PARAMS, 17);
                    CurrentParams.FuncSqrt = _PARAMS[19];

                    break;

                case "STATE":
                    DeviceError = _STATE[4];
                    OutCurrent = ByteArrayHelpers.ReadSingle(_STATE, 5);
                    Tempr = ByteArrayHelpers.ReadSingle(_STATE, 7);
                    ShowDeviceError = DeviceError != 0 && DeviceError != 0x800;
                    break;
            }
            
        }

        public override void SetCalibParams(CalibrateParam input)
        {
            NptParam_1KEX param = new NptParam_1KEX()
            {
                Sensor = new Sensor() { SensorUID = input.Sensor.SensorUID },
                MinValue = input.MinValue,
                MaxValue = input.MaxValue
            };
            SetParams(param);
        }

        public override void SetParams(NptParam input0)
        {
            _PARAMS_PREV = (UInt16[])_PARAMS.Clone();
            NptParam_1KEX input = (NptParam_1KEX) input0;

            ByteArrayHelpers.WriteSingle(ref _PARAMS, 0, input.ErrOut5);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 2, input.ErrOut4);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 4, input.ErrOut3);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 6, input.ErrOut2);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 8, input.ErrOut1);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 10, input.ErrOut0);
            _PARAMS[12] = (ushort) input.ModeOut;
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 13, input.Ftime);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 15, input.MaxValue);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 17, input.MinValue);
            _PARAMS[19] = (ushort)input.FuncSqrt;
            _PARAMS[20] = (ushort)input.ModeTermRes;
            _PARAMS[21] = (ushort) (input.ColdSold ? 1 : 0);
            _PARAMS[22] = (ushort)input.Sensor.SensorUID.Senstype;
            _PARAMS[23] = (ushort)input.Sensor.SensorUID.Class;
        }

        public override void SetCalibCmd(ushort cmd, float param)
        {
            CurrentCalibCommand = cmd;
            ByteArrayHelpers.WriteSingle(ref _CALIB_CMD, 0, param);
            _CALIB_CMD[3] = cmd;
        }
        public override void SetCalibEnterCmd()
        {
            SetCalibCmd(49, 0);
        }
        public override int GetCalibRezult()
        {
            return _CALIB_REZULT[5];
        }

    }
    public class Npt3_Registers : NptRegisters, INotifyPropertyChanged
    {

        public Npt3_Registers()
        {
            _STATE = new UInt16[0x1C];
            _PARAMS = new UInt16[10];
            _CALIB_REZULT = new UInt16[2];
        }

        NptParam CurrentParams;
        public override NptParam GetCurrentParams()
        {
            return CurrentParams;
        }


        public override void OnDeviceRegistersRead(string Name)
        {
            base.OnDeviceRegistersRead(Name);

            switch (Name)
            {
                case "PARAMS":
                    _PARAMS_PREV = (UInt16[])_PARAMS.Clone();
                    CurrentParams = new NptParam();
                    int sclass = _PARAMS[9] & 0xf, stype = _PARAMS[9] >> 8;

                    CurrentParams.Sensor = AppManager.Worker.FindSensor(DEVTYPE, new SensorUID() { Class = sclass, Senstype = stype });
                    CurrentParams.MaxValue = ByteArrayHelpers.ReadSingle(_PARAMS, 2);
                    CurrentParams.MinValue = ByteArrayHelpers.ReadSingle(_PARAMS, 0);
                    CurrentParams.SetErrorState(ByteArrayHelpers.ReadSingle(_PARAMS, 4));
                    CurrentParams.Ftime = ByteArrayHelpers.ReadSingle(_PARAMS, 6);
                    CurrentParams.ModeTermRes = _PARAMS[8] >> 8;
                    CurrentParams.ColdSold = (_PARAMS[8] & 0xf) == 1;
                    break;

                case "STATE":
                    OutCurrent = ByteArrayHelpers.ReadSingle(_STATE, 0x18);
                    Tempr = ByteArrayHelpers.ReadSingle(_STATE, 0x14);
                    DeviceError = (ushort)(_STATE[0x1A] << 16);
                    DeviceError |= _STATE[0x1B];
                    ShowDeviceError = (((DeviceError & 64) == 64) || ((DeviceError & 128) == 128));
                    break;
            }
        }



        public override void SetParams(NptParam input)
        {
            _PARAMS_PREV = (UInt16[])_PARAMS.Clone();

            ByteArrayHelpers.WriteSingle(ref _PARAMS, 0, input.MinValue);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 2, input.MaxValue);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 4, input.GetErrorState() );
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 6, input.Ftime);
            _PARAMS[8] = (ushort)(input.ModeTermRes << 8);
            _PARAMS[8] |= (ushort) (input.ColdSold ? 0 : 1);
            _PARAMS[9] = (ushort)(input.Sensor.SensorUID.Senstype << 8);
            _PARAMS[9] |= (ushort)input.Sensor.SensorUID.Class;
        }
        public override void SetCalibCmd(ushort cmd, float param)
        {
            CurrentCalibCommand = cmd;
            _CALIB_CMD[0] = (ushort)(cmd << 16);
            _CALIB_CMD[1] = (ushort)(cmd & 0xff);
            ByteArrayHelpers.WriteSingle(ref _CALIB_CMD, 2, param);
        }
        public override void SetCalibEnterCmd()
        {
            SetCalibCmd(40, 0);
        }

        public override int GetCalibRezult()
        {
            return (_CALIB_REZULT[0] << 16) + _CALIB_REZULT[1];
        }

    }
    public class Npt3Ex_Registers : NptRegisters
    {
        public Npt3Ex_Registers()
        {
            _STATE = new UInt16[0x1C];
            _PARAMS = new UInt16[10];
            _CALIB_REZULT = new UInt16[2];
        }

        NptParam CurrentParams;
        public override NptParam GetCurrentParams()
        {
            return CurrentParams;
        }

        public override void OnDeviceRegistersRead(string Name)
        {
            base.OnDeviceRegistersRead(Name);

            switch (Name)
            {
                case "PARAMS":
                    _PARAMS_PREV = (UInt16[])_PARAMS.Clone();
                    CurrentParams = new NptParam();
                    int sclass = _PARAMS[9] & 0xf, stype = _PARAMS[9] >> 8;

                    CurrentParams.Sensor = AppManager.Worker.FindSensor(DEVTYPE, new SensorUID() { Class = sclass, Senstype = stype });
                    CurrentParams.Sensor.MaxScale = ByteArrayHelpers.ReadSingle(_PARAMS, 2);
                    CurrentParams.Sensor.MinScale = ByteArrayHelpers.ReadSingle(_PARAMS, 0);
                    CurrentParams.SetErrorState( ByteArrayHelpers.ReadSingle(_PARAMS, 4));
                    CurrentParams.Ftime = ByteArrayHelpers.ReadSingle(_PARAMS, 6);
                    CurrentParams.ModeTermRes = _PARAMS[8] >> 8;
                    CurrentParams.ColdSold = (_PARAMS[8] & 0xf) == 1;
                    break;

                case "STATE":
                    OutCurrent = ByteArrayHelpers.ReadSingle(_STATE, 0x18);
                    Tempr = ByteArrayHelpers.ReadSingle(_STATE, 0x14);
                    DeviceError = (ushort)(_STATE[0x1A] << 16);
                    DeviceError |= _STATE[0x1B];
                    ShowDeviceError = (((DeviceError & 64) == 64) || ((DeviceError & 128) == 128));
                    break;
            }
        }

        public override void SetParams(NptParam input)
        {
            _PARAMS_PREV = (UInt16[])_PARAMS.Clone();

            ByteArrayHelpers.WriteSingle(ref _PARAMS, 0, input.Sensor.MinScale);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 2, input.Sensor.MaxScale);
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 4, input.GetErrorState() );
            ByteArrayHelpers.WriteSingle(ref _PARAMS, 6, input.Ftime);
            _PARAMS[8] = (ushort)(input.ModeTermRes << 8);
            _PARAMS[8] |= (ushort)(input.ColdSold ? 0 : 1);
            _PARAMS[9] = (ushort)(input.Sensor.SensorUID.Senstype << 8);
            _PARAMS[9] |= (ushort)input.Sensor.SensorUID.Class;
        }

        public override void SetCalibCmd(ushort cmd, float param)
        {
            CurrentCalibCommand = cmd;
            _CALIB_CMD[0] = (ushort)(cmd << 16);
            _CALIB_CMD[1] = (ushort)(cmd & 0xff);
            ByteArrayHelpers.WriteSingle(ref _CALIB_CMD, 2, param);
        }
        public override int GetCalibRezult()
        {
            return (_CALIB_REZULT[0] << 16) + _CALIB_REZULT[1];
        }
        public override void SetCalibEnterCmd()
        {
            SetCalibCmd(40, 0);
        }

    }
}
