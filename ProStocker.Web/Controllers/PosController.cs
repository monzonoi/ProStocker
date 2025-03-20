using Microsoft.AspNetCore.Mvc;
using ProStocker.Web.DAL;
using ProStocker.Web.Models;
using System.Text.Json; // Necesitamos este espacio de nombres
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Para ISession

namespace ProStocker.Web.Controllers
{
    [Authorize]
    public class PosController : Controller
    {
        private readonly DataAccess _dataAccess;

        public PosController(DataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public IActionResult Index(int? sucursalId, int? cajaId)
        {
            var model = new PosViewModel
            {
                SucursalId = sucursalId ?? 1,
                CajaId = cajaId ?? 1,
                TurnoActivo = _dataAccess.GetTurnoActivo(cajaId ?? 1)
            };
            return View(model);
        }

        public IActionResult AbrirTurno(int sucursalId, int cajaId)
        {
            var turno = new TurnoCaja
            {
                CajaId = cajaId,
                FechaInicio = DateTime.Now,
                Estado = "Activo"
            };
            _dataAccess.AbrirTurno(turno);
            var model = new PosViewModel
            {
                SucursalId = sucursalId,
                CajaId = cajaId,
                TurnoActivo = turno
            };
            return View("Index", model); // Devuelve la vista Index actualizada
        }

        public IActionResult Index(int sucursalId = 1, int cajaId = 1)
        {
            var turno = _dataAccess.GetTurnoActivo(cajaId);
            if (turno == null)
            {
                return RedirectToAction("AbrirTurno", new { sucursalId, cajaId });
            }
            ViewBag.SucursalId = sucursalId;
            ViewBag.CajaId = cajaId;
            ViewBag.TurnoId = turno.Id;
            return View(new Venta { SucursalId = sucursalId, CajaId = cajaId, TurnoId = turno.Id });
        }

        //public IActionResult AbrirTurno(int sucursalId, int cajaId)
        //{
        //    return View(new TurnoCaja { CajaId = cajaId });
        //}

        [HttpPost]
        public IActionResult AbrirTurno(TurnoCaja turno)
        {
            turno.FechaInicio = DateTime.Now;
            turno.UsuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value); // Usuario logueado
            turno.Estado = "Abierto";
            _dataAccess.AbrirTurno(turno);
            return RedirectToAction("Index", new { sucursalId = turno.CajaId == 1 ? 1 : 2, cajaId = turno.CajaId });
        }

        [HttpPost]
        public IActionResult AgregarProducto(string codigo, int sucursalId, int cajaId, int turnoId)
        {
            var articulo = _dataAccess.GetArticuloPorCodigo(codigo);
            if (articulo == null) return Json(new { success = false, message = "Producto no encontrado" });

            var stock = _dataAccess.GetStock(sucursalId, articulo.Id);
            if (stock == null || stock.Stock < 1) return Json(new { success = false, message = "Sin stock" });

            var venta = new Venta { SucursalId = sucursalId, CajaId = cajaId, TurnoId = turnoId };
            venta.Items.Add(new VentaItem { ArticuloId = articulo.Id, Descripcion = articulo.Descripcion, Cantidad = 1, Precio = articulo.Precio1 });
            HttpContext.Session.SetObject("VentaActual", venta); // Guardar en sesión
            return Json(new { success = true, item = venta.Items.Last() });
        }

        [HttpPost]
        public IActionResult FinalizarVenta(int sucursalId, int cajaId, int turnoId, decimal efectivo, decimal mercadoPago)
        {
            var venta = HttpContext.Session.GetObject<Venta>("VentaActual") ?? new Venta
            {
                SucursalId = sucursalId,
                CajaId = cajaId,
                TurnoId = turnoId
            };
            venta.Fecha = DateTime.Now;
            venta.VendedorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value); // Usuario logueado
            venta.TotalCosto = venta.Items.Sum(i => i.Cantidad * _dataAccess.GetArticuloPorCodigo(i.ArticuloId.ToString()).Costo);
            venta.TotalVenta = venta.Items.Sum(i => i.Subtotal);

            if (efectivo > 0) venta.Pagos.Add(new VentaPago { MetodoPago = "Efectivo", Monto = efectivo });
            if (mercadoPago > 0) venta.Pagos.Add(new VentaPago { MetodoPago = "MercadoPago", Monto = mercadoPago });

            if (venta.Pagos.Sum(p => p.Monto) < venta.TotalVenta)
                return Json(new { success = false, message = "Pago insuficiente" });

            _dataAccess.RegistrarVenta(venta);
            HttpContext.Session.Remove("VentaActual");
            return Json(new { success = true, message = "Venta registrada" });
        }

        public IActionResult CerrarTurno(int turnoId)
        {
            return View(new { TurnoId = turnoId });
        }

        [HttpPost]
        public IActionResult CerrarTurno(int turnoId, decimal montoReal)
        {
            _dataAccess.CerrarTurno(turnoId, montoReal);
            return RedirectToAction("Index", "Home");
        }
    }
}

// Extensión para manejar sesiones
public static class SessionExtensions
{
    public static void SetObject(this ISession session, string key, object value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}