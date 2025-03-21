// ICajaDAL.cs
using ProStocker.Web.Models;

namespace ProStocker.DAL.Interfaces
{
    public interface ICajaDAL
    {
        List<Caja> GetBySucursalId(int sucursalId);
        Caja GetById(int id);

        Task<List<Caja>> LeerCajasAsync();
        void CrearCaja(Caja caja);
        List<Caja> LeerCajas();

        void ActualizarCaja(Caja caja);
        void EliminarCaja(int id);

    }
}