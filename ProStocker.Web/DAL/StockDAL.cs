// StockDAL.cs
using ProStocker.DAL;
using ProStocker.DAL.Interfaces;
using ProStocker.Web.Models;
using System.Data.SQLite;

public class StockDAL : BaseDataAccess, IStockDAL
{
    public StockDAL(string connectionString) : base(connectionString) { }

    public List<ReporteStockMinimo> GetReporteStockMinimo(int? sucursalId)
    {
        var reportes = new List<ReporteStockMinimo>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand(
            @"SELECT s.Id AS SucursalId, s.Nombre AS SucursalNombre, 
                     a.Id AS ArticuloId, a.Descripcion AS ArticuloNombre, 
                     sp.Stock, sp.StockMinimo
              FROM StockPorSucursal sp
              JOIN Sucursales s ON sp.SucursalId = s.Id
              JOIN Articulos a ON sp.ArticuloId = a.Id
              WHERE sp.Stock <= sp.StockMinimo
              AND (@SucursalId IS NULL OR sp.SucursalId = @SucursalId)", conn);
        cmd.Parameters.AddWithValue("@SucursalId", (object)sucursalId ?? DBNull.Value);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            reportes.Add(new ReporteStockMinimo
            {
                Sucursal = new Sucursal { Id = reader.GetInt32(0), Nombre = reader.GetString(1) },
                Articulo = new Articulo { Id = reader.GetInt32(2), Descripcion = reader.GetString(3) },
                Stock = (decimal)reader.GetDouble(4),
                StockMinimo = (decimal)reader.GetDouble(5)
            });
        }
        return reportes;
    }

 
}