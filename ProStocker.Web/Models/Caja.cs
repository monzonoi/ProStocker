// Models/Caja.cs
namespace ProStocker.Web.Models
{
    public class Caja
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
    }
}