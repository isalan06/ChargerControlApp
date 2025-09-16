using Microsoft.AspNetCore.Mvc;
using ChargerControlApp.DataAccess.GPIO.Services;

namespace ChargerControlApp.Controllers
{
    public class UnitsController : Controller
    {
        [HttpPost]
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
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}