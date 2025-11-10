namespace FutronicService.Models
{
    public class HealthResponseData
    {
        public string Status { get; set; }
        public bool DeviceConnected { get; set; }
        public bool SdkInitialized { get; set; }
        public string DeviceModel { get; set; }
        public string SdkVersion { get; set; }
        public string Uptime { get; set; }
        public string LastError { get; set; }
 }

    public class ConfigResponseData
    {
        public int Threshold { get; set; }
        public int Timeout { get; set; }
        public string TempPath { get; set; }
        public bool OverwriteExisting { get; set; }
    }

    public class UpdateConfigRequest
    {
        public int? Threshold { get; set; }
        public int? Timeout { get; set; }
        public string TempPath { get; set; }
        public bool? OverwriteExisting { get; set; }
    }
}
