namespace futronic_cli
{
    public class CaptureResult
    {
        public byte[] Template { get; set; }
        public int ResultCode { get; set; }
        public int CurrentSample { get; set; }
        public bool Success { get; set; }
    }
}