using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using FutronicService.Hubs;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FutronicService.Services
{
    /// <summary>
    /// Servicio para enviar notificaciones de progreso durante capturas de huellas
    /// Soporta SignalR (WebSocket) y HTTP callbacks
    /// </summary>
    public interface IProgressNotificationService
    {
        Task NotifyAsync(string eventType, string message, object data = null, string dni = null, string callbackUrl = null);
        Task NotifyStartAsync(string dni, string operation, string callbackUrl = null);
        Task NotifySampleStartedAsync(string dni, int currentSample, int totalSamples, string callbackUrl = null);
        Task NotifySampleCapturedAsync(string dni, int currentSample, int totalSamples, double quality, byte[] imageData = null, string callbackUrl = null);
        Task NotifyCompleteAsync(string dni, bool success, string message, object data = null, string callbackUrl = null);
        Task NotifyErrorAsync(string dni, string error, string callbackUrl = null);
    }

    public class ProgressNotificationService : IProgressNotificationService
    {
        private readonly IHubContext<FingerprintHub> _hubContext;
        private readonly ILogger<ProgressNotificationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProgressNotificationService(
            IHubContext<FingerprintHub> hubContext,
            ILogger<ProgressNotificationService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _hubContext = hubContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Envía una notificación genérica
        /// </summary>
        public async Task NotifyAsync(string eventType, string message, object data = null, string dni = null, string callbackUrl = null)
        {
            var payload = new
            {
                eventType,
                message,
                data,
                dni,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            // Enviar por SignalR
            try
            {
                if (!string.IsNullOrEmpty(dni))
                {
                    // ? Usar DNI directamente como nombre del grupo (sin prefijo)
                    await _hubContext.Clients.Group(dni).SendAsync("ReceiveProgress", payload);
                    _logger.LogInformation($"?? SignalR notification sent to DNI group '{dni}': {eventType} - {message}");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveProgress", payload);
                    _logger.LogDebug($"SignalR notification sent to all clients: {eventType} - {message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send SignalR notification");
            }

            // Enviar por HTTP callback si se proporcionó URL
            if (!string.IsNullOrEmpty(callbackUrl))
            {
                await SendHttpCallbackAsync(callbackUrl, payload);
            }
        }

        /// <summary>
        /// Notifica el inicio de una operación
        /// </summary>
        public async Task NotifyStartAsync(string dni, string operation, string callbackUrl = null)
        {
            await NotifyAsync(
                eventType: "operation_started",
                message: $"Iniciando {operation}",
                data: new { operation },
                dni: dni,
                callbackUrl: callbackUrl
            );
        }

        /// <summary>
        /// Notifica el inicio de captura de una muestra
        /// </summary>
        public async Task NotifySampleStartedAsync(string dni, int currentSample, int totalSamples, string callbackUrl = null)
        {
            var progress = (currentSample * 100) / totalSamples;
            
            await NotifyAsync(
                eventType: "sample_started",
                message: $"Capturando muestra {currentSample}/{totalSamples}",
                data: new
                {
                    currentSample,
                    totalSamples,
                    progress
                },
                dni: dni,
                callbackUrl: callbackUrl
            );
        }

        /// <summary>
        /// Notifica la captura de una muestra con imagen en Base64
        /// </summary>
        public async Task NotifySampleCapturedAsync(string dni, int currentSample, int totalSamples, double quality, byte[] imageData = null, string callbackUrl = null)
        {
            var progress = (currentSample * 100) / totalSamples;
            
            var dataObj = new
            {
                currentSample,
                totalSamples,
                quality,
                progress,
                imageBase64 = imageData != null ? Convert.ToBase64String(imageData) : null,
                imageFormat = imageData != null ? "bmp" : null
            };

            await NotifyAsync(
                eventType: "sample_captured",
                message: $"Muestra {currentSample}/{totalSamples} capturada - Calidad: {quality:F2}",
                data: dataObj,
                dni: dni,
                callbackUrl: callbackUrl
            );
        }

        /// <summary>
        /// Notifica la finalización de una operación
        /// </summary>
        public async Task NotifyCompleteAsync(string dni, bool success, string message, object data = null, string callbackUrl = null)
        {
            await NotifyAsync(
                eventType: success ? "operation_completed" : "operation_failed",
                message: message,
                data: data,
                dni: dni,
                callbackUrl: callbackUrl
            );
        }

        /// <summary>
        /// Notifica un error
        /// </summary>
        public async Task NotifyErrorAsync(string dni, string error, string callbackUrl = null)
        {
            await NotifyAsync(
                eventType: "error",
                message: error,
                dni: dni,
                callbackUrl: callbackUrl
            );
        }

        /// <summary>
        /// Envía un callback HTTP a una URL externa
        /// </summary>
        private async Task SendHttpCallbackAsync(string callbackUrl, object payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var content = new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(callbackUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"HTTP callback failed: {response.StatusCode} - {callbackUrl}");
                }
                else
                {
                    _logger.LogDebug($"HTTP callback sent successfully to {callbackUrl}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error sending HTTP callback to {callbackUrl}");
            }
        }
    }
}
