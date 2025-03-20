using Microsoft.AspNetCore.Mvc;

namespace ProStocker.Web.Controllers
{
    public class TurnosCajaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
