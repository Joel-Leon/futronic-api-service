using System.Collections.Generic;

namespace FutronicService.Models
{
/// <summary>
    /// Request para verificación simplificada con captura automática
    /// </summary>
    public class VerifySimpleRequest
    {
        /// <summary>
     /// DNI del usuario a verificar
        /// </summary>
        public string Dni { get; set; }

 /// <summary>
        /// Dedo a verificar (ej: "indice-derecho")
   /// </summary>
   public string Dedo { get; set; }

        /// <summary>
        /// Ruta del template registrado (opcional, se puede construir desde DNI + Dedo)
  /// </summary>
        public string StoredTemplatePath { get; set; }

    /// <summary>
     /// Timeout para la captura en milisegundos (opcional, usa default si no se especifica)
        /// </summary>
        public int? Timeout { get; set; }
    }

    /// <summary>
 /// Request para registro con múltiples muestras
    /// </summary>
  public class RegisterMultiSampleRequest
  {
    /// <summary>
  /// DNI del usuario
        /// </summary>
        public string Dni { get; set; }

   /// <summary>
     /// Dedo a registrar (ej: "indice-derecho")
        /// </summary>
 public string Dedo { get; set; }

   /// <summary>
 /// Ruta base donde se guardará el template (sin extensión)
      /// </summary>
   public string OutputPath { get; set; }

        /// <summary>
        /// Número de muestras a capturar (default: 5, máx: 5)
        /// </summary>
   public int? SampleCount { get; set; }

 /// <summary>
     /// Timeout para cada captura en milisegundos
  /// </summary>
        public int? Timeout { get; set; }
    }

    /// <summary>
    /// Respuesta del registro con múltiples muestras
    /// </summary>
public class RegisterMultiSampleResponseData
 {
        public string TemplatePath { get; set; }
   public string ImagePath { get; set; }
        public double Quality { get; set; }
        public string Dni { get; set; }
     public string Dedo { get; set; }
        public int SamplesCollected { get; set; }
        public List<double> SampleQualities { get; set; }
        public double AverageQuality { get; set; }
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
