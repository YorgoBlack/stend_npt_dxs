using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace Helpers
{

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA
    {
        public UInt32 cbSize;
        public Guid ClassGuid;
        public UInt32 DevInst;
        public IntPtr Reserved;
    }

    public static class Win32Helper
    {
        const int DIGCF_DEFAULT = 0x1;
        const int DIGCF_PRESENT = 0x2;
        const int DIGCF_ALLCLASSES = 0x4;
        const int DIGCF_PROFILE = 0x8;
        const int DIGCF_DEVICEINTERFACE = 0x10;

        public static float ParseFloat(string str)
        {
            float val;
            bool oki = float.TryParse(str, out val);
            if (!oki)
            {
                oki = float.TryParse(str.Replace(".", ","), out val);
            }
            return val;
        }
        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static void CleanAttachedDevice(string devId, out string Location)
        {
            Location = "";
        }

        public static bool FindLocationInfo(string devId, out string Location, out string Port)
        {
            Location = "";
            Port = "";
            try
            {
                RegistryKey Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                if (Key == null) Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                RegistryKey Key1 = Key.OpenSubKey("System\\CurrentControlSet\\Enum\\" + devId, false);
                if (Key1 != null)
                {
                    if (Key1.GetValue("LocationInformation") != null)
                    {
                        Location = Key1.GetValue("LocationInformation").ToString();

                        Key1 = Key1.OpenSubKey("Device Parameters", false);
                        if (Key1 != null)
                        {
                            if (Key1.GetValue("PortName") != null)
                            {
                                Port = Key1.GetValue("PortName").ToString();
                            }
                        }
                        string savdevid = devId;
                        // проверка для FTDI 1
                        if ( Port == "" )
                        {
                            devId = "FTDIBUS\\" + devId.Replace("USB\\", "").Replace("&", "+").Replace("\\", "+") + "A\\0000\\Device Parameters";
                            Key1 = Key.OpenSubKey("System\\CurrentControlSet\\Enum\\" + devId, false);
                            if( Key1 != null )
                            {
                                if (Key1.GetValue("PortName") != null)
                                {
                                    Port = Key1.GetValue("PortName").ToString();
                                }
                            }
                        }
                        // проверка для FTDI 2
                        if (Port == "")
                        {
                            devId = ReplaceFirst(savdevid, "&", "+");
                            devId = devId.Replace("\\", "+");
                            devId = devId.Replace("USB+", "FTDIBUS\\") + "\\0000\\Device Parameters";
                            Key1 = Key.OpenSubKey("System\\CurrentControlSet\\Enum\\" + devId, false);
                            if (Key1 != null)
                            {
                                if (Key1.GetValue("PortName") != null)
                                {
                                    Port = Key1.GetValue("PortName").ToString();
                                }
                            }
                        }
                    }
                }


            }
            catch (Exception) { }

            return Location != "" && Port != "";
        }

        public static bool CheckDeviceForLocation(string location, out string Port, out string PNPDeviceID)
        {
            IntPtr h = SetupDiGetClassDevs(0, "USB", IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);

            SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

            Port = "";
            PNPDeviceID = "";
            if (h != null)
            {
                try
                {
                    for (uint devIndex = 0; ; devIndex++)
                    {
                        if (!SetupDiEnumDeviceInfo(h, devIndex, ref devInfoData))
                        {
                            break;
                        }
                        PNPDeviceID = "";
                        int nBytes = 512;
                        int RequiredSize = 0;
                        StringBuilder sb = new StringBuilder(nBytes);
                        if (SetupDiGetDeviceInstanceId(h, ref devInfoData, sb, nBytes, out RequiredSize))
                        {
                            RegistryKey Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                            if (Key == null) Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                            PNPDeviceID = sb.ToString();
                            RegistryKey Key1 = Key.OpenSubKey("System\\CurrentControlSet\\Enum\\" + PNPDeviceID, false);
                            if (Key1 != null)
                            {
                                if (Key1.GetValue("LocationInformation") != null)
                                {
                                    if (Key1.GetValue("LocationInformation").ToString() == location)
                                    {
                                        Key1 = Key1.OpenSubKey("Device Parameters");
                                        Port = "";
                                        if (Key1 != null)
                                        {
                                            if (Key1.GetValue("PortName") != null)
                                            {
                                                Port = Key1.GetValue("PortName").ToString();
                                            }
                                        }
                                        // check for FTDI
                                        if (Port == "")
                                        {
                                            string ftdi = "FTDIBUS\\" + PNPDeviceID.Replace("USB\\", "").Replace("&", "+").Replace("\\", "+") + "A\\0000\\Device Parameters";
                                            Key1 = Key.OpenSubKey("System\\CurrentControlSet\\Enum\\" + ftdi, false);
                                            if (Key1 != null)
                                            {
                                                if (Key1.GetValue("PortName") != null)
                                                {
                                                    Port = Key1.GetValue("PortName").ToString();
                                                    if( Port != null )
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(h);
                }


            }
            return PNPDeviceID != "" && Port != "";
        }

        public static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }

        static Encoding win1251 = Encoding.GetEncoding("windows-1251");
        public static string ToAsciiString(UInt16[] value)
        {
            int p;

            if (value == null) return "";

            List<byte> bytes = new List<byte>();

            for (int i = 0; i < value.Length; i++)
                bytes.AddRange(BitConverter.GetBytes(value[i]).Reverse());

            p = bytes.IndexOf(0);

            if (p < 0)
                return win1251.GetString(bytes.ToArray());
            else if (p == 0)
                return "";
            else
                return win1251.GetString(bytes.GetRange(0, p).ToArray());
        }



        [DllImport("setupapi.dll", CharSet = CharSet.Ansi, EntryPoint = "SetupDiGetClassDevsA")]
        public static extern IntPtr SetupDiGetClassDevs(int ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool SetupDiGetDeviceInstanceId(
           IntPtr DeviceInfoSet,
           ref SP_DEVINFO_DATA DeviceInfoData,
           StringBuilder DeviceInstanceId,
           int DeviceInstanceIdSize,
           out int RequiredSize
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);
    }
}