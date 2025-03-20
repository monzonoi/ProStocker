// Models/StockPorSucursal.cs
namespace ProStocker.Web.Models
{
    public class StockPorSucursal
    {
        public int SucursalId { get; set; }
        public int ArticuloId { get; set; }
        public decimal Stock { get; set; }
        public decimal StockMinimo { get; set; }
    }
}