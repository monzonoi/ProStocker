// IStockDAL.cs
namespace ProStocker.DAL.Interfaces
{
    public interface IStockDAL
    {
        List<ReporteStockMinimo> GetReporteStockMinimo(int? sucursalId);
    }
}