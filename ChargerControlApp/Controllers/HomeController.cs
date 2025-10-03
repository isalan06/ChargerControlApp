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


        // ���o StateMachine ���A
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
                return Json(new { success = true, message = "�w�}�l�۰ʴ��q�{�ǡC" });
            else
                return Json(new { success = false, message = "�L�k�}�l�۰ʴ��q�{�ǡA�нT�{�ثe���A�O�_�� Idle�B�w�������I�_�k�B�Lĵ���C" });
        }

        [HttpPost]
        public IActionResult StopAutoProcedure()
        {
            var result = _monitoringService.StopAutoProcedure();
            if (result)
                return Json(new { success = true, message = "�w����۰ʴ��q�{�ǡC" });
            else
                return Json(new { success = false, message = "�L�k����A�нT�{�ثe���A�O�_�����椤�C" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetAlarm()
        {
            _monitoringService.ResetAlarm();
            return Json(new { success = true, message = "�iĵ�w���m�C" });
        }

        [HttpPost]
        public IActionResult SystemReset()
        {
            _monitoringService.Reset();
            return Json(new { success = true, message = "�t�έ��m�wĲ�o�C" });
        }

        [HttpPost]
        public IActionResult HomeProcedure()
        {
            var result = _monitoringService.StartHomeProcedure();
            if (result)
                return Json(new { success = true, message = "�wĲ�o���I�_�k�C" });
            else
                return Json(new { success = true,message = "�L�k�}�l���I�_�k�A�нT�{�ثe���A�O�_�� Idle�B�i�H���I�_�k�B�Lĵ���C" });
        }

        public IActionResult Index()
        {
            // ���o RobotController �����
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
