// DAL/DataAccess.cs
using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using ProStocker.Web.Models;

namespace ProStocker.Web.DAL
{
    public class DataAccess
    {
        readonly string _connectionString;

        public DataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SQLiteConnection");
        }


        //public async Task<List<Caja>> LeerCajasAsync()
        //{
        //    var cajas = new List<Caja>();
        //    using var conn = await GetOpenConnectionAsync();
        //    var cmd = new SQLiteCommand("SELECT Id, Nombre, Total, Estado FROM Cajas", conn);
        //    try
        //    {
        //        using var reader = await cmd.ExecuteReaderAsync();
        //        while (await reader.ReadAsync())
        //        {
        //            cajas.Add(new Caja
        //            {
        //                Id = reader.GetInt32(0),
        //                Nombre = reader.GetString(1),
        //                Total = reader.GetDecimal(2),
        //                Estado = reader.GetString(3)
        //            });
        //        }
        //    }
        //    catch (SQLiteException ex) 
        //    {
        //        return new List<Caja>(); // Devolver lista vacía si la tabla no está creada
        //    }
        //    return cajas;
        //}

        public async Task<List<TipoUsuario>> LeerTiposUsuarioAsync()
        {
            var tipos = new List<TipoUsuario>();
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand("SELECT Id, Nombre, Descripcion FROM TiposUsuario", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tipos.Add(new TipoUsuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return tipos;
        }

        // Método auxiliar para abrir conexión asíncrona
        private async Task<SQLiteConnection> GetOpenConnectionAsync()
        {
            var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        // Crear usuario asíncrono
        public async Task CrearUsuarioAsync(Usuario usuario)
        {
            using var conn = await GetOpenConnectionAsync();
            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                var cmd = new SQLiteCommand(
                    "INSERT INTO Usuarios (Nombre, Usuario, Contrasena, TipoId) " +
                    "VALUES (@Nombre, @Usuario, @Contrasena, @TipoId); " +
                    "SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@Usuario", usuario.UsuarioNombre);
                cmd.Parameters.AddWithValue("@Contrasena", BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena));
                cmd.Parameters.AddWithValue("@TipoId", usuario.TipoId);
                int usuarioId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                foreach (var sucursalId in usuario.Sucursales)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) " +
                        "VALUES (@UsuarioId, @SucursalId)", conn);
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Leer usuarios asíncrono
        public async Task<List<Usuario>> LeerUsuariosAsync()
        {
            var usuarios = new List<Usuario>();
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand(
                "SELECT u.Id, u.Nombre, u.Usuario, u.TipoId " +
                "FROM Usuarios u", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var usuario = new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    UsuarioNombre = reader.GetString(2),
                    TipoId = reader.GetInt32(3)
                };
                usuario.Sucursales = await GetSucursalesPorUsuarioAsync(usuario.Id);
                usuarios.Add(usuario);
            }
            return usuarios;
        }

        // Actualizar usuario asíncrono
        public async Task ActualizarUsuarioAsync(Usuario usuario)
        {
            using var conn = await GetOpenConnectionAsync();
            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                var cmd = new SQLiteCommand(
                    "UPDATE Usuarios SET Nombre = @Nombre, Usuario = @Usuario, " +
                    "Contrasena = @Contrasena, TipoId = @TipoId WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", usuario.Id);
                cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@Usuario", usuario.UsuarioNombre);
                cmd.Parameters.AddWithValue("@Contrasena", string.IsNullOrEmpty(usuario.Contrasena)
                    ? await GetHashActualAsync(usuario.Id)
                    : BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena));
                cmd.Parameters.AddWithValue("@TipoId", usuario.TipoId);
                await cmd.ExecuteNonQueryAsync();

                cmd = new SQLiteCommand("DELETE FROM UsuarioSucursal WHERE UsuarioId = @UsuarioId", conn);
                cmd.Parameters.AddWithValue("@UsuarioId", usuario.Id);
                await cmd.ExecuteNonQueryAsync();

                foreach (var sucursalId in usuario.Sucursales)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) " +
                        "VALUES (@UsuarioId, @SucursalId)", conn);
                    cmd.Parameters.AddWithValue("@UsuarioId", usuario.Id);
                    cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Eliminar usuario asíncrono
        public async Task EliminarUsuarioAsync(int id)
        {
            using var conn = await GetOpenConnectionAsync();
            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                // Eliminar asignaciones de sucursales
                var cmd = new SQLiteCommand("DELETE FROM UsuarioSucursal WHERE UsuarioId = @UsuarioId", conn);
                cmd.Parameters.AddWithValue("@UsuarioId", id);
                await cmd.ExecuteNonQueryAsync();

                // Eliminar usuario
                cmd = new SQLiteCommand("DELETE FROM Usuarios WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Leer usuario por ID asíncrono
        public async Task<Usuario?> LeerUsuarioPorIdAsync(int id)
        {
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand(
                "SELECT Id, Nombre, Usuario, Contrasena, TipoId FROM Usuarios WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var usuario = new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    UsuarioNombre = reader.GetString(2),
                    Contrasena = reader.GetString(3),
                    TipoId = reader.GetInt32(4)
                };
                usuario.Sucursales = await GetSucursalesPorUsuarioAsync(usuario.Id);
                return usuario;
            }
            return null;
        }

        //// Leer tipos de usuario asíncrono
        //public async Task<List<TipoUsuario>> LeerTiposUsuarioAsync()
        //{
        //    var tipos = new List<TipoUsuario>();
        //    using var conn = await GetOpenConnectionAsync();
        //    var cmd = new SQLiteCommand("SELECT Id, Nombre, Descripcion FROM TiposUsuario", conn);
        //    using var reader = await cmd.ExecuteReaderAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        tipos.Add(new TipoUsuario
        //        {
        //            Id = reader.GetInt32(0),
        //            Nombre = reader.GetString(1),
        //            Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2)
        //        });
        //    }
        //    return tipos;
        //}

        // Método movido y hecho público
        public async Task InicializarTiposUsuarioAsync()
        {
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand(
                "INSERT OR IGNORE INTO TiposUsuario (Nombre, Descripcion) VALUES " +
                "('Admin', 'Usuario con permisos administrativos'), " +
                "('Vendedor', 'Usuario para ventas')", conn);
            await cmd.ExecuteNonQueryAsync();
        }



        // Obtener sucursales por usuario asíncrono
        private async Task<List<int>> GetSucursalesPorUsuarioAsync(int usuarioId)
        {
            var sucursales = new List<int>();
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand(
                "SELECT SucursalId FROM UsuarioSucursal WHERE UsuarioId = @UsuarioId", conn);
            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sucursales.Add(reader.GetInt32(0));
            }
            return sucursales;
        }

        // Obtener hash actual asíncrono
        private async Task<string> GetHashActualAsync(int usuarioId)
        {
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand("SELECT Contrasena FROM Usuarios WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", usuarioId);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        // Ejemplo: Leer sucursales asíncrono
        public async Task<List<Sucursal>> LeerSucursalesAsync()
        {
            var sucursales = new List<Sucursal>();
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand("SELECT Id, Nombre, Direccion FROM Sucursales", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sucursales.Add(new Sucursal
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return sucursales;
        }


        public DashboardViewModel GetDashboardData(int? sucursalId, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var model = new DashboardViewModel
            {
                Sucursales = LeerSucursales(),
                //Cajas = LeerCajas(),
                ReporteVentas = ReporteVentasPorSucursal(fechaInicio, fechaFin),
                ReporteStockMinimo = ReporteStockMinimo(),
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            // Calcular métricas
            var sql = "SELECT COUNT(*) as TotalTransacciones, SUM(TotalVenta) as VentasTotales, " +
                      "SUM(TotalVenta - TotalCosto) as GananciaTotal " +
                      "FROM Ventas WHERE Estado = 'Completada'";
            if (sucursalId.HasValue) sql += " AND SucursalId = @SucursalId";
            if (fechaInicio.HasValue) sql += " AND Fecha >= @FechaInicio";
            if (fechaFin.HasValue) sql += " AND Fecha <= @FechaFin";

            var cmd = new SQLiteCommand(sql, conn);
            if (sucursalId.HasValue) cmd.Parameters.AddWithValue("@SucursalId", sucursalId.Value);
            if (fechaInicio.HasValue) cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
            if (fechaFin.HasValue) cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Value.ToString("yyyy-MM-ddTHH:mm:ss"));

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model.TotalTransacciones = reader.GetInt32(0);
                model.VentasTotales = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                model.GananciaTotal = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                model.TicketPromedio = model.TotalTransacciones > 0 ? model.VentasTotales / model.TotalTransacciones : 0;
            }

            return model;
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
                "SELECT Id, Codigo, Descripcion, Imagen, Precio1, Precio2, Precio3, Costo " +
                "FROM Articulos WHERE Codigo = @Codigo", conn);
            cmd.Parameters.AddWithValue("@Codigo", codigo);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Articulo
                {
                    Id = reader.GetInt32(0),
                    Codigo = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    Imagen = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Precio1 = reader.GetDecimal(4),
                    Precio2 = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    Precio3 = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    Costo = reader.GetDecimal(7)
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




        // CRUD para Artículos
        public void CrearArticulo(Articulo articulo)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "INSERT INTO Articulos (Codigo, Descripcion, Imagen, Precio1, Precio2, Precio3, Costo) " +
                "VALUES (@Codigo, @Descripcion, @Imagen, @Precio1, @Precio2, @Precio3, @Costo)", conn);
            cmd.Parameters.AddWithValue("@Codigo", articulo.Codigo);
            cmd.Parameters.AddWithValue("@Descripcion", articulo.Descripcion);
            cmd.Parameters.AddWithValue("@Imagen", (object)articulo.Imagen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Precio1", articulo.Precio1);
            cmd.Parameters.AddWithValue("@Precio2", (object)articulo.Precio2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Precio3", (object)articulo.Precio3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Costo", articulo.Costo);
            cmd.ExecuteNonQuery();
        }

        public List<Articulo> LeerArticulos()
        {
            var articulos = new List<Articulo>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand("SELECT Id, Codigo, Descripcion, Imagen, Precio1, Precio2, Precio3, Costo FROM Articulos", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                articulos.Add(new Articulo
                {
                    Id = reader.GetInt32(0),
                    Codigo = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    Imagen = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Precio1 = reader.GetDecimal(4),
                    Precio2 = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    Precio3 = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    Costo = reader.GetDecimal(7)
                });
            }
            return articulos;
        }

        public void ActualizarArticulo(Articulo articulo)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "UPDATE Articulos SET Codigo = @Codigo, Descripcion = @Descripcion, Imagen = @Imagen, " +
                "Precio1 = @Precio1, Precio2 = @Precio2, Precio3 = @Precio3, Costo = @Costo WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", articulo.Id);
            cmd.Parameters.AddWithValue("@Codigo", articulo.Codigo);
            cmd.Parameters.AddWithValue("@Descripcion", articulo.Descripcion);
            cmd.Parameters.AddWithValue("@Imagen", (object)articulo.Imagen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Precio1", articulo.Precio1);
            cmd.Parameters.AddWithValue("@Precio2", (object)articulo.Precio2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Precio3", (object)articulo.Precio3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Costo", articulo.Costo);
            cmd.ExecuteNonQuery();
        }

        public void EliminarArticulo(int id)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand("DELETE FROM Articulos WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        // CRUD para StockPorSucursal
        public void CrearStockPorSucursal(StockPorSucursal stock)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "INSERT INTO StockPorSucursal (SucursalId, ArticuloId, Stock, StockMinimo) " +
                "VALUES (@SucursalId, @ArticuloId, @Stock, @StockMinimo)", conn);
            cmd.Parameters.AddWithValue("@SucursalId", stock.SucursalId);
            cmd.Parameters.AddWithValue("@ArticuloId", stock.ArticuloId);
            cmd.Parameters.AddWithValue("@Stock", stock.Stock);
            cmd.Parameters.AddWithValue("@StockMinimo", stock.StockMinimo);
            cmd.ExecuteNonQuery();
        }

        public List<StockPorSucursal> LeerStockPorSucursal()
        {
            var stocks = new List<StockPorSucursal>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand("SELECT SucursalId, ArticuloId, Stock, StockMinimo FROM StockPorSucursal", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                stocks.Add(new StockPorSucursal
                {
                    SucursalId = reader.GetInt32(0),
                    ArticuloId = reader.GetInt32(1),
                    Stock = reader.GetDecimal(2),
                    StockMinimo = reader.GetDecimal(3)
                });
            }
            return stocks;
        }

        public void ActualizarStockPorSucursal(StockPorSucursal stock)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "UPDATE StockPorSucursal SET Stock = @Stock, StockMinimo = @StockMinimo " +
                "WHERE SucursalId = @SucursalId AND ArticuloId = @ArticuloId", conn);
            cmd.Parameters.AddWithValue("@SucursalId", stock.SucursalId);
            cmd.Parameters.AddWithValue("@ArticuloId", stock.ArticuloId);
            cmd.Parameters.AddWithValue("@Stock", stock.Stock);
            cmd.Parameters.AddWithValue("@StockMinimo", stock.StockMinimo);
            cmd.ExecuteNonQuery();
        }

        public void EliminarStockPorSucursal(int sucursalId, int articuloId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "DELETE FROM StockPorSucursal WHERE SucursalId = @SucursalId AND ArticuloId = @ArticuloId", conn);
            cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
            cmd.Parameters.AddWithValue("@ArticuloId", articuloId);
            cmd.ExecuteNonQuery();
        }

        // CRUD para Cajas
        //public void CrearCaja(Caja caja)
        //{
        //    using var conn = new SQLiteConnection(_connectionString);
        //    conn.Open();
        //    var cmd = new SQLiteCommand(
        //        "INSERT INTO Cajas (SucursalId, Nombre, Estado) VALUES (@SucursalId, @Nombre, @Estado)", conn);
        //    cmd.Parameters.AddWithValue("@SucursalId", caja.SucursalId);
        //    cmd.Parameters.AddWithValue("@Nombre", caja.Nombre);
        //    cmd.Parameters.AddWithValue("@Estado", caja.Estado);
        //    cmd.ExecuteNonQuery();
        //}

        //public List<Caja> LeerCajas()
        //{
        //    var cajas = new List<Caja>();
        //    using var conn = new SQLiteConnection(_connectionString);
        //    conn.Open();
        //    var cmd = new SQLiteCommand("SELECT Id, SucursalId, Nombre, Estado FROM Cajas", conn);
        //    using var reader = cmd.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        cajas.Add(new Caja
        //        {
        //            Id = reader.GetInt32(0),
        //            SucursalId = reader.GetInt32(1),
        //            Nombre = reader.GetString(2),
        //            Estado = reader.GetString(3)
        //        });
        //    }
        //    return cajas;
        //}

        //public void ActualizarCaja(Caja caja)
        //{
        //    using var conn = new SQLiteConnection(_connectionString);
        //    conn.Open();
        //    var cmd = new SQLiteCommand(
        //        "UPDATE Cajas SET SucursalId = @SucursalId, Nombre = @Nombre, Estado = @Estado WHERE Id = @Id", conn);
        //    cmd.Parameters.AddWithValue("@Id", caja.Id);
        //    cmd.Parameters.AddWithValue("@SucursalId", caja.SucursalId);
        //    cmd.Parameters.AddWithValue("@Nombre", caja.Nombre);
        //    cmd.Parameters.AddWithValue("@Estado", caja.Estado);
        //    cmd.ExecuteNonQuery();
        //}

        //public void EliminarCaja(int id)
        //{
        //    using var conn = new SQLiteConnection(_connectionString);
        //    conn.Open();
        //    var cmd = new SQLiteCommand("DELETE FROM Cajas WHERE Id = @Id", conn);
        //    cmd.Parameters.AddWithValue("@Id", id);
        //    cmd.ExecuteNonQuery();
        //}

        // CRUD para TurnosCaja (ya tenemos Abrir y Cerrar, añadimos Leer y Actualizar)
        public List<TurnoCaja> LeerTurnosCaja()
        {
            var turnos = new List<TurnoCaja>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT Id, CajaId, UsuarioId, FechaInicio, FechaFin, MontoInicial, MontoFinal, MontoReal, Diferencia, Estado " +
                "FROM TurnosCaja", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                turnos.Add(new TurnoCaja
                {
                    Id = reader.GetInt32(0),
                    CajaId = reader.GetInt32(1),
                    UsuarioId = reader.GetInt32(2),
                    FechaInicio = DateTime.Parse(reader.GetString(3)),
                    FechaFin = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    MontoInicial = reader.GetDecimal(5),
                    MontoFinal = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    MontoReal = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    Diferencia = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                    Estado = reader.GetString(9)
                });
            }
            return turnos;
        }

        public void ActualizarTurnoCaja(TurnoCaja turno)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "UPDATE TurnosCaja SET CajaId = @CajaId, UsuarioId = @UsuarioId, FechaInicio = @FechaInicio, " +
                "FechaFin = @FechaFin, MontoInicial = @MontoInicial, MontoFinal = @MontoFinal, " +
                "MontoReal = @MontoReal, Diferencia = @Diferencia, Estado = @Estado WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", turno.Id);
            cmd.Parameters.AddWithValue("@CajaId", turno.CajaId);
            cmd.Parameters.AddWithValue("@UsuarioId", turno.UsuarioId);
            cmd.Parameters.AddWithValue("@FechaInicio", turno.FechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"));
            cmd.Parameters.AddWithValue("@FechaFin", (object)turno.FechaFin?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MontoInicial", turno.MontoInicial);
            cmd.Parameters.AddWithValue("@MontoFinal", (object)turno.MontoFinal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MontoReal", (object)turno.MontoReal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Diferencia", (object)turno.Diferencia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", turno.Estado);
            cmd.ExecuteNonQuery();
        }
    


    // Obtener sucursales
        public List<Sucursal> LeerSucursales()
        {
            var sucursales = new List<Sucursal>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand("SELECT Id, Nombre, Direccion FROM Sucursales", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sucursales.Add(new Sucursal
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return sucursales;
        }

        // Reporte de ventas por sucursal
        public List<(Sucursal Sucursal, decimal TotalVentas, decimal TotalGanancia)> ReporteVentasPorSucursal(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var reportes = new List<(Sucursal, decimal, decimal)>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var sql = "SELECT s.Id, s.Nombre, s.Direccion, SUM(v.TotalVenta) as TotalVentas, " +
                      "SUM(v.TotalVenta - v.TotalCosto) as TotalGanancia " +
                      "FROM Ventas v " +
                      "JOIN Sucursales s ON v.SucursalId = s.Id " +
                      "WHERE v.Estado = 'Completada'";
            if (fechaInicio.HasValue) sql += " AND v.Fecha >= @FechaInicio";
            if (fechaFin.HasValue) sql += " AND v.Fecha <= @FechaFin";
            sql += " GROUP BY s.Id, s.Nombre, s.Direccion";

            var cmd = new SQLiteCommand(sql, conn);
            if (fechaInicio.HasValue) cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
            if (fechaFin.HasValue) cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Value.ToString("yyyy-MM-ddTHH:mm:ss"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var sucursal = new Sucursal
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
                var totalVentas = reader.GetDecimal(3);
                var totalGanancia = reader.GetDecimal(4);
                reportes.Add((sucursal, totalVentas, totalGanancia));
            }
            return reportes;
        }

        // Reporte de stock mínimo por sucursal
        public List<(Sucursal Sucursal, Articulo Articulo, decimal Stock, decimal StockMinimo)> ReporteStockMinimo()
        {
            var reportes = new List<(Sucursal, Articulo, decimal, decimal)>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT s.Id, s.Nombre, s.Direccion, a.Id, a.Codigo, a.Descripcion, " +
                "sp.Stock, sp.StockMinimo " +
                "FROM StockPorSucursal sp " +
                "JOIN Sucursales s ON sp.SucursalId = s.Id " +
                "JOIN Articulos a ON sp.ArticuloId = a.Id " +
                "WHERE sp.Stock <= sp.StockMinimo", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var sucursal = new Sucursal
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
                var articulo = new Articulo
                {
                    Id = reader.GetInt32(3),
                    Codigo = reader.GetString(4),
                    Descripcion = reader.GetString(5)
                };
                var stock = reader.GetDecimal(6);
                var stockMinimo = reader.GetDecimal(7);
                reportes.Add((sucursal, articulo, stock, stockMinimo));
            }
            return reportes;
        }

        public async Task<Usuario?> AutenticarUsuarioAsync(string usuarioNombre, string contrasena)
        {
            using var conn = await GetOpenConnectionAsync();
            var cmd = new SQLiteCommand(
                "SELECT Id, Nombre, Usuario, Contrasena, TipoId " +
                "FROM Usuarios WHERE Usuario = @Usuario", conn);
            cmd.Parameters.AddWithValue("@Usuario", usuarioNombre);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var hashAlmacenado = reader.GetString(3);
                if (BCrypt.Net.BCrypt.Verify(contrasena, hashAlmacenado))
                {
                    return new Usuario
                    {
                        Id = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        UsuarioNombre = reader.GetString(2),
                        // No devolvemos Contrasena para evitar exponer el hash
                        TipoId = reader.GetInt32(4),
                        Sucursales = await GetSucursalesPorUsuarioAsync(reader.GetInt32(0))
                    };
                }
            }
            return null;
        }


        public Usuario AutenticarUsuario(string usuarioNombre, string contrasena)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT Id, Nombre, Usuario, Contrasena, Tipo FROM Usuarios WHERE Usuario = @Usuario", conn);
            cmd.Parameters.AddWithValue("@Usuario", usuarioNombre);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var hash = reader.GetString(3); // Contrasena
                if (BCrypt.Net.BCrypt.Verify(contrasena, hash))
                {
                    var usuario = new Usuario
                    {
                        Id = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        UsuarioNombre = reader.GetString(2),
                        Contrasena = hash,
                        
                        //Tipo = reader.GetString(4)
                    };
                    // Obtener sucursales asignadas
                    usuario.Sucursales = GetSucursalesPorUsuario(usuario.Id);
                    return usuario;
                }
            }
            return null;
        }


        public void CrearUsuario(Usuario usuario)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Insertar usuario
                var cmd = new SQLiteCommand(
                    "INSERT INTO Usuarios (Nombre, Usuario, Contrasena, Tipo) " +
                    "VALUES (@Nombre, @Usuario, @Contrasena, @Tipo); " +
                    "SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@Usuario", usuario.UsuarioNombre);
                cmd.Parameters.AddWithValue("@Contrasena", BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena));
                //cmd.Parameters.AddWithValue("@Tipo", usuario.Tipo);
                int usuarioId = Convert.ToInt32(cmd.ExecuteScalar());

                // Asignar sucursales
                foreach (var sucursalId in usuario.Sucursales)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) " +
                        "VALUES (@UsuarioId, @SucursalId)", conn);
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Usuario> LeerUsuarios()
        {
            var usuarios = new List<Usuario>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT Id, Nombre, Usuario, Tipo FROM Usuarios", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var usuario = new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    UsuarioNombre = reader.GetString(2),
                    //Tipo = reader.GetString(3)
                };
                usuario.Sucursales = GetSucursalesPorUsuario(usuario.Id);
                usuarios.Add(usuario);
            }
            return usuarios;
        }

        public void ActualizarUsuario(Usuario usuario)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Actualizar datos del usuario
                var cmd = new SQLiteCommand(
                    "UPDATE Usuarios SET Nombre = @Nombre, Usuario = @Usuario, " +
                    "Contrasena = @Contrasena, Tipo = @Tipo WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", usuario.Id);
                cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@Usuario", usuario.UsuarioNombre);
                cmd.Parameters.AddWithValue("@Contrasena", string.IsNullOrEmpty(usuario.Contrasena)
                    ? GetHashActual(usuario.Id) // No cambiar contraseña si está vacía
                    : BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena));
                //cmd.Parameters.AddWithValue("@Tipo", usuario.Tipo);
                cmd.ExecuteNonQuery();

                // Eliminar sucursales existentes
                cmd = new SQLiteCommand("DELETE FROM UsuarioSucursal WHERE UsuarioId = @UsuarioId", conn);
                cmd.Parameters.AddWithValue("@UsuarioId", usuario.Id);
                cmd.ExecuteNonQuery();

                // Reasignar sucursales
                foreach (var sucursalId in usuario.Sucursales)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) " +
                        "VALUES (@UsuarioId, @SucursalId)", conn);
                    cmd.Parameters.AddWithValue("@UsuarioId", usuario.Id);
                    cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void EliminarUsuario(int id)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Eliminar asignaciones de sucursales
                var cmd = new SQLiteCommand("DELETE FROM UsuarioSucursal WHERE UsuarioId = @UsuarioId", conn);
                cmd.Parameters.AddWithValue("@UsuarioId", id);
                cmd.ExecuteNonQuery();

                // Eliminar usuario
                cmd = new SQLiteCommand("DELETE FROM Usuarios WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private string GetHashActual(int usuarioId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand("SELECT Contrasena FROM Usuarios WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", usuarioId);
            return cmd.ExecuteScalar()?.ToString();
        }

        // Método existente para sucursales por usuario
        private List<int> GetSucursalesPorUsuario(int usuarioId)
        {
            var sucursales = new List<int>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT SucursalId FROM UsuarioSucursal WHERE UsuarioId = @UsuarioId", conn);
            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sucursales.Add(reader.GetInt32(0));
            }
            return sucursales;
        }

        internal Usuario? LeerUsuarioPorId(int id)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var cmd = new SQLiteCommand(
                "SELECT Id, Nombre, Usuario, Contrasena, Tipo FROM Usuarios WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var usuario = new Usuario
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    UsuarioNombre = reader.GetString(2),
                    Contrasena = reader.GetString(3),
                    //Tipo = reader.GetString(4)
                };
                usuario.Sucursales = GetSucursalesPorUsuario(usuario.Id);
                return usuario;
            }
            return null;
        }

    }
}
