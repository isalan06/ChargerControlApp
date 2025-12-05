using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Robot.Models;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Services;
using System.Threading.Tasks;
using TAC.Hardware;
using Nexano.FMS.DeviceController.Protos;

namespace ChargerControlApp.DataAccess.Robot.Services
{
    public class RobotService : IDisposable
    {
        private readonly HardwareManager _hardwareManager;
        private readonly SlotServices _slotServices;
        private readonly ChargingStationStateMachine _stationStateMachine;

        public bool IsProcedureRunning { get; internal set; }
        public bool IsMainProcedureRunning { get; internal set; }
        public int HomeProcedureCase { get; internal set; }

        private bool _procedureStopTrigger = false;
        private bool _mainProcedureStopTrigger = false;
        private CancellationTokenSource _cts;

        private List<ProcedureFrame> _procedureFrames = new List<ProcedureFrame>();
        public PosErrorFrame LastError { get; internal set; } = new PosErrorFrame();
        private PosErrorFrame _homeError { get; set; } = new PosErrorFrame();

        public string ProcedureStatusMessage { get; internal set; } = string.Empty;
        public string MainProcedureStatusMessage { get; internal set; } = string.Empty;
        public int MainProcedureCase { get; internal set; }
        private bool checkSensorPoint = false;
        public bool CheckSensorPoint { get { return checkSensorPoint; } }   

        private readonly ILogger<RobotService> _logger;

        public static bool IsManualMode { get; internal set; } = false;
        public StationState GetEquipmentStatus
        {
            get
            { 
                StationState result = StationState.Unspecified;

                if (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Error)
                    result = StationState.Error;
                else if (IsManualMode)
                    result = StationState.Manual;
                else if (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Manual)
                    result = StationState.Manual;
                else if (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Swapping)
                    result = StationState.Swapping;
                else if (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Idle)
                    result = StationState.Idle;
                else if (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Initial)
                    result = StationState.Initial;

                return result;
            }
        }
        public string GetStationStateString
        {
            get
            {
                return GetEquipmentStatus.ToString();
            }
        }
        public bool CanTransferToManualMode
        { 
            get
            {
                return ( !IsManualMode && 
                    ((_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Idle)
                    || (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Error))
                    || (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Initial));
            }
        }
        public bool CanCancelManualMode
        {
            get
            {
                return (IsManualMode &&
                    ((_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Manual)
                    || (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Idle)
                    || (_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Error)));
            }
        }



        #region Information

        /// <summary>
        /// 是否已完成復歸
        /// </summary>
        public bool IsHomeFinished { get {
                //return (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_High.Bits.HOME_END
                //    && _hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_High.Bits.HOME_END
                //    && _hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_High.Bits.HOME_END
                //    );
                return (_hardwareManager.Robot.Motors[0].IsHomeFinished
                    && _hardwareManager.Robot.Motors[1].IsHomeFinished
                    && _hardwareManager.Robot.Motors[2].IsHomeFinished
                    );
            } }

        /// <summary>
        /// 是否可以進行復歸
        /// </summary>
        public bool CanHome { get {
                return (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE &&
                    _hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE &&
                    _hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE
                    );
            } }

        /// <summary>
        /// 是否有任何軸發生警報 
        /// </summary>
        public bool IsMotorAlarm
        {
            get
            {
                return (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.ALM_A ||
                    _hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.ALM_A ||
                    _hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.ALM_A
                    );
            }
        }

        public bool IsPowerSupplyAlarm
        {
            get
            {
                bool result = false;

                for (int i = 0; i < HardwareManager.NPB450ControllerInstnaceNumber; i++)
                {
                    // 0xFFBF = 1111 1111 1011 1111, 忽略 Bit6 (OP_OFF)
                    if ((_hardwareManager.Charger[i].FAULT_STATUS.Data & 0xFFBF) != 0)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        public bool IsSlotAlarm
        {
            get
            {
                return _hardwareManager.SlotServices.IsAnySlotInErrorState;
            }
        }

        public bool IsProcedureAlarm
        {
            get
            {
                return (this.LastError.ErrorCode != 0);
            }
        }

        public bool IsCriticalAlarm
        {
            get
            {
                return (IsMotorAlarm || IsProcedureAlarm);
            }
        }
        public bool IsAlarm
        {
            get
            {
                return (IsMotorAlarm || IsPowerSupplyAlarm || IsSlotAlarm || IsProcedureAlarm);
            }
        }

        public bool IsAnyAlarm()
        {
            return (IsMotorAlarm || IsPowerSupplyAlarm || IsSlotAlarm || IsProcedureAlarm);
        }

        public bool GetSwapIndex(out int swapIn, out int swapOut)
        {
            swapIn = 0;
            swapOut = 0;
            bool result = false;
            int[] swapOuts = null;
            if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOuts))
            {
                swapOut = _hardwareManager.SwapOut(swapOuts);
                result = true;
            }

            return result;
        }

        #endregion

        #region constructor

        public RobotService(IServiceProvider serviceProvider)
        {
            // 取得 HardwareManager 實例
            _hardwareManager = serviceProvider.GetRequiredService<HardwareManager>();
            _logger = serviceProvider.GetRequiredService<ILogger<RobotService>>();
            _slotServices = serviceProvider.GetRequiredService<SlotServices>();
            _stationStateMachine = serviceProvider.GetRequiredService<ChargingStationStateMachine>();
        }

        #endregion

        #region IDisposable & Destructor

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~RobotService()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region function

        /// <summary>
        /// 中止目前正在執行的程序   
        /// </summary>
        public void StopProcedure()
        {

            if (!IsProcedureRunning)
            {
                if (IsTestMode) IsTestMode = false;
                return;
            }
            IsProcedureRunning = false;
            IsMainProcedureRunning = false;
            this._procedureStopTrigger = true;
            this._mainProcedureStopTrigger = true;
            _cts?.Cancel();
            _hardwareManager.Robot.AllStop();
            ProcedureStatusMessage = string.Empty;
            MainProcedureStatusMessage = string.Empty;
            IsTestMode = false; // 結束測試模式
        }

        public void StopAutoProcedure()
        {
            StopProcedure();
        }

        /// <summary>
        /// Start Home Procedure
        /// </summary>
        public void StartHomeProcedure()
        {
            if (IsProcedureRunning) return;

            LastError.Clear();
            HomeProcedureCase = 0;
            MainProcedureStatusMessage = string.Empty;
           
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            int timeoutMs = 6000000; // 600秒逾時，可依需求調整

            Task.Run(async () =>
            {
                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeoutCts.CancelAfter(timeoutMs);
                    try
                    {
                        bool result = await HomeProcedure();
                        if (!result) LastError = _homeError.Clone();
                    }
                    catch (OperationCanceledException)
                    {
                        if (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
                        {
                            // 逾時取消
                            LastError = _homeError.Clone();
                            StopProcedure();
                        }
                    }
                }
            }, token);
        }

        /// <summary>
        /// Start Rotate Procedure
        /// </summary>
        /// <param name="targetPosNo"></param>
        public void StartRotateProcedure(int targetPosNo)
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            DefaultRotateProcedure.R_TargetPosDataNo = targetPosNo;
            DefaultRotateProcedure.Refresh();
            _procedureFrames = DefaultRotateProcedure.ProcedureFrames;
            
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        public async Task<bool> StartHomeRotateMoveToR0()
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            _procedureFrames = new List<ProcedureFrame>()
            { new PosFrame{ AxisId = 0, PosDataNo = 0, Name="R0", Description="Rotate axis move to R0 when homing", DelayTime_ms=200} };


             var result = await ExecutePosActWhenHoming();

            return result;
        }

        public async Task<bool> StartHomeYMoveToY0()
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            _procedureFrames = new List<ProcedureFrame>()
            { new PosFrame{ AxisId = 1, PosDataNo = 0, Name="Y0", Description="Y axis move to Y0 when homing", DelayTime_ms=200, Timeout_ms=1200000 } };

            var result = await ExecutePosActWhenHoming();

            return result;
        }

        public async Task<bool> StartHomeZMoveToZ0()
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            _procedureFrames = new List<ProcedureFrame>()
            { new PosFrame{ AxisId = 2, PosDataNo = 0, Name="Z0", Description="Z axis move to Z0 when homing", DelayTime_ms=200} };
            var result = await ExecutePosActWhenHoming();
            return result;
        }


        /// <summary>
        /// Start Take Car Battery Procedure
        /// </summary>
        public void StartTakeCarBatteryProcedure()
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            checkSensorPoint = false;
            DefaultTakeCarBatteryProcedure.Refresh();
            _procedureFrames = DefaultTakeCarBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _logger.LogInformation("Start to execute procedure of taking car battery");
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        /// <summary>
        /// Start Place Car Battery Procedure
        /// </summary>
        public void StartPlaceCarBatteryProcedure()
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            checkSensorPoint = false;
            DefaultPlaceCarBatteryProcedure.Refresh();
            _procedureFrames = DefaultPlaceCarBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _logger.LogInformation("Start to execute procedure of placing car battery");
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        /// <summary>
        /// Start Place Slot Battery Procedure
        /// </summary>
        /// <param name="posNo"></param>
        public void StartTakeSlotBatteryProcedure(int posNo)
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            checkSensorPoint = false;
            DefaultTakeSlotBatteryProcedure.Z_Input = posNo;
            DefaultTakeSlotBatteryProcedure.Refresh();
            _procedureFrames = DefaultTakeSlotBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _logger.LogInformation($"Start to execute procedure of taking battery from pos #{posNo}");
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        /// <summary>
        /// Start Place Slot Battery Procedure
        /// </summary>
        /// <param name="posNo"></param>
        public void StartPlaceSlotBatteryProcedure(int posNo)
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            checkSensorPoint = false;
            DefaultPlaceSlotBatteryProcedure.Z_Input = posNo;
            DefaultPlaceSlotBatteryProcedure.Refresh();
            _procedureFrames = DefaultPlaceSlotBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _logger.LogInformation($"Start to execute procedure of placing battery to pos #{posNo}");
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        public void StartAutoProcedure()
        {
            if (IsMainProcedureRunning) return;

            LastError.Clear();
            MainProcedureCase = 0;
            MainProcedureStatusMessage = string.Empty;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await ExecuteAutoAct();
            }, token);
        }

        public void StartTestAutoProcedure(int testProcedureIndex, int testExecuteIndex, int testCycleNember)
        {
            if (IsMainProcedureRunning) return;

            LastError.Clear();
            MainProcedureCase = 0;
            MainProcedureStatusMessage = string.Empty;

            IsTestMode = true;
            TestProcedureIndex = testProcedureIndex;
            TestExecuteIndex = testExecuteIndex;
            TestCycleNumber = testCycleNember;
            TestCycleCount = 0;

            SetSlotInfoForTest(testProcedureIndex, testExecuteIndex);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await ExecuteAutoAct();
            }, token);
        }

        public async Task ResetAlarm()
        {
            _hardwareManager.Robot.AlarmReset(0, true);
            _hardwareManager.Robot.AlarmReset(1, true);
            _hardwareManager.Robot.AlarmReset(2, true);

            Task.Delay(100).Wait();

            _hardwareManager.Robot.AlarmReset(0, false);
            _hardwareManager.Robot.AlarmReset(1, false);
            _hardwareManager.Robot.AlarmReset(2, false);

            this.LastError.Clear();

            _hardwareManager.SlotServices.ResetAllAlarm();
        }

        public async Task ResetStatus()
        { 
            await ResetAlarm();

            IsProcedureRunning = false;
            IsMainProcedureRunning = false;
            ProcedureStatusMessage = string.Empty;
            MainProcedureStatusMessage = string.Empty;
        }

        public int GetRotatePosNo(int slotNo)
        {
            int result = -1;

            if(slotNo > 0 && slotNo <= 8)
            {
                if (slotNo <= 4) result = 1;
                else result = 2;
            }
            return result;
        }
        public bool IsSameSide(int posIn, int posOut)
        {
            bool result = false;

            int r1 = GetRotatePosNo(posIn);
            int r2 = GetRotatePosNo(posOut);

            if (r1 > 0)
                result = (r1 == r2);

            return result;
        }
        public int GetPlacePosNo(int slotNo)
        {
            int result = -1;

            if (slotNo > 0 && slotNo <= 8)
            {
                result = slotNo * 2 + 2;
            }
            return result;
        }

        public int GetTakePosNo(int slotNo)
        {
            int result = -1;

            if (slotNo > 0 && slotNo <= 8)
            {
                result = slotNo * 2 + 1;
            }

            return result;
        }

        public bool SwitchManualMode()
        {
            bool result = false;
            if (!IsManualMode)
            {
                if (CanTransferToManualMode)
                {
                    IsManualMode = true;
                    result = true;
                }
            }
            else
            {
                if (CanCancelManualMode)
                {
                    IsManualMode = false;
                    result = true;
                }
            }
            return result;
        }

        public bool RemoteSwappingTrigger()
        { 
            bool result = false;

            if (!IsManualMode)
            { 
                if(_stationStateMachine._currentState.GetCurrentStete() == ChargingState.Idle)
                {
                    this.StartAutoProcedure();
                    _stationStateMachine.HandleTransition(ChargingState.Swapping);
                    result = true;
                }
            }

            return result;
        }

        #endregion

        #region Home Procedure

        // 三軸復歸程序
        public async Task<bool> HomeProcedure()
        {
            bool result = false;
            HomeProcedureCase = 0;
            _homeError.Clear();
            IsProcedureRunning = true;

            var motor_rot_info = _hardwareManager.Robot.Motors[0].MotorInfo;
            var motor_y_info = _hardwareManager.Robot.Motors[1].MotorInfo;
            var motor_z_info = _hardwareManager.Robot.Motors[2].MotorInfo;
            try
            {
                if (_hardwareManager.modbusRTUService.IsRunning)
                {
                    do
                    {
                        // 程序中止
                        if (this._procedureStopTrigger)
                        {
                            this._procedureStopTrigger = false;
                            HomeProcedureCase = -98;
                            _homeError.ErrorCode = 50;
                            _homeError.ErrorMessage = "Manual stop homing!!";
                            break;
                        }

                        switch (HomeProcedureCase)
                        {
                            case 0: // 等待三軸 RDY-HOME-OPE
                                MainProcedureStatusMessage = $"[Home Case 0] Waiting RDY-HOME-OPE of 3 Axes";
                                if (CanHome)
                                {
                                    _hardwareManager.Robot.Home(1, true);
                                    HomeProcedureCase = 10;
                                }
                                break;

                            case 10: // Y軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                MainProcedureStatusMessage = $"[Home Case 10] Homing Y Axis: Waiting RDY-HOME-OPE OFF and MOVE ON";
                                _homeError.ErrorCode = 51;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if ((motor_y_info.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (motor_y_info.IO_Output_Low.Bits.MOVE == true))
                                {
                                    _hardwareManager.Robot.Home(1, false);
                                    HomeProcedureCase = 11;
                                }
                                break;

                            case 11: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                MainProcedureStatusMessage = $"[Home Case 11] Homing Y Axis: Waiting RDY-HOME-OPE ON and MOVE OFF";
                                _homeError.ErrorCode = 52;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if ((motor_y_info.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (motor_y_info.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 12; 
                                }
                                break;

                            case 12: // 等待 HOME_END訊號 -> ON
                                MainProcedureStatusMessage = $"[Home Case 12] Homing Y Axis: Waiting HOME_END ON";
                                _homeError.ErrorCode = 53;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if (motor_y_info.IO_Output_High.Bits.HOME_END)
                                {
                                    _hardwareManager.Robot.Motors[1].IsHomeFinished = true;
                                    HomeProcedureCase = 13; // Y軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                    Task.Delay(500).Wait(); // 等待500ms，確保位置穩定
                                }
                                break;

                            case 13: // Y軸移動到Y0
                                MainProcedureStatusMessage = $"[Home Case 13] Moving Y Axis to Y0 Position";
                                var exe_result_y = await StartHomeYMoveToY0();
                                if(exe_result_y)
                                {
                                    HomeProcedureCase = 20;
                                }
                                else
                                    HomeProcedureCase = -97; // Procedure Error
                                break;

                            case 20: // 等待三軸 RDY-HOME-OPE
                                MainProcedureStatusMessage = $"[Home Case 20] Waiting RDY-HOME-OPE of 3 Axes";
                                if (CanHome)
                                {
                                    _hardwareManager.Robot.Home(2, true);
                                    HomeProcedureCase = 21;
                                }
                                break;

                            case 21: // Z軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                MainProcedureStatusMessage = $"[Home Case 21] Homing Z Axis: Waiting RDY-HOME-OPE OFF and MOVE ON";
                                _homeError.ErrorCode = 54;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if ((motor_z_info.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (motor_z_info.IO_Output_Low.Bits.MOVE == true))
                                {
                                    _hardwareManager.Robot.Home(2, false);
                                    HomeProcedureCase = 22;
                                }
                                break;

                            case 22: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                MainProcedureStatusMessage = $"[Home Case 22] Homing Z Axis: Waiting RDY-HOME-OPE ON and MOVE OFF";
                                _homeError.ErrorCode = 55;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if ((motor_z_info.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (motor_z_info.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 23;
                                }
                                break;

                            case 23: // 等待 HOME_END訊號 -> ON
                                MainProcedureStatusMessage = $"[Home Case 23] Homing Z Axis: Waiting HOME_END ON";
                                _homeError.ErrorCode = 56;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if (motor_z_info.IO_Output_High.Bits.HOME_END == true)
                                {
                                    _hardwareManager.Robot.Motors[2].IsHomeFinished = true;
                                    HomeProcedureCase = 24; // Z軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                    Task.Delay(500).Wait(); // 等待500ms，確保位置穩定
                                }
                                break;

                            case 24: // Z軸移動到Z0
                                MainProcedureStatusMessage = $"[Home Case 24] Moving Z Axis to Z0 Position";
                                var exe_result_z = await StartHomeZMoveToZ0();
                                if(exe_result_z)
                                {
                                    HomeProcedureCase = 30;
                                }
                                else
                                    HomeProcedureCase = -97; // Procedure Error
                                break;

                            case 30: // 等待三軸 RDY-HOME-OPE
                                MainProcedureStatusMessage = $"[Home Case 30] Waiting RDY-HOME-OPE of 3 Axes";
                                if (CanHome)
                                {
                                    _hardwareManager.Robot.Home(0, true);
                                    HomeProcedureCase = 31;
                                }
                                break;

                            case 31: // 旋轉軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                MainProcedureStatusMessage = $"[Home Case 31] Homing Rotate Axis: Waiting RDY-HOME-OPE OFF and MOVE ON";
                                _homeError.ErrorCode = 57;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if ((motor_rot_info.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (motor_rot_info.IO_Output_Low.Bits.MOVE == true))
                                {
                                    _hardwareManager.Robot.Home(0, false);
                                    HomeProcedureCase = 32;
                                }
                                break;

                            case 32: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                MainProcedureStatusMessage = $"[Home Case 32] Homing Rotate Axis: Waiting RDY-HOME-OPE ON and MOVE OFF";
                                _homeError.ErrorCode = 58;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if ((motor_rot_info.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (motor_rot_info.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 33; 
                                }
                                break;

                            case 33: // 等待 HOME_END訊號 -> ON
                                MainProcedureStatusMessage = $"[Home Case 33] Homing Rotate Axis: Waiting HOME_END ON";
                                _homeError.ErrorCode = 59;
                                _homeError.ErrorMessage = $" Homing Timeout: case = {HomeProcedureCase}";
                                if (motor_rot_info.IO_Output_High.Bits.HOME_END == true)
                                {
                                    _hardwareManager.Robot.Motors[0].IsHomeFinished = true;
                                    //result = true;
                                    HomeProcedureCase = 34; // 旋轉軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                    Task.Delay(500).Wait(); // 等待500ms，確保位置穩定
                                }
                                break;

                            case 34: // 旋轉軸移動到R0
                                MainProcedureStatusMessage = $"[Home Case 34] Moving Rotate Axis to R0 Position";
                                var exe_result_r = await StartHomeRotateMoveToR0();
                                if (exe_result_r)
                                {
                                    result = true;
                                    HomeProcedureCase = -1; // 復歸完成
                                }
                                else
                                    HomeProcedureCase = -97; // Procedure Error

                                break;

                            case -97: // Procedure Error
                                MainProcedureStatusMessage = $"[Home Case -97] Homing Procedure Error Occurred";
                                _hardwareManager.Robot.AllStop();
                                break;

                            case -98: // Force to stop all procedure
                                MainProcedureStatusMessage = $"[Home Case -98] Homing Procedure Manually Stopped";
                                break;

                            case -99: // Unknow Error
                            default:
                                MainProcedureStatusMessage = $"[Home Case -99] Homing Procedure Unknown Error Occurred";
                                _hardwareManager.Robot.AllStop();
                                break;
                        }


                        //Thread.Sleep(100);
                        await Task.Delay(100);
                    } while (HomeProcedureCase >= 0);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RobotController HomeProcedure Exception: {ex.Message}");
                //LastError.ErrorCode = 80;
                //LastError.ErrorMessage = ex.Message;
                _homeError.ErrorCode = 50;
                _homeError.ErrorMessage = ex.Message;
            }
            IsProcedureRunning = false;
            return result;
        }

        #endregion

        #region Position Procedure

        /// <summary>
        /// Determines whether movement is possible along the specified axis.
        /// </summary>
        /// <param name="axisId">The identifier of the axis to check. Must be a valid axis ID.</param>
        /// <returns><see langword="true"/> if movement is possible along the specified axis; otherwise, <see langword="false"/>.</returns>
        public bool CanMove(int axisId)
        {
            bool result = false;

            if (axisId == 0) // Rotate Axis
            {
                if (_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
                {
                    if (_hardwareManager.Robot.Motors[2].MotorInfo.Pos_Actual >= _hardwareManager.Robot.Motors[2].MotorInfo.OpDataArray[19].Position) // Z Axis at upper limit
                    {
                        result = true;
                    }
                }
                else if (_hardwareManager.Robot.Motors[1].MotorInfo.Pos_Actual < 500)
                    result = true; // Y Axis at negative position
            }
            else if (axisId == 1) // Y Axis
            {
                if (_hardwareManager.Robot.InPositions(0, new int[] { 0, 1, 2 })) // Rotate Axis at position 0, 1, 2
                {
                    if (_hardwareManager.Robot.InPositions(2, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }))
                    {
                        result = true;
                    }
                }
            }
            else if (axisId == 2) // Z Axis
            {
                if (_hardwareManager.Robot.InPositions(0, new int[] { 0, 1, 2 })) // Rotate Axis at position 0, 1, 2
                {
                    if (!_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
                    {
                        if (_hardwareManager.Robot.ZAxisBetweenSlotOrCar()) // Z Axis between slot or car
                            result = true;
                    }
                    else
                        result = true;

                }
            }

            return result;
        }
        public string CanMoveMessage(int axisId)
        {
            var _motor_rot_info = _hardwareManager.Robot.Motors[0].MotorInfo;
            var _motor_y_info = _hardwareManager.Robot.Motors[1].MotorInfo;
            var _motor_z_info = _hardwareManager.Robot.Motors[2].MotorInfo;
            string result = string.Empty;
            if (axisId == 0) // Rotate Axis
            {
                if (CanMove(0))
                    result =  "Theta Axis can move.";
                else
                {
                    if (!_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
                    {
                        if(_hardwareManager.Robot.Motors[1].MotorInfo.Pos_Actual > 500)
                            result = $"Y Axis ({_motor_y_info.Pos_Actual}) is not at negative position.";
                        else
                            result = $"Y Axis ({_motor_y_info.Pos_Actual}) not at position 0 ({_motor_y_info.OpDataArray[0].Position}).";
                    }
                    else if (!(_motor_z_info.Pos_Actual >= _motor_z_info.OpDataArray[19].Position)) // Z Axis at upper limit
                    {
                        result = $"Z Axis ({_motor_z_info.Pos_Actual}) is up than upper limit ({_motor_z_info.OpDataArray[19].Position}).";
                    }
                    else
                    {
                        result = "Theta Axis cannot move.";
                    }
                }
            }
            else if (axisId == 1) // Y Axis
            {
                if (CanMove(1))
                    result =  "Y Axis can move.";
                else
                {
                    if (!(_hardwareManager.Robot.InPositions(0, new int[] { 0, 1, 2 }))) // Rotate Axis at position 0, 1, 2
                    {
                        result = $"Rotate Axis not at position 0, 1, or 2.";
                    }
                    else if (!(_hardwareManager.Robot.InPositions(2, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 })))
                    {
                        result = "Z Axis not at valid position.";
                    }
                    else
                    {
                        result = "Y Axis cannot move.";
                    }
                }
            }
            else if (axisId == 2) // Z Axis
            {
                if (CanMove(2))
                    result = "Z Axis can move.";
                else
                {
                    if (!(_hardwareManager.Robot.InPositions(0, new int[] { 0, 1, 2 }))) // Rotate Axis at position 0, 1, 2
                    {
                        result = "Rotate Axis not at position 0, 1, or 2.";
                    }
                    else if (!_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
                    {
                        result = "Y Axis not at position 0.";
                    }
                    else if (!_hardwareManager.Robot.ZAxisBetweenSlotOrCar()) // Z Axis between slot or car
                    {
                        result = "Z Axis not between slot or car.";
                    }
                    else
                        result = "Z Axis cannot move.";
                }
            }
            return result;
        }
        public async Task<bool> ExecutePosAct()
        {
            bool result = true;

            PosErrorFrame errorFrame = new PosErrorFrame();

            //if (IsProcedureRunning) return false;
            IsProcedureRunning = true;
            _cts = new CancellationTokenSource();
            try
            {
                foreach (var frame in _procedureFrames)
                {
                    if (!IsProcedureRunning) break;
                    if (frame is PosFrame posFrame)
                    {
                        ProcedureStatusMessage = $"AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}, Name={posFrame.Name}, Description={posFrame.Description}";

                        // 檢查軸是否可以動作
                        if (!CanMove(posFrame.AxisId))
                        {
                            result = false;
                            errorFrame.AxisId = posFrame.AxisId;
                            errorFrame.ErrorCode = 91;
                            errorFrame.ErrorMessage = $"Axis cannot move: AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}";
                            _logger.LogError(errorFrame.ErrorMessage);
                            LastError = errorFrame.Clone();
                            break;
                        }

                        // 設定每次動作的 timeout
                        using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                        {
                            errorFrame.AxisId = posFrame.AxisId;
                            errorFrame.ErrorCode = 90;
                            errorFrame.ErrorMessage = $"PosFrame Timeout: AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}, Timeout={posFrame.Timeout_ms}ms";
                            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(posFrame.Timeout_ms));
                            bool act_result = await _hardwareManager.Robot.MoveToPositionAsync(
                                posFrame.AxisId, posFrame.PosDataNo, timeoutCts.Token);

                            await Task.Delay((int)posFrame.DelayTime_ms);
                            // 可依 act_result 處理結果
                            if (!act_result)
                            {
                                result = false;
                                errorFrame.AxisId = posFrame.AxisId;
                                errorFrame.ErrorCode = 92;
                                //errorFrame.ErrorMessage = $"MoveToPositionAsync failed: AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}, Message={_hardwareManager.Robot.ErrorMessage}";
                                errorFrame.ErrorMessage = $"MoveToPositionAsync failed: {ConvertMovedAxisAndPosition(posFrame.AxisId, posFrame.PosDataNo)}, Message={_hardwareManager.Robot.ErrorMessage}";
                                _logger.LogError(errorFrame.ErrorMessage);
                                LastError = errorFrame.Clone();
                                break;
                            }
                        }
                    }
                    else if (frame is SensorFrame sensorFrame)
                    {
                        // 感測器動作
                        bool sensor_result = (bool)GPIOService.GetValue(sensorFrame.SensorName) == sensorFrame.CheckStatus;

                        if (HardwareManager.SensorCheckPass) // 測試用，強制感測器檢查通過
                        {
                        }
                        else if (!sensor_result)
                        {
                            errorFrame.AxisId = -1;
                            errorFrame.ErrorCode = 94;
                            errorFrame.ErrorMessage = $"Sensor check failed: SensorName={sensorFrame.SensorName}, ExpectedStatus={sensorFrame.CheckStatus}";
                            _logger.LogError(errorFrame.ErrorMessage);
                            LastError = errorFrame.Clone();
                            if (sensorFrame.IfCheckPoint)
                                this.checkSensorPoint = true;
                            result = false;
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 可選：處理中斷後的清理
                _hardwareManager.Robot.AllStop();
                if (_procedureStopTrigger)
                {
                    // 使用者主動中止
                    errorFrame.AxisId = -1;
                    errorFrame.ErrorCode = 93;
                    errorFrame.ErrorMessage = "Procedure Stopped by User.";

                }

                _logger.LogError(errorFrame.ErrorMessage);
                LastError = errorFrame.Clone();
                result = false;
            }
            finally
            {
                ProcedureStatusMessage = $"ExecutePosAct Finished!";
                IsProcedureRunning = false;
                _cts = null;
            }

            return result;
        }

        /// <summary>
        /// Execute Position Action When Homing
        /// The same as ExecutePosAct but without checking CanMove
        /// This is used in Homing Procedure
        /// This function only execute PosFrame in the procedure frames
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ExecutePosActWhenHoming()
        {
            bool result = true;

            PosErrorFrame errorFrame = new PosErrorFrame();

            //if (IsProcedureRunning) return false;
            IsProcedureRunning = true;
            _cts = new CancellationTokenSource();
            try
            {
                foreach (var frame in _procedureFrames)
                {
                    if (!IsProcedureRunning) break;
                    if (frame is PosFrame posFrame)
                    {
                        ProcedureStatusMessage = $"AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}, Name={posFrame.Name}, Description={posFrame.Description}";

                        // 設定每次動作的 timeout
                        using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                        {
                            errorFrame.AxisId = posFrame.AxisId;
                            errorFrame.ErrorCode = 90;
                            errorFrame.ErrorMessage = $"PosFrame Timeout: AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}, Timeout={posFrame.Timeout_ms}ms";
                            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(posFrame.Timeout_ms));
                            bool act_result = await _hardwareManager.Robot.MoveToPositionAsync(
                                posFrame.AxisId, posFrame.PosDataNo, timeoutCts.Token);

                            await Task.Delay((int)posFrame.DelayTime_ms);
                            // 可依 act_result 處理結果
                            if (!act_result)
                            {
                                result = false;
                                errorFrame.AxisId = posFrame.AxisId;
                                errorFrame.ErrorCode = 92;
                                //errorFrame.ErrorMessage = $"MoveToPositionAsync failed: AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}, Message={_hardwareManager.Robot.ErrorMessage}";
                                errorFrame.ErrorMessage = $"MoveToPositionAsync failed: {ConvertMovedAxisAndPosition(posFrame.AxisId, posFrame.PosDataNo)}, Message={_hardwareManager.Robot.ErrorMessage}";
                                _logger.LogError(errorFrame.ErrorMessage);
                                LastError = errorFrame.Clone();
                                break;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 可選：處理中斷後的清理
                _hardwareManager.Robot.AllStop();
                if (_procedureStopTrigger)
                {
                    // 使用者主動中止
                    errorFrame.AxisId = -1;
                    errorFrame.ErrorCode = 93;
                    errorFrame.ErrorMessage = "Procedure Stopped by User.";

                }

                _logger.LogError(errorFrame.ErrorMessage);
                LastError = errorFrame.Clone();
                result = false;
            }
            finally
            {
                ProcedureStatusMessage = $"ExecutePosAct Finished!";
                IsProcedureRunning = false;
                _cts = null;
            }

            return result;
        }

        #endregion

        #region Auto Procedure

        public async Task ExecuteAutoAct()
        {
            MainProcedureCase = 0;
            //IsProcedureRunning = true;
            IsMainProcedureRunning = true;

            int swapIn = 0, swapOut = 0;
            int[] swapOuts = null;

            try
            {
                if (_hardwareManager.modbusRTUService.IsRunning)
                {
                    do
                    {
                        // 程序中止
                        if (this._mainProcedureStopTrigger)
                        {
                            this._mainProcedureStopTrigger = false;
                            MainProcedureCase = -98;
                            break;
                        }

                        switch (MainProcedureCase)
                        {
                            case 0: // 檢查是否已經執行原點復歸
                                MainProcedureStatusMessage = $"[Auto Case 0] Checking if Homing is Finished";
                                if (this.IsHomeFinished)
                                {
                                    MainProcedureCase = 1;
                                }
                                else
                                {
                                    MainProcedureCase = -1;
                                    LastError.ErrorCode = 11;
                                    LastError.ErrorMessage = $"尚未執行原點復歸";
                                }
                                
                                break;

                            case 1: // 檢查電池狀態及產生路徑點位
                                if (IsTestMode) // 測試模式下使用測試程序的路徑
                                {
                                    MainProcedureStatusMessage = $"[Auto Case 1] Generating Swap Slot Info for Test Procedure";
                                    if(CheckSwapSlotInfoForTest(TestProcedureIndex, TestExecuteIndex, out swapIn, out swapOut))
                                    {
                                        MainProcedureCase = 10;
                                    }
                                    else
                                    {
                                        MainProcedureCase = -1;
                                        LastError.ErrorCode = 12;
                                        LastError.ErrorMessage = $"無法產生路徑";
                                    }
                                }
                                else // 正常模式下使用實際的路徑產生
                                {
                                    MainProcedureStatusMessage = $"[Auto Case 1] Generating Swap Slot Info";
                                    if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOuts))
                                    {
                                        swapOut = _hardwareManager.SwapOut(swapOuts);
                                        MainProcedureCase = 10;
                                    }
                                    else
                                    {
                                        MainProcedureCase = -1;
                                        LastError.ErrorCode = 12;
                                        LastError.ErrorMessage = $"無法產生路徑";
                                    }
                                }
                                break;

                            case 10: // 旋轉到R-0
                                MainProcedureStatusMessage = $"[Auto Case 10] Rotating to R-0 Position";
                                StartRotateProcedure(0);
                                MainProcedureCase = 11;
                                break;

                            case 11: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 11] Checking Rotate Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 20;
                                    else
                                        MainProcedureCase = -97;  
                                }
                                break;

                            case 20: // 從車上取出電池
                                MainProcedureStatusMessage = $"[Auto Case 20] Taking Battery from Car";
                                StartTakeCarBatteryProcedure();
                                MainProcedureCase = 21;
                                break;

                            case 21: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 21] Checking Take Car Battery Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 30;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 30: // 旋轉到Slot放置面
                                MainProcedureStatusMessage = $"[Auto Case 30] Rotating to Slot Placement Position for Slot {swapIn}";
                                StartRotateProcedure(this.GetRotatePosNo(swapIn));
                                MainProcedureCase = 31;
                                break;

                            case 31: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 31] Checking Rotate Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 40;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 40: // 放置電池到slot中
                                MainProcedureStatusMessage = $"[Auto Case 40] Placing Battery into Slot {swapIn}";
                                StartPlaceSlotBatteryProcedure(this.GetPlacePosNo(swapIn));
                                MainProcedureCase = 41;
                                break;

                            case 41: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 41] Checking Place Slot Battery Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 42;
                                    else if(IsTestMode) // 測試模式下不進行感測器檢查點判斷
                                    {
                                        // 測試模式下直接結束
                                        MainProcedureCase = -97;
                                    }
                                    else // 正常模式下進行感測器檢查點判斷
                                    {
                                        if (checkSensorPoint)
                                        {
                                            // 感測器檢查點失敗，代表slot已經有電池，無法放入，將註記slot狀態為狀態錯誤，並在近進行一次路徑判別
                                            _hardwareManager.SlotServices.SetBatteryMemory(swapIn - 1, true, false);
                                            _hardwareManager.SlotServices.TransitionTo(swapIn - 1, SlotState.StateError);
                                            if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOuts))
                                            {
                                                swapOut = _hardwareManager.SwapOut(swapOuts);
                                                MainProcedureCase = 30;
                                            }
                                            else
                                            {
                                                MainProcedureCase = -1;
                                                LastError.ErrorCode = 11;
                                                LastError.ErrorMessage = $"無法產生路徑";
                                            }
                                        }
                                        else
                                            MainProcedureCase = -97;
                                    }
                                }
                                break;

                            case 42: // 成功放入電池
                                MainProcedureStatusMessage = $"[Auto Case 42] Successfully Placed Battery into Slot {swapIn}";
                                _hardwareManager.SlotServices.TransitionTo(swapIn - 1, SlotState.Idle); // 成功放入電池，將slot狀態改為Idle
                                _slotServices.SetBatteryMemory(swapIn - 1, true);
                                MainProcedureCase = 43;
                                break;

                            case 43: // 檢查是否在同一側
                                MainProcedureStatusMessage = $"[Auto Case 43] Checking if Swap In and Swap Out are on the Same Side";
                                if (this.IsSameSide(swapIn, swapOut))
                                {
                                    MainProcedureCase = 52; // 同側直接進行取電池
                                }
                                else
                                {
                                    MainProcedureCase = 50; // 不同側需要先旋轉到R-?
                                }
                                break;

                            case 50: // 旋轉到R-?
                                MainProcedureStatusMessage = $"[Auto Case 50] Rotating to Slot Removal Position for Slot {swapOut}";
                                StartRotateProcedure(this.GetRotatePosNo(swapOut));
                                break;

                            case 51: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 51] Checking Rotate Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 52;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 52: // 取出電池前先停止充電
                                MainProcedureStatusMessage = $"[Auto Case 52] Stopping Charging before Taking Battery from Slot {swapOut}";
                                _hardwareManager.SlotServices.TransitionTo(swapOut - 1, SlotState.StopCharge); // 取出電池前將slot狀態改為StopCharge
                                _hardwareManager.Charger[swapOut-1].StopCharging(); // 取出電池前先停止充電
                                MainProcedureCase = 60;
                                break;

                            case 60: // 從slot取出電池
                                MainProcedureStatusMessage = $"[Auto Case 60] Taking Battery from Slot {swapOut}";
                                StartTakeSlotBatteryProcedure(this.GetTakePosNo(swapOut));
                                MainProcedureCase = 61;
                                break;

                            case 61: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 61] Checking Take Slot Battery Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 62;
                                    else if(IsTestMode) // 測試模式下不進行感測器檢查點判斷
                                    {
                                        // 測試模式下直接結束
                                        MainProcedureCase = -97;
                                    }
                                    else
                                    {
                                        if (checkSensorPoint)
                                        {
                                            // 感測器檢查點失敗，代表slot已經沒電池，無法取出，將註記slot狀態為空，並在近進行一次路徑判別
                                            _hardwareManager.SlotServices.SetBatteryMemory(swapOut - 1, false, false);
                                            _hardwareManager.SlotServices.TransitionTo(swapOut - 1, SlotState.StateError);
                                            if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOuts))
                                            {
                                                swapOut = _hardwareManager.SwapOut(swapOuts);
                                                MainProcedureCase = 50;
                                            }
                                            else
                                            {
                                                MainProcedureCase = -1;
                                                LastError.ErrorCode = 11;
                                                LastError.ErrorMessage = $"無法產生路徑";
                                            }
                                        }
                                        else
                                            MainProcedureCase = -97;
                                    }
                                }
                                break;

                            case 62: // 成功取出電池
                                MainProcedureStatusMessage = $"[Auto Case 62] Successfully Took Battery from Slot {swapOut}";
                                _slotServices.SetBatteryMemory(swapOut - 1, false);
                                _hardwareManager.SlotServices.TransitionTo(swapOut - 1, SlotState.Initialization); // 成功取出電池，將slot狀態改為Empty
                                
                                MainProcedureCase = 70;
                                break;

                            case 70: // 旋轉到R-0
                                MainProcedureStatusMessage = $"[Auto Case 70] Rotating to R-0 Position";
                                StartRotateProcedure(0);
                                MainProcedureCase = 71;
                                break;

                            case 71: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 71] Checking Rotate Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 80;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 80: // 將電池放回車上
                                MainProcedureStatusMessage = $"[Auto Case 80] Placing Battery back to Car";
                                StartPlaceCarBatteryProcedure();
                                MainProcedureCase = 81;
                                break;

                            case 81: // 檢查 Procedure 結束後是否有錯誤
                                MainProcedureStatusMessage = $"[Auto Case 81] Checking Place Car Battery Procedure Result";
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 90; // 整個程序結束
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 90: // Procedure Success
                                MainProcedureStatusMessage = $"[Auto Case 90] Auto Procedure Completed Successfully";
                                if (IsTestMode) // 測試模式下檢查是否完成所有測試循環
                                {
                                    if (CheckFinishAllTestCycles())
                                    {
                                        _stationStateMachine.HandleTransition(ChargingState.Idle);
                                        IsTestMode = false; // 結束測試模式
                                        MainProcedureCase = -1;
                                    }
                                    else
                                    {
                                        MainProcedureCase = 1; // 繼續下一次測試程序
                                    }
                                }
                                else
                                {

                                    // 整個程序成功結束，將station狀態更新
                                    _stationStateMachine.HandleTransition(ChargingState.Idle);
                                    MainProcedureCase = -1;
                                }
                                break;

                            case -97: // Procedure Error
                                MainProcedureStatusMessage = $"[Auto Case -97] Auto Procedure Error Occurred";
                                break;

                            case -98: // Force to stop all procedure
                                MainProcedureStatusMessage = $"[Auto Case -98] Auto Procedure Manually Stopped";
                                break;

                            case -99: // Unknow Error
                            default:
                                MainProcedureStatusMessage = $"[Auto Case -99] Auto Procedure Unknown Error Occurred";
                                _hardwareManager.Robot.AllStop();
                                break;
                        }

                        await Task.Delay(10);
                    }
                    while (MainProcedureCase >= 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RobotController MainProcedure Exception: {ex.Message}");
                LastError.ErrorCode = 81;
                LastError.ErrorMessage = ex.Message;
            }
            IsMainProcedureRunning = false;
        }

        #endregion

        #region Test Function for Auto Procedure

        public bool IsTestMode { get; internal set; } = false; // Enable test mode for auto procedure
        public int TestProcedureIndex { get; set; } = 1; // 1: Test#1 
        public string TestProcedureName
        {
            get
            {
                switch (TestProcedureIndex)
                {
                    case 1:
                        return "Default Test#1 Procedure";
                    default:
                        return "Unknown Test Procedure";
                }
            }
        }

        public int TestStartIndex { get; set; } = 0; // Start index for test mode: Different TestProcedureIndex may have different meaning

        public int TestExecuteIndex { get; internal set; } = 0; // Execute index for test mode: Different TestProcedureIndex may have different meaning
        public int TestCycleNumber { get; set; } = 5; // Number of cycles to run in test mode
        public int TestCycleCount { get; internal set; } = 0; // Current cycle count

        /// <summary>
        /// Check Swap Slot Info for Test Mode
        /// </summary>
        /// <param name="testProcedureIndex"></param>
        /// <param name="testExecuteIndex"></param>
        /// <param name="swapIn"></param>
        /// <param name="swapOut"></param>
        /// <returns></returns>
        public bool CheckSwapSlotInfoForTest(int testProcedureIndex, int testExecuteIndex, out int swapIn, out int swapOut)
        {
            swapIn = 1;
            swapOut = 2;

            if (IsTestMode)
            {
                if (TestProcedureIndex == 1)
                {
                    DefaultTest1Procedure.Refresh();
                    int length =  DefaultTest1Procedure.ProcedureFrames.Count;

                    // Set swapIn and swapOut based on testExecuteIndex
                    if (testExecuteIndex < length)
                    {
                        var frame = DefaultTest1Procedure.ProcedureFrames[testExecuteIndex]; // Get the frame based on index
                        if (frame is TestSlotFrame testFrame) // Check if it's a TestSlotFrame
                        {
                            swapIn = testFrame.SlotSwapInNo;
                            swapOut = testFrame.SlotSwapOutNo;

                            return true;
                        }
                    }
                }

            }
            return false;
        }

        /// <summary>
        /// Check Finish All Test Cycles
        /// </summary>
        /// <returns></returns>
        public bool CheckFinishAllTestCycles()
        {
            bool result = false;
            if (IsTestMode)
            {
                if (TestProcedureIndex == 1)
                {
                    DefaultTest1Procedure.Refresh();
                    int length = DefaultTest1Procedure.ProcedureFrames.Count;
                    TestExecuteIndex++;
                    if (TestExecuteIndex >= length)
                    {
                        TestExecuteIndex = 0;
                        TestCycleCount++;
                    }

                    if (TestCycleCount >= TestCycleNumber)
                        result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Set Slot Info for Test Mode
        /// </summary>
        /// <param name="testProcedureIndex"></param>
        /// <param name="testExecuteIndex"></param>
        public void SetSlotInfoForTest(int testProcedureIndex, int testExecuteIndex)
        {
            if (testProcedureIndex == 1)
            {
                DefaultTest1Procedure.Refresh();
                int length = DefaultTest1Procedure.ProcedureFrames.Count;

                for (int i=0;i< length; i++)
                {
                    bool empty = (testExecuteIndex == i);

                    if (empty)
                        _hardwareManager.SlotServices.SetBatteryMemory(i, false);
                    else
                        _hardwareManager.SlotServices.SetBatteryMemory(i, true);
                    _hardwareManager.SlotServices.TransitionTo(i, SlotState.Initialization);
                }
            }
        }

        #endregion

        #region Command Function

        public string ConvertMovedAxisAndPosition(int axis, int position)
        {
            string result = "";

            switch (axis)
            {
                case 0: result = "旋轉軸"; break;
                case 1: result = "Y軸"; break;
                case 2: result = "Z軸"; break;
                default: result = $"未知軸-{axis}"; break;
            }

            result += $"移動到位置{position}";

            return result;
        }

        #endregion
    }
}
