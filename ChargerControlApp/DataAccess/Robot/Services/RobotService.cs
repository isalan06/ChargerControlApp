using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Robot.Models;
using ChargerControlApp.DataAccess.Slot.Services;
using ChargerControlApp.Hardware;
using System.Threading.Tasks;

namespace ChargerControlApp.DataAccess.Robot.Services
{
    public class RobotService : IDisposable
    {
        private readonly HardwareManager _hardwareManager;

        public bool IsProcedureRunning { get; internal set; }
        public bool IsMainProcedureRunning { get; internal set; }
        public int HomeProcedureCase { get; internal set; }

        private bool _procedureStopTrigger = false;
        private bool _mainProcedureStopTrigger = false;
        private CancellationTokenSource _cts;

        private List<ProcedureFrame> _procedureFrames = new List<ProcedureFrame>();
        public PosErrorFrame LastError { get; internal set; } = new PosErrorFrame();

        public string ProcedureStatusMessage { get; internal set; } = string.Empty;
        public string MainProcedureStatusMessage { get; internal set; } = string.Empty;
        public int MainProcedureCase { get; internal set; }
        private bool checkSensorPoint = false;
        public bool CheckSensorPoint { get { return checkSensorPoint; } }   

        private readonly ILogger<RobotService> _logger;


        #region Information

        public bool IsHomeFinished { get {
                return (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_High.Bits.HOME_END
                    && _hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_High.Bits.HOME_END
                    && _hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_High.Bits.HOME_END
                    );
            } }
        public bool CanHome { get {
                return (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE &&
                    _hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE &&
                    _hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE
                    );
            } }

        #endregion

        #region constructor

        public RobotService(IServiceProvider serviceProvider)
        {
            // 取得 HardwareManager 實例
            _hardwareManager = serviceProvider.GetRequiredService<HardwareManager>();
            _logger = serviceProvider.GetRequiredService<ILogger<RobotService>>();
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
            if (!IsProcedureRunning) return;
            IsProcedureRunning = false;
            IsMainProcedureRunning = false;
            this._procedureStopTrigger = true;
            this._mainProcedureStopTrigger = true;
            _cts?.Cancel();
            _hardwareManager.Robot.AllStop();
            ProcedureStatusMessage = string.Empty;
            MainProcedureStatusMessage = string.Empty;

        }

        /// <summary>
        /// Start Home Procedure
        /// </summary>
        public void StartHomeProcedure()
        {
            LastError.Clear();
            HomeProcedureCase = 0;
            if (IsProcedureRunning) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await HomeProcedure();
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
            _procedureFrames = DefaultRotateProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
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
        /// <param name="slotNo"></param>
        public void StartTakeSlotBatteryProcedure(int slotNo)
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            checkSensorPoint = false;
            DefaultTakeSlotBatteryProcedure.Z_Input = slotNo;
            _procedureFrames = DefaultTakeSlotBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _logger.LogInformation($"Start to execute procedure of taking battery from slot #{slotNo}");
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        /// <summary>
        /// Start Place Slot Battery Procedure
        /// </summary>
        /// <param name="slotNo"></param>
        public void StartPlaceSlotBatteryProcedure(int slotNo)
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            IsProcedureRunning = true;
            checkSensorPoint = false;
            DefaultPlaceSlotBatteryProcedure.Z_Input = slotNo;
            _procedureFrames = DefaultPlaceSlotBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _logger.LogInformation($"Start to execute procedure of placing battery to slot #{slotNo}");
            Task.Run(async () =>
            {
                await ExecutePosAct();
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

        #endregion

        #region Home Procedure

        // 三軸復歸程序
        public async Task<bool> HomeProcedure()
        {
            bool result = false;
            HomeProcedureCase = 0;
            IsProcedureRunning = true;
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
                            break;
                        }

                        switch (HomeProcedureCase)
                        {
                            case 0: // 等待三軸 RDY-HOME-OPE
                                if (CanHome)
                                {
                                    _hardwareManager.Robot.Home(1, true);
                                    HomeProcedureCase = 10;
                                }
                                break;

                            case 10: // Y軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                if ((_hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (_hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.MOVE == true))
                                {
                                    _hardwareManager.Robot.Home(1, false);
                                    HomeProcedureCase = 11;
                                }
                                break;

                            case 11: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                if ((_hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (_hardwareManager.Robot.Motors[1].MotorInfo.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 20; // Y軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                }
                                break;

                            case 20: // 等待三軸 RDY-HOME-OPE
                                if (CanHome)
                                {
                                    _hardwareManager.Robot.Home(2, true);
                                    HomeProcedureCase = 21;
                                }
                                break;

                            case 21: // Z軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                if ((_hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (_hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.MOVE == true))
                                {
                                    _hardwareManager.Robot.Home(2, false);
                                    HomeProcedureCase = 22;
                                }
                                break;

                            case 22: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                if ((_hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (_hardwareManager.Robot.Motors[2].MotorInfo.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = 30; // Z軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                }
                                break;

                            case 30: // 等待三軸 RDY-HOME-OPE
                                if (CanHome)
                                {
                                    _hardwareManager.Robot.Home(0, true);
                                    HomeProcedureCase = 31;
                                }
                                break;

                            case 31: // 旋轉軸復歸, 確認RDY-HOME-OPE訊號 -> OFF 且 MOVE訊號 -> ON
                                if ((_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == false) &&
                                    (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.MOVE == true))
                                {
                                    _hardwareManager.Robot.Home(0, false);
                                    HomeProcedureCase = 32;
                                }
                                break;

                            case 32: // 等待 RDY-HOME-OPE訊號 -> ON 且 MOVE訊號 -> OFF
                                if ((_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE == true) &&
                                    (_hardwareManager.Robot.Motors[0].MotorInfo.IO_Output_Low.Bits.MOVE == false))
                                {
                                    HomeProcedureCase = -1; // 旋轉軸復歸完成 若還要再進行其他動作，請在此設定 caseIndex
                                }
                                break;

                            case -98: // Force to stop all procedure
                                break;

                            case -99: // Unknow Error
                            default:
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
                LastError.ErrorCode = 80;
                LastError.ErrorMessage = ex.Message;
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
                    if (_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
                    {
                        if (_hardwareManager.Robot.ZAxisBetweenSlotOrCar()) // Z Axis between slot or car
                            result = true;
                    }
                }
            }

            return result;
        }

        public async Task<bool> ExecutePosAct()
        {
            bool result = true;

            PosErrorFrame errorFrame = new PosErrorFrame();

            if (IsProcedureRunning) return false;
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
                                errorFrame.ErrorMessage = $"MoveToPositionAsync failed: AxisId={posFrame.AxisId}, PosDataNo={posFrame.PosDataNo}";
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

                        if (!sensor_result)
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

        #endregion

        #region Auto Procedure

        public async Task ExecuteAutoAct()
        {
            MainProcedureCase = 0;
            IsProcedureRunning = true;

            int swapIn = 0, swapOut = 0;

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
                                if (this.CanHome)
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
                                if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOut))
                                {
                                    MainProcedureCase = 10;
                                }
                                else
                                {
                                    MainProcedureCase = -1;
                                    LastError.ErrorCode = 11;
                                    LastError.ErrorMessage = $"無法產生路徑";
                                }
                                break;

                            case 10: // 旋轉到R-0
                                StartRotateProcedure(0);
                                MainProcedureCase = 11;
                                break;

                            case 11: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 20;
                                    else
                                        MainProcedureCase = -97;  
                                }
                                break;

                            case 20: // 從車上取出電池
                                StartTakeCarBatteryProcedure();
                                MainProcedureCase = 21;
                                break;

                            case 21: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 30;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 30: // 旋轉到Slot放置面
                                StartRotateProcedure(this.GetRotatePosNo(swapIn));
                                MainProcedureCase = 31;
                                break;

                            case 31: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 40;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 40: // 放置電池到slot中
                                StartPlaceSlotBatteryProcedure(this.GetPlacePosNo(swapIn));
                                MainProcedureCase = 41;
                                break;

                            case 41: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 42;
                                    else
                                    {
                                        if(checkSensorPoint)
                                        {
                                            // 感測器檢查點失敗，代表slot已經有電池，無法放入，將註記slot狀態為狀態錯誤，並在近進行一次路徑判別
                                            _hardwareManager.SlotServices.SetBatteryMemory(swapIn - 1, true, false);
                                            _hardwareManager.SlotServices.TransitionTo(swapIn - 1, SlotState.StateError);
                                            if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOut))
                                            {
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
                                _hardwareManager.SlotServices.TransitionTo(swapIn - 1, SlotState.Idle); // 成功放入電池，將slot狀態改為Idle
                                MainProcedureCase = 43;
                                break;

                            case 43: // 檢查是否在同一側
                                if(this.IsSameSide(swapIn, swapOut))
                                {
                                    MainProcedureCase = 52; // 同側直接進行取電池
                                }
                                else
                                {
                                    MainProcedureCase = 50; // 不同側需要先旋轉到R-?
                                }
                                break;

                            case 50: // 旋轉到R-?
                                StartRotateProcedure(this.GetRotatePosNo(swapOut));
                                break;

                            case 51: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 52;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 52: // 取出電池前先停止充電
                                _hardwareManager.SlotServices.TransitionTo(swapOut - 1, SlotState.StopCharge); // 取出電池前將slot狀態改為StopCharge
                                _hardwareManager.Charger[swapOut-1].StopCharging(); // 取出電池前先停止充電
                                MainProcedureCase = 60;
                                break;

                            case 60: // 從slot取出電池
                                StartTakeSlotBatteryProcedure(this.GetTakePosNo(swapOut));
                                MainProcedureCase = 61;
                                break;

                            case 61: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 70;
                                    else
                                    {
                                        if (checkSensorPoint)
                                        {
                                            // 感測器檢查點失敗，代表slot已經沒電池，無法取出，將註記slot狀態為空，並在近進行一次路徑判別
                                            _hardwareManager.SlotServices.SetBatteryMemory(swapOut - 1, false, false);
                                            _hardwareManager.SlotServices.TransitionTo(swapOut - 1, SlotState.StateError);
                                            if (_hardwareManager.SlotServices.GetSwapSlotInfo(out swapIn, out swapOut))
                                            {
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

                            case 70: // 旋轉到R-0
                                StartRotateProcedure(0);
                                MainProcedureCase = 71;
                                break;

                            case 71: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 80;
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 80: // 將電池放回車上
                                StartPlaceCarBatteryProcedure();
                                MainProcedureCase = 81;
                                break;

                            case 81: // 檢查 Procedure 結束後是否有錯誤
                                if (!this.IsProcedureRunning)
                                {
                                    if (LastError.ErrorCode == 0)
                                        MainProcedureCase = 90; // 整個程序結束
                                    else
                                        MainProcedureCase = -97;
                                }
                                break;

                            case 90: // Procedure Success
                                // 整個程序成功結束，將station狀態更新
                                MainProcedureCase = -1;
                                break;

                            case -97: // Procedure Error
                                break;

                            case -98: // Force to stop all procedure
                                break;

                            case -99: // Unknow Error
                            default:
                                _hardwareManager.Robot.AllStop();
                                break;
                        }

                        await Task.Delay(100);
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
            IsProcedureRunning = false;
        }

        #endregion
    }
}
