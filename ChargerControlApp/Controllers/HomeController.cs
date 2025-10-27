using ChargerControlApp.DataAccess.Modbus.Services;
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
        private readonly GrpcClientService _grpcClientService;

        public HomeController(ILogger<HomeController> logger, RobotController robotController, ChargingStationStateMachine stateMachine, HardwareManager hardwareManager, RobotService robotService, MonitoringService monitoringService, GrpcClientService grpcClientService)
        {
            _logger = logger;
            _robotController = robotController;
            _stateMachine = stateMachine;
            _hardwareManager = hardwareManager;
            _robotService = robotService;
            _monitoringService = monitoringService;
            _grpcClientService = grpcClientService;
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
                modbusActFreq = ModbusRTUService.InterFrameActMilliseconds.ToString("F0"),
                modbusReadTime = ModbusRTUService.FrameReadMilliseconds.ToString("F0"),
                motorAlarm = _robotService.IsMotorAlarm,
                psuAlarm = _robotService.IsPowerSupplyAlarm,
                slotAlarm = _robotService.IsSlotAlarm,
                procedureAlarm = _robotService.IsProcedureAlarm,
                mainProcedureCase = _robotService.MainProcedureCase,
                isManualMode = RobotService.IsManualMode,
                isOnline = GrpcClientService.IsOnline,
                errorMessage = _robotService.LastError.ErrorMessage,
                mainProcedureStatusMessage = _robotService.MainProcedureStatusMessage,
                procedureStatusMessage = _robotService.ProcedureStatusMessage,
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
        public IActionResult SystemForceToIdle()
        {
            _monitoringService.ForceReset();
            return Json(new { success = true, message = "�j�m��Idle���A�wĲ�o�C" });
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

        [HttpPost]
        public IActionResult SwitchManualMode()
        { 
            var result = _robotService.SwitchManualMode();
            if (result)
            {
                if (RobotService.IsManualMode)
                    return Json(new { success = true, message = $"�w�����ܤ�ʼҦ��C" });
                else
                    return Json(new { success = true, message = $"�w�����ܦ۰ʼҦ��C" });
            }
            else
            {
                if(RobotService.IsManualMode)
                    return Json(new { success = false, message = $"�L�k�����ܦ۰ʼҦ��C" });
                else
                    return Json(new { success = false, message = $"�L�k�����ܤ�ʼҦ��C" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetGrpcOnlineStatus(string op)
        {
            bool isOnline = GrpcClientService.IsOnline;
            string message = "";

            if (op == "connect")
            {
                if (isOnline)
                {
                    message = "�w�s�u�A���ݭn�b����s�u�ʧ@";
                }
                else
                {
                    bool result = await _grpcClientService.ManualOnline();
                    message = result ? "�s�u���\" : "�s�u����";
                }
            }
            else if (op == "disconnect")
            {
                if (!isOnline)
                {
                    message = "�w���u�A���ݭn�b�������u�ʧ@";
                }
                else
                {
                    // ���]�� ManualOffline ��k�A�Ш̹�ڪA�Ƚվ�
                    bool result = await _grpcClientService.ManualOffline();
                    message = result ? "���u���\" : "���u����";
                }
            }
            else
            {
                message = isOnline ? "�w�s�u" : "���s�u";
            }

            return Json(new { isOnline, message });
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
