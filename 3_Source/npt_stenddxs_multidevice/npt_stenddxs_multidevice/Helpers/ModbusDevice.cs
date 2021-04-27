using Cet.IO;
using Cet.IO.Protocols;
using Cet.IO.Serial;
using NptMultiSlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusHelpers
{
    [DataContract]
    [Serializable]
    public class TagContainer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertychanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public interface INotifiedRegisters
    {
        void OnPropertychanged(string PropName);
    }

    public class SerialDevice
    {
        protected SerialPortEx uart = new SerialPortEx();
        protected SerialPortParams prm = new SerialPortParams("9600,N,8,1", false);
        public virtual bool IsConnected()
        {
            return true;
        }
    }

    public class ModbusDevice<T> : NptWorker where T : NptRegisters, INotifyPropertyChanged,INotifiedRegisters, new()
    {
        readonly ModbusCommand FuncWriteMultipleRegisters = new ModbusCommand(ModbusCommand.FuncWriteMultipleRegisters);
        readonly ModbusCommand FuncReadMultipleRegisters = new ModbusCommand(ModbusCommand.FuncReadMultipleRegisters);
        readonly ModbusAdapter<T> adapter = new ModbusAdapter<T>();
        readonly ModbusClient driver = new ModbusClient(new ModbusRtuCodec());
        ICommClient medium = null;
        public T RegistersData;
        Thread mThread;
        volatile bool cancelled = false;

        public override NptRegisters GetRegisters()
        {
            return RegistersData;
        }
        
        public ModbusDevice()
        {
            RegistersData = new T();
        }

        public string PortInfo()
        {
            return uart.BaudRate + "," + uart.Parity + "," + uart.DataBits + "," + uart.StopBits;
        }

        public string PortName { get { return uart.PortName; } }
        public string ErrMessage { get; private set; } = "";

        public override bool Connect(string PortName)
        {
            bool oki = true;
            try
            {
                if (uart.IsOpen)
                {
                    uart.Close();
                }
                uart.SetParams(prm);
                uart.PortName = PortName;
                uart.Open();
                medium = uart.GetClient(prm);
            }
            catch(Exception e)
            {
                ErrMessage = e.Message;
                oki = false;
            }
            return oki;
        }
        public bool DisConnect()
        {
            bool oki = true;
            try
            {
                if( uart.IsOpen )
                {
                    uart.Close();
                }
                medium = null;
            }
            catch (ArgumentException e)
            {
                oki = false;
            }
            catch (Exception e)
            {
                oki = false;
            }
            return oki;
        }

        int GetCountRegsByTagOffset(int offset)
        {
            string name = adapter.Addresses.Where(x => x.Value == offset).Select(x => x.Key).Single();
            if (name == null)
            {
                throw new Exception("Field by address " + offset + " not found");
            }
            T tags = new T();
            FieldInfo info = tags.GetType().GetField(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (info == null)
            {
                throw new Exception("Field by name " + name + " not found");
            }
            return info.GetValue(tags) is Array ? (info.GetValue(tags) as Array).Length : 1;
        }

        public override bool ExecuteWrite(int offset)
        {
            FuncWriteMultipleRegisters.Offset = offset;
            FuncWriteMultipleRegisters.Count = GetCountRegsByTagOffset(offset);
            FuncWriteMultipleRegisters.Data = new ushort[FuncWriteMultipleRegisters.Count];
            adapter.Write(FuncWriteMultipleRegisters, ref RegistersData);
            CommResponse Response = driver.ExecuteGeneric(medium, FuncWriteMultipleRegisters);
            return Response.Status == CommResponse.Ack;
        }
        public override bool ExecuteRead(int offset)
        {
            return ExecuteRead(offset, GetCountRegsByTagOffset(offset) );
        }

        public bool ExecuteRead(int Offset, int Count)
        {
            if (medium == null) throw new Exception("Medium is not yet initialyzed");

            FuncReadMultipleRegisters.Offset = Offset;
            FuncReadMultipleRegisters.Count = Count;

            CommResponse Response = driver.ExecuteGeneric(medium, FuncReadMultipleRegisters, 3, 500);
            if ((Response.Status == CommResponse.Ack) && (Response.Data.IncomingData != null))
            {
                adapter.Read((ModbusCommand)Response.Data.UserData, ref RegistersData);

                foreach (string tagname in adapter.TagUsedLastRW)
                {
                    RegistersData.OnPropertychanged(tagname);
                    RegistersData.OnDeviceRegistersRead(tagname);

                }
                    

                return true;
            }
            else
                return false;
        }

        
        void Run()
        {
            bool connected = false;
            while (!cancelled)
            {
                LastError = "";
                try
                {
                    if (!connected)
                    {
                        connected = Connect(uart.PortName);
                    }
                    if (connected)
                    {
                        connected = false;
                        connected = ExecCommand();
                        Thread.Sleep(200);
                    }
                    else
                        Thread.Sleep(500);
                }
                catch (InvalidOperationException e)
                { LastError = e.Message; }
                catch (UnauthorizedAccessException e)
                { LastError = e.Message; }
                catch (ArgumentOutOfRangeException e)
                { LastError = e.Message; }
                catch (ArgumentException e)
                { LastError = e.Message; }
                catch (Exception e)
                { LastError = e.Message; }
                finally
                {
                    if (LastError != "")
                    {
                        Console.WriteLine(LastError);
                    }
                }
            }
            cancelled = true;

        }

        public override void Stop()
        {
            cancelled = true;
            Thread.Sleep(300);
            DisConnect();
        }
        public override void Start()
        {
            Stop();
            cancelled = false;
            mThread = new Thread(Run);
            mThread.Start();
        }

        public void Dispose()
        {
            Stop();
            //mThread.Abort(this);
            //mThread.Join();
        }



    }


}
