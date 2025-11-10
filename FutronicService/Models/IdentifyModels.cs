using System.Collections.Generic;

namespace FutronicService.Models
{
    public class TemplateInfo
    {
        public string Dni { get; set; }
   public string Dedo { get; set; }
        public string TemplatePath { get; set; }
    }

    public class IdentifyRequest
    {
  public string CapturedTemplate { get; set; }
        public List<TemplateInfo> Templates { get; set; }
    }

    public class IdentifyResponseData
{
        public bool Matched { get; set; }
        public string Dni { get; set; }
    public string Dedo { get; set; }
        public string TemplatePath { get; set; }
        public int Score { get; set; }
  public int Threshold { get; set; }
        public int BestScore { get; set; }
public int TotalCompared { get; set; }
 }
}
