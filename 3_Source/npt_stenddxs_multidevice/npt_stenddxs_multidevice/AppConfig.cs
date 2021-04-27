using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml;

namespace NptMultiSlot
{
    [DataContract]
    public class AppConfig : INotifyPropertyChanged
    {
        [DataMember]
        public ObservableCollection<Slot> Slots { get; set; }

        [DataMember]
        public Slot DefaulSlot { get; set; }

        [DataMember] public bool EnableLogFile = false;

        public Slot _SlotSelected;
        public Slot SlotSelected { get { return _SlotSelected; }
            set {
                _SlotSelected = value;
                OnPropertychanged("SlotSelected");
                OnPropertychanged("VisiControls");
            }
        }

        DevEventArgs DevEvent;

        string _ConfigureMsg;
        public string ConfigureMsg { get { return _ConfigureMsg; } set { _ConfigureMsg = value; OnPropertychanged("ConfigureMsg"); } }

        public Visibility VisiControls { get { return SlotSelected == null ? Visibility.Hidden : Visibility.Visible; } }


        public void BindSelectedSlot()
        {
            var list = (from s in Slots where s.ConnectorId == DevEvent.ConnectorId && s.Name != SlotSelected.Name select s).ToList();
            foreach(var s in list)
            {
                s.OnDeviceRemoving(new DevEventArgs() { ConnectorId = DevEvent.ConnectorId, CleanConnectorId = true });
            }
            var slot = (from s in Slots where s.Name == SlotSelected.Name select s).FirstOrDefault();
            slot.ConnectorId = DevEvent.ConnectorId;
            slot.OnDeviceAttached( DevEvent );

            SlotSelected = null;
            ConfigureMsg = "Подключите (переподключите) прибор";
        }
        public void OnDeviceAttached(DevEventArgs e)
        {
            Console.WriteLine("Attached "  + e.PortName + " on " + e.ConnectorId);
            DevEvent = e;

            SlotSelected = (from s in Slots where s.ConnectorId == e.ConnectorId select s).FirstOrDefault();
            if( SlotSelected == null )
            {
                SlotSelected = (from s in Slots where s.ConnectorId == null select s).FirstOrDefault();
                if( SlotSelected == null )
                {
                    SlotSelected = (from s in Slots select s).FirstOrDefault();
                }
            }
            
            
            ConfigureMsg = AppManager.Worker.DevTypesList[e.NptType].DevName + " подключен к " + e.ConnectorId;
            OnPropertychanged("SlotSelected");
        }

        public void OnNewDevEvent(DevEventArgs e)
        {
            ConfigureMsg = "Устройство подключено. Проверяем прибор ...";
        }
        public void OnDeviceNotDetected(DevEventArgs e)
        {
            ConfigureMsg = "Прибор не обнаружен. Для продолжения подключите след.прибор.";
        }

        public void OnDeviceRemoving(DevEventArgs e)
        {
            SlotSelected = null;
            ConfigureMsg = "Подключите (переподключите) прибор";
        }

        public void SaveConfig(string FileName)
        {
            XmlWriter writer = XmlWriter.Create(FileName, new XmlWriterSettings { Indent = true, IndentChars = "\t" });
            (new DataContractSerializer(typeof(AppConfig))).WriteObject(writer, this);
            writer.Close();
        }

        public bool LoadConfig(string FileName, Dictionary<string, Collection<Sensor>> SensorsByNpt, bool force = false)
        {

            bool ret = true;
            XmlReader reader = XmlReader.Create(FileName);
            try
            {
                DataContractSerializer dcs = new DataContractSerializer(typeof(AppConfig));

                AppConfig config = (AppConfig)(new DataContractSerializer(typeof(AppConfig))).ReadObject(reader);

                foreach (var prop in this.GetType().GetProperties())
                {
                    if (prop.CanWrite)
                    {
                        object obj = prop.GetValue(config, null);
                        if (obj != null)
                        {
                            this.GetType().GetProperty(prop.Name).SetValue(this, obj, null);
                        }
                    }
                }
                reader.Close();
                Slots = config.Slots ?? new ObservableCollection<Slot>();
                if( Slots.Count == 0 )
                {
                    for(int i=1; i<=10; i++)
                    {
                        Slots.Add(new Slot() { Name = i.ToString(), UseWarmUp = true, UseWriteFactorySettings = true });
                    }
                }

                for(int ndx = 0; ndx < Slots.Count; ndx++)
                {
                    Slots[ndx].FactorySettings = Slots[ndx].FactorySettings ?? new Dictionary<string, NptParam>();
                    Slots[ndx].CalibrateSettings = Slots[ndx].CalibrateSettings ?? new Dictionary<string, CalibrateParam>();
                    Slots[ndx].ProcessLogLines = new ObservableCollection<ColoredText>();
                    Slots[ndx].WorkFlowState = WFState.Nothing;

                    foreach (var key in SensorsByNpt.Keys)
                    {
                        if( !Slots[ndx].FactorySettings.ContainsKey(key) )
                        {
                            Slots[ndx].FactorySettings.Add(key, null);
                        }
                        if (!Slots[ndx].CalibrateSettings.ContainsKey(key))
                        {
                            Slots[ndx].CalibrateSettings.Add(key, null);
                        }
                    }
                }

                DefaulSlot = DefaulSlot ?? new Slot() { Name = "777" };
                DefaulSlot.FactorySettings = DefaulSlot.FactorySettings ?? new Dictionary<string, NptParam>();
                DefaulSlot.CalibrateSettings = DefaulSlot.CalibrateSettings ?? new Dictionary<string, CalibrateParam>();

                foreach (var key in SensorsByNpt.Keys)
                {
                    if (!DefaulSlot.FactorySettings.ContainsKey(key))
                    {
                        DefaulSlot.FactorySettings.Add(key, null);
                    }
                    if (!DefaulSlot.CalibrateSettings.ContainsKey(key))
                    {
                        DefaulSlot.CalibrateSettings.Add(key, null);
                    }
                }
            }
            catch (Exception e)
            {
                if (force)
                {
                    ret = false;
                }
                else
                {
                    reader.Close();
                    Slots.Clear();
                    XmlWriter writer = XmlWriter.Create(FileName, new XmlWriterSettings { Indent = true, IndentChars = "\t" });
                    (new DataContractSerializer(typeof(AppConfig))).WriteObject(writer, this);
                    writer.Close();
                    LoadConfig(FileName, SensorsByNpt, true);
                }
            }

            return ret;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertychanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}