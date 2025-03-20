using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProStocker.Web.DAL;
using ProStocker.Web.Models;

namespace ProStocker.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly DataAccess _dataAccess;

        public UsuariosController(DataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public IActionResult Index()
        {
            return View(_dataAccess.LeerUsuarios());
        }

        public IActionResult Create()
        {
            ViewBag.Sucursales = _dataAccess.LeerSucursales();
            return View(new Usuario());
        }

        [HttpPost]
        public IActionResult Create(Usuario usuario, int[] sucursales)
        {
            if (ModelState.IsValid)
            {
                usuario.Sucursales = sucursales.ToList();
                _dataAccess.CrearUsuario(usuario);
                return RedirectToAction("Index");
            }
            ViewBag.Sucursales = _dataAccess.LeerSucursales();
            return View(usuario);
        }

        public IActionResult Edit(int id)
        {
            var usuario = _dataAccess.LeerUsuarios().FirstOrDefault(u => u.Id == id);
            if (usuario == null) return NotFound();
            ViewBag.Sucursales = _dataAccess.LeerSucursales();
            return View(usuario);
        }

        [HttpPost]
        public IActionResult Edit(Usuario usuario, int[] sucursales)
        {
            if (ModelState.IsValid)
            {
                usuario.Sucursales = sucursales.ToList();
                _dataAccess.ActualizarUsuario(usuario);
                return RedirectToAction("Index");
            }
            ViewBag.Sucursales = _dataAccess.LeerSucursales();
            return View(usuario);
        }

        public IActionResult Delete(int id)
        {
            var usuario = _dataAccess.LeerUsuarios().FirstOrDefault(u => u.Id == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _dataAccess.EliminarUsuario(id);
            return RedirectToAction("Index");
        }
    }
}