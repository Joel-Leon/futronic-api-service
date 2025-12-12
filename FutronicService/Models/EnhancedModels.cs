using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FutronicService.Models
{
    /// <summary>
    /// Request para verificación simplificada con captura automática
    /// </summary>
    public class VerifySimpleRequest
    {
        /// <summary>
        /// DNI del usuario a verificar (REQUERIDO)
        /// </summary>
        [Required(ErrorMessage = "El campo Dni es requerido")]
        public string Dni { get; set; }

        /// <summary>
        /// Dedo a verificar (ej: "indice-derecho")
        /// </summary>
        public string? Dedo { get; set; }

        /// <summary>
        /// Ruta del template registrado (opcional, se puede construir desde DNI + Dedo)
        /// </summary>
        public string? StoredTemplatePath { get; set; }

        /// <summary>
        /// Timeout para la captura en milisegundos (opcional, usa default si no se especifica)
        /// </summary>
        public int? Timeout { get; set; }
        
        /// <summary>
        /// Si se debe incluir la imagen capturada en Base64 en la respuesta (default: true)
        /// Las imágenes se envían automáticamente por SignalR durante la captura
        /// </summary>
        public bool? IncludeCapturedImage { get; set; } = true;
    }

    /// <summary>
    /// Request para registro con múltiples muestras
    /// </summary>
    public class RegisterMultiSampleRequest
    {
        /// <summary>
        /// DNI del usuario (REQUERIDO)
        /// </summary>
        [Required(ErrorMessage = "El campo Dni es requerido")]
        public string Dni { get; set; }

        /// <summary>
        /// Dedo a registrar (ej: "indice-derecho")
        /// </summary>
        public string? Dedo { get; set; }

        /// <summary>
        /// Ruta base donde se guardará el template (sin extensión)
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// Número de muestras a capturar (default: 5, máx: 10)
        /// </summary>
        public int? SampleCount { get; set; }

        /// <summary>
        /// Timeout para cada captura en milisegundos
        /// </summary>
        public int? Timeout { get; set; }
        
        /// <summary>
        /// Si se deben incluir las imágenes en Base64 en las notificaciones de SignalR (default: true)
        /// Las imágenes se envían en tiempo real por cada muestra capturada vía SignalR
        /// </summary>
        public bool? IncludeImages { get; set; } = true;
    }

    /// <summary>
    /// Respuesta del registro con múltiples muestras
    /// </summary>
    public class RegisterMultiSampleResponseData
    {
        /// <summary>
        /// DNI del usuario registrado
        /// </summary>
        public string Dni { get; set; }
        
        /// <summary>
        /// Dedo registrado
        /// </summary>
     public string Dedo { get; set; }
     
        /// <summary>
        /// Ruta del archivo template (.tml)
        /// </summary>
        public string TemplatePath { get; set; }
        
        /// <summary>
        /// Ruta de la primera imagen (mejor calidad)
        /// </summary>
   public string ImagePath { get; set; }
   
        /// <summary>
        /// Lista de rutas de todas las imágenes guardadas
        /// </summary>
        public List<string> ImagePaths { get; set; }
        
        /// <summary>
        /// Ruta del archivo metadata.json
        /// </summary>
        public string MetadataPath { get; set; }
        
        /// <summary>
        /// Calidad de la mejor muestra capturada
        /// </summary>
        public double Quality { get; set; }
        
        /// <summary>
        /// Número de muestras capturadas exitosamente
        /// </summary>
        public int SamplesCollected { get; set; }
        
        /// <summary>
        /// Lista de calidades de cada muestra
        /// </summary>
        public List<double> SampleQualities { get; set; }
        
        /// <summary>
        /// Calidad promedio de todas las muestras
        /// </summary>
        public double AverageQuality { get; set; }
        
        /// <summary>
        /// Imágenes de las muestras en formato Base64 (opcional)
        /// Solo se incluyen si se solicita explícitamente para no sobrecargar la respuesta
        /// </summary>
        public List<ImageData> Images { get; set; }
    }
    
    /// <summary>
    /// Datos de una imagen capturada
    /// </summary>
    public class ImageData
    {
        /// <summary>
        /// Número de muestra (1-based)
        /// </summary>
        public int SampleNumber { get; set; }
        
        /// <summary>
        /// Calidad de esta imagen específica
        /// </summary>
        public double Quality { get; set; }
        
        /// <summary>
        /// Imagen en formato Base64
        /// </summary>
        public string ImageBase64 { get; set; }
        
        /// <summary>
        /// Formato de la imagen (ej: "bmp")
        /// </summary>
        public string Format { get; set; }
        
        /// <summary>
        /// Ruta del archivo de imagen
        /// </summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Respuesta de verificación simplificada
    /// </summary>
    public class VerifySimpleResponseData : VerifyResponseData
    {
        public string Dni { get; set; }
     public string Dedo { get; set; }
 public double CaptureQuality { get; set; }
    }

    /// <summary>
    /// Request para identificación en vivo (captura automática del dispositivo)
    /// </summary>
    public class IdentifyLiveRequest
    {
    /// <summary>
        /// Directorio donde buscar templates recursivamente
        /// </summary>
        public string TemplatesDirectory { get; set; }

        /// <summary>
   /// Timeout para la captura en milisegundos
 /// </summary>
        public int? Timeout { get; set; }
    }

    /// <summary>
    /// Respuesta de identificación en vivo
    /// </summary>
  public class IdentifyLiveResponseData
    {
        public bool Matched { get; set; }
        public string Dni { get; set; }
        public string Dedo { get; set; }
 public string TemplatePath { get; set; }
 public int Score { get; set; }
        public int Threshold { get; set; }
        public int MatchIndex { get; set; }
 public int TotalCompared { get; set; }
    }
}
