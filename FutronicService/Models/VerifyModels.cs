namespace FutronicService.Models
{
    public class VerifyRequest
  {
        public string StoredTemplate { get; set; }
  public string CapturedTemplate { get; set; }
    }

    public class VerifyResponseData
    {
        public bool Verified { get; set; }
        public int Score { get; set; }
        public int Threshold { get; set; }
        public string TemplatePath { get; set; }
        
        /// <summary>
        /// Calidad de la huella capturada
        /// </summary>
        public double CapturedQuality { get; set; }
        
        /// <summary>
        /// Imagen capturada en Base64 (opcional)
        /// </summary>
        public string CapturedImageBase64 { get; set; }
        
        /// <summary>
        /// Formato de la imagen capturada
        /// </summary>
        public string CapturedImageFormat { get; set; }
    }
}
