using ChargerControlApp.Hardware;
using Microsoft.AspNetCore.Mvc;

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

            var motor = _robotController.Motors[motorId];
            bool current = motor.MotorInfo.IO_Input_Low.Bits.S_ON;
            bool next = !current;
            _robotController.ServerOn(motorId, next);
            return Json(new { serverOn = next });
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

        [HttpGet]
        public IActionResult MotorInfoPartial()
        {
            var motors = _robotController.Motors.Select(m => m.MotorInfo).ToList();
            return PartialView("_MotorInfoList", motors);
        }
    }
}