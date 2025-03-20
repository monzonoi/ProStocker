using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProStocker.Web.DAL;
using ProStocker.Web.Models;

namespace ProStocker.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DataAccess _dataAccess;

        public DashboardController(DataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public IActionResult Index(int? sucursalId, int? cajaId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var sucursalesPermitidas = User.Claims.Where(c => c.Type == "Sucursal").Select(c => int.Parse(c.Value)).ToList();
            var todasSucursales = _dataAccess.LeerSucursales();
            var model = new DashboardViewModel
            {
                Sucursales = User.IsInRole("Admin") ? todasSucursales : todasSucursales.Where(s => sucursalesPermitidas.Contains(s.Id)).ToList(),
                Cajas = _dataAccess.LeerCajas(),
                ReporteVentas = _dataAccess.ReporteVentasPorSucursal(fechaInicio, fechaFin),
                ReporteStockMinimo = _dataAccess.ReporteStockMinimo(),
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            model.SucursalSeleccionada = sucursalId.HasValue && model.Sucursales.Any(s => s.Id == sucursalId.Value)
                ? model.Sucursales.FirstOrDefault(s => s.Id == sucursalId.Value)
                : model.Sucursales.FirstOrDefault();

            model.Cajas = model.Cajas.Where(c => c.SucursalId == model.SucursalSeleccionada?.Id).ToList();
            model.CajaSeleccionada = cajaId.HasValue && model.Cajas.Any(c => c.Id == cajaId.Value)
                ? model.Cajas.FirstOrDefault(c => c.Id == cajaId.Value)
                : model.Cajas.FirstOrDefault();

            if (model.CajaSeleccionada != null)
            {
                model.TurnoActivo = _dataAccess.GetTurnoActivo(model.CajaSeleccionada.Id);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult FiltrarReportes(DateTime? fechaInicio, DateTime? fechaFin, int sucursalId, int cajaId)
        {
            return RedirectToAction("Index", new { sucursalId, cajaId, fechaInicio, fechaFin });
        }
    }
}