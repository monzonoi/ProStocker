// IVentaDAL.cs
namespace ProStocker.DAL.Interfaces
{
    public interface IVentaDAL
    {
        List<ReporteVenta> GetReporteVentas(int? sucursalId, DateTime fechaInicio, DateTime fechaFin);
    }
}