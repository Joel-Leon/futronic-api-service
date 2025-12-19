using FutronicService.Models;
using FutronicService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FutronicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ILogger<ConfigurationController> _logger;
        private readonly IConfigurationService _configService;

        public ConfigurationController(
            ILogger<ConfigurationController> logger,
            IConfigurationService configService)
        {
            _logger = logger;
            _configService = configService;
        }

        /// <summary>
        /// Obtener configuración actual
        /// GET /api/configuration
        /// </summary>
        [HttpGet]
        public ActionResult<ApiResponse<FingerprintConfiguration>> GetConfiguration()
        {
            try
            {
                var config = _configService.GetConfiguration();
                
                return Ok(new ApiResponse<FingerprintConfiguration>
                {
                    Success = true,
                    Message = "Configuración obtenida correctamente",
                    Data = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener configuración");
                return StatusCode(500, new ApiResponse<FingerprintConfiguration>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Actualizar configuración completa
        /// PUT /api/configuration
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> UpdateConfiguration(
            [FromBody] FingerprintConfiguration config)
        {
            try
            {
                // Validar configuración
                var validation = _configService.ValidateConfiguration(config);
                
                if (!validation.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Configuración inválida",
                        Data = new { Errors = validation.Errors, Warnings = validation.Warnings }
                    });
                }

                var success = await _configService.UpdateConfigurationAsync(config);

                if (success)
                {
                    return Ok(new ApiResponse<FingerprintConfiguration>
                    {
                        Success = true,
                        Message = "? Configuración actualizada correctamente",
                        Data = config
                    });
                }

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error al guardar configuración"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar configuración");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Actualizar valores específicos de configuración (PATCH)
        /// PATCH /api/configuration
        /// </summary>
        [HttpPatch]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> UpdatePartialConfiguration(
            [FromBody] Dictionary<string, object> updates)
        {
            try
            {
                var success = await _configService.UpdatePartialConfigurationAsync(updates);

                if (success)
                {
                    var updatedConfig = _configService.GetConfiguration();
                    
                    return Ok(new ApiResponse<FingerprintConfiguration>
                    {
                        Success = true,
                        Message = $"? {updates.Count} valores actualizados correctamente",
                        Data = updatedConfig
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error al actualizar configuración"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar configuración parcial");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Validar configuración sin guardar
        /// POST /api/configuration/validate
        /// </summary>
        [HttpPost("validate")]
        public ActionResult<ApiResponse<ConfigurationValidationResult>> ValidateConfiguration(
            [FromBody] FingerprintConfiguration config)
        {
            try
            {
                var validation = _configService.ValidateConfiguration(config);

                return Ok(new ApiResponse<ConfigurationValidationResult>
                {
                    Success = validation.IsValid,
                    Message = validation.IsValid ? "? Configuración válida" : "?? Configuración inválida",
                    Data = validation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar configuración");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Recargar configuración desde archivo
        /// POST /api/configuration/reload
        /// </summary>
        [HttpPost("reload")]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> ReloadConfiguration()
        {
            try
            {
                await _configService.ReloadConfigurationAsync();
                var config = _configService.GetConfiguration();

                return Ok(new ApiResponse<FingerprintConfiguration>
                {
                    Success = true,
                    Message = "?? Configuración recargada correctamente",
                    Data = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recargar configuración");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Restaurar configuración por defecto
        /// POST /api/configuration/reset
        /// </summary>
        [HttpPost("reset")]
        public async Task<ActionResult<ApiResponse<FingerprintConfiguration>>> ResetConfiguration()
        {
            try
            {
                var success = await _configService.ResetToDefaultAsync();

                if (success)
                {
                    var config = _configService.GetConfiguration();
                    
                    return Ok(new ApiResponse<FingerprintConfiguration>
                    {
                        Success = true,
                        Message = "?? Configuración restaurada a valores por defecto",
                        Data = config
                    });
                }

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error al restaurar configuración"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restaurar configuración");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtener schema de configuración con descripciones
        /// GET /api/configuration/schema
        /// </summary>
        [HttpGet("schema")]
        public ActionResult<ApiResponse<object>> GetConfigurationSchema()
        {
            var schema = new
            {
                properties = new Dictionary<string, object>
                {
                    ["threshold"] = new { type = "integer", min = 0, max = 100, default_ = 70, description = "Umbral de coincidencia (más alto = más estricto)" },
                    ["timeout"] = new { type = "integer", min = 5000, max = 60000, default_ = 30000, description = "Timeout en ms para captura" },
                    ["captureMode"] = new { type = "string", enum_ = new[] { "screen", "file" }, default_ = "screen", description = "Modo de captura" },
                    ["showImage"] = new { type = "boolean", default_ = true, description = "Mostrar imagen durante captura" },
                    ["saveImage"] = new { type = "boolean", default_ = false, description = "Guardar imagen como archivo" },
                    ["detectFakeFinger"] = new { type = "boolean", default_ = false, description = "Detectar dedos falsos" },
                    ["maxFramesInTemplate"] = new { type = "integer", min = 1, max = 10, default_ = 5, description = "Frames máximos en template" },
                    ["disableMIDT"] = new { type = "boolean", default_ = false, description = "Deshabilitar detección de movimiento incremental" },
                    ["maxRotation"] = new { type = "integer", min = 0, max = 199, default_ = 199, description = "Rotación máxima permitida (166=tolerante, 199=estricto)" },
                    ["minQuality"] = new { type = "integer", min = 0, max = 100, default_ = 50, description = "Calidad mínima aceptable" }
                }
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Schema de configuración",
                Data = schema
            });
        }
    }
}
