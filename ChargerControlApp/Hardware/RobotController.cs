using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Motor.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using System.Collections;
using System.Runtime.Serialization.DataContracts;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChargerControlApp.Hardware
{
    public class RobotController : IDisposable
    {
        public const int MOTOR_COUNT = 3;
        private byte[] MOTOR_ADDRESSES = new byte[] { 1, 2, 3 };
        public SingleMotorService[] Motors;
        private IModbusRTUService _modbusService;

        public bool IsRunning { get; internal set; } = false;

        private Queue<MotorFrame> _manualCommand = new Queue<MotorFrame>();

        #region Constructor

        private RobotController()
        { }

        public RobotController(IModbusRTUService modbusService) : this()
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));

            Motors = new SingleMotorService[MOTOR_COUNT];
            for (int i = 0; i < Motors.Length; i++)
                Motors[i] = new SingleMotorService(_modbusService, MOTOR_ADDRESSES[i]);

        }

        #endregion

        #region Disposable Support and Destructor

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    source.Cancel();
                    IsRunning = false;
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~RobotController()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Task

        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken ct = source.Token;
        private int _routeIndex = 0;

        private Task DoWork()
        {
            return Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && IsRunning)
                {
                    //Console.WriteLine("ModbusRTUService is running...");
                    // Your periodic work here

                    if (_manualCommand.Count > 0)
                    {
                        var command = _manualCommand.Dequeue();
                        var writeResult = Motors[command.Id].WriteFrame(command);
                        bool write_finished = writeResult.Result;

                        if (write_finished)
                        { 
                        
                        }
                    }

                    // Route Proocess for each motor
                    var result = Motors[_routeIndex].ExecuteRouteProcessOnce();
                    bool finished  = result.Result;
                    if (finished)
                    { 
                        if(++_routeIndex >= MOTOR_COUNT)
                            _routeIndex = 0;
                    }

                    Thread.Sleep(10); // Adjust the delay as needed
                }
            }, ct);
        }

        #endregion

        #region Functions

        public void Open()
        {
            IsRunning = true;
            DoWork();
        }

        public void Close()
        {
            IsRunning = false;
        }

        public bool ServerOn(int motorId, bool state)
        { 
            bool result = false;

            if(motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_Low.Bits.S_ON = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_Low.Data };

                _manualCommand.Enqueue(command);
                result = true;
            }

            return result;
        }

        public bool AlarmReset(int motorId, bool state)
        {
            bool result = false;

            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_Low.Bits.ALM_RST = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_Low.Data };

                _manualCommand.Enqueue(command);
                result = true;
            }

            return result;
        }

        public bool Home(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_High.Bits.HOME = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool Stop(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_Low.Bits.STOP = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_Low.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="motorId"></param>
        /// <param name="mode"></param>
        /// 0: Low Ppeed; 1; High Speed; 2: Pitch
        /// <returns></returns>
        public bool SetJogMode(int motorId, int mode)
        {
            bool result = false;

            ushort[] _mode = new ushort[] { 0, 52, 0, 53 }
            ;
            if (mode == 0)
                _mode = new ushort[] { 0, 48, 0, 49 };
            else if(mode == 1)
                _mode = new ushort[] { 0, 50, 0, 51 };



            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {

                var command = MotorCommandList.CommandMap["WriteJogMode"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = (ushort)_mode.Length;
                command.DataFrame.Data = _mode ;
                _manualCommand.Enqueue(command);
                result = true;
            }

            return result;
        }

        public bool SetJOG_FW(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_High.Bits.FW_JOG_P = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool SetJOG_RV(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_High.Bits.RV_JOG_P = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool SetJog(int motorId, string dir, bool state)
        {
            if (motorId < 0 || motorId >= MOTOR_COUNT)
                return false;

            if (dir == "FW")
                Motors[motorId].IO_Input_High.Bits.FW_JOG_P = state;
            else if (dir == "RV")
                Motors[motorId].IO_Input_High.Bits.RV_JOG_P = state;
            else
                return false;

            var command = MotorCommandList.CommandMap["WriteInputHigh"];
            command.Id = (byte)motorId;
            command.DataFrame.DataNumber = 1;
            command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_High.Data };
            _manualCommand.Enqueue(command);
            return true;
        }

        public bool SetDataNo_M(int motorId, int dataNo)
        {
            bool result = false;

            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].CurrentDataNo = dataNo;

                BitArray bitArray = new BitArray(new int[] { dataNo });
                bool[] boolArray = new bool[bitArray.Length];
                bitArray.CopyTo(boolArray, 0);

                Motors[motorId].IO_Input_High.Bits.M0 = boolArray[0];
                Motors[motorId].IO_Input_High.Bits.M1 = boolArray[1];
                Motors[motorId].IO_Input_High.Bits.M2 = boolArray[2];
                Motors[motorId].IO_Input_High.Bits.M3 = boolArray[3];
                Motors[motorId].IO_Input_High.Bits.M4 = boolArray[4];
                Motors[motorId].IO_Input_High.Bits.M5 = boolArray[5];
                Motors[motorId].IO_Input_High.Bits.M6 = boolArray[6];
                Motors[motorId].IO_Input_High.Bits.M7 = boolArray[7];

                var command = MotorCommandList.CommandMap["WriteInputHigh"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;

            }

            return result;
        }

        public bool SetStart(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].IO_Input_High.Bits.START = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        #endregion
    }

}
