using FutronicService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FutronicService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _configFilePath;
        private FingerprintConfiguration _currentConfig;
        private readonly object _configLock = new object();

        public ConfigurationService(
            ILogger<ConfigurationService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "fingerprint-config.json");
            
            // Cargar configuración inicial
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            lock (_configLock)
            {
                try
                {
                    // Intentar cargar desde archivo personalizado
                    if (File.Exists(_configFilePath))
                    {
                        var json = File.ReadAllText(_configFilePath);
                        _currentConfig = JsonConvert.DeserializeObject<FingerprintConfiguration>(json);
                        _logger.LogInformation($"? Configuración cargada desde: {_configFilePath}");
                    }
                    else
                    {
                        // Cargar desde appsettings.json
                        _currentConfig = new FingerprintConfiguration
                        {
                            Threshold = _configuration.GetValue<int>("Fingerprint:Threshold", 70),
                            Timeout = _configuration.GetValue<int>("Fingerprint:Timeout", 30000),
                            CaptureMode = _configuration.GetValue<string>("Fingerprint:CaptureMode", "screen"),
                            ShowImage = _configuration.GetValue<bool>("Fingerprint:ShowImage", true),
                            SaveImage = _configuration.GetValue<bool>("Fingerprint:SaveImage", false),
                            DetectFakeFinger = _configuration.GetValue<bool>("Fingerprint:DetectFakeFinger", false),
                            MaxFramesInTemplate = _configuration.GetValue<int>("Fingerprint:MaxFramesInTemplate", 5),
                            DisableMIDT = _configuration.GetValue<bool>("Fingerprint:DisableMIDT", false),
                            MaxRotation = _configuration.GetValue<int>("Fingerprint:MaxRotation", 199),
                            TemplatePath = _configuration.GetValue<string>("Fingerprint:TempPath", "C:/temp/fingerprints"),
                            CapturePath = _configuration.GetValue<string>("Fingerprint:CapturePath", "C:/temp/fingerprints/captures"),
                            OverwriteExisting = _configuration.GetValue<bool>("Fingerprint:OverwriteExisting", false),
                            MaxTemplatesPerIdentify = _configuration.GetValue<int>("Fingerprint:MaxTemplatesPerIdentify", 500),
                            DeviceCheckRetries = _configuration.GetValue<int>("Fingerprint:DeviceCheckRetries", 3),
                            DeviceCheckDelayMs = _configuration.GetValue<int>("Fingerprint:DeviceCheckDelayMs", 1000),
                            MinQuality = _configuration.GetValue<int>("Fingerprint:MinQuality", 50),
                            CompressImages = _configuration.GetValue<bool>("Fingerprint:CompressImages", false),
                            ImageFormat = _configuration.GetValue<string>("Fingerprint:ImageFormat", "bmp")
                        };

                        _logger.LogInformation("?? Configuración cargada desde appsettings.json");
                        
                        // Guardar configuración por primera vez
                        SaveConfigurationAsync().Wait();
                    }

                    _logger.LogInformation($"?? Configuración activa: Threshold={_currentConfig.Threshold}, MaxRotation={_currentConfig.MaxRotation}, DetectFakeFinger={_currentConfig.DetectFakeFinger}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Error al cargar configuración, usando valores por defecto");
                    _currentConfig = new FingerprintConfiguration();
                }
            }
        }

        public FingerprintConfiguration GetConfiguration()
        {
            lock (_configLock)
            {
                // Retornar copia para evitar modificaciones directas
                return JsonConvert.DeserializeObject<FingerprintConfiguration>(
                    JsonConvert.SerializeObject(_currentConfig));
            }
        }

        public async Task<bool> UpdateConfigurationAsync(FingerprintConfiguration config)
        {
            try
            {
                // Validar configuración
                var validation = ValidateConfiguration(config);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"?? Configuración inválida: {string.Join(", ", validation.Errors)}");
                    return false;
                }

                lock (_configLock)
                {
                    _currentConfig = config;
                }

                // Guardar en archivo
                var success = await SaveConfigurationAsync();

                if (success)
                {
                    _logger.LogInformation("? Configuración actualizada y guardada correctamente");
                    
                    // Mostrar warnings si hay
                    if (validation.Warnings.Any())
                    {
                        _logger.LogWarning($"?? Advertencias: {string.Join(", ", validation.Warnings)}");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al actualizar configuración");
                return false;
            }
        }

        public async Task<bool> UpdatePartialConfigurationAsync(Dictionary<string, object> updates)
        {
            try
            {
                lock (_configLock)
                {
                    var configType = typeof(FingerprintConfiguration);

                    foreach (var update in updates)
                    {
                        var property = configType.GetProperty(update.Key);
                        if (property != null && property.CanWrite)
                        {
                            try
                            {
                                var value = Convert.ChangeType(update.Value, property.PropertyType);
                                property.SetValue(_currentConfig, value);
                                _logger.LogInformation($"?? Actualizado {update.Key} = {value}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"?? No se pudo actualizar {update.Key}: {ex.Message}");
                            }
                        }
                    }
                }

                // Validar y guardar
                var validation = ValidateConfiguration(_currentConfig);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"?? Configuración resultante inválida: {string.Join(", ", validation.Errors)}");
                    return false;
                }

                return await SaveConfigurationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al actualizar configuración parcial");
                return false;
            }
        }

        public ConfigurationValidationResult ValidateConfiguration(FingerprintConfiguration config)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            // Validar usando Data Annotations
            var context = new ValidationContext(config);
            var validationResults = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(config, context, validationResults, true))
            {
                result.IsValid = false;
                result.Errors.AddRange(validationResults.Select(vr => vr.ErrorMessage));
            }

            // Validaciones personalizadas
            if (config.MaxRotation < 166)
            {
                result.Warnings.Add("?? MaxRotation < 166 puede permitir coincidencias con huellas muy rotadas");
            }

            if (config.MaxRotation > 199)
            {
                result.Errors.Add("? MaxRotation no puede ser mayor a 199 (límite del SDK)");
                result.IsValid = false;
            }

            if (config.DetectFakeFinger && config.Timeout < 10000)
            {
                result.Warnings.Add("?? DetectFakeFinger requiere timeout >= 10 segundos para funcionar correctamente");
            }

            if (config.MaxFramesInTemplate > 7)
            {
                result.Warnings.Add("?? MaxFramesInTemplate > 7 puede generar plantillas muy grandes");
            }

            if (config.MinQuality < 30)
            {
                result.Warnings.Add("?? MinQuality < 30 puede aceptar huellas de muy baja calidad");
            }

            if (!Directory.Exists(Path.GetDirectoryName(config.TemplatePath)))
            {
                result.Warnings.Add($"?? Directorio de plantillas no existe: {config.TemplatePath}");
            }

            return result;
        }

        public async Task ReloadConfigurationAsync()
        {
            await Task.Run(() => LoadConfiguration());
            _logger.LogInformation("?? Configuración recargada");
        }

        public async Task<bool> ResetToDefaultAsync()
        {
            try
            {
                lock (_configLock)
                {
                    _currentConfig = new FingerprintConfiguration();
                }

                var success = await SaveConfigurationAsync();
                
                if (success)
                {
                    _logger.LogInformation("?? Configuración restaurada a valores por defecto");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al restaurar configuración por defecto");
                return false;
            }
        }

        public async Task<bool> SaveConfigurationAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_currentConfig, Formatting.Indented);
                await File.WriteAllTextAsync(_configFilePath, json);
                _logger.LogInformation($"?? Configuración guardada en: {_configFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al guardar configuración");
                return false;
            }
        }
    }
}
