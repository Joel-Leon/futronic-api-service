namespace futronic_cli
{
    public class VerificationResult
    {
        public bool Verified { get; set; }
        public int ResultCode { get; set; }
        public int FarnValue { get; set; } = -1;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}