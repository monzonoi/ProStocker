// DAL/DataAccess.cs
using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using ProStocker.Web.Models;

namespace ProStocker.Web.DAL
{
    public class DataAccess
    {
        private readonly string _connectionString;

        public DataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SQLiteConnection");
        }

        // Abrir turno
        public int AbrirTurno(TurnoCaja turno)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "INSERT INTO TurnosCaja (CajaId, UsuarioId, FechaInicio, MontoInicial, Estado) " +
                "VALUES (@CajaId, @UsuarioId, @FechaInicio, @MontoInicial, 'Abierto'); " +
                "SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@CajaId", turno.CajaId);
            cmd.Parameters.AddWithValue("@UsuarioId", turno.UsuarioId);
            cmd.Parameters.AddWithValue("@FechaInicio", turno.FechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"));
            cmd.Parameters.AddWithValue("@MontoInicial", turno.MontoInicial);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Obtener turno activo por caja
        public TurnoCaja GetTurnoActivo(int cajaId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT Id, CajaId, UsuarioId, FechaInicio, MontoInicial, Estado " +
                "FROM TurnosCaja WHERE CajaId = @CajaId AND Estado = 'Abierto'", conn);
            cmd.Parameters.AddWithValue("@CajaId", cajaId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TurnoCaja
                {
                    Id = reader.GetInt32(0),
                    CajaId = reader.GetInt32(1),
                    UsuarioId = reader.GetInt32(2),
                    FechaInicio = DateTime.Parse(reader.GetString(3)),
                    MontoInicial = reader.GetDecimal(4),
                    Estado = reader.GetString(5)
                };
            }
            return null;
        }

        // Buscar artículo por código
        public Articulo GetArticuloPorCodigo(string codigo)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT Id, Codigo, Descripcion, Precio1, Costo FROM Articulos WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigo);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Articulo
                {
                    Id = reader.GetInt32(0),
                    Codigo = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    Precio1 = reader.GetDecimal(3),
                    Costo = reader.GetDecimal(4)
                };
            }
            return null;
        }

        // Verificar stock
        public StockPorSucursal GetStock(int sucursalId, int articuloId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT SucursalId, ArticuloId, Stock, StockMinimo FROM StockPorSucursal " +
                "WHERE SucursalId = @SucursalId AND ArticuloId = @ArticuloId", conn);
            cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
            cmd.Parameters.AddWithValue("@ArticuloId", articuloId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new StockPorSucursal
                {
                    SucursalId = reader.GetInt32(0),
                    ArticuloId = reader.GetInt32(1),
                    Stock = reader.GetDecimal(2),
                    StockMinimo = reader.GetDecimal(3)
                };
            }
            return null;
        }

        // Registrar venta
        public int RegistrarVenta(Venta venta)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Insertar venta
                var cmd = new SQLiteCommand(
                    "INSERT INTO Ventas (SucursalId, CajaId, TurnoId, Fecha, VendedorId, TotalCosto, TotalVenta, Estado) " +
                    "VALUES (@SucursalId, @CajaId, @TurnoId, @Fecha, @VendedorId, @TotalCosto, @TotalVenta, 'Completada'); " +
                    "SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@SucursalId", venta.SucursalId);
                cmd.Parameters.AddWithValue("@CajaId", venta.CajaId);
                cmd.Parameters.AddWithValue("@TurnoId", venta.TurnoId);
                cmd.Parameters.AddWithValue("@Fecha", venta.Fecha.ToString("yyyy-MM-ddTHH:mm:ss"));
                cmd.Parameters.AddWithValue("@VendedorId", venta.VendedorId);
                cmd.Parameters.AddWithValue("@TotalCosto", venta.TotalCosto);
                cmd.Parameters.AddWithValue("@TotalVenta", venta.TotalVenta);
                int ventaId = Convert.ToInt32(cmd.ExecuteScalar());

                // Insertar pagos
                foreach (var pago in venta.Pagos)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO VentaPagos (VentaId, MetodoPago, Monto, Detalle, Estado) " +
                        "VALUES (@VentaId, @MetodoPago, @Monto, @Detalle, 'Confirmado')", conn);
                    cmd.Parameters.AddWithValue("@VentaId", ventaId);
                    cmd.Parameters.AddWithValue("@MetodoPago", pago.MetodoPago);
                    cmd.Parameters.AddWithValue("@Monto", pago.Monto);
                    cmd.Parameters.AddWithValue("@Detalle", pago.Detalle ?? "");
                    cmd.ExecuteNonQuery();
                }

                // Actualizar stock
                foreach (var item in venta.Items)
                {
                    cmd = new SQLiteCommand(
                        "UPDATE StockPorSucursal SET Stock = Stock - @Cantidad " +
                        "WHERE SucursalId = @SucursalId AND ArticuloId = @ArticuloId", conn);
                    cmd.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue("@SucursalId", venta.SucursalId);
                    cmd.Parameters.AddWithValue("@ArticuloId", item.ArticuloId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return ventaId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Cerrar turno
        public void CerrarTurno(int turnoId, decimal montoReal)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "UPDATE TurnosCaja SET FechaFin = @FechaFin, MontoReal = @MontoReal, " +
                "MontoFinal = (SELECT SUM(Monto) FROM VentaPagos WHERE VentaId IN " +
                "(SELECT Id FROM Ventas WHERE TurnoId = @TurnoId)), " +
                "Diferencia = @MontoReal - (SELECT SUM(Monto) FROM VentaPagos WHERE VentaId IN " +
                "(SELECT Id FROM Ventas WHERE TurnoId = @TurnoId)), " +
                "Estado = 'Cerrado' WHERE Id = @TurnoId", conn);
            cmd.Parameters.AddWithValue("@FechaFin", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
            cmd.Parameters.AddWithValue("@MontoReal", montoReal);
            cmd.Parameters.AddWithValue("@TurnoId", turnoId);
            cmd.ExecuteNonQuery();
        }
    }
}