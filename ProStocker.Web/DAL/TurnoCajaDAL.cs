// TurnoCajaDAL.cs
using ProStocker.DAL;
using ProStocker.DAL.Interfaces;
using ProStocker.Web.Models;
using System.Data.SQLite;

public class TurnoCajaDAL : BaseDataAccess, ITurnoCajaDAL
{
    public TurnoCajaDAL(string connectionString) : base(connectionString) { }

    public TurnoCaja GetTurnoActivoByCajaId(int cajaId)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SQLiteCommand("SELECT Id, CajaId, UsuarioId, FechaInicio, FechaFin, MontoInicial, MontoFinal, MontoReal, Diferencia, Estado FROM TurnosCaja WHERE CajaId = @CajaId AND Estado = 'Abierto' LIMIT 1", conn);
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
                FechaFin = reader.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(reader.GetString(4)),
                MontoInicial = (decimal)reader.GetDouble(5),
                MontoFinal = reader.IsDBNull(6) ? (decimal?)null : (decimal)reader.GetDouble(6),
                MontoReal = reader.IsDBNull(7) ? (decimal?)null : (decimal)reader.GetDouble(7),
                Diferencia = reader.IsDBNull(8) ? (decimal?)null : (decimal)reader.GetDouble(8),
                Estado = reader.GetString(9)
            };
        }
        return null;
    }
}