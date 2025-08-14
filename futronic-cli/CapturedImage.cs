using System;

namespace futronic_cli
{
    public class CapturedImage
    {
        public byte[] ImageData { get; set; }
        public int SampleIndex { get; set; }
        public DateTime CaptureTime { get; set; }
        public double Quality { get; set; } // Calculado por análisis de imagen
    }
}