# ?? Integración Backend con Futronic Service

## ?? Índice
1. [Arquitectura General](#arquitectura-general)
2. [Configuración](#configuración)
3. [Implementación](#implementación)
4. [Endpoints Disponibles](#endpoints-disponibles)
5. [Ejemplos de Uso](#ejemplos-de-uso)
6. [Manejo de Errores](#manejo-de-errores)
7. [Mejores Prácticas](#mejores-prácticas)

---

## ??? Arquitectura General

### **Fuente de Verdad (Single Source of Truth)**

```
???????????????????????????????????????????????????????????????
?                    FLUJO DE CONFIGURACIÓN                    ?
???????????????????????????????????????????????????????????????

Frontend (Admin Panel)
    ?
    ? 1. Usuario modifica configuración
    ?
Backend API
    ?
    ? 2. Valida datos (opcional)
    ?
Futronic Service API
PUT /api/fingerprint/config
    ?
    ? 3. Valida y persiste en fingerprint-config.json
    ?
Response con configuración actualizada
    ?
    ?
Backend API
    ?
    ? 4. Guarda COPIA en BD (auditoría/respaldo)
    ?
Response a Frontend
```

### **Principios Fundamentales**

? **DO (Hacer):**
- Futronic Service es la **única fuente de verdad**
- Backend **siempre** actualiza primero el Futronic Service
- BD del backend solo guarda **copia de respaldo**
- Usar endpoints de Futronic Service para leer configuración actual

? **DON'T (No hacer):**
- Modificar BD y esperar sincronización automática
- Usar configuración de BD como fuente de verdad
- Sobrescribir configuración sin validar primero

---

## ?? Configuración

### **1. Configurar URL del Servicio**

#### `appsettings.json`
```json
{
  "FutronicService": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": 30,
    "RetryCount": 3,
    "RetryDelayMs": 1000
  }
}
```

#### `appsettings.Production.json`
```json
{
  "FutronicService": {
    "BaseUrl": "http://your-futronic-server:5000",
    "Timeout": 30
  }
}
```

### **2. Registrar HttpClient en `Program.cs` (.NET 8)**

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Registrar HttpClient para Futronic Service
builder.Services.AddHttpClient("FutronicService", client =>
{
    var baseUrl = builder.Configuration["FutronicService:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("FutronicService:Timeout", 30)
    );
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Registrar servicio de sincronización
builder.Services.AddScoped<IFingerprintConfigSyncService, FingerprintConfigSyncService>();

var app = builder.Build();
app.Run();
```

---

## ?? Implementación

### **Servicio de Sincronización**

Crea `Services/FingerprintConfigSyncService.cs`:

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YourBackend.Models;

namespace YourBackend.Services
{
    public interface IFingerprintConfigSyncService
    {
        Task<(bool success, FingerprintConfiguration config)> UpdateConfigurationAsync(FingerprintConfiguration newConfig);
        Task<FingerprintConfiguration> GetConfigurationAsync();
        Task<ConfigurationValidationResult> ValidateConfigurationAsync(FingerprintConfiguration config);
        Task<(bool success, FingerprintConfiguration config)> UpdatePartialConfigurationAsync(Dictionary<string, object> updates);
        Task<(bool success, FingerprintConfiguration config)> ResetConfigurationAsync();
    }

    public class FingerprintConfigSyncService : IFingerprintConfigSyncService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FingerprintConfigSyncService> _logger;
        private readonly YourDbContext _dbContext; // Tu DbContext

        public FingerprintConfigSyncService(
            IHttpClientFactory httpClientFactory,
            ILogger<FingerprintConfigSyncService> logger,
            YourDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Actualiza la configuración en Futronic Service (fuente de verdad)
        /// y luego guarda una copia en la BD local
        /// </summary>
        public async Task<(bool success, FingerprintConfiguration config)> UpdateConfigurationAsync(
            FingerprintConfiguration newConfig)
        {
            try
            {
                _logger.LogInformation("?? Sincronizando configuración con Futronic Service...");
                
                var client = _httpClientFactory.CreateClient("FutronicService");
                
                // 1?? PRIMERO: Actualizar en Futronic Service (fuente de verdad)
                var response = await client.PutAsJsonAsync("api/fingerprint/config", newConfig);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"? Error al actualizar Futronic Service: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Response: {errorContent}");
                    return (false, null);
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FingerprintConfiguration>>();
                
                if (apiResponse?.Success == true)
                {
                    _logger.LogInformation("? Configuración actualizada en Futronic Service");
                    
                    // 2?? SEGUNDO: Guardar copia en BD local (para auditoría)
                    await SaveConfigurationCopyToDatabase(apiResponse.Data);
                    
                    return (true, apiResponse.Data);
                }
                
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "? Error de comunicación con Futronic Service");
                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error inesperado al actualizar configuración");
                return (false, null);
            }
        }

        /// <summary>
        /// Obtiene la configuración actual desde Futronic Service
        /// </summary>
        public async Task<FingerprintConfiguration> GetConfigurationAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("FutronicService");
                
                var response = await client.GetFromJsonAsync<ApiResponse<FingerprintConfiguration>>(
                    "api/fingerprint/config");

                if (response?.Success == true)
                {
                    _logger.LogInformation("? Configuración obtenida desde Futronic Service");
                    
                    // Actualizar copia en BD
                    await SaveConfigurationCopyToDatabase(response.Data);
                    return response.Data;
                }
                
                // Fallback: si el servicio no responde, usar copia de BD
                _logger.LogWarning("?? Futronic Service no disponible, usando copia de BD");
                return await GetConfigurationFromDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al obtener configuración");
                
                // Fallback a BD
                return await GetConfigurationFromDatabase();
            }
        }

        /// <summary>
        /// Valida configuración ANTES de guardar
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(
            FingerprintConfiguration config)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("FutronicService");
                
                var response = await client.PostAsJsonAsync(
                    "api/fingerprint/config/validate", config);

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ConfigurationValidationResult>>();
                
                if (apiResponse?.Success == true)
                {
                    return apiResponse.Data;
                }
                
                return new ConfigurationValidationResult 
                { 
                    IsValid = false, 
                    Errors = new List<string> { "Error al validar configuración" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al validar configuración");
                return new ConfigurationValidationResult 
                { 
                    IsValid = false, 
                    Errors = new List<string> { $"Error de comunicación: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Actualiza campos específicos de la configuración (PATCH)
        /// </summary>
        public async Task<(bool success, FingerprintConfiguration config)> UpdatePartialConfigurationAsync(
            Dictionary<string, object> updates)
        {
            try
            {
                _logger.LogInformation($"?? Actualizando {updates.Count} campos en Futronic Service...");
                
                var client = _httpClientFactory.CreateClient("FutronicService");
                
                // Crear request con los campos a actualizar
                var updateRequest = new
                {
                    Threshold = updates.ContainsKey("Threshold") ? (int?)updates["Threshold"] : null,
                    Timeout = updates.ContainsKey("Timeout") ? (int?)updates["Timeout"] : null,
                    MaxRotation = updates.ContainsKey("MaxRotation") ? (int?)updates["MaxRotation"] : null,
                    TempPath = updates.ContainsKey("TemplatePath") ? updates["TemplatePath"]?.ToString() : null,
                    OverwriteExisting = updates.ContainsKey("OverwriteExisting") ? (bool?)updates["OverwriteExisting"] : null
                };
                
                var response = await client.PatchAsJsonAsync("api/fingerprint/config", updateRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"? Error en PATCH: {response.StatusCode}");
                    return (false, null);
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FingerprintConfiguration>>();
                
                if (apiResponse?.Success == true)
                {
                    _logger.LogInformation("? Actualización parcial exitosa");
                    await SaveConfigurationCopyToDatabase(apiResponse.Data);
                    return (true, apiResponse.Data);
                }
                
                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error en actualización parcial");
                return (false, null);
            }
        }

        /// <summary>
        /// Restaura la configuración a valores por defecto
        /// </summary>
        public async Task<(bool success, FingerprintConfiguration config)> ResetConfigurationAsync()
        {
            try
            {
                _logger.LogWarning("?? Restaurando configuración a valores por defecto...");
                
                var client = _httpClientFactory.CreateClient("FutronicService");
                
                var response = await client.PostAsync("api/fingerprint/config/reset", null);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"? Error al resetear: {response.StatusCode}");
                    return (false, null);
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FingerprintConfiguration>>();
                
                if (apiResponse?.Success == true)
                {
                    _logger.LogInformation("? Configuración restaurada a valores por defecto");
                    await SaveConfigurationCopyToDatabase(apiResponse.Data);
                    return (true, apiResponse.Data);
                }
                
                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al resetear configuración");
                return (false, null);
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS PARA MANEJO DE BD
        // ============================================

        private async Task SaveConfigurationCopyToDatabase(FingerprintConfiguration config)
        {
            try
            {
                // Buscar configuración existente
                var existingConfig = await _dbContext.FingerprintConfigurations
                    .FirstOrDefaultAsync();

                if (existingConfig != null)
                {
                    // Actualizar configuración existente
                    existingConfig.Threshold = config.Threshold;
                    existingConfig.Timeout = config.Timeout;
                    existingConfig.MaxRotation = config.MaxRotation;
                    existingConfig.DetectFakeFinger = config.DetectFakeFinger;
                    existingConfig.TemplatePath = config.TemplatePath;
                    existingConfig.OverwriteExisting = config.OverwriteExisting;
                    existingConfig.LastUpdated = DateTime.UtcNow;
                    existingConfig.LastSyncedAt = DateTime.UtcNow;
                    
                    _dbContext.FingerprintConfigurations.Update(existingConfig);
                }
                else
                {
                    // Crear nueva configuración
                    var newConfig = new FingerprintConfigurationEntity
                    {
                        Threshold = config.Threshold,
                        Timeout = config.Timeout,
                        MaxRotation = config.MaxRotation,
                        DetectFakeFinger = config.DetectFakeFinger,
                        TemplatePath = config.TemplatePath,
                        OverwriteExisting = config.OverwriteExisting,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow,
                        LastSyncedAt = DateTime.UtcNow
                    };
                    
                    await _dbContext.FingerprintConfigurations.AddAsync(newConfig);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("?? Copia de configuración guardada en BD");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error al guardar copia en BD (no crítico)");
                // No lanzar excepción - esto es solo una copia de respaldo
            }
        }

        private async Task<FingerprintConfiguration> GetConfigurationFromDatabase()
        {
            try
            {
                var configEntity = await _dbContext.FingerprintConfigurations
                    .FirstOrDefaultAsync();

                if (configEntity != null)
                {
                    return new FingerprintConfiguration
                    {
                        Threshold = configEntity.Threshold,
                        Timeout = configEntity.Timeout,
                        MaxRotation = configEntity.MaxRotation,
                        DetectFakeFinger = configEntity.DetectFakeFinger,
                        TemplatePath = configEntity.TemplatePath,
                        OverwriteExisting = configEntity.OverwriteExisting
                    };
                }
                
                // Si no hay configuración en BD, retornar valores por defecto
                _logger.LogWarning("?? No hay configuración en BD, usando valores por defecto");
                return new FingerprintConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al leer configuración de BD");
                return new FingerprintConfiguration(); // Valores por defecto
            }
        }
    }

    // ============================================
    // MODELOS
    // ============================================

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string ErrorCode { get; set; }
    }
}
```

### **Entidad de Base de Datos**

```csharp
// Models/FingerprintConfigurationEntity.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourBackend.Models
{
    [Table("FingerprintConfigurations")]
    public class FingerprintConfigurationEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(0, 100)]
        public int Threshold { get; set; } = 70;

        [Required]
        [Range(5000, 60000)]
        public int Timeout { get; set; } = 30000;

        [Required]
        [Range(0, 199)]
        public int MaxRotation { get; set; } = 199;

        public bool DetectFakeFinger { get; set; } = false;

        [Required]
        [MaxLength(500)]
        public string TemplatePath { get; set; } = "C:/temp/fingerprints";

        public bool OverwriteExisting { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        [Required]
        public DateTime LastSyncedAt { get; set; }
    }
}
```

---

## ?? Endpoints Disponibles

### **Base URL:** `http://localhost:5000/api/fingerprint`

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/config` | Obtener configuración actual |
| `PUT` | `/config` | Actualizar configuración completa |
| `PATCH` | `/config` | Actualizar campos específicos |
| `POST` | `/config/validate` | Validar configuración sin guardar |
| `POST` | `/config/reset` | Restaurar valores por defecto |
| `POST` | `/config/reload` | Recargar desde archivo |

---

## ?? Ejemplos de Uso

### **1. Obtener Configuración Actual**

```csharp
// En tu controller
[HttpGet("fingerprint/config")]
public async Task<IActionResult> GetFingerprintConfig()
{
    var config = await _fingerprintConfigSyncService.GetConfigurationAsync();
    
    if (config == null)
    {
        return StatusCode(503, new { error = "Futronic Service no disponible" });
    }
    
    return Ok(config);
}
```

### **2. Actualizar Configuración Completa**

```csharp
[HttpPut("fingerprint/config")]
public async Task<IActionResult> UpdateFingerprintConfig([FromBody] FingerprintConfiguration newConfig)
{
    // 1?? Validar primero (opcional pero recomendado)
    var validation = await _fingerprintConfigSyncService.ValidateConfigurationAsync(newConfig);
    
    if (!validation.IsValid)
    {
        return BadRequest(new 
        { 
            error = "Configuración inválida",
            errors = validation.Errors,
            warnings = validation.Warnings
        });
    }
    
    // 2?? Actualizar en Futronic Service (fuente de verdad)
    var (success, updatedConfig) = await _fingerprintConfigSyncService.UpdateConfigurationAsync(newConfig);
    
    if (!success)
    {
        return StatusCode(500, new { error = "Error al actualizar configuración" });
    }
    
    // 3?? La copia en BD ya se guardó automáticamente
    return Ok(new 
    { 
        message = "Configuración actualizada exitosamente",
        config = updatedConfig
    });
}
```

### **3. Actualizar Solo un Campo (PATCH)**

```csharp
[HttpPatch("fingerprint/config/threshold")]
public async Task<IActionResult> UpdateThreshold([FromBody] int newThreshold)
{
    var updates = new Dictionary<string, object>
    {
        { "Threshold", newThreshold }
    };
    
    var (success, updatedConfig) = await _fingerprintConfigSyncService.UpdatePartialConfigurationAsync(updates);
    
    if (!success)
    {
        return StatusCode(500, new { error = "Error al actualizar threshold" });
    }
    
    return Ok(new 
    { 
        message = $"Threshold actualizado a {newThreshold}",
        config = updatedConfig
    });
}
```

### **4. Validar Antes de Guardar**

```csharp
[HttpPost("fingerprint/config/validate")]
public async Task<IActionResult> ValidateFingerprintConfig([FromBody] FingerprintConfiguration config)
{
    var validation = await _fingerprintConfigSyncService.ValidateConfigurationAsync(config);
    
    return Ok(new
    {
        isValid = validation.IsValid,
        errors = validation.Errors,
        warnings = validation.Warnings
    });
}
```

### **5. Restaurar a Valores Por Defecto**

```csharp
[HttpPost("fingerprint/config/reset")]
public async Task<IActionResult> ResetFingerprintConfig()
{
    var (success, defaultConfig) = await _fingerprintConfigSyncService.ResetConfigurationAsync();
    
    if (!success)
    {
        return StatusCode(500, new { error = "Error al restaurar configuración" });
    }
    
    return Ok(new 
    { 
        message = "Configuración restaurada a valores por defecto",
        config = defaultConfig
    });
}
```

---

## ?? Manejo de Errores

### **Estrategia de Fallback**

```csharp
public async Task<FingerprintConfiguration> GetConfigurationWithFallback()
{
    try
    {
        // 1. Intentar obtener desde Futronic Service
        return await _fingerprintConfigSyncService.GetConfigurationAsync();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogWarning(ex, "Futronic Service no disponible, usando copia de BD");
        
        // 2. Fallback a BD
        return await GetConfigurationFromDatabase();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error crítico al obtener configuración");
        
        // 3. Último fallback: valores por defecto
        return new FingerprintConfiguration();
    }
}
```

### **Códigos de Error del Futronic Service**

| Código | Descripción |
|--------|-------------|
| `GET_CONFIG_ERROR` | Error al obtener configuración |
| `UPDATE_CONFIG_FAILED` | Validación fallida al actualizar |
| `UPDATE_CONFIG_ERROR` | Error interno al actualizar |
| `PATCH_CONFIG_FAILED` | Error en actualización parcial |
| `VALIDATION_ERROR` | Error al validar configuración |
| `RESET_CONFIG_FAILED` | Error al restaurar por defecto |
| `RELOAD_CONFIG_ERROR` | Error al recargar desde archivo |

---

## ? Mejores Prácticas

### **1. Validar Antes de Guardar**

```csharp
// ? BIEN
var validation = await _service.ValidateConfigurationAsync(newConfig);
if (validation.IsValid)
{
    await _service.UpdateConfigurationAsync(newConfig);
}

// ? MAL
await _service.UpdateConfigurationAsync(newConfig); // Sin validar
```

### **2. Usar PATCH para Cambios Parciales**

```csharp
// ? BIEN - Solo actualiza lo necesario
var updates = new Dictionary<string, object> { { "Threshold", 80 } };
await _service.UpdatePartialConfigurationAsync(updates);

// ? MAL - Actualiza todo innecesariamente
var config = await _service.GetConfigurationAsync();
config.Threshold = 80;
await _service.UpdateConfigurationAsync(config);
```

### **3. Manejar Timeouts**

```csharp
// ? BIEN
builder.Services.AddHttpClient("FutronicService", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Timeout adecuado
});

// ? MAL
client.Timeout = TimeSpan.FromSeconds(5); // Muy corto para operaciones de configuración
```

### **4. Logging Adecuado**

```csharp
// ? BIEN
_logger.LogInformation("Actualizando configuración: Threshold={Threshold}", newThreshold);

try
{
    await _service.UpdateConfigurationAsync(config);
    _logger.LogInformation("? Configuración actualizada exitosamente");
}
catch (Exception ex)
{
    _logger.LogError(ex, "? Error al actualizar configuración");
}

// ? MAL
Console.WriteLine("Actualizando..."); // Sin logging estructurado
```

### **5. Auditoría de Cambios**

```csharp
// Agregar auditoría a tu entidad
public class FingerprintConfigurationAudit
{
    public int Id { get; set; }
    public int ConfigurationId { get; set; }
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
}
```

---

## ?? Seguridad

### **Autenticación/Autorización**

```csharp
[Authorize(Roles = "Admin")]
[HttpPut("fingerprint/config")]
public async Task<IActionResult> UpdateFingerprintConfig(...)
{
    // Solo administradores pueden modificar configuración
}
```

### **Validación de Input**

```csharp
[HttpPut("fingerprint/config")]
public async Task<IActionResult> UpdateFingerprintConfig(
    [FromBody] FingerprintConfiguration config)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    // Validación adicional
    if (config.Threshold < 0 || config.Threshold > 100)
    {
        return BadRequest("Threshold debe estar entre 0 y 100");
    }
    
    // Continuar...
}
```

---

## ?? Testing

### **Unit Test Ejemplo**

```csharp
[Fact]
public async Task UpdateConfiguration_ShouldReturnSuccess_WhenValid()
{
    // Arrange
    var mockHttpClient = new Mock<IHttpClientFactory>();
    var service = new FingerprintConfigSyncService(mockHttpClient.Object, ...);
    var newConfig = new FingerprintConfiguration { Threshold = 80 };
    
    // Act
    var (success, config) = await service.UpdateConfigurationAsync(newConfig);
    
    // Assert
    Assert.True(success);
    Assert.NotNull(config);
    Assert.Equal(80, config.Threshold);
}
```

---

## ?? Soporte

Para problemas o dudas:
- Revisar logs del Futronic Service en: `logs/futronic-service.log`
- Verificar conectividad: `GET http://localhost:5000/api/fingerprint/health`
- Revisar configuración persistida: `fingerprint-config.json`

---

## ?? Recursos Adicionales

- [Documentación Frontend](./FRONTEND-INTEGRATION.md)
- [API Reference Completa](./API-REFERENCE.md)
- [Troubleshooting Guide](./TROUBLESHOOTING.md)
