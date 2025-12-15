using ChargerControlApp.Hardware;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace ChargerControlApp.Controllers
{
    public class SystemController : Controller
    {
        public SystemController()
        {
        }
        public IActionResult Index()
        {
            var model = new Dictionary<string, string> { 
                ["App"] = Assembly.GetEntryAssembly().GetName()?.Version?.ToString(),
                ["Runtime"] = Environment.Version.ToString()
            };
            return View(model);
        }

        // 回傳 CANBus 狀態與每一台設備的 CycleCount，供前端定期查詢
        [HttpGet]
        public IActionResult CanbusStatus()
        {
            var deviceMaxCount = NPB450Controller.NPB450ControllerInstnaceMaxNumber;
            var deviceUseCount = HardwareManager.NPB450ControllerInstnaceNumber;

            uint connectedCount = 0;
            var cycles = new List<object>();

            var chargers = HttpContext.RequestServices.GetService<NPB450Controller[]>();
            if (chargers != null)
            {
                for (int i = 0; i < chargers.Length; i++)
                {
                    var c = chargers[i];
                    ulong cycle = 0;
                    if (c != null)
                    {
                        cycle = c.CycleCount;
                        if (c.IsCompletedOneTime && !c.IsReadTimeout)
                            connectedCount++;
                    }

                    cycles.Add(new
                    {
                        index = i + 1, // 顯示為 1-based index
                        cycleCount = cycle,
                        enabled = (i < HardwareManager.NPB450ControllerInstnaceNumber)
                    });
                }
            }

            return Json(new
            {
                deviceMaxCount,
                deviceUseCount,
                connectedCount,
                cycles
            });
        }


    }
}
