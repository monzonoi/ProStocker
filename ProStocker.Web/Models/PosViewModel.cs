namespace ProStocker.Web.Models
{
    public class PosViewModel
    {
        public int SucursalId { get; set; }
        public int CajaId { get; set; }
        public TurnoCaja TurnoActivo { get; set; }
    }
}