using ProStocker.DAL;
using ProStocker.DAL.Interfaces;
using ProStocker.Web.Models;
using System.Data.SQLite;

public class SucursalDAL : BaseDataAccess, ISucursalDAL
{
    public SucursalDAL(string connectionString) : base(connectionString) { }

    public List<Sucursal> GetAll()
    {
        var sucursales = new List<Sucursal>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("SELECT Id, Nombre, Direccion FROM Sucursales", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sucursales.Add(new Sucursal
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Direccion = reader.GetString(2)
            });
        }
        return sucursales;
    }


    public Sucursal GetById(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("SELECT Id, Nombre, Direccion FROM Sucursales WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Sucursal
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Direccion = reader.GetString(2)
            };
        }
        return null;
    }
}