using Microsoft.AspNetCore.Mvc;
using ProStocker.Web.DAL;
using ProStocker.Web.Models;

namespace ProStocker.Web.Controllers
{
    public class ArticulosController : Controller
    {
        private readonly DataAccess _dataAccess;

        public ArticulosController(DataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public IActionResult Index()
        {
            return View(_dataAccess.LeerArticulos());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Articulo articulo)
        {
            if (ModelState.IsValid)
            {
                _dataAccess.CrearArticulo(articulo);
                return RedirectToAction("Index");
            }
            return View(articulo);
        }

        public IActionResult Edit(int id)
        {
            var articulo = _dataAccess.LeerArticulos().FirstOrDefault(a => a.Id == id);
            if (articulo == null) return NotFound();
            return View(articulo);
        }

        [HttpPost]
        public IActionResult Edit(Articulo articulo)
        {
            if (ModelState.IsValid)
            {
                _dataAccess.ActualizarArticulo(articulo);
                return RedirectToAction("Index");
            }
            return View(articulo);
        }

        public IActionResult Delete(int id)
        {
            var articulo = _dataAccess.LeerArticulos().FirstOrDefault(a => a.Id == id);
            if (articulo == null) return NotFound();
            return View(articulo);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _dataAccess.EliminarArticulo(id);
            return RedirectToAction("Index");
        }
    }
}