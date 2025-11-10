using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FutronicService.Utils
{
  public static class ImageUtils
    {
public static byte[] ConvertBitmapToBytes(object bitmap)
        {
       try
    {
    var bitmapType = bitmap.GetType();

       if (bitmapType.FullName != "System.Drawing.Bitmap")
       {
        return null;
     }

                using (var memoryStream = new MemoryStream())
    {
           var imageFormatType = bitmapType.Assembly.GetType("System.Drawing.Imaging.ImageFormat");
   if (imageFormatType != null)
   {
           var bmpFormatProp = imageFormatType.GetProperty("Bmp");
      if (bmpFormatProp != null)
   {
     var bmpFormat = bmpFormatProp.GetValue(null);
             var saveMethod = bitmapType.GetMethod("Save", new Type[] { typeof(Stream), bmpFormat.GetType() });
        if (saveMethod != null)
     {
   saveMethod.Invoke(bitmap, new object[] { memoryStream, bmpFormat });
return memoryStream.ToArray();
       }
      }
           }

         var saveMethodSimple = bitmapType.GetMethod("Save", new Type[] { typeof(Stream) });
        if (saveMethodSimple != null)
   {
        saveMethodSimple.Invoke(bitmap, new object[] { memoryStream });
        return memoryStream.ToArray();
   }
    }

      return null;
  }
 catch
    {
  return null;
}
     }

        public static double CalculateImageQuality(byte[] imageData)
  {
      try
  {
           if (imageData.Length < 100) return 0.0;

     int startOffset = 54;
  if (imageData.Length < startOffset) startOffset = 0;

     byte[] pixelData = new byte[imageData.Length - startOffset];
    Array.Copy(imageData, startOffset, pixelData, 0, pixelData.Length);

        var histogram = new int[256];
         foreach (byte pixel in pixelData)
     {
histogram[pixel]++;
   }

         double entropy = 0.0;
    foreach (int count in histogram)
        {
    if (count > 0)
     {
  double probability = (double)count / pixelData.Length;
   entropy -= probability * Math.Log(probability, 2.0);
       }
  }

       byte minVal = pixelData.Min();
          byte maxVal = pixelData.Max();
       double contrast = (double)(maxVal - minVal) / 255.0;

      double avgGradient = 0.0;
      if (pixelData.Length > 1)
       {
 for (int i = 1; i < Math.Min(1000, pixelData.Length); i++)
       {
          avgGradient += Math.Abs(pixelData[i] - pixelData[i - 1]);
   }
     avgGradient /= Math.Min(1000, pixelData.Length - 1);
       avgGradient /= 255.0;
          }

        double quality = (entropy / 8.0) * 40 + contrast * 30 + avgGradient * 30;
      quality = Math.Max(0, Math.Min(100, quality));

    return quality;
   }
    catch
     {
  return 50.0;
    }
 }

        public static List<CapturedImage> SelectBestImages(List<CapturedImage> allImages, int targetSamples)
        {
 if (allImages.Count == 0) return new List<CapturedImage>();

            var sortedImages = allImages.OrderByDescending(img => img.Quality).ToList();

   int selectCount = 1;

            if (allImages.Count >= 5) selectCount = Math.Min(3, allImages.Count);
       else if (allImages.Count >= 4) selectCount = 2;
  else if (allImages.Count >= 3) selectCount = 1;
 else if (allImages.Count >= 2) selectCount = 1;

selectCount = Math.Min(selectCount, allImages.Count);

  var selected = sortedImages.Take(selectCount).ToList();
  return selected;
        }
    }
}
