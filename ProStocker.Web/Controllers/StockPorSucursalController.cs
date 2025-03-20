using Microsoft.AspNetCore.Mvc;

namespace ProStocker.Web.Controllers
{
    public class StockPorSucursalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
