// Models/Usuario.cs
using System.ComponentModel.DataAnnotations;

namespace ProStocker.Web.Models
{
    public class TipoUsuario
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }

    public class Usuario
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        [Required]
        public string UsuarioNombre { get; set; }
        public string Contrasena { get; set; }
        [Required]
        public int TipoId { get; set; } // Ahora es un ID
        public List<int> Sucursales { get; set; } = new List<int>();
    }

}

// Models/LoginViewModel.cs
namespace ProStocker.Web.Models
{
    public class LoginViewModel
    {
        public string UsuarioNombre { get; set; }
        public string Contrasena { get; set; }
    }
}