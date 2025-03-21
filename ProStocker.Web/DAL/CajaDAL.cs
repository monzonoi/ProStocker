// CajaDAL.cs
using ProStocker.DAL;
using ProStocker.DAL.Interfaces;
using ProStocker.Web.Models;
using System.Data.SQLite;

public class CajaDAL : BaseDataAccess, ICajaDAL
{
    public CajaDAL(string connectionString) : base(connectionString) { }


    public async Task<List<Caja>> LeerCajasAsync()
    {
        var cajas = new List<Caja>();
        using var conn = GetConnection();
        var cmd = new SQLiteCommand("SELECT Id, Nombre, Total, Estado FROM Cajas", conn);
        try
        {
            await conn.OpenAsync(); // Abrir la conexión de manera asíncrona
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                cajas.Add(new Caja
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Total = reader.GetDecimal(2),
                    Estado = reader.GetString(3)
                });
            }
        }
        catch (SQLiteException ex)
        {
            return new List<Caja>(); // Devolver lista vacía si la tabla no está creada
        }
        return cajas;
    }

    public void CrearCaja(Caja caja)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand(
            "INSERT INTO Cajas (SucursalId, Nombre, Estado) VALUES (@SucursalId, @Nombre, @Estado)", conn);
        cmd.Parameters.AddWithValue("@SucursalId", caja.SucursalId);
        cmd.Parameters.AddWithValue("@Nombre", caja.Nombre);
        cmd.Parameters.AddWithValue("@Estado", caja.Estado);
        cmd.ExecuteNonQuery();
    }


    public void ActualizarCaja(Caja caja)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand(
            "UPDATE Cajas SET SucursalId = @SucursalId, Nombre = @Nombre, Estado = @Estado WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", caja.Id);
        cmd.Parameters.AddWithValue("@SucursalId", caja.SucursalId);
        cmd.Parameters.AddWithValue("@Nombre", caja.Nombre);
        cmd.Parameters.AddWithValue("@Estado", caja.Estado);
        cmd.ExecuteNonQuery();
    }

    public void EliminarCaja(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("DELETE FROM Cajas WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }


    public List<Caja> LeerCajas()
    {
        var cajas = new List<Caja>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("SELECT Id, SucursalId, Nombre, Estado FROM Cajas", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            cajas.Add(new Caja
            {
                Id = reader.GetInt32(0),
                SucursalId = reader.GetInt32(1),
                Nombre = reader.GetString(2),
                Estado = reader.GetString(3)
            });
        }
        return cajas;
    }


    public List<Caja> GetBySucursalId(int sucursalId)
    {
        var cajas = new List<Caja>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("SELECT Id, SucursalId, Nombre, Estado FROM Cajas WHERE SucursalId = @SucursalId", conn);
        cmd.Parameters.AddWithValue("@SucursalId", sucursalId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            cajas.Add(new Caja
            {
                Id = reader.GetInt32(0),
                SucursalId = reader.GetInt32(1),
                Nombre = reader.GetString(2),
                Estado = reader.GetString(3)
            });
        }
        return cajas;
    }

    public Caja GetById(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("SELECT Id, SucursalId, Nombre, Estado FROM Cajas WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Caja
            {
                Id = reader.GetInt32(0),
                SucursalId = reader.GetInt32(1),
                Nombre = reader.GetString(2),
                Estado = reader.GetString(3)
            };
        }
        return null;
    }
}