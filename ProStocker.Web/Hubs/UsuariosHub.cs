using Microsoft.AspNetCore.SignalR;

namespace ProStocker.Web.Hubs
{
    public class UsuariosHub : Hub
    {
        // Método para enviar notificaciones a todos los clientes conectados
        public async Task SendNotification(string message, string type)
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }

        // Método para notificar actualización de la lista de usuarios
        public async Task UpdateUsuariosList()
        {
            await Clients.All.SendAsync("RefreshUsuariosList");
        }
    }
}