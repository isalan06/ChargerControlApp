using Microsoft.AspNetCore.Mvc;
using ChargerControlApp.DataAccess.GPIO.Services;
using ChargerControlApp.DataAccess.Slot.Services;
using TAC.Hardware; // 假設 StationState enum 在這裡

namespace ChargerControlApp.Controllers
{
    public class UnitsController : Controller
    {
        private readonly SlotServices _slotServices;

        public UnitsController(SlotServices slotServices)
        {
            _slotServices = slotServices;
        }

        // 取得 Station 狀態
        [HttpGet]
        public IActionResult GetStationState()
        {
            return Json(new { state = _slotServices.StationState.ToString() });
        }

        // 設定 Station 狀態
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetStationState(string state)
        {
            if (Enum.TryParse<StationState>(state, out var newState))
            {
                _slotServices.StationState = newState;
                return Json(new { success = true, state = newState.ToString() });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TogglePin(string pin)
        {
            if (pin == "1")
            {
                GPIOService.Pin1Value = !GPIOService.Pin1Value;
                GPIOService.Pin1.Value = GPIOService.Pin1Value;
            }
            else if (pin == "2")
            {
                GPIOService.Pin2Value = !GPIOService.Pin2Value;
                GPIOService.Pin2.Value = GPIOService.Pin2Value;
            }
            // 回傳目前狀態
            return Json(new { pin1 = GPIOService.Pin1Value, pin2 = GPIOService.Pin2Value });
        }

        // 新增一個API供前端定期取得狀態
        [HttpGet]
        public IActionResult GetPinStatus()
        {
            return Json(new { pin1 = GPIOService.Pin1Value, pin2 = GPIOService.Pin2Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetSlotChargeState(int index, string state)
        {
            // 取得 SlotInfo 陣列
            var slots = _slotServices.SlotInfo;
            if (index < 0 || index >= slots.Length)
                return Json(new { success = false });

            // 將 state 轉成 Enum
            if (!Enum.TryParse<SlotChargeState>(state, out var newState))
                return Json(new { success = false });

            // 更新 ChargeState
            slots[index].ChargeState = newState;

            // 可視需要儲存或通知其他服務
            return Json(new { success = true, state = newState.ToString() });
        }

        // 新增一個API供前端取得插槽狀態
        [HttpGet]
        public IActionResult GetSlotStatus()
        {
            var slots = _slotServices.SlotInfo;
            var result = slots.Select(s => new {
                chargingProcessValue = s.ChargingProcessValue,
                chargeState = s.ChargeState.ToString(),
                machineState = s.State.GetCurrentStateName()
            }).ToArray();
            return Json(result);
        }

        [HttpPost]
        public IActionResult SetSlotState(int index, string state)
        {
            if (Enum.TryParse<SlotState>(state, out var slotState))
            {
                _slotServices.TransitionTo(index, slotState);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}