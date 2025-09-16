using Microsoft.AspNetCore.Mvc;
using ChargerControlApp.Services;
using ChargerControlApp.DataAccess.Slot.Services;
using System.Linq;

namespace ChargerControlApp.Controllers
{
    public class GrpcController : Controller
    {
        private readonly SlotServices _slotServices;

        public GrpcController(SlotServices slotServices)
        {
            _slotServices = slotServices;
        }

        public IActionResult Index(string tab = "server")
        {
            var stationState = _slotServices.StationState?.ToString() ?? "Unknown";
            var logMessages = SwappingStationService.LogMessages.Reverse().Take(100).ToList();

            ViewBag.StationState = stationState;
            ViewBag.LogMessages = logMessages;
            ViewBag.ActiveTab = tab;

            return View();
        }

        [HttpGet]
        public IActionResult GetLogs()
        {
            var logMessages = SwappingStationService.LogMessages.Reverse().Take(100).ToList();
            return Json(logMessages);
        }
    }
}