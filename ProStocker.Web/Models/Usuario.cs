// Models/Usuario.cs
namespace ProStocker.Web.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string UsuarioNombre { get; set; } // Campo "Usuario" en la tabla
        public string Contrasena { get; set; }   // Campo "Contrasena" en la tabla
        public string Tipo { get; set; }         // "Admin" o "Usuario"
        public List<int> Sucursales { get; set; } = new List<int>(); // Sucursales asignadas
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