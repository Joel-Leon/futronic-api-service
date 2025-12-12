namespace FutronicService.Utils
{
    public static class ErrorCodes
    {
        public const string DEVICE_NOT_CONNECTED = "DEVICE_NOT_CONNECTED";
        public const string CAPTURE_TIMEOUT = "CAPTURE_TIMEOUT";
        public const string CAPTURE_FAILED = "CAPTURE_FAILED";
        public const string CAPTURE_CANCELLED = "CAPTURE_CANCELLED";
        public const string QUALITY_TOO_LOW = "QUALITY_TOO_LOW";
        public const string FINGER_REMOVED = "FINGER_REMOVED";
        public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";
        public const string INVALID_TEMPLATE = "INVALID_TEMPLATE";
        public const string SAVE_FAILED = "SAVE_FAILED";
        public const string COMPARISON_FAILED = "COMPARISON_FAILED";
        public const string INVALID_INPUT = "INVALID_INPUT";
        public const string FILE_EXISTS = "FILE_EXISTS";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";

        /// <summary>
        /// Mapea códigos de error del SDK Futronic a mensajes descriptivos en español
        /// Basado en la documentación del SDK: ftrScanAPI.h
        /// </summary>
        public static string GetFutronicErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                0 => "Operación exitosa",

                // Errores de dispositivo (1-10, 202)
                1 => "Error al abrir el dispositivo. Verifique que el lector de huellas esté conectado correctamente por USB.",
                2 => "Dispositivo no conectado o desconectado durante la operación. Reconecte el dispositivo USB.",
                3 => "Error de comunicación con el dispositivo. Intente reconectar el dispositivo.",
                4 => "Error al leer datos del dispositivo. Verifique la conexión USB.",
                5 => "Error al escribir datos al dispositivo. Verifique la conexión USB.",
                6 => "Dispositivo ocupado. Espere a que termine la operación actual.",

                // Errores de captura
                8 => "Timeout: No se detectó huella dentro del tiempo límite. Coloque el dedo en el sensor.",
                202 => "Error de captura: Dispositivo no conectado o no responde. Verifique la conexión USB y que los drivers estén instalados correctamente.",

                // Errores de calidad
                10 => "Calidad de imagen demasiado baja. Limpie el sensor y el dedo, luego intente nuevamente.",
                11 => "Imagen demasiado oscura. Presione el dedo con más firmeza en el sensor.",
                12 => "Imagen demasiado clara. Presione el dedo con menos fuerza en el sensor.",
                13 => "Huella dañada o ilegible. Intente con otro dedo.",
                14 => "Área de huella insuficiente. Asegúrese de cubrir completamente el sensor con el dedo.",
                15 => "Huella fuera de foco. Mantenga el dedo inmóvil durante la captura.",

                // Errores de registro/enrollment
                20 => "Error de registro: Muestras inconsistentes. Asegúrese de usar el mismo dedo en todas las capturas.",
                21 => "Error de registro: No se pudieron capturar suficientes muestras. Intente nuevamente.",
                22 => "Error de registro: Las muestras capturadas no coinciden entre sí. Use el mismo dedo consistentemente.",
                23 => "Error de registro: Calidad insuficiente en las muestras. Limpie el sensor y asegure buena presión.",

                // Errores de verificación
                40 => "Error de verificación: Template de referencia inválido o corrupto.",
                41 => "Error de verificación: No se pudo comparar las huellas.",
                42 => "Huellas no coinciden (por debajo del umbral de seguridad).",

                // Errores de memoria
                60 => "Error de memoria: No hay suficiente memoria disponible.",
                61 => "Error al asignar memoria para el template.",
                62 => "Buffer insuficiente para almacenar los datos.",

                // Errores de parámetros
                80 => "Parámetro inválido en la operación.",
                81 => "Valor de umbral (threshold) fuera de rango válido.",
                82 => "Configuración del dispositivo inválida.",

                // Errores de SDK/Licencia
                100 => "SDK no inicializado correctamente. Reinstale los drivers.",
                101 => "Versión del SDK incompatible.",
                102 => "Error de licencia del SDK.",
                103 => "DLL del SDK no encontrada (ftrapi.dll). Copie los archivos necesarios al directorio de la aplicación.",

                // Error genérico
                _ => $"Error del SDK (código {errorCode}). Consulte los logs o contacte soporte técnico."
            };
        }

        /// <summary>
        /// Determina el código de error de la API basado en el código del SDK
        /// </summary>
        public static string GetApiErrorCode(int sdkErrorCode)
        {
            return sdkErrorCode switch
            {
                0 => null, // Sin error
                1 or 2 or 3 or 4 or 5 or 6 or 202 => DEVICE_NOT_CONNECTED,
                8 => CAPTURE_TIMEOUT,
                10 or 11 or 12 or 13 or 14 or 15 or 23 => QUALITY_TOO_LOW,
                20 or 21 or 22 => CAPTURE_FAILED,
                40 or 41 => INVALID_TEMPLATE,
                42 => COMPARISON_FAILED,
                60 or 61 or 62 => INTERNAL_ERROR,
                80 or 81 or 82 => INVALID_INPUT,
                100 or 101 or 102 or 103 => DEVICE_NOT_CONNECTED,
                _ => CAPTURE_FAILED
            };
        }

        /// <summary>
        /// Obtiene recomendaciones de solución basadas en el código de error
        /// </summary>
        public static string GetErrorSolution(int errorCode)
        {
            return errorCode switch
            {
                1 or 2 or 202 => "Soluciones sugeridas:\n1. Verifique que el dispositivo USB esté conectado\n2. Reinstale los drivers de Futronic\n3. Intente usar otro puerto USB\n4. Reinicie el servicio",
                8 => "Soluciones sugeridas:\n1. Aumente el timeout en la configuración\n2. Asegúrese de colocar el dedo cuando se indique\n3. Verifique que el sensor esté limpio",
                10 or 23 => "Soluciones sugeridas:\n1. Limpie el sensor con alcohol isopropílico\n2. Limpie y seque su dedo\n3. Presione con firmeza pero sin exceso\n4. Intente con otro dedo si el problema persiste",
                20 or 21 or 22 => "Soluciones sugeridas:\n1. Use el mismo dedo en todas las capturas\n2. Mantenga una posición similar en cada muestra\n3. No mueva el dedo durante la captura",
                103 => "Soluciones sugeridas:\n1. Copie ftrapi.dll al directorio de la aplicación\n2. Verifique que esté en PATH del sistema\n3. Reinstale el SDK de Futronic",
                _ => "Consulte los logs del servidor para más detalles."
            };
        }
    }
}
