// Models/Articulo.cs
namespace ProStocker.Web.Models
{
    public class Articulo
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string? Imagen { get; set; } // Nullable para coincidir con la base de datos
        public decimal Precio1 { get; set; }
        public decimal? Precio2 { get; set; } // Nullable
        public decimal? Precio3 { get; set; } // Nullable
        public decimal Costo { get; set; }
    }
}