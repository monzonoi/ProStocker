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
            ViewBag.Sucursales = _dataAccess.LeerSucursales(); // Añadimos las sucursales al ViewBag
            return View(model);
        }

        //public IActionResult Index()
        //{
        //    return View(_dataAccess.LeerUsuarios());
        //}
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Sucursales = _dataAccess.LeerSucursales();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Create(Usuario usuario, int[] Sucursales)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    usuario.Sucursales = Sucursales?.ToList() ?? new List<int>();
                    _dataAccess.CrearUsuario(usuario);
                    var model = new UsuariosViewModel
                    {
                        Usuarios = _dataAccess.LeerUsuarios()
                    };
                    TempData["Notification"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Usuario creado exitosamente.", type = "success" });
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        ViewBag.Sucursales = _dataAccess.LeerSucursales();
                        return View("Index", model);
                    }
                    return View("Index", model);
                }
                ViewBag.Sucursales = _dataAccess.LeerSucursales();
                return PartialView("Create", usuario);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear usuario: {ex.Message}");
                TempData["Notification"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = $"Error al crear usuario: {ex.Message}", type = "error" });
                ViewBag.Sucursales = _dataAccess.LeerSucursales();
                return PartialView("Create", usuario);
            }
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var usuario = _dataAccess.LeerUsuarioPorId(id);
            if (usuario == null) return NotFound();
            ViewBag.Sucursales = _dataAccess.LeerSucursales();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Edit", usuario);
            }
            return View(usuario);
        }

        [HttpPost]
        public IActionResult Edit(Usuario usuario, int[] Sucursales)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    usuario.Sucursales = Sucursales?.ToList() ?? new List<int>();
                    _dataAccess.ActualizarUsuario(usuario);
                    var model = new UsuariosViewModel
                    {
                        Usuarios = _dataAccess.LeerUsuarios()
                    };
                    TempData["Notification"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Usuario actualizado exitosamente.", type = "success" });
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        ViewBag.Sucursales = _dataAccess.LeerSucursales();
                        return View("Index", model);
                    }
                    return View("Index", model);
                }
                ViewBag.Sucursales = _dataAccess.LeerSucursales();
                return PartialView("Edit", usuario);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar usuario: {ex.Message}");
                TempData["Notification"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = $"Error al actualizar usuario: {ex.Message}", type = "error" });
                ViewBag.Sucursales = _dataAccess.LeerSucursales();
                return PartialView("Edit", usuario);
            }
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
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return View("Index", model); // Devuelve la vista completa para AJAX
                }
                return View("Index", model);
            }
            return PartialView("Edit", usuario);
        }



        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                _dataAccess.EliminarUsuario(id);
                var model = new UsuariosViewModel
                {
                    Usuarios = _dataAccess.LeerUsuarios()
                };
                TempData["Notification"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Usuario eliminado exitosamente.", type = "success" });
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    ViewBag.Sucursales = _dataAccess.LeerSucursales();
                    return View("Index", model);
                }
                return View("Index", model);
            }
            catch (Exception ex)
            {
                TempData["Notification"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = $"Error al eliminar usuario: {ex.Message}", type = "error" });
                ViewBag.Sucursales = _dataAccess.LeerSucursales();
                return View("Index");
            }
        }

    }
}