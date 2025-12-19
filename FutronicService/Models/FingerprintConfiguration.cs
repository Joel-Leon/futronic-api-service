using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FutronicService.Models
{
    /// <summary>
    /// Configuración del servicio de huellas dactilares
    /// </summary>
    public class FingerprintConfiguration
    {
        /// <summary>
        /// Umbral de coincidencia (0-100). Valores más altos = más estricto
        /// </summary>
        [Range(0, 100)]
        public int Threshold { get; set; } = 70;

        /// <summary>
        /// Timeout en milisegundos para operaciones de captura
        /// </summary>
        [Range(5000, 60000)]
        public int Timeout { get; set; } = 30000;

        /// <summary>
        /// Modo de captura: "screen" (temporal) o "file" (guardar imagen)
        /// </summary>
        [RegularExpression("^(screen|file)$")]
        public string CaptureMode { get; set; } = "screen";

        /// <summary>
        /// Mostrar imagen de huella en pantalla durante captura
        /// </summary>
        public bool ShowImage { get; set; } = true;

        /// <summary>
        /// Guardar imagen de huella como archivo BMP
        /// </summary>
        public bool SaveImage { get; set; } = false;

        /// <summary>
        /// Detectar dedos falsos/artificiales (liveness detection)
        /// </summary>
        public bool DetectFakeFinger { get; set; } = false;

        /// <summary>
        /// Número máximo de frames en el template (1-10)
        /// Más frames = mejor calidad pero plantillas más grandes
        /// </summary>
        [Range(1, 10)]
        public int MaxFramesInTemplate { get; set; } = 5;

        /// <summary>
        /// Deshabilitar detección de movimiento incremental del dedo (MIDT)
        /// true = solo detecta dedo colocado/retirado (más rápido)
        /// false = detecta movimiento fino del dedo (más preciso)
        /// </summary>
        public bool DisableMIDT { get; set; } = false;

        /// <summary>
        /// Valor de rotación máxima permitida para matching (0-199)
        /// 166 = default del SDK (más tolerante)
        /// 199 = más restrictivo (requiere mejor alineación)
        /// Valores más altos = menor tolerancia a rotación
        /// </summary>
        [Range(0, 199)]
        public int MaxRotation { get; set; } = 199;

        /// <summary>
        /// Ruta de almacenamiento de plantillas
        /// </summary>
        public string TemplatePath { get; set; } = "C:/temp/fingerprints";

        /// <summary>
        /// Ruta de almacenamiento de imágenes capturadas
        /// </summary>
        public string CapturePath { get; set; } = "C:/temp/fingerprints/captures";

        /// <summary>
        /// Permitir sobrescribir huellas existentes
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// Máximo de plantillas a comparar en identificación
        /// </summary>
        [Range(1, 10000)]
        public int MaxTemplatesPerIdentify { get; set; } = 500;

        /// <summary>
        /// Reintentos de verificación de dispositivo al iniciar
        /// </summary>
        [Range(1, 10)]
        public int DeviceCheckRetries { get; set; } = 3;

        /// <summary>
        /// Delay en ms entre reintentos de verificación de dispositivo
        /// </summary>
        [Range(100, 5000)]
        public int DeviceCheckDelayMs { get; set; } = 1000;

        /// <summary>
        /// Calidad mínima aceptable para una muestra (0-100)
        /// </summary>
        [Range(0, 100)]
        public int MinQuality { get; set; } = 50;

        /// <summary>
        /// Habilitar compresión de imágenes en Base64
        /// </summary>
        public bool CompressImages { get; set; } = false;

        /// <summary>
        /// Formato de imagen: "bmp", "png", "jpg"
        /// </summary>
        [RegularExpression("^(bmp|png|jpg)$")]
        public string ImageFormat { get; set; } = "bmp";
    }

    /// <summary>
    /// Respuesta de validación de configuración
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
