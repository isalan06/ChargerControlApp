using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.Hardware;
using ChargerControlApp.Models;
using ChargerControlApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChargerControlApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RobotController _robotController;
        private readonly ChargingStationStateMachine _stateMachine;
        private readonly HardwareManager _hardwareManager;
        private readonly RobotService _robotService;
        private readonly MonitoringService _monitoringService;

        public HomeController(ILogger<HomeController> logger, RobotController robotController, ChargingStationStateMachine stateMachine, HardwareManager hardwareManager, RobotService robotService, MonitoringService monitoringService)
        {
            _logger = logger;
            _robotController = robotController;
            _stateMachine = stateMachine;
            _hardwareManager = hardwareManager;
            _robotService = robotService;
            _monitoringService = monitoringService;
        }


        // 取得 StateMachine 狀態
        [HttpGet]
        public IActionResult GetStationState()
        {
            return Json(new { 
                state = _stateMachine.GetCurrentStateName(),
                psuCount = HardwareManager.NPB450ControllerInstnaceNumber,
                canbus = _hardwareManager.CanbusConnected,
                modbus = _hardwareManager.ModbusConnected,
                motorAlarm = _robotService.IsMotorAlarm,
                psuAlarm = _robotService.IsPowerSupplyAlarm,
                slotAlarm = _robotService.IsSlotAlarm,
                procedureAlarm = _robotService.IsProcedureAlarm,
                mainProcedureCase = _robotService.MainProcedureCase
            });
        }

        [HttpPost]
        public IActionResult StartAutoProcedure()
        {
            var result = _monitoringService.StartAutoProcedure();
            if (result)
                return Json(new { success = true, message = "已開始自動換電程序。" });
            else
                return Json(new { success = false, message = "無法開始自動換電程序，請確認目前狀態是否為 Idle、已完成原點復歸且無警報。" });
        }

        [HttpPost]
        public IActionResult StopAutoProcedure()
        {
            var result = _monitoringService.StopAutoProcedure();
            if (result)
                return Json(new { success = true, message = "已停止自動換電程序。" });
            else
                return Json(new { success = false, message = "無法停止，請確認目前狀態是否為執行中。" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetAlarm()
        {
            _monitoringService.ResetAlarm();
            return Json(new { success = true, message = "告警已重置。" });
        }

        [HttpPost]
        public IActionResult SystemReset()
        {
            _monitoringService.Reset();
            return Json(new { success = true, message = "系統重置已觸發。" });
        }

        [HttpPost]
        public IActionResult HomeProcedure()
        {
            var result = _monitoringService.StartHomeProcedure();
            if (result)
                return Json(new { success = true, message = "已觸發原點復歸。" });
            else
                return Json(new { success = true,message = "無法開始原點復歸，請確認目前狀態是否為 Idle、可以原點復歸且無警報。" });
        }

        public IActionResult Index()
        {
            // 取得 RobotController 的資料
            //var status = _robotController.Motors[0].IO_Output_Low.Bits.SON_MON; 
            //ViewBag.ServerON = status;
            return View();
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
