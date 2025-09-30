using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Robot.Models;
using ChargerControlApp.Hardware;

namespace ChargerControlApp.DataAccess.Robot.Services
{
    public class RobotService : IDisposable
    {
        private readonly HardwareManager _hardwareManager;

        public bool IsProcedureRunning { get; internal set; }
        public int HomeProcedureCase { get; internal set; }

        private bool _procedureStopTrigger = false;
        private CancellationTokenSource _cts;

        private List<ProcedureFrame> _procedureFrames = new List<ProcedureFrame>();
        public PosErrorFrame LastError { get; internal set; } = new PosErrorFrame();

        public string ProcedureStatusMessage { get; internal set; } = string.Empty;


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

        public void StopProcedure()
        {
            if (!IsProcedureRunning) return;
            IsProcedureRunning = false;
            this._procedureStopTrigger = true;
            _cts?.Cancel();
            _hardwareManager.Robot.AllStop();
            ProcedureStatusMessage = string.Empty;

        }

        public void StartHomeProcedure()
        {
            LastError.Clear();
            if (IsProcedureRunning) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await HomeProcedure();
            }, token);
        }

        public void StartRotateProcedure(int targetPosNo)
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            _procedureFrames = DefaultRotateProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
        }

        public void StartTakeCarBatteryProcedure()
        {
            LastError.Clear();
            ProcedureStatusMessage = string.Empty;
            if (IsProcedureRunning) return;
            _procedureFrames = DefaultTakeCarBatteryProcedure.ProcedureFrames;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                await ExecutePosAct();
            }, token);
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
                if(_hardwareManager.Robot.InPosition(1, 0)) // Y Axis at position 0
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
                        if(_hardwareManager.Robot.ZAxisBetweenSlotOrCar()) // Z Axis between slot or car
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
                                LastError = errorFrame.Clone();
                                break;
                            }
                        }
                    }
                    else if(frame is SensorFrame sensorFrame)
                    {
                        // 感測器動作
                        bool sensor_result = (bool)GPIOService.GetValue(sensorFrame.SensorName) == sensorFrame.CheckStatus;

                        if (!sensor_result)
                        {
                            errorFrame.AxisId = -1;
                            errorFrame.ErrorCode = 94;
                            errorFrame.ErrorMessage = $"Sensor check failed: SensorName={sensorFrame.SensorName}, ExpectedStatus={sensorFrame.CheckStatus}";
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 可選：處理中斷後的清理
                _hardwareManager.Robot.AllStop();
                if(_procedureStopTrigger)
                {
                    // 使用者主動中止
                    errorFrame.AxisId = -1;
                    errorFrame.ErrorCode = 93;
                    errorFrame.ErrorMessage = "Procedure Stopped by User.";
                }

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

    }
}
