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
            var model = new UsuariosViewModel
            {
                Usuarios = _dataAccess.LeerUsuarios()
            };
            return View(model);
        }

        //public IActionResult Index()
        //{
        //    return View(_dataAccess.LeerUsuarios());
        //}

        [HttpGet]
        public IActionResult Create()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create"); // Vista parcial para el modal
            }
            return View();
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

        [HttpPost]
        public IActionResult Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                _dataAccess.CrearUsuario(usuario);
                var model = new UsuariosViewModel
                {
                    Usuarios = _dataAccess.LeerUsuarios()
                };
                return View("Index", model);
            }
            return PartialView("Create", usuario);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var usuario = _dataAccess.LeerUsuarioPorId(id);
            if (usuario == null) return NotFound();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Edit", usuario); // Vista parcial para el modal
            }
            return View(usuario);
        }

        [HttpPost]
        public IActionResult Edit(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                _dataAccess.ActualizarUsuario(usuario);
                var model = new UsuariosViewModel
                {
                    Usuarios = _dataAccess.LeerUsuarios()
                };
                return View("Index", model);
            }
            return PartialView("Edit", usuario);
        }

        //public IActionResult Edit(int id)
        //{
        //    var usuario = _dataAccess.LeerUsuarios().FirstOrDefault(u => u.Id == id);
        //    if (usuario == null) return NotFound();
        //    ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //    return View(usuario);
        //}

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