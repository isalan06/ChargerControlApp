using Microsoft.AspNetCore.Mvc;

namespace ChargerControlApp.Controllers
{
    public class GrpcController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}