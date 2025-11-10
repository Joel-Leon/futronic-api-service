namespace FutronicService.Models
{
    public class RegisterRequest
    {
        public string Dni { get; set; }
  public string Dedo { get; set; }
        public string OutputPath { get; set; }
    }

    public class RegisterResponseData
    {
        public string TemplatePath { get; set; }
        public string ImagePath { get; set; }
 public double Quality { get; set; }
        public string Dni { get; set; }
  public string Dedo { get; set; }
    }
}
