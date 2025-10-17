using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Modbus.Interfaces;
using ChargerControlApp.DataAccess.Motor.Interfaces;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using ChargerControlApp.DataAccess.Robot.Models;
using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Serialization.DataContracts;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChargerControlApp.Hardware
{
    public class RobotController : IDisposable
    {
        public const int MOTOR_COUNT = 3;
        public static int PositionInPos_Offset = 3000; // unit: step => for checking if reached position
        private byte[] MOTOR_ADDRESSES = new byte[] { 1, 2, 3 };
        public SingleMotorService[] Motors;
        private IModbusRTUService _modbusService;

        public bool IsRunning { get; internal set; } = false;

        private Queue<MotorFrame> _manualCommand = new Queue<MotorFrame>();
        private bool _procedureStop = false;
        public bool IsProcedureRunnging { get; internal set; } = false;
        public bool CanHome { get {
                return Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE &&
                        Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE &&
                        Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE;
            } }
        public int HomeProcedureCase { get; internal set; } = 0;
        public string ErrorMessage { get; internal set; } = string.Empty;

        #region Constructor

        private RobotController()
        { }

        public RobotController(IModbusRTUService modbusService) : this()
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));

            Motors = new SingleMotorService[MOTOR_COUNT];
            for (int i = 0; i < Motors.Length; i++)
                Motors[i] = new SingleMotorService(_modbusService, MOTOR_ADDRESSES[i], i);

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

        private CancellationTokenSource source = new CancellationTokenSource();
        //private static CancellationToken ct = source.Token;
        private int _routeIndex = 0;

        private Task DoWork()
        {
            CancellationToken ct = source.Token;

            return Task.Run(async () =>
            {
                bool ExecutedOnce = false; // Flag to ensure initialization runs only once

                while (!ct.IsCancellationRequested && IsRunning)
                {
                    //Console.WriteLine("ModbusRTUService is running...");
                    // Your periodic work here

                    if (_modbusService.IsRunning && !ExecutedOnce)
                    {
                        ExecutedOnce = true;
                        InitializeOnce();
                    }

                    if (_manualCommand.Count > 0)
                    {
                        var command = _manualCommand.Dequeue();


                        if (command.Name == "ReadJogAndHomeSetting")
                        {
                            var readResult = await Motors[command.Id].ReadFrame(command);
                            if (readResult != null)
                            {
                                if (readResult.Length == 32)
                                {
                                    Motors[command.Id].MotorInfo.Jog_Home_Setting.Set(readResult);
                                }
                            }
                        }
                        else if (command.Name == "ReadOpData")
                        {
                            if (command.SubFrames != null)
                            {
                                for (int i = 0; i < command.SubFrames.Count; i++)
                                {
                                    var readResult = await Motors[command.Id].ReadFrame(command.SubFrames[i]);

                                    if (readResult != null)
                                    {
                                        Motors[command.Id].MotorInfo.OpDataArray[i].FromUShortArray(readResult);
                                    }
                                }
                            }
                        }
                        else if ((command.Name == "WriteOpData") || (command.Name == "WriteOpData_DefaultVelocityForJog"))
                        {
                            if (command.SubFrames != null)
                            {
                                for (int i = 0; i < command.SubFrames.Count; i++)
                                {
                                    var writeResult = await Motors[command.Id].WriteFrame(command.SubFrames[i]);
                                    bool write_finished = writeResult;
                                    if (write_finished)
                                    {
                                    }
                                }
                            }
                        }
                        else
                        {

                            var writeResult = await Motors[command.Id].WriteFrame(command);
                            bool write_finished = writeResult;

                            if (write_finished)
                            {

                            }
                        }
                    }

                    // Route Proocess for each motor
                    var result = await Motors[_routeIndex].ExecuteRouteProcessOnce();
                    bool finished  = result;
                    if (finished)
                    { 
                        if(++_routeIndex >= MOTOR_COUNT)
                            _routeIndex = 0;
                    }

                    // Update GPIO status from motor 1 - Y axis
                    GPIOService.Pin1ValueFromMotor = Motors[1].MotorInfo.IO_Output_High.Bits.R0_R;
                    GPIOService.Pin2ValueFromMotor = Motors[1].MotorInfo.IO_Output_High.Bits.R1_R;

                    //Thread.Sleep(10); // Adjust the delay as needed
                    await Task.Delay(1); // Adjust the delay as needed
                }
            }, ct);
        }

        #endregion

        #region Functions

        // Initialize once - set jog mode to pitch for all motors
        private void InitializeOnce()
        {
            for(int i=0;i< MOTOR_COUNT; i++)
            {
                this.SetJogMode(i, 2);
                this.WriteOpData_DefaultVelocityForSpd(i);
                this.WriteROutFunction_26to29(i);
                this.ReadJogAndHomeSetting(i);
                this.ReadOpData(i);
                
            }
        }

        // Check if motor in position
        public bool InPosition(int motorId, int posIndex)
        {
            if (motorId < 0 || motorId >= MOTOR_COUNT)
                return false;
            return Math.Abs(Motors[motorId].MotorInfo.Pos_Actual - Motors[motorId].MotorInfo.OpDataArray[posIndex].Position) <= PositionInPos_Offset;
        }
        public bool InPositions(int motorId, int[] posIndexArray)
        {
            if (motorId < 0 || motorId >= MOTOR_COUNT)
                return false;
            foreach(var posIndex in posIndexArray)
            {
                if (Math.Abs(Motors[motorId].MotorInfo.Pos_Actual - Motors[motorId].MotorInfo.OpDataArray[posIndex].Position) <= PositionInPos_Offset)
                    return true;
            }
            return false;
        }
        public bool ZAxisBetweenSlotOrCar()
        {


            for (int i = 1; i < 19; i += 2)
            {
                if ((Motors[2].MotorInfo.Pos_Actual < Motors[2].MotorInfo.OpDataArray[i].Position + PositionInPos_Offset) &&
                    (Motors[2].MotorInfo.Pos_Actual > Motors[2].MotorInfo.OpDataArray[i + 1].Position - PositionInPos_Offset))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Command

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
                Motors[motorId].MotorInfo.IO_Input_Low.Bits.S_ON = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.DataNumber = 1;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_Low.Data };

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
                Motors[motorId].MotorInfo.IO_Input_Low.Bits.ALM_RST = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_Low.Data };

                _manualCommand.Enqueue(command);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Home
        /// </summary>
        /// <param name="motorId"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool Home(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].MotorInfo.IO_Input_High.Bits.HOME = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <param name="motorId"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool Stop(int motorId, bool state)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].MotorInfo.IO_Input_Low.Bits.STOP = state;
                var command = MotorCommandList.CommandMap["WriteInputLow"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_Low.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public void AllStop()
        {
            this.Stop(0, true);
            this.Stop(1, true);
            this.Stop(2, true);

            Task.Delay(50).Wait();

            this.Stop(0, false);
            this.Stop(1, false);
            this.Stop(2, false);
        }
        public void SetAllServo(bool state)
        {
            this.ServerOn(0, state);
            this.ServerOn(1, state);
            this.ServerOn(2, state);
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

                var command = MotorCommandList.CommandMap["WriteJogMode"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
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
                Motors[motorId].MotorInfo.IO_Input_High.Bits.FW_JOG_P = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
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
                Motors[motorId].MotorInfo.IO_Input_High.Bits.RV_JOG_P = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
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
                Motors[motorId].MotorInfo.IO_Input_High.Bits.FW_JOG_P = state;
            else if (dir == "RV")
                Motors[motorId].MotorInfo.IO_Input_High.Bits.RV_JOG_P = state;
            else
                return false;

            var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
            command.Id = (byte)motorId;
            command.DataFrame.SlaveAddress = (byte)(motorId + 1);
            command.DataFrame.DataNumber = 1;
            command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
            _manualCommand.Enqueue(command);
            return true;
        }

        public bool SetJogSpd(int motorId, string dir, bool state)
        {
            if (motorId < 0 || motorId >= MOTOR_COUNT)
                return false;

            if (dir == "FW")
                Motors[motorId].MotorInfo.IO_Input_High.Bits.FW_SPD = state;
            else if (dir == "RV")
                Motors[motorId].MotorInfo.IO_Input_High.Bits.RV_SPD = state;
            else
                return false;

            var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
            command.Id = (byte)motorId;
            command.DataFrame.SlaveAddress = (byte)(motorId + 1);
            command.DataFrame.DataNumber = 1;
            command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
            _manualCommand.Enqueue(command);
            return true;
        }

        public bool SetDataNo_M(int motorId, int dataNo)
        {
            bool result = false;

            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                Motors[motorId].MotorInfo.CurrentDataNo = dataNo;

                BitArray bitArray = new BitArray(new int[] { dataNo });
                bool[] boolArray = new bool[bitArray.Length];
                bitArray.CopyTo(boolArray, 0);

                Motors[motorId].MotorInfo.IO_Input_High.Bits.M0 = boolArray[0];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M1 = boolArray[1];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M2 = boolArray[2];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M3 = boolArray[3];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M4 = boolArray[4];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M5 = boolArray[5];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M6 = boolArray[6];
                Motors[motorId].MotorInfo.IO_Input_High.Bits.M7 = boolArray[7];

                var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
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
                Motors[motorId].MotorInfo.IO_Input_High.Bits.START = state;
                var command = MotorCommandList.CommandMap["WriteInputHigh"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 1;
                command.DataFrame.Data = new ushort[] { Motors[motorId].MotorInfo.IO_Input_High.Data };
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool ReadJogAndHomeSetting(int motorId)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                var command = MotorCommandList.CommandMap["ReadJogAndHomeSetting"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 32;
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool WriteJogAndHomeSetting(int motorId)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                var command = MotorCommandList.CommandMap["WriteJogAndHomeSetting"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.DataFrame.DataNumber = 32;
                command.DataFrame.Data = Motors[motorId].MotorInfo.Jog_Home_Setting.Get();
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool ReadOpData(int motorId)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                var command = MotorCommandList.CommandMap["ReadOpData"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                command.SetSubFramesSlaveAddress((byte)(motorId + 1));
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool WriteOpData(int motorId)
        {
            bool result = false;

            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                var command = MotorCommandList.CommandMap["WriteOpData"].Clone();
                command.Id = (byte)motorId;
                if(command.SubFrames != null)
                {
                    for(int i=0; i< command.SubFrames.Count; i++)
                    {
                        command.SubFrames[i].DataFrame.SlaveAddress = (byte)(motorId + 1);
                        command.SubFrames[i].DataFrame.DataNumber = 6;
                        command.SubFrames[i].DataFrame.Data = Motors[motorId].MotorInfo.OpDataArray[i].ToUShortArray();
                    }
                    _manualCommand.Enqueue(command);
                    result = true;
                }
            }

            return result;
        }

        public bool WriteOpData_Position(int motorId, int posIndex, int position) 
        {
            bool result = false;

            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                if (posIndex >= 0 && posIndex < Motors[motorId].MotorInfo.OpDataArray.Length)
                {
                    Motors[motorId].MotorInfo.OpDataArray[posIndex].Position = Motors[motorId].MotorInfo.Pos_Actual;//position;
                    var command = MotorCommandList.CommandMap["WriteOpData_Position"].Clone();
                    command.Id = (byte)motorId;
                    if (command.SubFrames != null)
                    {
                        command.DataFrame = command.SubFrames[posIndex].DataFrame.Clone();
                        command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                        command.DataFrame.DataNumber = 2;
                        command.DataFrame.Data = Motors[motorId].MotorInfo.OpDataArray[posIndex].ToPositionUShortArray();
                        _manualCommand.Enqueue(command);
                        result = true;
                    }
                }
            }

            return result;
        }

        public bool WriteOpData_DefaultVelocityForSpd(int motorId)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                var command = MotorCommandList.CommandMap["WriteOpData_DefaultVelocityForJog"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }

        public bool WriteROutFunction_26to29(int motorId)
        {
            bool result = false;
            if (motorId >= 0 && motorId < MOTOR_COUNT)
            {
                var command = MotorCommandList.CommandMap["WriteROutFunction_26to29"].Clone();
                command.Id = (byte)motorId;
                command.DataFrame.SlaveAddress = (byte)(motorId + 1);
                _manualCommand.Enqueue(command);
                result = true;
            }
            return result;
        }


        #endregion

        #region Procedure

        // 三軸復歸程序
        public async Task<bool> HomeProcedure()
        {
            bool result = false;
            HomeProcedureCase = 0;
            IsProcedureRunnging = true;
            try
            {
                if (_modbusService.IsRunning)
                {
                    do
                    {
                        // 程序中止
                        if (this._procedureStop)
                        {
                            this._procedureStop = false;
                            break;
                        }

                        switch (HomeProcedureCase)
                        {
                            case 0: // 等待三軸 RDY-HOME-OPE
                                if (CanHome)
                                {
                                    this.Home(1, true);
                                    HomeProcedureCase = 10;
                                }
                                break;

                            case 10: // Y軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                if ((this.Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (this.Motors[1].MotorInfo.IO_Output_Low.Bits.MOVE == true))
                                {
                                    this.Home(1, false);
                                    HomeProcedureCase = 11;
                                }
                                break;

                            case 11: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                if ((this.Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (this.Motors[1].MotorInfo.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 20; // Y軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                }
                                break;

                            case 20: // 等待三軸 RDY-HOME-OPE
                                if (CanHome)
                                {
                                    this.Home(2, true);
                                    HomeProcedureCase = 21;
                                }
                                break;

                            case 21: // Z軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                if ((this.Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (this.Motors[2].MotorInfo.IO_Output_Low.Bits.MOVE == true))
                                {
                                    this.Home(2, false);
                                    HomeProcedureCase = 22;
                                }
                                break;

                            case 22: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                if ((this.Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (this.Motors[2].MotorInfo.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 30; // Z軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                }
                                break;

                            case 30: // 等待三軸 RDY-HOME-OPE
                                if (CanHome)
                                {
                                    this.Home(0, true);
                                    HomeProcedureCase = 31;
                                }
                                break;

                            case 31: // 旋轉軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                if ((this.Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (this.Motors[0].MotorInfo.IO_Output_Low.Bits.MOVE == true))
                                {
                                    this.Home(0, false);
                                    HomeProcedureCase = 32;
                                }
                                break;

                            case 32: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                if ((this.Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (this.Motors[0].MotorInfo.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 100; // 旋轉軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                }
                                break;

                            case 99: // Unknow Error
                            default:
                                this.AllStop();
                                HomeProcedureCase = 100;
                                break;
                        }


                        //Thread.Sleep(100);
                        await Task.Delay(100);
                    } while (HomeProcedureCase > 99);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RobotController HomeProcedure Exception: {ex.Message}");
            }
            IsProcedureRunnging = false;
            return result;
        }

        public async Task<bool> MoveToPositionAsync(int axisId, int posDataNo, CancellationToken cancellationToken)
        {
            bool return_value = false;
            ErrorMessage = string.Empty;

            ErrorMessage = $"Wait RDY_SD_OPE == false => {Motors[axisId].MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE}";
            while (!Motors[axisId].MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
            }

            var result = this.SetDataNo_M(axisId, posDataNo); // 設定要進行的位置
            result = this.SetStart(axisId, true); // 下達開始命令

            ErrorMessage = $"Wait RDY_SD_OPE == true => {Motors[axisId].MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE}";
            while (Motors[axisId].MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
            }

            result = this.SetStart(axisId, false); // 將開始命令取消掉

            ErrorMessage = $"Wait RDY_SD_OPE == true => {Motors[axisId].MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE}";
            while (!Motors[axisId].MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
            }

            //while (!Motors[axisId].MotorInfo.IO_Output_Low.Bits.IN_POS)
            //{
            //    cancellationToken.ThrowIfCancellationRequested();
            //    await Task.Delay(100, cancellationToken);
            //}

            await Task.Delay(200, cancellationToken); // 等待一段時間讓馬達穩定

            return_value = InPosition(axisId, posDataNo);
            ErrorMessage = $"Check PosNo [{posDataNo}] value = {Motors[axisId].MotorInfo.OpDataArray[posDataNo].Position} ; Real value={Motors[axisId].MotorInfo.Pos_Actual}";

            return return_value;
        }

        public async Task<bool> CheckSensors(string sensorName, bool checkStatus, CancellationToken cancellationToken)
        {
            bool return_value = false;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (sensorName == "BatteryExistInFork")
                    return_value = GPIOService.BatteryExistInFork == checkStatus;
                else if (sensorName == "BatteryExistInSlot")
                    return_value = GPIOService.BatteryExistInSlot == checkStatus;
                await Task.Delay(100, cancellationToken);
            } while (!return_value);
            return return_value;
        }
        #endregion
    }

}
