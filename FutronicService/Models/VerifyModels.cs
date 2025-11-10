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
    }
}
