using System;
using System.Collections.Generic;
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
        private readonly IConfigurationService _configService;
  private readonly ILogger<FingerprintController> _logger;

        public FingerprintController(
            IFingerprintService fingerprintService,
            IConfigurationService configService,
            ILogger<FingerprintController> logger)
{
     _fingerprintService = fingerprintService;
     _configService = configService;
   _logger = logger;
    }

        // ============================================
        // CONFIGURACIÓN (Single Source of Truth)
        // ============================================

        /// <summary>
        /// GET /api/fingerprint/config
        /// Obtiene la configuración actual del servicio
        /// </summary>
        [HttpGet("config")]
        public ActionResult<ApiResponse<FingerprintConfiguration>> GetConfiguration()
        {
            try
            {
                _logger.LogInformation("?? GET /api/fingerprint/config");
                var config = _configService.GetConfiguration();
                return Ok(ApiResponse<FingerprintConfiguration>.SuccessResponse(
                    "Configuración obtenida exitosamente", config));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener configuración");
                return StatusCode(500, ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    ex.Message, "GET_CONFIG_ERROR"));
            }
        }

        /// <summary>
        /// PUT /api/fingerprint/config
        /// Actualiza la configuración completa (FUENTE DE VERDAD)
        /// </summary>
        [HttpPut("config")]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> UpdateConfiguration(
            [FromBody] FingerprintConfiguration config)
        {
            try
            {
                _logger.LogInformation("?? PUT /api/fingerprint/config");
                var success = await _configService.UpdateConfigurationAsync(config);
                
                if (success)
                {
                    // ? IMPORTANTE: Recargar configuración en el servicio de huellas
                    _fingerprintService.ReloadConfiguration();
                    
                    var updatedConfig = _configService.GetConfiguration();
                    
                    _logger.LogInformation("? Configuración actualizada y recargada en todos los servicios");
                    
                    return Ok(ApiResponse<FingerprintConfiguration>.SuccessResponse(
                        "Configuración actualizada exitosamente", updatedConfig));
                }
                
                return BadRequest(ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    "Error al actualizar configuración", "UPDATE_CONFIG_FAILED"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar configuración");
                return StatusCode(500, ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    ex.Message, "UPDATE_CONFIG_ERROR"));
            }
        }

        /// <summary>
        /// PATCH /api/fingerprint/config
        /// Actualiza campos específicos de la configuración
        /// </summary>
        [HttpPatch("config")]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> PatchConfiguration(
            [FromBody] UpdateConfigRequest request)
        {
            try
            {
                _logger.LogInformation("?? PATCH /api/fingerprint/config");
                
                var updates = new Dictionary<string, object>();
                if (request.Threshold.HasValue) updates["Threshold"] = request.Threshold.Value;
                if (request.Timeout.HasValue) updates["Timeout"] = request.Timeout.Value;
                if (request.MaxRotation.HasValue) updates["MaxRotation"] = request.MaxRotation.Value;
                if (!string.IsNullOrEmpty(request.TempPath)) updates["TemplatePath"] = request.TempPath;
                if (request.OverwriteExisting.HasValue) updates["OverwriteExisting"] = request.OverwriteExisting.Value;

                var success = await _configService.UpdatePartialConfigurationAsync(updates);
                
                if (success)
                {
                    // ? IMPORTANTE: Recargar configuración en el servicio de huellas
                    _fingerprintService.ReloadConfiguration();
                    
                    var updatedConfig = _configService.GetConfiguration();
                    
                    _logger.LogInformation($"? Configuración actualizada ({updates.Count} campos) y recargada");
                    
                    return Ok(ApiResponse<FingerprintConfiguration>.SuccessResponse(
                        $"Configuración actualizada ({updates.Count} campos)", updatedConfig));
                }
                
                return BadRequest(ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    "Error en actualización parcial", "PATCH_CONFIG_FAILED"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en actualización parcial");
                return StatusCode(500, ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    ex.Message, "PATCH_CONFIG_ERROR"));
            }
        }

        /// <summary>
        /// POST /api/fingerprint/config/validate
        /// Valida una configuración sin guardarla
        /// </summary>
        [HttpPost("config/validate")]
        public ActionResult<ApiResponse<ConfigurationValidationResult>> ValidateConfiguration(
            [FromBody] FingerprintConfiguration config)
        {
            try
            {
                _logger.LogInformation("?? POST /api/fingerprint/config/validate");
                var validationResult = _configService.ValidateConfiguration(config);
                return Ok(ApiResponse<ConfigurationValidationResult>.SuccessResponse(
                    validationResult.IsValid ? "Configuración válida" : "Configuración inválida",
                    validationResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar configuración");
                return StatusCode(500, ApiResponse<ConfigurationValidationResult>.ErrorResponse(
                    ex.Message, "VALIDATION_ERROR"));
            }
        }

        /// <summary>
        /// POST /api/fingerprint/config/reset
        /// Restaura la configuración a valores por defecto
        /// </summary>
        [HttpPost("config/reset")]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> ResetConfiguration()
        {
            try
            {
                _logger.LogWarning("?? POST /api/fingerprint/config/reset");
                var success = await _configService.ResetToDefaultAsync();
                
                if (success)
                {
                    // ? IMPORTANTE: Recargar configuración en el servicio de huellas
                    _fingerprintService.ReloadConfiguration();
                    
                    var defaultConfig = _configService.GetConfiguration();
                    
                    _logger.LogInformation("? Configuración restaurada y recargada");
                    
                    return Ok(ApiResponse<FingerprintConfiguration>.SuccessResponse(
                        "Configuración restaurada a valores por defecto", defaultConfig));
                }
                
                return StatusCode(500, ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    "Error al restaurar configuración", "RESET_CONFIG_FAILED"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear configuración");
                return StatusCode(500, ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    ex.Message, "RESET_CONFIG_ERROR"));
            }
        }

        /// <summary>
        /// POST /api/fingerprint/config/reload
        /// Recarga la configuración desde el archivo
        /// </summary>
        [HttpPost("config/reload")]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> ReloadConfiguration()
        {
            try
            {
                _logger.LogInformation("?? POST /api/fingerprint/config/reload");
                await _configService.ReloadConfigurationAsync();
                
                // ? IMPORTANTE: Recargar configuración en el servicio de huellas
                _fingerprintService.ReloadConfiguration();
                
                var config = _configService.GetConfiguration();
                
                _logger.LogInformation("? Configuración recargada desde archivo y aplicada");
                
                return Ok(ApiResponse<FingerprintConfiguration>.SuccessResponse(
                    "Configuración recargada desde archivo", config));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recargar configuración");
                return StatusCode(500, ApiResponse<FingerprintConfiguration>.ErrorResponse(
                    ex.Message, "RELOAD_CONFIG_ERROR"));
            }
        }

        // ============================================
        // OPERACIONES DE HUELLA DACTILAR
        // ============================================

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
