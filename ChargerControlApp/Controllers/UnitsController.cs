using Microsoft.AspNetCore.Mvc;

namespace ChargerControlApp.Controllers
{
    public class UnitsController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
