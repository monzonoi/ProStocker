// Models/Usuario.cs
namespace ProStocker.Web.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string UsuarioNombre { get; set; } // Renombramos "Usuario" a "UsuarioNombre" para evitar confusión
        public string Contrasena { get; set; } // Hash con BCrypt
        public string Tipo { get; set; } // "Admin" o "Vendedor"
        public bool Activo { get; set; } = true;
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