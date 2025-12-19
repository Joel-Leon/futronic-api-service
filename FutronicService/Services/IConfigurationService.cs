using FutronicService.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FutronicService.Services
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Obtener configuración actual
        /// </summary>
        FingerprintConfiguration GetConfiguration();

        /// <summary>
        /// Actualizar configuración completa
        /// </summary>
        Task<bool> UpdateConfigurationAsync(FingerprintConfiguration config);

        /// <summary>
        /// Actualizar valores específicos de configuración
        /// </summary>
        Task<bool> UpdatePartialConfigurationAsync(Dictionary<string, object> updates);

        /// <summary>
        /// Validar configuración
        /// </summary>
        ConfigurationValidationResult ValidateConfiguration(FingerprintConfiguration config);

        /// <summary>
        /// Recargar configuración desde archivo
        /// </summary>
        Task ReloadConfigurationAsync();

        /// <summary>
        /// Restaurar configuración por defecto
        /// </summary>
        Task<bool> ResetToDefaultAsync();

        /// <summary>
        /// Guardar configuración actual en archivo
        /// </summary>
        Task<bool> SaveConfigurationAsync();
    }
}
