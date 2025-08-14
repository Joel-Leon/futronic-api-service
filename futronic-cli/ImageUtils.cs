using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace futronic_cli
{
    public static class ImageUtils
    {
        public static byte[] ConvertBitmapToBytes(object bitmap)
        {
            try
            {
                // Usar reflexión para trabajar con System.Drawing.Bitmap sin referencia directa
                var bitmapType = bitmap.GetType();

                // Verificar que sea un Bitmap
                if (bitmapType.FullName != "System.Drawing.Bitmap")
                {
                    Console.WriteLine($"⚠️ Tipo inesperado: {bitmapType.FullName}");
                    return null;
                }

                // Usar el método Save para convertir a BMP
                using (var memoryStream = new MemoryStream())
                {
                    // Obtener ImageFormat.Bmp usando reflexión
                    var imageFormatType = bitmapType.Assembly.GetType("System.Drawing.Imaging.ImageFormat");
                    if (imageFormatType != null)
                    {
                        var bmpFormatProp = imageFormatType.GetProperty("Bmp");
                        if (bmpFormatProp != null)
                        {
                            var bmpFormat = bmpFormatProp.GetValue(null);

                            // Llamar bitmap.Save(stream, ImageFormat.Bmp)
                            var saveMethod = bitmapType.GetMethod("Save", new Type[] { typeof(Stream), bmpFormat.GetType() });
                            if (saveMethod != null)
                            {
                                saveMethod.Invoke(bitmap, new object[] { memoryStream, bmpFormat });
                                return memoryStream.ToArray();
                            }
                        }
                    }

                    // Fallback: guardar sin formato específico (puede funcionar)
                    var saveMethodSimple = bitmapType.GetMethod("Save", new Type[] { typeof(Stream) });
                    if (saveMethodSimple != null)
                    {
                        saveMethodSimple.Invoke(bitmap, new object[] { memoryStream });
                        return memoryStream.ToArray();
                    }
                }

                Console.WriteLine("⚠️ No se pudo convertir bitmap a bytes");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error convirtiendo bitmap: {ex.Message}");
                return null;
            }
        }

        public static double CalculateImageQuality(byte[] imageData)
        {
            try
            {
                // Análisis básico de calidad basado en:
                // 1. Entropía (variabilidad de píxeles)
                // 2. Contraste
                // 3. Presencia de detalles

                if (imageData.Length < 100) return 0.0;

                // Saltar header BMP si existe (típicamente primeros 54 bytes)
                int startOffset = 54;
                if (imageData.Length < startOffset) startOffset = 0;

                byte[] pixelData = new byte[imageData.Length - startOffset];
                Array.Copy(imageData, startOffset, pixelData, 0, pixelData.Length);

                // Calcular entropía
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

                // Calcular contraste (diferencia entre min y max)
                byte minVal = pixelData.Min();
                byte maxVal = pixelData.Max();
                double contrast = (double)(maxVal - minVal) / 255.0;

                // Calcular variabilidad local (gradiente promedio)
                double avgGradient = 0.0;
                if (pixelData.Length > 1)
                {
                    for (int i = 1; i < Math.Min(1000, pixelData.Length); i++)
                    {
                        avgGradient += Math.Abs(pixelData[i] - pixelData[i - 1]);
                    }
                    avgGradient /= Math.Min(1000, pixelData.Length - 1);
                    avgGradient /= 255.0; // Normalizar
                }

                // Combinar métricas en un score de calidad (0-100)
                double quality = (entropy / 8.0) * 40 + contrast * 30 + avgGradient * 30;
                quality = Math.Max(0, Math.Min(100, quality));

                return quality;
            }
            catch
            {
                return 50.0; // Calidad neutral por defecto
            }
        }

        public static List<CapturedImage> SelectBestImages(List<CapturedImage> allImages, int targetSamples)
        {
            if (allImages.Count == 0) return new List<CapturedImage>();

            // Ordenar por calidad descendente
            var sortedImages = allImages.OrderByDescending(img => img.Quality).ToList();

            // Determinar cuántas imágenes seleccionar basado en el número capturado
            int selectCount = 1; // Por defecto, siempre al menos 1

            if (allImages.Count >= 5) selectCount = Math.Min(3, allImages.Count); // Si capturó 5+, tomar las mejores 3
            else if (allImages.Count >= 4) selectCount = 2; // Si capturó 4, tomar las mejores 2
            else if (allImages.Count >= 3) selectCount = 1; // Si capturó 3, tomar la mejor 1
            else if (allImages.Count >= 2) selectCount = 1; // Si capturó 2, tomar la mejor 1

            // Asegurar que no seleccionemos más de las disponibles
            selectCount = Math.Min(selectCount, allImages.Count);

            Console.WriteLine($"🔍 Selección inteligente:");
            Console.WriteLine($"   • Imágenes disponibles: {allImages.Count}");
            Console.WriteLine($"   • Seleccionando las mejores: {selectCount}");

            var selected = sortedImages.Take(selectCount).ToList();

            // Mostrar estadísticas
            if (allImages.Count > 0)
            {
                Console.WriteLine($"   • Rango de calidad: {allImages.Min(img => img.Quality):F2} - {allImages.Max(img => img.Quality):F2}");
                Console.WriteLine($"   • Calidad promedio seleccionadas: {selected.Average(img => img.Quality):F2}");
            }

            return selected;
        }
    }
}