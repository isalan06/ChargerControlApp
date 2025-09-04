using System.Diagnostics;
using ChargerControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using ChargerControlApp.Hardware;

namespace ChargerControlApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RobotController _robotController;

        public HomeController(ILogger<HomeController> logger, RobotController robotController)
        {
            _logger = logger;
            _robotController = robotController;
        }

        public IActionResult Index()
        {
            // 取得 RobotController 的資料
            var status = _robotController.Motors[0].IO_Output_Low.Bits.SON_MON; 
            ViewBag.ServerON = status;
            return View();
        }

        // 新增此 Action 供 AJAX 呼叫
        [HttpGet]
        public IActionResult GetRobotStatus()
        {
            var status = _robotController.Motors[0].IO_Output_Low.Bits.SON_MON;
            return Json(new { serverOn = status });
        }

        [HttpGet]
        public IActionResult GetMotor0Status()
        {
            var m = _robotController.Motors[0];
            return Json(new
            {
                posActual = m.Pos_Actual,
                velActual = m.Vel_Actual,
                errorCode = m.ErrorCode,
                sOn = m.IO_Output_Low.Bits.SON_MON
        });
        }

        [HttpGet]
        public IActionResult GetMotorsStatus()
        {
            var motors = _robotController.Motors;
            var result = motors.Select((m, i) => new
            {
                id = i,
                posActual = m.Pos_Actual,
                velActual = m.Vel_Actual,
                errorCode = m.ErrorCode,
                sOn = m.IO_Output_Low.Bits.SON_MON,
                rdyDdOpe = m.IO_Output_Low.Bits.RDY_DD_OPE,
                stopR = m.IO_Output_Low.Bits.STOP_R,
                freeR = m.IO_Output_Low.Bits.FREE_R,
                armA = m.IO_Output_Low.Bits.ALM_A,
                sysBsy = m.IO_Output_Low.Bits.SYS_BSY,
                inPos = m.IO_Output_Low.Bits.IN_POS,
                rdyHomeOpe = m.IO_Output_Low.Bits.RDY_HOME_OPE,
                rdyFwrvOpe = m.IO_Output_Low.Bits.RDY_FWRV_OPE,
                rdySdOpe = m.IO_Output_Low.Bits.RDY_SD_OPE,
                move = m.IO_Output_Low.Bits.MOVE,
                selectDataNo = m.OpData_IdSelect,
                currentDataNo = m.OpData_IdOp,
                jogMode = m.JogMode,
                dataNo = m.CurrentDataNo,
                opData_IdSelect = m.OpData_IdSelect,
                opData_IdOp = m.OpData_IdOp,
                opData_Pos_Command = m.OpData_Pos_Command,
                opData_Vel_Command = m.OpData_Vel_Command,
                opData_Pos_Actual = m.OpData_Pos_Actual,
                opData_Vel_Actual = m.OpData_Vel_Actual
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public IActionResult ToggleServo(int motorId)
        {
            if (motorId < 0 || motorId >= _robotController.Motors.Length)
                return BadRequest();

            var motor = _robotController.Motors[motorId];
            bool current = motor.IO_Input_Low.Bits.S_ON;
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


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
