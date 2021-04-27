using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Helpers;
using ModbusHelpers;

namespace NptMultiSlot
{
    public class DevTypeCode
    { public string Code; public string DevName; public string IniFileName; public string ClassName; public NptWorker Device; }

    public class DevEventArgs : EventArgs
    {
        public DevEventArgs() { CleanConnectorId = false; }
        public string ConnectorId;
        public string PortName;
        public string NptType;
        public string PNPDeviceID;
        public bool CleanConnectorId;
    }
    public class AppManager
    {
        public event DevAttachedHandler DevAttachedEvent;
        public event DevAttachedHandler NewDevEvent;
        public event DevAttachedHandler DevRemovingEvent;
        public event DevAttachedHandler DevNotDetectedEvent;

        Dictionary<string, string> AttachedDevices = new Dictionary<string, string>();
        public delegate void DevAttachedHandler(DevEventArgs e);

        public static AppManager Worker { get; private set; } = new AppManager();
        public string CurrentFileName { get; private set; }
        public string LogFileName { get; private set; }
        public AppConfig AppConfig { get; set; } = new AppConfig();

        public Dictionary<string, DevTypeCode> DevTypesList = new Dictionary<string, DevTypeCode>
        {
            { "1KEX", new DevTypeCode() { Code = "1KEX", DevName = "НПТ-1К.00.1.3", IniFileName = "NPT1KEX.ini", Device = new ModbusDevice<Npt1K_Registers>() } },
            { "NPT3", new DevTypeCode() { Code = "NPT3", DevName = "НПТ-3.00.1.2", IniFileName = "NPT3.ini", Device = new ModbusDevice<Npt3_Registers>() } },
            { "3.Ex", new DevTypeCode() { Code = "3.Ex", DevName = "НПТ-3.00.1.2.Ех", IniFileName = "NPT3EX.ini",  Device = new ModbusDevice<Npt3Ex_Registers>()} }
        };

        public Dictionary<string, SensorUID> DefaultSensorByDev = new Dictionary<string, SensorUID>
        {
            { "1KEX", new SensorUID() { Class = 0, Senstype = 0 } },
            { "NPT3", new SensorUID() { Class = 0, Senstype = 0 } },
            { "3.Ex", new SensorUID() { Class = 0, Senstype = 0 } }
        };

        Dictionary<string, Collection<Sensor>> SensorsByNpt = new Dictionary<string, Collection<Sensor>>
        {
            { "1KEX", new Collection<Sensor>() },
            { "NPT3", new Collection<Sensor>() },
            { "3.Ex", new Collection<Sensor>() }
        };

        public Dictionary<string, Dictionary<SensorUID,string>> SensorsNamesByNpt = new Dictionary<string, Dictionary<SensorUID, string>>
        {
            { "1KEX", new Dictionary<SensorUID, string>() },
            { "NPT3", new Dictionary<SensorUID, string>() },
            { "3.Ex", new Dictionary<SensorUID, string>() }
        };

        AppManager()
        {
            AppConfig = new AppConfig();

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string appname = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductName;
            if (!System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OWEN"))
                System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OWEN");
            if (!System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OWEN\\" + appname))
                System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OWEN\\" + appname);
            CurrentFileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OWEN\\" + appname + "\\settings.xml";
            LogFileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\OWEN\\" + appname + "\\app.log";

            if (!System.IO.File.Exists(CurrentFileName))
            {
                AppConfig.SaveConfig(CurrentFileName);
            }

            
            DevAttachedEvent += AppConfig.OnDeviceAttached;
            DevRemovingEvent += AppConfig.OnDeviceRemoving;
            NewDevEvent += AppConfig.OnNewDevEvent;
            DevNotDetectedEvent += AppConfig.OnDeviceNotDetected;

            LoadSensors();
        }

        private ManagementEventWatcher watcherAttach;
        private ManagementEventWatcher watcherRemove;

        public void WriteLog(string msg)
        {
            if (AppConfig != null)
            {
                if (AppConfig.EnableLogFile)
                {
                    lock (LogFileName)
                    {
                        System.IO.File.AppendAllText(LogFileName, msg + Environment.NewLine);
                    }
                }
            }
        }

        public void AttachUsbEvents()
        {
            watcherAttach = new ManagementEventWatcher();
            watcherAttach.EventArrived += DeviceAttaching;
            watcherAttach.Query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            watcherAttach.Start();

            watcherRemove = new ManagementEventWatcher();
            watcherRemove.EventArrived += new EventArrivedEventHandler(DeviceRemoving);
            watcherRemove.Query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            watcherRemove.Start();

        }
        private void DeviceAttaching(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string PNPDeviceID = instance.Properties["PNPDeviceID"].Value.ToString();
            WriteLog("Attached " + PNPDeviceID);
            bool rez = Helpers.Win32Helper.FindLocationInfo(PNPDeviceID, out string connectorid, out string portname);
            WriteLog("Find, connectorid:" + connectorid + " portname:" + portname);
            if (rez)
            {
                NewDevEvent(new DevEventArgs());
                TryConnectNpt(portname, connectorid, PNPDeviceID);
            }
        }
        private void DeviceRemoving(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string PNPDeviceID = instance.Properties["PNPDeviceID"].Value.ToString();
            string connectorid = (from x in AttachedDevices where x.Value == PNPDeviceID select x.Key).SingleOrDefault<string>();

            if (connectorid != null)
            {
                AttachedDevices.Remove(connectorid);
                Helpers.Win32Helper.CleanAttachedDevice(PNPDeviceID, out string connId_removed);
                DevRemovingEvent(new DevEventArgs() { ConnectorId = connectorid });
            }
        }

        public void CheckConnectedDevices()
        {
            foreach(var slot in AppConfig.Slots)
            {
                if( !string.IsNullOrEmpty(slot.ConnectorId) )
                {
                    string Port;
                    string PNPDeviceID;
                    if ( Win32Helper.CheckDeviceForLocation(slot.ConnectorId, out Port, out PNPDeviceID) )
                    {
                        TryConnectNpt(Port,slot.ConnectorId,PNPDeviceID);
                    }
                    
                }
            }
        }

        public void TryConnectNpt(string portname, string connectorId, string PNPDeviceID)
        {
            ModbusDevice<NptRegisters> dev = new ModbusDevice<NptRegisters>();
            WriteLog("Try connect " + PNPDeviceID + " on " + portname);
            if (dev.Connect(portname))
            {
                WriteLog("Port Info for " + PNPDeviceID + ":"+ dev.PortInfo());
                System.Threading.Thread.Sleep(1000);

                WriteLog("Read DEVTYPE " + PNPDeviceID + " from " + NptRegisters.Registers._DEVTYPE);
                if (dev.ExecuteRead(NptRegisters.Registers._DEVTYPE, 2))
                {
                    System.Threading.Thread.Sleep(50);
                    WriteLog("Read VERSION " + PNPDeviceID + " from " + NptRegisters.Registers._VERSION);
                    if (dev.ExecuteRead(NptRegisters.Registers._VERSION, 2))
                    {
                        WriteLog("Check DEVTYP " + PNPDeviceID + " for " + dev.RegistersData.DEVTYPE);
                        if (DevTypesList.ContainsKey(dev.RegistersData.DEVTYPE))
                        {
                            dev.DisConnect();
                            AttachedDevices.Add(connectorId, PNPDeviceID);
                            DevAttachedEvent(new DevEventArgs() { ConnectorId = connectorId, PortName = portname, NptType = dev.RegistersData.DEVTYPE, PNPDeviceID = PNPDeviceID });
                            return;
                        }
                    }
                }
            }
            DevNotDetectedEvent(new DevEventArgs());
            dev.DisConnect();
        }

        public void StopDevices()
        {
            foreach(var slot in AppConfig.Slots)
            {
                slot.StopDevice();
            }
        }
        public void SaveConfig(string FileName)
        {
            this.SaveConfig(FileName);
        }

        public void SaveConfig()
        {
            AppConfig.SaveConfig(CurrentFileName);
        }

        public bool LoadConfig()
        {
            return this.LoadConfig(CurrentFileName);
        }

        public ObservableCollection<Sensor> GetSensorsByNpt(string DevParamKey)
        {
            return new ObservableCollection<Sensor>( SensorsByNpt[DevParamKey] );
        }
        public ObservableCollection<Sensor> GetSensorsForCalibrate(string DevParamKey)
        {
            List<Sensor> l = (from p in SensorsByNpt["1KEX"] where p.UseForCalibrate == 1 select p).ToList<Sensor>();
            return new ObservableCollection<Sensor>(l);
        }

        public string FindSensorTypeName(int devndx, SensorUID uid)
        {
            return (from p in SensorsByNpt.ElementAt(devndx).Value where p.SensorUID.Class == uid.Class && p.SensorUID.Senstype == uid.Senstype select p.TypeName).
                SingleOrDefault<string>();
        }

        public Sensor FindSensor(string key, SensorUID sensorUID)
        {
            if (!SensorsByNpt.ContainsKey(key)) throw new Exception("Не найден прибор типа " + key);
            Sensor sensor = (from p in SensorsByNpt[key] where p.SensorUID.Class == sensorUID.Class && p.SensorUID.Senstype == sensorUID.Senstype select p).FirstOrDefault<Sensor>();
            if (sensor == null) throw new Exception("Не найден датчик " + sensorUID.Class + ":" + sensorUID.Senstype + " для прибора " + key);
            return sensor;
        }

        public Sensor FindSensorForDXS(string key, SensorUID sensorUID)
        {
            if (!SensorsByNpt.ContainsKey(key)) throw new Exception("Не найден прибор типа " + key);
            Sensor sensor = (from p in SensorsByNpt[key] where p.UseForCalibrate == 1 && p.SensorUID.Class == sensorUID.Class && p.SensorUID.Senstype == sensorUID.Senstype select p).FirstOrDefault<Sensor>();
            if (sensor == null) throw new Exception("Не найден датчик " + sensorUID.Class + ":" + sensorUID.Senstype + " для прибора " + key);
            return sensor;
        }

        public Sensor FindDefaultSensorForDXS(string key)
        {
            Sensor sensor = (from p in SensorsByNpt[key] where p.UseForCalibrate == 1 select p).FirstOrDefault<Sensor>();
            return sensor;
        }

        public Sensor GetDefaultSensorForDXS()
        {
            Sensor s = FindDefaultSensorForDXS("1KEX");
            return new Sensor()
            {
                SensorUID = s.SensorUID,
                DevType = "1KEX", 
                MaxCalibrate = s.MaxCalibrate,
                MinCalibrate = s.MinCalibrate,
                MaxScale = s.MaxScale,
                MinScale = s.MinScale,
                MinRangeDisabled = s.MinRangeDisabled,
                MinRangeEnabled = s.MinRangeEnabled,
                UnitMeas = s.UnitMeas,
                UseForCalibrate = s.UseForCalibrate
            };
        }

        public CalibrateParam GetDefaultCalibrateParam()
        {
            Sensor s = GetDefaultSensorForDXS();
            return new CalibrateParam()
            {
                Sensor = s,
                MaxValue = s.MaxCalibrate, MinValue = s.MinCalibrate, DeltaTempDXS = 0.1f, TempDXS = 0, TimeForDXS = 10
            };
        }

        public Sensor GetDefaultSensor(string key)
        {
            Sensor s = FindSensor(key, Worker.DefaultSensorByDev[key]);
            return new Sensor() {
                SensorUID = s.SensorUID,  DevType = key,
                MaxCalibrate = s.MaxCalibrate, MinCalibrate = s.MinCalibrate,
                MaxScale = s.MaxScale, MinScale = s.MinScale,
                MinRangeDisabled = s.MinRangeDisabled, MinRangeEnabled = s.MinRangeEnabled,
                UnitMeas = s.UnitMeas, UseForCalibrate = s.UseForCalibrate };
        }
        public NptParam GetDefaultParam(string key)
        {
            Sensor s = FindSensor(key, Worker.DefaultSensorByDev[key]);
            return key == "1KEX" ? 
                new NptParam_1KEX() { Sensor = s, MinValue = s.MinScale, MaxValue = s.MaxScale } : 
                new NptParam() { Sensor = s, MinValue = s.MinScale, MaxValue = s.MaxScale };
        }

        public int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }

        bool LoadSensors()
        {
            bool rez = true;
            string errmsg = "";
            Helpers.IniFile snames_ini = new Helpers.IniFile("SensorData\\Russian.lng");

            foreach(var key in SensorsByNpt.Keys)
            {
                SensorsByNpt[key].Clear();
                SensorsNamesByNpt[key].Clear();

                Helpers.IniFile iniFile = new Helpers.IniFile("SensorData\\" + DevTypesList[key].IniFileName);
                string buf = iniFile.Read("QuantitySensorsType", "Global");
                int cnt = int.Parse(buf);
                List<Sensor> List = new List<Sensor>();
                for (int i = 1; i <= cnt; i++)
                {
                    int ndx;
                    try
                    {
                        if (key == "1KEX")
                        {
                            ndx = i + 299;
                        }
                        else
                        {
                            ndx = 149 + int.Parse(iniFile.Read("TypeName", "Sensor" + i));
                        }
                        SensorUID sensor = new SensorUID()
                        {
                            Class = int.Parse(iniFile.Read("sClass", "Sensor" + i) ?? "0"),
                            Senstype = int.Parse(iniFile.Read("senstype", "Sensor" + i) ?? "0"),
                        };

                        SensorsNamesByNpt[key].Add(sensor, snames_ini.Read(ndx.ToString(), "text"));

                        SensorsByNpt[key].Add (new Sensor()
                        {
                            DevType = key, 
                            SensorUID = sensor,
                            UnitMeas = iniFile.Read("UnitMeas", "Sensor" + i) ?? "0",
                            UseForCalibrate = int.Parse(iniFile.Read("UseForCalibrate", "Sensor" + i) ?? "0"),
                            MinScale = Win32Helper.ParseFloat(iniFile.Read("minScale", "Sensor" + i) ?? "0"),
                            MaxScale = Win32Helper.ParseFloat(iniFile.Read("maxScale", "Sensor" + i) ?? "0"),
                            MinRangeDisabled = Win32Helper.ParseFloat(iniFile.Read("minRangeDisabled", "Sensor" + i) ?? "0"),
                            MinRangeEnabled = Win32Helper.ParseFloat(iniFile.Read("minRangeEnabled", "Sensor" + i) ?? "0"),
                            MinCalibrate = Win32Helper.ParseFloat(iniFile.Read("minCalibrate", "Sensor" + i) ?? "0"),
                            MaxCalibrate = Win32Helper.ParseFloat(iniFile.Read("maxCalibrate", "Sensor" + i) ?? "0"),
                        });

                    }
                    catch (Exception e0)
                    {
                        rez = false;
                        errmsg = GetLineNumber(e0) + ":" + e0.Message;
                        break;
                    }
                }
            }
            if (!rez)
            {
                MessageBox.Show("Ошибка чтения настроек." + errmsg);
                Environment.Exit(1);
            }
            return rez;
        }

        public bool LoadConfig(string FileName)
        {
            bool rez = AppConfig.LoadConfig(FileName, SensorsByNpt);
            if( rez )
            {
                foreach (var slot in AppConfig.Slots)
                {
                    DevAttachedEvent += slot.OnDeviceAttached;
                    DevRemovingEvent += slot.OnDeviceRemoving;
                }
            }
            if ( !rez)
            {
                MessageBox.Show("Ошибка загрузки конфигурации");
                Environment.Exit(1);
            }
            return rez;
        }
    }
}
