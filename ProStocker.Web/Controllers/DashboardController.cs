using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProStocker.Web.DAL;
using ProStocker.Web.Models;
using System.Security.Claims;
using ProStocker.DAL.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProStocker.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DataAccess _dataAccess;

        private readonly ISucursalDAL _sucursalDAL;
        private readonly ICajaDAL _cajaDAL;
        private readonly ITurnoCajaDAL _turnoDAL;
        private readonly IVentaDAL _ventaDAL;
        private readonly IStockDAL _stockDAL;

        //public DashboardController(DataAccess dataAccess)
        //{
        //    _dataAccess = dataAccess;
        //}

        public DashboardController(
        ISucursalDAL sucursalDAL,
        ICajaDAL cajaDAL,
        ITurnoCajaDAL turnoDAL,
        IVentaDAL ventaDAL,
        IStockDAL stockDAL)
        {
            _sucursalDAL = sucursalDAL;
            _cajaDAL = cajaDAL;
            _turnoDAL = turnoDAL;
            _ventaDAL = ventaDAL;
            _stockDAL = stockDAL;
        }

        [HttpGet]
        [Route("Dashboard/Index")]
        [Authorize]
        [Authorize]
        public IActionResult Index()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioId))
            {
                return RedirectToAction("Login", "Dashboard");
            }

            var sucursales = _sucursalDAL.GetAll() ?? new List<Sucursal>();
            Sucursal sucursalSeleccionada;
            if (sucursales.Any())
            {
                sucursalSeleccionada = sucursales.First();
            }
            else
            {
                sucursalSeleccionada = new Sucursal { Id = 0, Nombre = "Sin Sucursal", Direccion = "" };
            }

            var cajas = sucursales.Any() ? (_cajaDAL.GetBySucursalId(sucursalSeleccionada.Id) ?? new List<Caja>()) : new List<Caja>();
            Caja cajaSeleccionada;
            if (cajas.Any())
            {
                cajaSeleccionada = cajas.First();
            }
            else
            {
                cajaSeleccionada = new Caja { Id = 0, SucursalId = sucursalSeleccionada.Id, Nombre = "Sin Caja", Estado = "Inactiva" };
            }

            var turnoActivo = cajaSeleccionada.Id != 0 ? (_turnoDAL.GetTurnoActivoByCajaId(cajaSeleccionada.Id) ?? new TurnoCaja { Id = 0, CajaId = cajaSeleccionada.Id, UsuarioId = usuarioId, FechaInicio = DateTime.Now, Estado = "Cerrado" }) : new TurnoCaja { Id = 0, CajaId = 0, UsuarioId = usuarioId, FechaInicio = DateTime.Now, Estado = "Cerrado" };

            var fechaInicio = DateTime.Today;
            var fechaFin = DateTime.Now;

            var reporteVentas = sucursales.Any() ? (_ventaDAL.GetReporteVentas(sucursalSeleccionada.Id, fechaInicio, fechaFin) ?? new List<ReporteVenta>()) : new List<ReporteVenta>();
            var reporteStockMinimo = sucursales.Any() ? (_stockDAL.GetReporteStockMinimo(sucursalSeleccionada.Id) ?? new List<ReporteStockMinimo>()) : new List<ReporteStockMinimo>();

          

            // Agregar logs para depuración
            Console.WriteLine($"ReporteVentas Count: {reporteVentas.Count}");
            Console.WriteLine($"ReporteStockMinimo Count: {reporteStockMinimo.Count}");

            var ventasTotales = reporteVentas.Sum(r => r.TotalVentas);
            var gananciaTotal = reporteVentas.Sum(r => r.TotalGanancia);
            var totalTransacciones = reporteVentas.Count;
            var ticketPromedio = totalTransacciones > 0 ? ventasTotales / totalTransacciones : 0m;

            var model = new DashboardViewModel
            {
                Sucursales = sucursales,
                SucursalSeleccionada = sucursalSeleccionada,
                Cajas = cajas,
                CajaSeleccionada = cajaSeleccionada,
                TurnoActivo = turnoActivo,
                VentasTotales = ventasTotales,
                GananciaTotal = gananciaTotal,
                TicketPromedio = ticketPromedio,
                TotalTransacciones = totalTransacciones,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                ReporteVentas = new List<(Sucursal Sucursal, decimal TotalVentas, decimal TotalGanancia)>(),
                ReporteStockMinimo = new List<(Sucursal Sucursal, Articulo Articulo, decimal Stock, decimal StockMinimo)>()
            };

            ViewBag.OpcionesMenu = new SelectList(new List<string>(), "Value", "Text");
            return View(model);
        }


        //public async Task<IActionResult> Index()
        //{
        //    var model = new DashboardViewModel
        //    {
        //        Cajas = await _cajaDAL.LeerCajasAsync() ?? new List<Caja>()
        //    };
        //    if (model.Cajas.Any())
        //    {
        //        model.CajaSeleccionada = model.Cajas.First(); // Por defecto
        //    }
        //    else
        //    {
        //        model.CajaSeleccionada = new Caja { Id = 0, Nombre = "Sin cajas" }; // Evitar null
        //    }
        //    Console.WriteLine($"Cajas enviadas al dashboard: {model.Cajas.Count}");

        //    // Obtenemos las sucursales
        //    var sucursales = _sucursalDAL.GetAll() ?? new List<Sucursal>();

        //    // Seleccionamos una sucursal predeterminada
        //    Sucursal sucursalSeleccionada;
        //    if (sucursales.Any())
        //    {
        //        sucursalSeleccionada = sucursales.First();
        //    }
        //    else
        //    {
        //        sucursalSeleccionada = new Sucursal { Id = 0, Nombre = "Sin Sucursal", Direccion = "" };
        //    }

        //    return View("Index", model);
        //}

        [HttpGet]
        [Route("Dashboard/Login")]
        [AllowAnonymous] // Permitir acceso sin autenticación
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("Dashboard/Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string usuarioNombre, string contrasena)
        {
            var usuario = await _dataAccess.AutenticarUsuarioAsync(usuarioNombre, contrasena);
            if (usuario != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.UsuarioNombre),
                    new Claim(ClaimTypes.Role, (await _dataAccess.LeerTiposUsuarioAsync())
                        .FirstOrDefault(t => t.Id == usuario.TipoId)?.Nombre ?? "Usuario")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToAction("Index");
            }
            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        [HttpPost]
        [Route("Dashboard/Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        //public IActionResult Index(int? sucursalId, int? cajaId, DateTime? fechaInicio, DateTime? fechaFin)
        //{
        //    if (!User.Identity.IsAuthenticated)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var sucursalesPermitidas = User.Claims.Where(c => c.Type == "Sucursal").Select(c => int.Parse(c.Value)).ToList();
        //    var model = _dataAccess.GetDashboardData(sucursalId, fechaInicio, fechaFin);

        //    if (!User.IsInRole("Admin"))
        //    {
        //        model.Sucursales = model.Sucursales.Where(s => sucursalesPermitidas.Contains(s.Id)).ToList();
        //    }

        //    model.SucursalSeleccionada = sucursalId.HasValue && model.Sucursales.Any(s => s.Id == sucursalId.Value)
        //        ? model.Sucursales.FirstOrDefault(s => s.Id == sucursalId.Value)
        //        : model.Sucursales.FirstOrDefault();

        //    model.Cajas = model.Cajas.Where(c => c.SucursalId == model.SucursalSeleccionada?.Id).ToList();
        //    model.CajaSeleccionada = cajaId.HasValue && model.Cajas.Any(c => c.Id == cajaId.Value)
        //        ? model.Cajas.FirstOrDefault(c => c.Id == cajaId.Value)
        //        : model.Cajas.FirstOrDefault();

        //    if (model.CajaSeleccionada != null)
        //    {
        //        model.TurnoActivo = _dataAccess.GetTurnoActivo(model.CajaSeleccionada.Id);
        //    }

        //    return View(model);
        //}

        [HttpPost]
        public IActionResult FiltrarReportes(DateTime? fechaInicio, DateTime? fechaFin, int sucursalId, int cajaId)
        {
            return RedirectToAction("Index", new { sucursalId, cajaId, fechaInicio, fechaFin });
        }

        [HttpGet]
        public IActionResult GetCajasPorSucursal(int sucursalId)
        {
            var cajas = _cajaDAL.LeerCajas().Where(c => c.SucursalId == sucursalId).Select(c => new { c.Id, c.Nombre });
            return Json(cajas);
        }

        [HttpGet]
        public IActionResult GetTurnoActivo(int cajaId)
        {
            var turno = _dataAccess.GetTurnoActivo(cajaId);
            var caja = _cajaDAL.LeerCajas().FirstOrDefault(c => c.Id == cajaId);
            return Json(new
            {
                turnoActivo = turno != null,
                cajaNombre = caja?.Nombre,
                fechaInicio = turno?.FechaInicio.ToString("o"),
                sucursalId = caja?.SucursalId
            });
        }
    }
}