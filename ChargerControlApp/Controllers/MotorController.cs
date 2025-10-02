using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;
using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Models;
using ChargerControlApp.Models.Motor;
using ChargerControlApp.Test.Robot;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChargerControlApp.Controllers
{
    public class MotorController : Controller
    {
        private readonly ILogger<MotorController> _logger;
        private readonly RobotController _robotController;
        private readonly RobotService _robotService;
        private readonly RobotTestProcedure _robotTestProcedure;

        public MotorController(ILogger<MotorController> logger, RobotController robotController, RobotTestProcedure robotTestProcedure, RobotService robotService)
        {
            _logger = logger;
            _robotController = robotController;
            _robotTestProcedure = robotTestProcedure;
            _robotService = robotService;
        }
        public IActionResult Index()
        {
            return View();
        }

        // 新增此 Action 供 AJAX 呼叫
        [HttpGet]
        public IActionResult GetRobotStatus()
        {
            var status = _robotController.Motors[0].MotorInfo.IO_Output_Low.Bits.SON_MON;
            return Json(new { serverOn = status });
        }

        [HttpGet]
        public IActionResult GetMotor0Status()
        {
            var m = _robotController.Motors[0];
            return Json(new
            {
                posActual = m.MotorInfo.Pos_Actual,
                velActual = m.MotorInfo.Vel_Actual,
                errorCode = m.MotorInfo.ErrorCode,
                sOn = m.MotorInfo.IO_Output_Low.Bits.SON_MON
            });
        }

        [HttpGet]
        public IActionResult GetMotorsStatus()
        {
            var motors = _robotController.Motors;
            var result = motors.Select((m, i) => new
            {
                id = i,
                posActual = m.MotorInfo.Pos_Actual,
                velActual = m.MotorInfo.Vel_Actual,
                errorCode = m.MotorInfo.ErrorCode,
                errorMessage = m.MotorInfo.ErrorMessage,
                sOn = m.MotorInfo.IO_Output_Low.Bits.SON_MON,
                rdyDdOpe = m.MotorInfo.IO_Output_Low.Bits.RDY_DD_OPE,
                stopR = m.MotorInfo.IO_Output_Low.Bits.STOP_R,
                freeR = m.MotorInfo.IO_Output_Low.Bits.FREE_R,
                armA = m.MotorInfo.IO_Output_Low.Bits.ALM_A,
                sysBsy = m.MotorInfo.IO_Output_Low.Bits.SYS_BSY,
                inPos = m.MotorInfo.IO_Output_Low.Bits.IN_POS,
                rdyHomeOpe = m.MotorInfo.IO_Output_Low.Bits.RDY_HOME_OPE,
                rdyFwrvOpe = m.MotorInfo.IO_Output_Low.Bits.RDY_FWRV_OPE,
                rdySdOpe = m.MotorInfo.IO_Output_Low.Bits.RDY_SD_OPE,
                move = m.MotorInfo.IO_Output_Low.Bits.MOVE,
                selectDataNo = m.MotorInfo.OpData_IdSelect,
                currentDataNo = m.MotorInfo.OpData_IdOp,
                jogMode = m.MotorInfo.JogMode,
                dataNo = m.MotorInfo.CurrentDataNo,
                opData_IdSelect = m.MotorInfo.OpData_IdSelect,
                opData_IdOp = m.MotorInfo.OpData_IdOp,
                opData_Pos_Command = m.MotorInfo.OpData_Pos_Command,
                opData_Vel_Command = m.MotorInfo.OpData_Vel_Command,
                opData_Pos_Actual = m.MotorInfo.OpData_Pos_Actual,
                opData_Vel_Actual = m.MotorInfo.OpData_Vel_Actual,
                r0R = m.MotorInfo.IO_Output_High.Bits.R0_R,
                r1R = m.MotorInfo.IO_Output_High.Bits.R1_R,
                homeEnd = m.MotorInfo.IO_Output_High.Bits.HOME_END
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public IActionResult ToggleServo(int motorId)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            // 依需求切換伺服狀態
            // 例如：RobotController.ServerOn(motorId, !目前狀態);
            // 請根據您的伺服狀態取得方式調整
            bool newState = !_robotController.Motors[motorId].MotorInfo.IO_Input_Low.Bits.S_ON;
            bool result = _robotController.ServerOn(motorId, newState);

            return Json(new { success = result });
        }

        [HttpPost]
        public IActionResult SetAlarm(int motorId, bool state)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();

            _robotController.AlarmReset(motorId, state);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetHome(int motorId, bool state)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            _robotController.Home(motorId, state);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetStop(int motorId, bool state)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            _robotController.Stop(motorId, state);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetJogMode(int motorId, int mode)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            _robotController.SetJogMode(motorId, mode);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetJog(int motorId, string dir, bool state)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();

            // dir: "FW" or "RV"
            _robotController.SetJog(motorId, dir, state);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetDataNo_M(int motorId, int dataNo)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            _robotController.SetDataNo_M(motorId, dataNo);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetStart(int motorId, bool state)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            _robotController.SetStart(motorId, state);
            return Json(new { success = true });
        }

        // 取得 Jog&Home 參數
        [HttpGet]
        public IActionResult GetJogHomeParams(int motorId)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            var motorInfo = _robotController.Motors[motorId].MotorInfo;
            var values = motorInfo.Jog_Home_Setting.ToArray();
            return Json(values);
        }

        // 單一參數寫入
        [HttpPost]
        public IActionResult SetJogHomeParam([FromBody] JogHomeParamUpdateDto dto)
        {
            if (dto.MotorId < 0 || dto.MotorId >= _robotController.Motors.Length)
                return BadRequest();
            //var motorInfo = _robotController.Motors[dto.MotorId].MotorInfo;
            //if (dto.Index < 0 || dto.Index >= motorInfo.Motor_Jog_Home_Setting.Count)
              //  return BadRequest("Index out of range");
            //motorInfo.Motor_Jog_Home_Setting[dto.Index].Value = dto.Value;
            // 實際儲存邏輯
            return Ok();
        }

        // 批次寫入全部參數
        [HttpPost]
        public IActionResult SaveJogHomeParams([FromBody] JogHomeParamBatchUpdateDto dto)
        {
            if (dto.MotorId < 0 || dto.MotorId >= _robotController.Motors.Length)
                return BadRequest();
            var motorInfo  = _robotController.Motors[dto.MotorId].MotorInfo;
            if (dto.Values.Count != motorInfo.Jog_Home_Setting.ToArray().Length)
                return BadRequest("參數數量不符");
            int[] setValues = new int[dto.Values.Count];
            for (int i = 0; i < dto.Values.Count; i++)
            {
                setValues[i] = int.TryParse(dto.Values[i], out int val) ? val : 0;
            }

            motorInfo.Jog_Home_Setting.FromArray(setValues);
            // 實際儲存邏輯
            _robotController.WriteJogAndHomeSetting(dto.MotorId);
            return Ok();
        }

        
        [HttpPost]
        public IActionResult SetSpd(int motorId, string dir, bool state)
        {
            // 依照 dir（"FW" 或 "RV"）與 state（true/false）呼叫服務
            var result = _robotController.SetJogSpd(motorId, dir, state);
            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetPosVelData(int motorId)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();

            // 先從 Modbus 讀取最新資料
            _robotController.ReadOpData(motorId);

            // 建議加一點延遲，確保資料已經回來（依你的架構可調整，或用 await/Task）
            //System.Threading.Thread.Sleep(100); // 100ms，依實際情況可調整
            await Task.Delay(100);

            var opDataArray = _robotController.Motors[motorId].MotorInfo.OpDataArray;
            var result = new List<object>();
            for (int i = 0; i < 20; i++)
            {
                var op = (opDataArray != null && i < opDataArray.Length)
                    ? opDataArray[i]
                    : new MotorInfo.MotorOpDataDto();
                result.Add(new
                {
                    opType = op.OpType,
                    position = op.Position,
                    velocity = op.Velocity
                });
            }
            return Json(result);
        }

        [HttpPost]
        public IActionResult SetPosVelData([FromBody] PosVelUpdateDto dto)
        {
            // dto.index, dto.type ('position' or 'velocity'), dto.value
            // 請根據你的資料結構寫入對應 MotorInfo.OpDataArray
            // ...
            if(dto.MotorId < 0 || dto.MotorId >= _robotController.Motors.Length)
                return BadRequest();
            if (dto.Index < 0 || dto.Index >= _robotController.Motors[dto.MotorId].MotorInfo.OpDataArray.Length)
            {
                if (dto.Type == "position")
                    _robotController.Motors[dto.MotorId].MotorInfo.OpDataArray[dto.Index].Position = int.TryParse(dto.Value, out int pos) ? pos : 0;
                else if (dto.Type == "velocity")
                    _robotController.Motors[dto.MotorId].MotorInfo.OpDataArray[dto.Index].Velocity = int.TryParse(dto.Value, out int vel) ? vel : 0;
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult SavePosVelData([FromBody] SavePosVelDto dto)
        {
            if (dto.MotorId < 0 || dto.MotorId >= _robotController.Motors.Length)
                return BadRequest();

            var opDataArray = _robotController.Motors[dto.MotorId].MotorInfo.OpDataArray;
            foreach (var item in dto.OpDataList)
            {
                if (item.Index >= 0 && item.Index < opDataArray.Length)
                {
                    if (int.TryParse(item.Position, out int pos))
                        opDataArray[item.Index].Position = pos;
                    if (int.TryParse(item.Velocity, out int vel))
                        opDataArray[item.Index].Velocity = vel;
                }
            }
            // 呼叫 WriteOpData
            _robotController.WriteOpData(dto.MotorId);

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetPosition([FromBody] SetPositionRequest req)
        {
            // 取得 RobotController 實例，依你的注入方式調整
  
            var result = _robotController.WriteOpData_Position(req.MotorId, req.PosIndex, req.Position);
            return Json(new { success = result });
        }

        [HttpPost]
        public IActionResult SetAllServoOn()
        {
            _robotController.SetAllServo(true);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SetAllServoOff()
        {
            _robotController.SetAllServo(false);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult AllStop()
        {
            _robotController.AllStop();
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetTestProcedureStatus()
        {
                return Json(new { isRunning = _robotTestProcedure.IsRunning });
        }

        [HttpPost]
        public IActionResult StartTestProcedure1()
        {
            _robotTestProcedure.TryStartTestProcedure1Background();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult StopTestProcedure()
        {
            _robotTestProcedure.StopProcedure();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> HomeProcedure()
        {
            // 假設 _robotController 是你的 RobotController 實例
            await _robotController.HomeProcedure();
            return Ok(new { success = true });
        }

        [HttpPost]
        public IActionResult SetSpeedDataNo_M(int motorId, int speedDataNo)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();

            // 這裡假設你有對應的設定方法，請依實際需求調整
            _robotController.SetDataNo_M(motorId, speedDataNo);

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult StopProcedure()
        {
            _robotService.StopProcedure();
            return Ok();
        }

        [HttpGet]
        public IActionResult GetRobotProcedureStatus()
        {
            return Json(new
            {
                isProcedureRunning = _robotService.IsProcedureRunning,
                isHomeFinished = _robotService.IsHomeFinished,
                homeProcedureCase = _robotService.HomeProcedureCase,
                errorCode = _robotService.LastError.ErrorCode,
                errorMessage = _robotService.LastError.ErrorMessage,
                procedureStatusMessage = _robotService.ProcedureStatusMessage
            });
        }

        [HttpPost]
        public IActionResult StartHomeProcedure()
        {
            // 取得 RobotService 實例
            _robotService.StartHomeProcedure();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ClearLastError()
        {
            _robotService.LastError.Clear();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult StartRotateProcedure([FromBody] RotateProcedureRequest req)
        {
            _robotService.StartRotateProcedure(req.targetPosNo);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult StartTakeCarBatteryProcedure()
        {
            try
            {
                _robotService.StartTakeCarBatteryProcedure();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult StartPlaceCarBatteryProcedure()
        {
            try
            {
                _robotService.StartPlaceCarBatteryProcedure();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult StartPlaceSlotBatteryProcedure([FromBody] SlotRequest req)
        {
            try
            {
                _robotService.StartPlaceSlotBatteryProcedure(req.slotNo);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult StartTakeSlotBatteryProcedure([FromBody] SlotRequest req)
        {
            try
            {
                _robotService.StartTakeSlotBatteryProcedure(req.slotNo);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

    }

    
    

    

    

    
}

