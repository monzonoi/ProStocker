using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ProStocker.Web.DAL;
using ProStocker.Web.Hubs;
using ProStocker.Web.Models;

namespace ProStocker.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly DataAccess _dataAccess;
        private readonly IHubContext<UsuariosHub> _hubContext;

        public UsuariosController(DataAccess dataAccess, IHubContext<UsuariosHub> hubContext)
        {
            _dataAccess = dataAccess;
            _hubContext = hubContext;
        }
        public async Task<IActionResult> Index()
        {
            var model = new UsuariosViewModel
            {
                Usuarios = await _dataAccess.LeerUsuariosAsync()
            };
            ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
            return View(model);
        }

     

        [HttpGet]
        [Route("Usuarios/Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create");
            }
            return View();
        }

        //[HttpPost]
        //public async Task<IActionResult> Create(Usuario usuario, int[] Sucursales)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            usuario.Sucursales = Sucursales?.ToList() ?? new List<int>();
        //            await _dataAccess.CrearUsuarioAsync(usuario);
        //            var model = new UsuariosViewModel
        //            {
        //                Usuarios = await _dataAccess.LeerUsuariosAsync()
        //            };
        //            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Usuario creado exitosamente.", "success");
        //            await _hubContext.Clients.All.SendAsync("RefreshUsuariosList");
        //            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //            {
        //                ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
        //                return View("Index", model);
        //            }
        //            return View("Index", model);
        //        }
        //        ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
        //        return PartialView("Create", usuario);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Error al crear usuario: {ex.Message}", "error");
        //        ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
        //        return PartialView("Create", usuario);
        //    }
        //}

        [HttpGet]
        [Route("Usuarios/Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _dataAccess.LeerUsuarioPorIdAsync(id);
            if (usuario == null) return NotFound();
            ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Edit", usuario);
            }
            return View(usuario);
        }

        [HttpPost]
        [Route("Usuarios/Edit/{id}")]
        public async Task<IActionResult> Edit(Usuario usuario, int[] Sucursales)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    usuario.Sucursales = Sucursales?.ToList() ?? new List<int>();
                    await _dataAccess.ActualizarUsuarioAsync(usuario);
                    var model = new UsuariosViewModel
                    {
                        Usuarios = await _dataAccess.LeerUsuariosAsync()
                    };
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Usuario actualizado exitosamente.", "success");
                    await _hubContext.Clients.All.SendAsync("RefreshUsuariosList");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
                        return View("Index", model);
                    }
                    return View("Index", model);
                }
                ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
                return PartialView("Edit", usuario);
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Error al actualizar usuario: {ex.Message}", "error");
                ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
                return PartialView("Edit", usuario);
            }
        }

        [HttpPost]
        [Route("Usuarios/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _dataAccess.EliminarUsuarioAsync(id);
                var model = new UsuariosViewModel
                {
                    Usuarios = await _dataAccess.LeerUsuariosAsync()
                };
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Usuario eliminado exitosamente.", "success");
                await _hubContext.Clients.All.SendAsync("RefreshUsuariosList");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
                    return View("Index", model);
                }
                return View("Index", model);
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Error al eliminar usuario: {ex.Message}", "error");
                ViewBag.Sucursales = await _dataAccess.LeerSucursalesAsync();
                return View("Index");
            }
        }

        [HttpPost]
        [Route("Usuarios/Create")]
        public async Task<IActionResult> Create(Usuario usuario, int[] Sucursales)
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
                    // Enviar notificación y actualizar lista vía SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Usuario creado exitosamente.", "success");
                    await _hubContext.Clients.All.SendAsync("RefreshUsuariosList");
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
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Error al crear usuario: {ex.Message}", "error");
                ViewBag.Sucursales = _dataAccess.LeerSucursales();
                return PartialView("Create", usuario);
            }
        }

        //[HttpGet]
        //public IActionResult Edit(int id)
        //{
        //    var usuario = _dataAccess.LeerUsuarioPorId(id);
        //    if (usuario == null) return NotFound();
        //    ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //    {
        //        return PartialView("Edit", usuario);
        //    }
        //    return View(usuario);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Edit(Usuario usuario, int[] Sucursales)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            usuario.Sucursales = Sucursales?.ToList() ?? new List<int>();
        //            _dataAccess.ActualizarUsuario(usuario);
        //            var model = new UsuariosViewModel
        //            {
        //                Usuarios = _dataAccess.LeerUsuarios()
        //            };
        //            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Usuario actualizado exitosamente.", "success");
        //            await _hubContext.Clients.All.SendAsync("RefreshUsuariosList");
        //            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //            {
        //                ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //                return View("Index", model);
        //            }
        //            return View("Index", model);
        //        }
        //        ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //        return PartialView("Edit", usuario);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Error al actualizar usuario: {ex.Message}", "error");
        //        ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //        return PartialView("Edit", usuario);
        //    }
        //}

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



        //[HttpPost]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    try
        //    {
        //        _dataAccess.EliminarUsuario(id);
        //        var model = new UsuariosViewModel
        //        {
        //            Usuarios = _dataAccess.LeerUsuarios()
        //        };
        //        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Usuario eliminado exitosamente.", "success");
        //        await _hubContext.Clients.All.SendAsync("RefreshUsuariosList");
        //        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //        {
        //            ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //            return View("Index", model);
        //        }
        //        return View("Index", model);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Error al eliminar usuario: {ex.Message}", "error");
        //        ViewBag.Sucursales = _dataAccess.LeerSucursales();
        //        return View("Index");
        //    }
        //}

    }
}