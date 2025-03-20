namespace ProStocker.Web.Models
{
    public class DashboardViewModel
    {
        public List<Sucursal> Sucursales { get; set; }
        public Sucursal SucursalSeleccionada { get; set; }
        public List<Caja> Cajas { get; set; }
        public Caja CajaSeleccionada { get; set; }
        public TurnoCaja TurnoActivo { get; set; }
        public List<(Sucursal Sucursal, decimal TotalVentas, decimal TotalGanancia)> ReporteVentas { get; set; }
        public List<(Sucursal Sucursal, Articulo Articulo, decimal Stock, decimal StockMinimo)> ReporteStockMinimo { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal VentasTotales { get; set; }
        public decimal GananciaTotal { get; set; }
        public decimal TicketPromedio { get; set; }
        public int TotalTransacciones { get; set; }
    }
}