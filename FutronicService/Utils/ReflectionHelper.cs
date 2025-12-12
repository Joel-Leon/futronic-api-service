using System;

namespace FutronicService.Utils
{
    /// <summary>
    /// Clase auxiliar para configurar propiedades del SDK de Futronic mediante reflexión
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Intenta establecer una propiedad en un objeto mediante reflexión
        /// </summary>
        /// <param name="obj">Objeto que contiene la propiedad</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <param name="value">Valor a establecer</param>
        public static void TrySetProperty(object obj, string propertyName, object value)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var targetType = property.PropertyType;
                    object finalValue = value;

                    // Convertir el valor al tipo correcto si es necesario
                    if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                    {
                        try
                        {
                            finalValue = Convert.ChangeType(value, targetType);
                        }
                        catch
                        {
                            // Si la conversión falla, salir silenciosamente
                            return;
                        }
                    }

                    property.SetValue(obj, finalValue, null);
                }
            }
            catch
            {
                // Silenciar errores de configuración - algunas propiedades pueden no estar disponibles
                // en todas las versiones del SDK de Futronic
            }
        }

        /// <summary>
        /// Intenta obtener el valor de una propiedad mediante reflexión
        /// </summary>
        /// <param name="obj">Objeto que contiene la propiedad</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <returns>Valor de la propiedad o null si no se puede obtener</returns>
        public static object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    return property.GetValue(obj, null);
                }
            }
            catch
            {
                // Silenciar errores
            }
            return null;
        }
    }
}
