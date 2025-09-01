using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Motor.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using System.Runtime.Serialization.DataContracts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChargerControlApp.Hardware
{
    public class RobotController : IDisposable
    {
        public const int MOTOR_COUNT = 1;
        private byte[] MOTOR_ADDRESSES = new byte[] { 1, 2, 3 };
        private SingleMotorService[] _motors;
        private IModbusRTUService _modbusService;

        public bool IsRunning { get; internal set; } = false;

        private Queue<MotorFrame> _manualCommand = new Queue<MotorFrame>();


        #region Constructor

        private RobotController()
        { }

        public RobotController(IModbusRTUService modbusService) : this()
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));

            _motors = new SingleMotorService[MOTOR_COUNT];
            for (int i = 0; i < _motors.Length; i++)
                _motors[i] = new SingleMotorService(_modbusService, MOTOR_ADDRESSES[i]);

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
                        var writeResult = _motors[command.Id].WriteFrame(command);
                        bool write_finished = writeResult.Result;

                        if (write_finished)
                        { 
                        
                        }
                    }

                    // Route Proocess for each motor
                    var result = _motors[_routeIndex].ExecuteRouteProcessOnce();
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
                _motors[motorId].IO_Input_Low.Bits.S_ON = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"];
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { _motors[motorId].IO_Input_Low.Data };

                _manualCommand.Enqueue(command);
                result = true;
            }

            return result;
        }

        #endregion
    }

}
