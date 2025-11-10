using System;

namespace FutronicService.Utils
{
    public class CapturedImage
    {
        public byte[] ImageData { get; set; }
        public int SampleIndex { get; set; }
        public DateTime CaptureTime { get; set; }
        public double Quality { get; set; }
    }
}
