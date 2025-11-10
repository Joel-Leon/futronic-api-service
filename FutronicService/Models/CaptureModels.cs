namespace FutronicService.Models
{
    public class CaptureRequest
    {
        public int Timeout { get; set; } = 30000;
        public string CallbackUrl { get; set; } // URL para enviar eventos de progreso
    }

    public class CaptureResponseData
    {
        public string TemplatePath { get; set; }
        public string ImagePath { get; set; }
        public double Quality { get; set; }
        public string Timestamp { get; set; }
    }
}
