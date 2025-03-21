using ProStocker.Web.Models;

namespace ProStocker.DAL.Interfaces
{
    public interface ISucursalDAL
    {
        List<Sucursal> GetAll();
        Sucursal GetById(int id);
    }
}