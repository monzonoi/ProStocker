// ITurnoCajaDAL.cs
using ProStocker.Web.Models;

namespace ProStocker.DAL.Interfaces
{
    public interface ITurnoCajaDAL
    {
        TurnoCaja GetTurnoActivoByCajaId(int cajaId);
    }
}