using System;

namespace futronic_cli
{
    public static class ReflectionHelper
    {
        public static void TrySetProperty(object obj, string propertyName, object value)
        {
            try
            {
                var p = obj.GetType().GetProperty(propertyName);
                if (p != null && p.CanWrite)
                {
                    var targetType = p.PropertyType;
                    object finalValue = value;

                    if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                    {
                        try
                        {
                            finalValue = Convert.ChangeType(value, targetType);
                        }
                        catch
                        {
                            return;
                        }
                    }

                    p.SetValue(obj, finalValue, null);
                }
            }
            catch
            {
                // Silenciar errores de configuración
            }
        }
    }
}