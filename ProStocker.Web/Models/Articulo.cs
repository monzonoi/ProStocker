// Models/Articulo.cs
namespace ProStocker.Web.Models
{
    public class Articulo
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio1 { get; set; }
        public decimal Costo { get; set; }
    }
}