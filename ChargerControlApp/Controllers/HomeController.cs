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
