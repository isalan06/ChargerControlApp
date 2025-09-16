using ChargerControlApp.Hardware;
using Microsoft.AspNetCore.Mvc;
using ChargerControlApp.Models;
using System.Collections.Generic;
using ChargerControlApp.DataAccess.Motor.Models;
using ChargerControlApp.DataAccess.Motor.Services;

namespace ChargerControlApp.Controllers
{
    public class MotorController : Controller
    {
        private readonly ILogger<MotorController> _logger;
        private readonly RobotController _robotController;

        public MotorController(ILogger<MotorController> logger, RobotController robotController)
        {
            _logger = logger;
            _robotController = robotController;
        }
        public IActionResult Index()
        {
            return View();
        }

        // 穝糤 Action ㄑ AJAX ㊣
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
                opData_Vel_Actual = m.MotorInfo.OpData_Vel_Actual
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public IActionResult ToggleServo(int motorId)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            // ㄌ惠―ち传狝篈
            // ㄒRobotController.ServerOn(motorId, !ヘ玡篈);
            // 叫沮眤狝篈眔よΑ秸俱
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

        // 眔 Jog&Home 把计
        [HttpGet]
        public IActionResult GetJogHomeParams(int motorId)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();
            var motorInfo = _robotController.Motors[motorId].MotorInfo;
            var values = motorInfo.Jog_Home_Setting.ToArray();
            return Json(values);
        }

        // 虫把计糶
        [HttpPost]
        public IActionResult SetJogHomeParam([FromBody] JogHomeParamUpdateDto dto)
        {
            if (dto.MotorId < 0 || dto.MotorId >= _robotController.Motors.Length)
                return BadRequest();
            //var motorInfo = _robotController.Motors[dto.MotorId].MotorInfo;
            //if (dto.Index < 0 || dto.Index >= motorInfo.Motor_Jog_Home_Setting.Count)
              //  return BadRequest("Index out of range");
            //motorInfo.Motor_Jog_Home_Setting[dto.Index].Value = dto.Value;
            // 龟悔纗呸胯
            return Ok();
        }

        // уΩ糶场把计
        [HttpPost]
        public IActionResult SaveJogHomeParams([FromBody] JogHomeParamBatchUpdateDto dto)
        {
            if (dto.MotorId < 0 || dto.MotorId >= _robotController.Motors.Length)
                return BadRequest();
            var motorInfo  = _robotController.Motors[dto.MotorId].MotorInfo;
            if (dto.Values.Count != motorInfo.Jog_Home_Setting.ToArray().Length)
                return BadRequest("把计计秖ぃ才");
            int[] setValues = new int[dto.Values.Count];
            for (int i = 0; i < dto.Values.Count; i++)
            {
                setValues[i] = int.TryParse(dto.Values[i], out int val) ? val : 0;
            }

            motorInfo.Jog_Home_Setting.FromArray(setValues);
            // 龟悔纗呸胯
            _robotController.WriteJogAndHomeSetting(dto.MotorId);
            return Ok();
        }

    }

    // DTO for 虫把计穝
    public class JogHomeParamUpdateDto
    {
        public int MotorId { get; set; }
        public int Index { get; set; }
        public string Value { get; set; }
    }
    public class JogHomeParamBatchUpdateDto
    {
        public int MotorId { get; set; }
        public List<string> Values { get; set; }
    }
}