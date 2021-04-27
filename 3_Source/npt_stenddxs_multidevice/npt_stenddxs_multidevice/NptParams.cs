using ModbusHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NptMultiSlot
{

    [Serializable]
    [DataContract]
    public class SensorUID
    {
        [DataMember] public int Class { get; set; }
        [DataMember] public int Senstype { get; set; }
    }


    [Serializable]
    [DataContract]
    public class Sensor : INotifyPropertyChanged
    {
        [DataMember] public string DevType { get; set; }
        [DataMember] public SensorUID SensorUID { get; set; }
        public string Id { get { return SensorUID.Class + "*" + SensorUID.Senstype; } }
        public string UnitMeas { get; set; }
        public float MinScale { get; set; }
        public float MaxScale { get; set; }
        public float MinRangeDisabled { get; set; }
        public float MinRangeEnabled { get; set; }
        public int UseForCalibrate { get; set; }
        public float MinCalibrate { get; set; }
        public float MaxCalibrate { get; set; }

        public string TypeName
        {
            get
            {
                var sensors = AppManager.Worker.SensorsNamesByNpt[DevType];
                string s = (from p in sensors where p.Key.Class == SensorUID.Class && p.Key.Senstype == SensorUID.Senstype select p.Value).First();
                return s;
            }
        }

        public override bool Equals(object obj)
        {
            Sensor other = obj as Sensor;
            if (other == null)
            {
                return false;
            }

            return (Id == other.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Sensor me, Sensor other)
        {
            return Equals(me, other);
        }

        public static bool operator !=(Sensor me, Sensor other)
        {
            return Equals(me, other);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertychanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return TypeName;
        }

        public Sensor Clone()
        {
            return new Sensor()
            {
                DevType = DevType, MaxCalibrate = MaxCalibrate, MinCalibrate = MinCalibrate, MaxScale = MaxScale, MinScale = MinScale,
                MinRangeDisabled = MinRangeDisabled, MinRangeEnabled = MinRangeDisabled, UnitMeas = UnitMeas, UseForCalibrate = UseForCalibrate,
                SensorUID = new SensorUID() { Class = SensorUID.Class, Senstype = SensorUID.Senstype }
            };
        }
    }

    [Serializable]
    [DataContract]
    public class CalibrateParam : TagContainer, INotifyPropertyChanged
    {
        [DataMember] public Sensor Sensor { get; set; }
        [DataMember] public float TempDXS { get; set; }
        [DataMember] public float DeltaTempDXS { get; set; }
        [DataMember] public int TimeForDXS { get; set; }
        [DataMember] public float MinValue { get; set; }
        [DataMember] public float MaxValue { get; set; }

        public CalibrateParam Clone()
        {
            return new CalibrateParam
            {
                Sensor = Sensor.Clone(),
                TempDXS = TempDXS,
                DeltaTempDXS = DeltaTempDXS,
                TimeForDXS = TimeForDXS,
                MaxValue = MaxValue,
                MinValue = MinValue
            };
        }
    }


    [Serializable]
    [DataContract]
    [KnownType(typeof(NptParam_1KEX))]
    public class NptParam : TagContainer, INotifyPropertyChanged
    {
        public NptParam()
        {
            Ftime = 0; ModeTermRes = 2; ColdSold = true;
            ErrOut0 = 22;
        }

        [DataMember] public Sensor Sensor { get; set; }
        [DataMember] public float Ftime { get; set; }
        [DataMember] public float ErrOut0 { get; set; }
        [DataMember] public bool ColdSold { get; set; }
        [DataMember] public float MinValue { get; set; }
        [DataMember] public float MaxValue { get; set; }
        public int ModeTermResNdx { get { return ModeTermRes < 2 ? 0 : ModeTermRes - 2; } set { ModeTermRes = 2 + value; } }

        int _ModeTermRes;
        [DataMember] public int ModeTermRes { get { return _ModeTermRes < 2 ? 2 + _ModeTermRes : _ModeTermRes; } set { _ModeTermRes = value; } }

        public virtual float GetErrorState() { return ErrOut0; }
        public virtual void  SetErrorState(float val) { ErrOut0 = val; }

        public virtual NptParam Clone()
        {
            return new NptParam()
            {
                Sensor = Sensor.Clone(), ColdSold = ColdSold, ErrOut0 = ErrOut0,
                Ftime = Ftime, MaxValue = MaxValue, MinValue = MinValue, ModeTermRes = ModeTermRes 
            };
        }

    }


    [Serializable]
    [DataContract]
    public class NptParam_1KEX : NptParam, INotifyPropertyChanged
    {
        public NptParam_1KEX()
        {
            Ftime = 0; ModeTermRes = 2; ColdSold = true; ModeOut = 0;
            ErrOut0 = 23; ErrOut1 = 23; ErrOut2 = 6; ErrOut3 = 11; ErrOut4 = 5.5f; ErrOut5 = 11; FuncSqrt = 0;
        }

        public override NptParam Clone()
        {
            return new NptParam_1KEX()
            {
                Sensor = Sensor.Clone(),
                ColdSold = ColdSold,
                ErrOut0 = ErrOut0,
                ErrOut1 = ErrOut1,
                ErrOut2 = ErrOut2,
                ErrOut3 = ErrOut3,
                ErrOut4 = ErrOut4,
                ErrOut5 = ErrOut5,
                Ftime = Ftime,
                MaxValue = MaxValue,
                MinValue = MinValue,
                ModeTermRes = ModeTermRes, 
                FuncSqrt = FuncSqrt,
                ModeOut = ModeOut
            };
        }

        [DataMember] public int ModeOut { get; set; }
        [DataMember] public float ErrOut1 { get; set; }
        [DataMember] public float ErrOut2 { get; set; }
        [DataMember] public float ErrOut3 { get; set; }
        [DataMember] public float ErrOut4 { get; set; }
        [DataMember] public float ErrOut5 { get; set; }

        public override float GetErrorState()
        {
            switch (ModeOut)
            {

                case 0: return ErrOut0;
                case 1: return ErrOut1;
                case 2: return ErrOut2;
                case 3: return ErrOut3;
                case 4: return ErrOut4;
                case 5: return ErrOut5;
                default:
                    throw new Exception("Invalid ModeOut Type");
            }
        }
        public override void SetErrorState(float val)
        {
            switch (ModeOut)
            {
                case 0: ErrOut0 = val; break;
                case 1: ErrOut1 = val; break;
                case 2: ErrOut2 = val; break;
                case 3: ErrOut3 = val; break;
                case 4: ErrOut4 = val; break;
                case 5: ErrOut5 = val; break;
                default:
                    throw new Exception("Invalid ModeOut Type");
            }
        }

        public int FuncSqrt { get; set; }
    }
}
