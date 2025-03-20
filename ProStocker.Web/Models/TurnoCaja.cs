// Models/TurnoCaja.cs
namespace ProStocker.Web.Models
{
    public class TurnoCaja
    {
        public int Id { get; set; }
        public int CajaId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal? MontoFinal { get; set; }
        public decimal? MontoReal { get; set; }
        public decimal? Diferencia { get; set; }
        public string Estado { get; set; }
    }
}