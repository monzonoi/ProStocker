// VentaDAL.cs
using ProStocker.DAL;
using ProStocker.DAL.Interfaces;
using ProStocker.Web.Models;
using System.Data.SQLite;

public class VentaDAL : BaseDataAccess, IVentaDAL
{
    public VentaDAL(string connectionString) : base(connectionString) { }

    public List<ReporteVenta> GetReporteVentas(int? sucursalId, DateTime fechaInicio, DateTime fechaFin)
    {
        var reportes = new List<ReporteVenta>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand(
            @"SELECT s.Id AS SucursalId, s.Nombre AS SucursalNombre, 
                     SUM(v.TotalVenta) AS TotalVentas, 
                     SUM(v.TotalVenta - v.TotalCosto) AS TotalGanancia
              FROM Ventas v
              JOIN Sucursales s ON v.SucursalId = s.Id
              WHERE v.Fecha BETWEEN @FechaInicio AND @FechaFin
              AND (@SucursalId IS NULL OR v.SucursalId = @SucursalId)
              GROUP BY s.Id, s.Nombre", conn);
        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"));
        cmd.Parameters.AddWithValue("@FechaFin", fechaFin.ToString("yyyy-MM-ddTHH:mm:ss"));
        cmd.Parameters.AddWithValue("@SucursalId", (object)sucursalId ?? DBNull.Value);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            reportes.Add(new ReporteVenta
            {
                Sucursal = new Sucursal { Id = reader.GetInt32(0), Nombre = reader.GetString(1) },
                TotalVentas = (decimal)reader.GetDouble(2),
                TotalGanancia = (decimal)reader.GetDouble(3)
            });
        }
        return reportes;
    }
}