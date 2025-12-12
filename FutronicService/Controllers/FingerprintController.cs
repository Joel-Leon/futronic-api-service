using System;
using System.Threading.Tasks;
using FutronicService.Models;
using FutronicService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FutronicService.Controllers
{
    [ApiController]
    [Route("api/fingerprint")]
    public class FingerprintController : ControllerBase
 {
        private readonly IFingerprintService _fingerprintService;
  private readonly ILogger<FingerprintController> _logger;

        public FingerprintController(IFingerprintService fingerprintService, ILogger<FingerprintController> logger)
{
     _fingerprintService = fingerprintService;
   _logger = logger;
    }

   /// <summary>
   /// POST /api/fingerprint/identify
  /// Identifica huella entre múltiples templates (1:N)
        /// </summary>
    [HttpPost("identify")]
     public async Task<IActionResult> Identify([FromBody] IdentifyRequest request)
      {
     _logger.LogInformation($"Identify endpoint called with {request?.Templates?.Count ?? 0} templates");

  if (request == null)
      {
      return BadRequest(ApiResponse<object>.ErrorResponse("Request body es requerido", "INVALID_INPUT"));
   }

  var result = await _fingerprintService.IdentifyAsync(request);

   if (!result.Success)
    {
 return GetErrorStatusCode(result);
   }

return Ok(result);
      }

  /// <summary>
   /// POST /api/fingerprint/identify-live
     /// Identifica usuario capturando del dispositivo y buscando en directorio usando SDK
/// </summary>
        [HttpPost("identify-live")]
        public async Task<IActionResult> IdentifyLive([FromBody] IdentifyLiveRequest request)
     {
   _logger.LogInformation($"IdentifyLive endpoint called for directory: {request?.TemplatesDirectory}");

     if (request == null)
            {
      return BadRequest(ApiResponse<object>.ErrorResponse("Request body es requerido", "INVALID_INPUT"));
   }

            var result = await _fingerprintService.IdentifyLiveAsync(request);

if (!result.Success)
       {
      return GetErrorStatusCode(result);
    }

   return Ok(result);
}

   /// <summary>
  /// GET /api/fingerprint/config
 /// Obtiene configuración actual
 /// </summary>
  [HttpGet("config")]
   public IActionResult GetConfig()
        {
     _logger.LogInformation("GetConfig endpoint called");
 var result = _fingerprintService.GetConfig();
     return Ok(result);
     }

        /// <summary>
  /// POST /api/fingerprint/config
  /// Actualiza configuración en runtime
 /// </summary>
 [HttpPost("config")]
        public IActionResult UpdateConfig([FromBody] UpdateConfigRequest request)
   {
 _logger.LogInformation("UpdateConfig endpoint called");

        if (request == null)
      {
 return BadRequest(ApiResponse<object>.ErrorResponse("Request body es requerido", "INVALID_INPUT"));
       }

 var result = _fingerprintService.UpdateConfig(request);

      if (!result.Success)
      {
    return BadRequest(result);
     }

   return Ok(result);
    }

        /// <summary>
     /// POST /api/fingerprint/verify-simple
     /// Verifica identidad capturando huella automáticamente usando SDK de Futronic
        /// </summary>
    [HttpPost("verify-simple")]
public async Task<IActionResult> VerifySimple([FromBody] VerifySimpleRequest request)
        {
            _logger.LogInformation($"VerifySimple endpoint called for DNI: {request?.Dni}");

  if (request == null)
   {
                return BadRequest(ApiResponse<object>.ErrorResponse("Request body es requerido", "INVALID_INPUT"));
            }

        var result = await _fingerprintService.VerifySimpleAsync(request);

      if (!result.Success)
     {
 return GetErrorStatusCode(result);
       }

       return Ok(result);
     }

    /// <summary>
        /// POST /api/fingerprint/register-multi
/// Registra huella con múltiples muestras (5 por defecto) - RECOMENDADO
        /// </summary>
        [HttpPost("register-multi")]
        public async Task<IActionResult> RegisterMultiSample([FromBody] RegisterMultiSampleRequest request)
        {
     _logger.LogInformation($"RegisterMultiSample endpoint called for DNI: {request?.Dni}, Samples: {request?.SampleCount ?? 5}");

        if (request == null)
  {
       return BadRequest(ApiResponse<object>.ErrorResponse("Request body es requerido", "INVALID_INPUT"));
   }

    var result = await _fingerprintService.RegisterMultiSampleAsync(request);

       if (!result.Success)
  {
    return GetErrorStatusCode(result);
  }

     return Ok(result);
        }

        /// <summary>
        /// POST /api/fingerprint/capture
        /// Captura una huella temporal sin asociarla a DNI (para testing)
   /// </summary>
   [HttpPost("capture")]
        public async Task<IActionResult> Capture([FromBody] CaptureRequest request)
        {
            _logger.LogInformation("Capture endpoint called");

            if (request == null)
            {
                request = new CaptureRequest(); // Usar valores por defecto
  }

    var result = await _fingerprintService.CaptureAsync(request);

   if (!result.Success)
  {
    return GetErrorStatusCode(result);
    }

            return Ok(result);
     }

        /// <summary>
        /// POST /api/fingerprint/test-signalr
        /// Endpoint de prueba para verificar que SignalR funciona correctamente
        /// </summary>
        [HttpPost("test-signalr")]
        public IActionResult TestSignalR([FromBody] TestSignalRRequest request)
        {
            _logger.LogInformation($"Test SignalR endpoint called for DNI: {request?.Dni}");

            if (request == null || string.IsNullOrEmpty(request.Dni))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "DNI es requerido en el body: { \"dni\": \"12345678\" }",
                    "INVALID_INPUT"
                ));
            }

            try
            {
                // Inyectar el servicio de notificaciones
                var progressService = HttpContext.RequestServices.GetService(typeof(IProgressNotificationService)) as IProgressNotificationService;

                if (progressService == null)
                {
                    return StatusCode(500, ApiResponse<object>.ErrorResponse(
                        "IProgressNotificationService no está disponible",
                        "SERVICE_ERROR"
                    ));
                }

                // Enviar notificación de prueba
                var testData = new
                {
                    test = true,
                    message = "Esta es una notificación de prueba de SignalR",
                    timestamp = DateTime.UtcNow,
                    sampleNumber = 1,
                    quality = 95.5,
                    imageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==" // 1x1 pixel transparente
                };

                progressService.NotifyAsync(
                    eventType: "test",
                    message: $"?? Test de SignalR para DNI: {request.Dni}",
                    data: testData,
                    dni: request.Dni
                ).Wait();

                _logger.LogInformation($"? Test SignalR notification sent to DNI group: {request.Dni}");

                return Ok(ApiResponse<object>.SuccessResponse(
                    $"Notificación de prueba enviada al grupo DNI: {request.Dni}",
                    new
                    {
                        dni = request.Dni,
                        eventType = "test",
                        sentAt = DateTime.UtcNow,
                        instructions = new[]
                        {
                            "1. Conecta tu frontend a SignalR: http://localhost:5000/hubs/fingerprint",
                            "2. Suscríbete al DNI usando: connection.invoke('SubscribeToDni', '" + request.Dni + "')",
                            "3. Escucha eventos con: connection.on('ReceiveProgress', callback)",
                            "4. Deberías recibir esta notificación de prueba"
                        }
                    }
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificación de prueba");
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    $"Error enviando notificación: {ex.Message}",
                    "TEST_ERROR"
                ));
            }
        }

  private IActionResult GetErrorStatusCode<T>(ApiResponse<T> response)
        {
            if (response.Error == null)
   {
 return StatusCode(500, response);
     }

         switch (response.Error)
{
  case "DEVICE_NOT_CONNECTED":
      return StatusCode(503, response);
    case "CAPTURE_TIMEOUT":
       return StatusCode(408, response);
    case "FILE_NOT_FOUND":
      return NotFound(response);
 case "INVALID_INPUT":
  case "INVALID_TEMPLATE":
    return BadRequest(response);
     default:
  return StatusCode(500, response);
      }
     }
    }

// Modelo para el request de test
public class TestSignalRRequest
{
    public string Dni { get; set; }
}
}
