using ChargerControlApp.DataAccess.Modbus.Services;
using ChargerControlApp.DataAccess.Robot.Services;
using ChargerControlApp.DataAccess.Slot.Services;
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
        private readonly SlotServices _slotServices;

        public HomeController(ILogger<HomeController> logger, RobotController robotController, ChargingStationStateMachine stateMachine, HardwareManager hardwareManager, RobotService robotService, MonitoringService monitoringService, GrpcClientService grpcClientService, SlotServices slotServices)
        {
            _logger = logger;
            _robotController = robotController;
            _stateMachine = stateMachine;
            _hardwareManager = hardwareManager;
            _robotService = robotService;
            _monitoringService = monitoringService;
            _grpcClientService = grpcClientService;
            _slotServices = slotServices;
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
                isGrpcAutoLoading = GrpcClientService.IsGrpcWaitRegisterResponse
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
        public IActionResult SlotReset()
        {
            _slotServices.ResetAllSlotStatus();
            return Json(new { success = true, message = "Slot重置已觸發。" });
        }

        [HttpPost]
        public IActionResult SystemReset()
        {
            _monitoringService.Reset();
            return Json(new { success = true, message = "系統重置已觸發。" });
        }

        [HttpPost]
        public IActionResult SystemForceToIdle()
        {
            _monitoringService.ForceReset();
            return Json(new { success = true, message = "強置到Idle狀態已觸發。" });
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

        [HttpPost]
        public IActionResult SwitchManualMode()
        { 
            var result = _robotService.SwitchManualMode();
            if (result)
            {
                if (RobotService.IsManualMode)
                    return Json(new { success = true, message = $"已切換至手動模式。" });
                else
                    return Json(new { success = true, message = $"已切換至自動模式。" });
            }
            else
            {
                if(RobotService.IsManualMode)
                    return Json(new { success = false, message = $"無法切換至自動模式。" });
                else
                    return Json(new { success = false, message = $"無法切換至手動模式。" });
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
                    message = "已連線，不需要在執行連線動作";
                }
                else
                {
                    bool result = await _grpcClientService.ManualOnline();
                    message = result ? "連線成功" : "連線失敗";
                }
            }
            else if (op == "disconnect")
            {
                if (!isOnline)
                {
                    message = "已離線，不需要在執行離線動作";
                }
                else
                {
                    // 假設有 ManualOffline 方法，請依實際服務調整
                    bool result = await _grpcClientService.ManualOffline();
                    message = result ? "離線成功" : "離線失敗";
                }
            }
            else
            {
                message = isOnline ? "已連線" : "未連線";
            }

            return Json(new { isOnline, message });
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
