using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FutronicService.Hubs
{
    /// <summary>
    /// Hub de SignalR para notificaciones en tiempo real durante capturas de huellas
    /// </summary>
    public class FingerprintHub : Hub
    {
        /// <summary>
        /// Envía una notificación de progreso a todos los clientes conectados
        /// </summary>
        public async Task SendProgressToAll(string eventType, string message, object data = null)
        {
            await Clients.All.SendAsync("ReceiveProgress", new
            {
                eventType,
                message,
                data,
                timestamp = DateTime.Now.ToString("o")
            });
        }

        /// <summary>
        /// Envía una notificación de progreso a un cliente específico
        /// </summary>
        public async Task SendProgressToClient(string connectionId, string eventType, string message, object data = null)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveProgress", new
            {
                eventType,
                message,
                data,
                timestamp = DateTime.Now.ToString("o")
            });
        }

        /// <summary>
        /// Envía una notificación de progreso a un grupo específico (por DNI)
        /// </summary>
        public async Task SendProgressToGroup(string groupName, string eventType, string message, object data = null)
        {
            await Clients.Group(groupName).SendAsync("ReceiveProgress", new
            {
                eventType,
                message,
                data,
                timestamp = DateTime.Now.ToString("o")
            });
        }

        /// <summary>
        /// Suscribe al cliente a notificaciones de un DNI específico
        /// IMPORTANTE: Usa el DNI directamente como nombre del grupo (sin prefijo)
        /// </summary>
        public async Task SubscribeToDni(string dni)
        {
            // ? Usar DNI directamente como nombre del grupo (sin prefijo "dni_")
            await Groups.AddToGroupAsync(Context.ConnectionId, dni);
            Console.WriteLine($"? SignalR: Client {Context.ConnectionId} subscribed to DNI group: {dni}");
        }

        /// <summary>
        /// Desuscribe al cliente de notificaciones de un DNI específico
        /// </summary>
        public async Task UnsubscribeFromDni(string dni)
        {
            // ? Usar DNI directamente como nombre del grupo
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, dni);
            Console.WriteLine($"? SignalR: Client {Context.ConnectionId} unsubscribed from DNI group: {dni}");
        }

        /// <summary>
        /// Se ejecuta cuando un cliente se conecta
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"?? SignalR: Client connected - ConnectionId: {Context.ConnectionId}");
            
            await Clients.Caller.SendAsync("Connected", new
            {
                connectionId = Context.ConnectionId,
                message = "Connected to Futronic SignalR Hub",
                timestamp = DateTime.Now.ToString("o")
            });
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Se ejecuta cuando un cliente se desconecta
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"?? SignalR: Client disconnected with error - ConnectionId: {Context.ConnectionId}, Error: {exception.Message}");
            }
            else
            {
                Console.WriteLine($"?? SignalR: Client disconnected - ConnectionId: {Context.ConnectionId}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
