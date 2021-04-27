using Helpers;
using ModbusHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NptMultiSlot
{
    public enum NptCommand
    {
        ReadState, StartWorkFlow, EndWorkFlow, WriteFactorySettings, Calibrate, WarmingUp, СheckUp, WaitWarmingUp, WaitCalibRez, WaitCheckUp, AbortOperation
    }

    public enum WFState
    {
        Nothing, Operation, Error, Oki
    }

    public abstract class NptWorker : SerialDevice
    {
        public Stopwatch stopWatch = new Stopwatch();

        public Slot Slot;

        protected string LastError;
        public abstract bool ExecuteRead(int offset);
        public abstract bool ExecuteWrite(int offset);
        public abstract bool Connect(string PortName);
        public abstract void Stop();
        public abstract void Start();

        public abstract NptRegisters GetRegisters();

        public long PrevOperationMs = 0;

        public long TotalTimes = 0;

        public Dictionary<NptCommand, int> TimesByOpearation = new Dictionary<NptCommand, int>()
        { {NptCommand.WarmingUp,10}, {NptCommand.Calibrate,4}, { NptCommand.СheckUp,6}, {NptCommand.WriteFactorySettings,3 } };

        public NptCommand Command_prev { get; private set; }
        NptCommand Command_curr;
        bool LastIOResult = true;
        public Queue<NptCommand> WorkFlow = new Queue<NptCommand>();
        
        public NptCommand Command { get { return Command_curr; }
            set {
                Command_prev = Command_curr;
                Command_curr = value;
            } }
        protected bool ExecCommand()
        {
            TimesByOpearation[NptCommand.WarmingUp] = Slot.CalibrateParam.TimeForDXS;

            if( LastError != "" )
            {
                Slot.OnDeviceRemoving(new DevEventArgs());
            }

            if( (new List<NptCommand>() { NptCommand.WarmingUp, NptCommand.СheckUp, NptCommand.Calibrate, NptCommand.WriteFactorySettings, NptCommand.StartWorkFlow}).
                Contains(Command) )
            {
                Slot.WorkFlowState = WFState.Operation;
                Slot.EnableControls = false;
                Slot.WorkFlowBtnChecked = true;
                
                if ( Slot.CleanLogBeforeStart  && (WorkFlow.Count() == 0) )
                {
                    Slot.CleanLog();
                }

            }
            else if(Command == NptCommand.AbortOperation)
            {
                Command = NptCommand.ReadState;
                Slot.WorkFlowBtnChecked = false;
                Slot.OnPropertychanged("WarmUpTimerVisi");
                WorkFlow.Clear();
                if( Slot.WorkFlowState == WFState.Operation)
                {
                    Slot.WorkFlowState = WFState.Nothing;
                }
            }

            switch (Command)
            {
                case NptCommand.WaitWarmingUp:
                    {
                        ExecuteRead(NptRegisters.Registers._STATE);
                        Slot.OnPropertychanged("Tempr");
                        Slot.OnPropertychanged("ProgressPos");

                        if (stopWatch.ElapsedMilliseconds > (1000 * TimesByOpearation[NptCommand.WarmingUp]) )
                        {
                            Slot.WorkFlowBtnChecked = false;
                            Slot.Log("Восстановление параметров прибора", Brushes.Black);
                            Slot.Log("Прогрев завершён", Brushes.Black);
                            GetRegisters().RestoreParams();
                            ExecuteWrite(NptRegisters.Registers._PARAMS);
                            Slot.OnPropertychanged("WarmUpTimerVisi");

                            Slot.HeartColor = Brushes.Green;

                            Slot.OnPropertychanged("ProgressPos");
                            Slot.WorkFlowState = WFState.Oki;
                            Thread.Sleep(100);
                            PrevOperationMs += stopWatch.ElapsedMilliseconds;
                            Command = WorkFlow.Count == 0 ? NptCommand.ReadState : WorkFlow.Dequeue();
                        }
                        break;
                    }

                case NptCommand.WarmingUp:
                    {
                        Slot.HeartColor = Brushes.Yellow;
                        Slot.Log("Запись параметров калибровки", Brushes.Black);
                        ExecuteRead(NptRegisters.Registers._PARAMS);
                        GetRegisters().OnDeviceRegistersRead("PARAMS");
                        // записываем настройки каибровки 
                        Slot.DevParamKey = GetRegisters().DEVTYPE;
                        GetRegisters().SetCalibParams(Slot.CalibrateParam);
                        LastIOResult = ExecuteWrite(NptRegisters.Registers._PARAMS);
                        if (LastIOResult)
                        {
                            Slot.Log("Прогрев", Brushes.Black);
                            stopWatch.Restart();
                            Command = NptCommand.WaitWarmingUp;
                            Slot.OnPropertychanged("ProgressPos");
                        }
                        else
                        {
                            Slot.HeartColor = Brushes.Red;
                            Slot.Log("Не удалось записать параметры калибровки", Brushes.Red);
                            Command = NptCommand.ReadState;
                            Slot.WorkFlowState = WFState.Error;
                            Slot.WorkFlowBtnChecked = false;
                        }

                        break;
                    }

                case NptCommand.WriteFactorySettings:
                    {
                        Slot.WriteFactorySettingsColor = Brushes.Yellow;
                        stopWatch.Restart();
                        Slot.OnPropertychanged("ProgressPos");
                        Slot.DevParamKey = GetRegisters().DEVTYPE;
                        GetRegisters().SetParams(Slot.NptParam);
                        LastIOResult = ExecuteWrite(NptRegisters.Registers._PARAMS);
                        Thread.Sleep(1000);
                        Slot.OnPropertychanged("ProgressPos");
                        if (!LastIOResult)
                        {
                            Slot.Log("Ошибка записи ЗУ", Brushes.Red);
                            Slot.WriteFactorySettingsColor = Brushes.Red;
                            Slot.WorkFlowState = WFState.Error;
                        }
                        else
                        {
                            Slot.Log("Запись ЗУ - успешно", Brushes.Black);
                            ExecuteRead(NptRegisters.Registers._PARAMS);
                            GetRegisters().OnDeviceRegistersRead("PARAMS");
                            Slot.WorkFlowState = WFState.Oki;
                            Slot.WriteFactorySettingsColor = Brushes.Green;
                        }
                        Slot.WorkFlowBtnChecked = false;
                        Slot.OnPropertychanged("ProgressPos");
                        Thread.Sleep(100);
                        PrevOperationMs += stopWatch.ElapsedMilliseconds;
                        Command = WorkFlow.Count == 0 ? NptCommand.ReadState : WorkFlow.Dequeue();
                        break;
                    }

                case NptCommand.WaitCheckUp:
                    {
                        Slot.OnPropertychanged("ProgressPos");
                        NptCommand rez_cmd = NptCommand.ReadState;
                        if (stopWatch.ElapsedMilliseconds > (1000*TimesByOpearation[NptCommand.СheckUp]) )
                        {
                            LastIOResult = ExecuteRead(NptRegisters.Registers._STATE);
                            if (!LastIOResult)
                            {
                                Slot.CheckUpColor = Brushes.Red;
                                Slot.Log("Ошибка чтения ", Brushes.Red);
                                Slot.WorkFlowState = WFState.Error;
                                rez_cmd = NptCommand.ReadState;
                                WorkFlow.Clear();
                            }
                            else
                            {
                                GetRegisters().OnDeviceRegistersRead("STATE");
                                float eps = (GetRegisters().Tempr - Slot.TempDXS);
                                Slot.DevParamKey = GetRegisters().DEVTYPE;
                                if (Math.Abs(eps) > Slot.CalibrateParam.DeltaTempDXS)
                                {
                                    Slot.CheckUpColor = Brushes.Red;
                                    Slot.Log("Ошибка", Brushes.Red);
                                    Slot.WorkFlowState = WFState.Error;
                                    rez_cmd = NptCommand.ReadState;
                                    WorkFlow.Clear();
                                }
                                else
                                {
                                    Slot.CheckUpColor = Brushes.Green;
                                    Slot.Log("Успешно", Brushes.Black);
                                    Slot.WorkFlowState = WFState.Oki;
                                    rez_cmd = WorkFlow.Count == 0 ? NptCommand.ReadState : WorkFlow.Dequeue();
                                }
                            }
                            Slot.Log("Восстановление параметров прибора", Brushes.Black);
                            GetRegisters().RestoreParams();
                            ExecuteWrite(NptRegisters.Registers._PARAMS);

                            Slot.WorkFlowBtnChecked = false;
                            Slot.OnPropertychanged("ProgressPos");
                            Thread.Sleep(100);
                            PrevOperationMs += stopWatch.ElapsedMilliseconds;
                            Command = rez_cmd;
                        }

                        break;
                    }

                case NptCommand.СheckUp:
                    {
                        Slot.CheckUpColor = Brushes.Yellow;
                        ExecuteRead(NptRegisters.Registers._PARAMS);
                        GetRegisters().OnDeviceRegistersRead("PARAMS");
                        // записываем настройки каибровки 
                        Slot.Log("Запись параметров калибровки", Brushes.Black);
                        Slot.DevParamKey = GetRegisters().DEVTYPE;
                        GetRegisters().SetCalibParams(Slot.CalibrateParam);
                        LastIOResult = ExecuteWrite(NptRegisters.Registers._PARAMS);
                        Slot.Log("Проверка", Brushes.Black);
                        stopWatch.Restart();
                        Slot.OnPropertychanged("ProgressPos");

                        if (LastIOResult && !GetRegisters().ShowDeviceError)
                        {
                            Command = NptCommand.WaitCheckUp;
                            break;
                        }
                        if (GetRegisters().ShowDeviceError)
                        {
                            Slot.Log("Ошибка прибора, не удалось выполнить проверку", Brushes.Red);
                        }
                        else
                        {
                            Slot.Log("Не удалось записать параметры калибровки", Brushes.Red);
                        }
                        Slot.Log("Восстановление параметров прибора", Brushes.Black);
                        GetRegisters().RestoreParams();
                        ExecuteWrite(NptRegisters.Registers._PARAMS);

                        Slot.WorkFlowState = WFState.Error;
                        Slot.WorkFlowBtnChecked = false;
                        Slot.CheckUpColor = Brushes.Red;
                        Command = NptCommand.ReadState;
                        break;
                    }

                case NptCommand.Calibrate:
                    {
                        Slot.CalibColor = Brushes.Yellow;
                        Slot.Log("Калибровка", Brushes.Black);
                        stopWatch.Restart();

                        // считываем и сохраняем параметры 
                        ExecuteRead(NptRegisters.Registers._PARAMS);
                        GetRegisters().OnDeviceRegistersRead("PARAMS");
                        Slot.OnPropertychanged("ProgressPos");

                        // записываем настройки каибровки 
                        Slot.DevParamKey = GetRegisters().DEVTYPE;
                        GetRegisters().SetCalibParams(Slot.CalibrateParam);
                        LastIOResult = ExecuteWrite(NptRegisters.Registers._PARAMS);
                        Slot.OnPropertychanged("ProgressPos");

                        if (LastIOResult)
                        {
                            // входим в режим калибровки
                            GetRegisters().SetCalibEnterCmd();
                            ExecuteWrite(NptRegisters.Registers._CALIB_CMD);
                            Slot.OnPropertychanged("ProgressPos");

                            Thread.Sleep(500);
                            LastIOResult = ExecuteRead(NptRegisters.Registers._CALIB_REZULT);
                            Slot.OnPropertychanged("ProgressPos");

                            if (GetRegisters().GetCalibRezult() != 0)
                            {
                                Command = NptCommand.WaitCalibRez;
                                break;
                            }
                            else
                            {
                                Slot.Log("Ошибка перехода в режим калибровки (" + GetRegisters().GetCalibRezult() + ")", Brushes.Red);
                                GetRegisters().RestoreParams();
                                ExecuteWrite(NptRegisters.Registers._PARAMS);
                            }
                        }
                        else
                        {
                            Slot.Log("Ошибка установки параметров калибровки", Brushes.Red);
                        }
                        Slot.CalibColor = Brushes.Red;
                        Slot.WorkFlowState = WFState.Error;
                        Slot.WorkFlowBtnChecked = false;
                        Command = NptCommand.ReadState;
                        break;
                    }

                case NptCommand.WaitCalibRez:
                    {
                        LastIOResult = ExecuteRead(NptRegisters.Registers._CALIB_REZULT);
                        Slot.OnPropertychanged("ProgressPos");
                        WFState rez_state = Slot.WorkFlowState;
                        if (!LastIOResult)
                        {
                            Slot.Log("Ошибка чтения результатов калибровки", Brushes.Red);
                            Slot.CalibColor = Brushes.Red;
                            rez_state = WFState.Error;
                        }
                        else
                        {
                            if (GetRegisters().GetCalibRezult() == 100 + GetRegisters().CurrentCalibCommand) // calib - oki
                            {
                                Slot.Log("Запись результатов калибровки", Brushes.Black);
                                GetRegisters().SetCalibCmd(50, 0);
                                ExecuteWrite(NptRegisters.Registers._CALIB_CMD);
                                Slot.CalibColor = Brushes.Green;
                                rez_state = WFState.Oki;
                                Thread.Sleep(500);
                            }
                            else if (GetRegisters().GetCalibRezult() == 200 + GetRegisters().CurrentCalibCommand) // calib - err
                            {
                                Slot.Log("Ошибка выполнения команды калибровки", Brushes.Black);
                                Slot.CalibColor = Brushes.Red;
                                rez_state = WFState.Error;
                            }
                            else if (stopWatch.ElapsedMilliseconds > (1000 * TimesByOpearation[NptCommand.Calibrate])) // истёк таймаут
                            {
                                Slot.Log("Истёк таймаут выполнения команды калибровки (" + GetRegisters().GetCalibRezult() + ")", Brushes.Black);
                                Slot.CalibColor = Brushes.Red;
                                rez_state = WFState.Error;
                            }
                            else
                            {
                                break; // ожидаем
                            }
                        }

                        
                        //Slot.Log("Выход из режима калибровки", Brushes.Black);
                        GetRegisters().SetCalibCmd(60, 0);
                        ExecuteWrite(NptRegisters.Registers._CALIB_CMD);
                        Slot.OnPropertychanged("ProgressPos");
                        Thread.Sleep(500);
                        Slot.Log("Восстановление параметров прибора", Brushes.Black);
                        GetRegisters().RestoreParams();
                        ExecuteWrite(NptRegisters.Registers._PARAMS);
                        Slot.WorkFlowBtnChecked = false;
                        Slot.WorkFlowState = rez_state;
                        Slot.OnPropertychanged("ProgressPos");
                        Thread.Sleep(100);
                        PrevOperationMs += stopWatch.ElapsedMilliseconds;
                        Command = WorkFlow.Count == 0 ? NptCommand.ReadState : WorkFlow.Dequeue();
                        break;
                    }

                case NptCommand.EndWorkFlow:
                    WorkFlow.Clear();
                    Command = NptCommand.ReadState;
                    Slot.OnPropertychanged("ProgressPos");
                    break;

                case NptCommand.StartWorkFlow:
                    {
                        if (Slot.CleanLogBeforeStart)
                        {
                            Slot.CleanLog();
                        }

                        Slot.HeartColor = Slot.BtnNothingColor;
                        Slot.CheckUpColor = Slot.BtnNothingColor;
                        Slot.CalibColor = Slot.BtnNothingColor;
                        Slot.WriteFactorySettingsColor = Slot.BtnNothingColor;
                        WorkFlow.Clear();
                        TotalTimes = 0;
                        PrevOperationMs = 0;
                        if (Slot.UseWarmUp)
                        {
                            TotalTimes += TimesByOpearation[NptCommand.WarmingUp];
                            WorkFlow.Enqueue(NptCommand.WarmingUp);
                        }
                        TotalTimes += TimesByOpearation[NptCommand.Calibrate];
                        WorkFlow.Enqueue(NptCommand.Calibrate);
                        TotalTimes += TimesByOpearation[NptCommand.СheckUp];
                        WorkFlow.Enqueue(NptCommand.СheckUp);
                        if (Slot.UseWriteFactorySettings)
                        {
                            TotalTimes += TimesByOpearation[NptCommand.WriteFactorySettings];
                            WorkFlow.Enqueue(NptCommand.WriteFactorySettings);
                        }
                        WorkFlow.Enqueue(NptCommand.EndWorkFlow);

                        TotalTimes = 1000 * (TotalTimes);
                        Command = WorkFlow.Count == 0 ? NptCommand.ReadState : WorkFlow.Dequeue();
                        break;
                    }

                case NptCommand.ReadState:
                    LastIOResult = ExecuteRead(NptRegisters.Registers._STATE);
                    Slot.OnPropertychanged("Tempr");
                    break;

            }

            return true;
        }
    }

}
