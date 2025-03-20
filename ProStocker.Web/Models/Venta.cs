// Models/Venta.cs
namespace ProStocker.Web.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public int CajaId { get; set; }
        public int TurnoId { get; set; }
        public DateTime Fecha { get; set; }
        public int VendedorId { get; set; }
        public decimal TotalCosto { get; set; }
        public decimal TotalVenta { get; set; }
        public string Estado { get; set; }
        public List<VentaPago> Pagos { get; set; } = new List<VentaPago>();
        public List<VentaItem> Items { get; set; } = new List<VentaItem>();
    }

    public class VentaItem
    {
        public int ArticuloId { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Cantidad * Precio;
    }

    public class VentaPago
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public string MetodoPago { get; set; }
        public decimal Monto { get; set; }
        public string Detalle { get; set; }
        public string Estado { get; set; }
    }
}