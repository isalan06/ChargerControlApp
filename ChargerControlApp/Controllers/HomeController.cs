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
                sOn = m.IO_Output_Low.Bits.SON_MON
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
