using Microsoft.AspNetCore.Mvc;

namespace ProStocker.Web.Controllers
{
    public class CajasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
