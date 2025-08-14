using Futronic.SDKHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;

namespace futronic_cli
{
    class Program
    {
        // Estructura para almacenar información de cada imagen capturada
        public class CapturedImage
        {
            public byte[] ImageData { get; set; }
            public int SampleIndex { get; set; }
            public DateTime CaptureTime { get; set; }
            public double Quality { get; set; } // Calculado por análisis de imagen
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("=== Futronic CLI - Gestión Inteligente de Huellas ===");
                    Console.WriteLine("Uso:");
                    Console.WriteLine("  futronic-cli.exe capture <nombre_registro> [opciones]");
                    Console.WriteLine("  futronic-cli.exe verify <nombre_registro> [opciones]");
                    Console.WriteLine("\nOpciones de captura:");
                    Console.WriteLine("  --samples N        Número de muestras (3-10, default: 5)");
                    Console.WriteLine("  --fast            Modo rápido");
                    Console.WriteLine("  --finger LABEL    Etiqueta del dedo");
                    Console.WriteLine("  --output-dir DIR  Directorio base (default: './registros')");
                    Console.WriteLine("\nEjemplo:");
                    Console.WriteLine("  futronic-cli.exe capture juan_perez --samples 7 --finger pulgar_derecho");
                    return;
                }

                string command = args[0].ToLower();

                switch (command)
                {
                    case "capture":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Especifica el nombre del registro: futronic-cli.exe capture <nombre>");
                            return;
                        }
                        CaptureFingerprint(args[1], args);
                        break;

                    case "verify":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Especifica el nombre del registro: futronic-cli.exe verify <nombre>");
                            return;
                        }
                        VerifyFingerprint(args[1], args);
                        break;

                    default:
                        Console.WriteLine("❌ Comando desconocido. Use: capture o verify");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void TrySetProperty(object obj, string propertyName, object value)
        {
            try
            {
                var p = obj.GetType().GetProperty(propertyName);
                if (p != null && p.CanWrite)
                {
                    var targetType = p.PropertyType;
                    object finalValue = value;

                    if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                    {
                        try
                        {
                            finalValue = Convert.ChangeType(value, targetType);
                        }
                        catch
                        {
                            return;
                        }
                    }

                    p.SetValue(obj, finalValue, null);
                }
            }
            catch
            {
                // Silenciar errores de configuración
            }
        }

        static bool GetBoolArg(string[] args, string name, bool defaultValue)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = args[i].Substring(name.Length + 1).ToLowerInvariant();
                    if (val == "1" || val == "true" || val == "on" || val == "yes") return true;
                    if (val == "0" || val == "false" || val == "off" || val == "no") return false;
                }
            }
            return defaultValue;
        }

        static int GetIntArg(string[] args, string name, int defaultValue)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int v))
                        return v;
                }
                else if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = args[i].Substring(name.Length + 1);
                    if (int.TryParse(val, out int v)) return v;
                }
            }
            return defaultValue;
        }

        static string GetStringArg(string[] args, string name, string defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return defaultValue;
        }

        static void CaptureFingerprint(string registrationName, string[] args)
        {
            // Validar nombre del registro
            registrationName = SanitizeFilename(registrationName);
            if (string.IsNullOrWhiteSpace(registrationName))
            {
                Console.WriteLine("❌ Nombre de registro inválido");
                Environment.Exit(1);
            }

            // Configuración
            int samples = GetIntArg(args, "--samples", 5);
            samples = Math.Max(3, Math.Min(10, samples));
            int retries = GetIntArg(args, "--retries", 3);
            bool fast = GetBoolArg(args, "--fast", false);
            string fingerLabel = GetStringArg(args, "--finger", "unknown");
            string outputDir = GetStringArg(args, "--output-dir", "./registros");

            // Crear estructura de directorios
            string registrationDir = Path.Combine(outputDir, registrationName);
            string imagesDir = Path.Combine(registrationDir, "images");

            Directory.CreateDirectory(registrationDir);
            Directory.CreateDirectory(imagesDir);

            Console.WriteLine("=== CAPTURA INTELIGENTE DE HUELLA ===");
            Console.WriteLine($"📁 Registro: {registrationName}");
            Console.WriteLine($"📂 Directorio: {registrationDir}");
            Console.WriteLine($"🔬 Muestras objetivo: {samples} | Modo rápido: {fast}");
            Console.WriteLine($"👆 Dedo: {fingerLabel}");

            byte[] capturedTemplate = null;
            List<CapturedImage> allImages = new List<CapturedImage>();
            string errorMessage = null;

            bool TryCaptureOnce(out byte[] templateOut, out int lastResultCodeOut, out List<CapturedImage> imagesOut)
            {
                var done = new ManualResetEvent(false);
                byte[] localTemplate = null;
                int localResultCode = 0;
                List<CapturedImage> localImages = new List<CapturedImage>();

                var enrollment = new FutronicEnrollment
                {
                    FakeDetection = false,
                    MaxModels = samples
                };

                // Configuraciones optimizadas
                TrySetProperty(enrollment, "FastMode", fast);
                TrySetProperty(enrollment, "FFDControl", true);
                TrySetProperty(enrollment, "FARN", 100);
                TrySetProperty(enrollment, "Version", 0x02030000);
                TrySetProperty(enrollment, "DetectFakeFinger", false);
                TrySetProperty(enrollment, "MIOTOff", 2000);
                TrySetProperty(enrollment, "DetectCore", true);
                TrySetProperty(enrollment, "ImageQuality", 50);

                int currentSample = 0;

                // Configurar captura de imágenes
                try
                {
                    var eventInfo = enrollment.GetType().GetEvent("UpdateScreenImage");
                    if (eventInfo != null)
                    {
                        Console.WriteLine("✅ Sistema de captura de imágenes activado");

                        Action<object> imageHandler = (bitmap) =>
                        {
                            try
                            {
                                if (bitmap != null)
                                {
                                    byte[] imageData = ConvertBitmapToBytes(bitmap);
                                    if (imageData != null && imageData.Length > 0)
                                    {
                                        // Calcular calidad de la imagen
                                        double quality = CalculateImageQuality(imageData);

                                        var capturedImage = new CapturedImage
                                        {
                                            ImageData = imageData,
                                            SampleIndex = currentSample,
                                            CaptureTime = DateTime.Now,
                                            Quality = quality
                                        };

                                        localImages.Add(capturedImage);
                                        Console.WriteLine($"📸 Imagen capturada - Muestra: {currentSample}, Calidad: {quality:F2}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Error capturando imagen: {ex.Message}");
                            }
                        };

                        var handlerType = eventInfo.EventHandlerType;
                        var convertedHandler = Delegate.CreateDelegate(handlerType, imageHandler.Target, imageHandler.Method);
                        eventInfo.AddEventHandler(enrollment, convertedHandler);
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Captura de imágenes no disponible");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error configurando imágenes: {ex.Message}");
                }

                enrollment.OnPutOn += (FTR_PROGRESS p) =>
                {
                    currentSample++;
                    Console.WriteLine($"→ Muestra {currentSample}/{samples}: Apoye el dedo firmemente.");
                    Console.WriteLine("  💡 Consejo: Mantenga presión constante para mejor calidad");
                };

                enrollment.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    if (currentSample < samples)
                    {
                        Console.WriteLine($"→ ✅ Muestra {currentSample} capturada. Retire el dedo completamente.");
                        Console.WriteLine("  💡 Para la siguiente: varíe ligeramente rotación y presión");
                    }
                    else
                    {
                        Console.WriteLine("→ 🔄 Procesando template final...");
                    }
                };

                enrollment.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Señal ambigua detectada. Limpie el sensor y reposicione.");
                    return true;
                };

                enrollment.OnEnrollmentComplete += (bool success, int result) =>
                {
                    try
                    {
                        localResultCode = result;
                        if (success)
                        {
                            localTemplate = enrollment.Template;
                            Console.WriteLine($"✅ ¡Captura exitosa!");
                            Console.WriteLine($"   📊 Template: {localTemplate?.Length ?? 0} bytes");
                            Console.WriteLine($"   📸 Total de imágenes: {localImages.Count}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Captura falló: {GetErrorDescription(result)}");
                        }
                    }
                    finally
                    {
                        done.Set();
                    }
                };

                Console.WriteLine("\n🚀 Iniciando proceso de captura...");
                Console.WriteLine("📋 Instrucciones:");
                Console.WriteLine("   • Cada muestra debe cubrir completamente el sensor");
                Console.WriteLine("   • Varíe ligeramente la rotación entre muestras");
                Console.WriteLine("   • Mantenga presión firme pero no excesiva");
                Console.WriteLine();

                enrollment.Enrollment();
                done.WaitOne();

                templateOut = localTemplate;
                lastResultCodeOut = localResultCode;
                imagesOut = localImages;

                return (localTemplate != null && localTemplate.Length > 0);
            }

            // Proceso de captura con reintentos
            int attempts = 0;
            while (attempts <= retries)
            {
                attempts++;
                Console.WriteLine($"\n{'=',60}");
                Console.WriteLine($"🎯 INTENTO {attempts} DE {retries + 1}");
                Console.WriteLine($"{'=',60}");

                if (TryCaptureOnce(out capturedTemplate, out int code, out allImages))
                {
                    errorMessage = null;
                    break;
                }

                bool shouldRetry = false;
                switch (code)
                {
                    case 11:
                        Console.WriteLine("🔄 Calidad insuficiente. Limpie el sensor y apoye más firmemente.");
                        shouldRetry = true;
                        break;
                    case 203:
                        Console.WriteLine("🔄 Dedo retirado muy rápido. Mantenga quieto hasta indicación.");
                        shouldRetry = true;
                        break;
                    case 4:
                        Console.WriteLine("🔄 Tiempo agotado. Reintente con movimientos más controlados.");
                        shouldRetry = true;
                        break;
                    default:
                        errorMessage = GetErrorDescription(code);
                        break;
                }

                if (shouldRetry && attempts <= retries)
                {
                    Console.WriteLine($"🔁 Reintentando en 3 segundos... ({retries + 1 - attempts} intentos restantes)");
                    Thread.Sleep(3000);
                }
                else
                {
                    errorMessage = GetErrorDescription(code);
                    break;
                }
            }

            // Procesar y guardar resultados
            if (capturedTemplate != null && capturedTemplate.Length > 0)
            {
                Console.WriteLine($"\n🎉 ¡REGISTRO COMPLETADO EXITOSAMENTE!");
                Console.WriteLine($"{'=',60}");

                // Seleccionar las mejores imágenes
                var selectedImages = SelectBestImages(allImages, samples);
                Console.WriteLine($"📊 Análisis de calidad:");
                Console.WriteLine($"   • Total capturadas: {allImages.Count} imágenes");
                Console.WriteLine($"   • Seleccionadas: {selectedImages.Count} mejores");

                // Guardar template
                string templatePath = Path.Combine(registrationDir, $"{registrationName}.tml");
                byte[] demoTemplate = ConvertToDemo(capturedTemplate, registrationName);
                File.WriteAllBytes(templatePath, demoTemplate);
                Console.WriteLine($"✅ Template guardado: {templatePath}");

                // Guardar imágenes seleccionadas
                Console.WriteLine($"\n📸 Guardando imágenes seleccionadas:");
                for (int i = 0; i < selectedImages.Count; i++)
                {
                    var img = selectedImages[i];
                    string imagePath = Path.Combine(imagesDir, $"{registrationName}_best_{i + 1:D2}.bmp");
                    File.WriteAllBytes(imagePath, img.ImageData);
                    Console.WriteLine($"   📷 Imagen {i + 1}: calidad {img.Quality:F2} -> {Path.GetFileName(imagePath)}");
                }

                // Guardar metadatos completos
                var metaPath = Path.Combine(registrationDir, "metadata.json");
                var metadata = new
                {
                    registrationName = registrationName,
                    fingerLabel = fingerLabel,
                    captureDate = DateTime.Now.ToString("O"),
                    settings = new
                    {
                        samples = samples,
                        fastMode = fast,
                        retries = retries
                    },
                    results = new
                    {
                        templateSize = capturedTemplate.Length,
                        totalImages = allImages.Count,
                        selectedImages = selectedImages.Count,
                        averageQuality = selectedImages.Average(img => img.Quality)
                    },
                    images = selectedImages.Select((img, idx) => new
                    {
                        index = idx + 1,
                        quality = img.Quality,
                        sampleIndex = img.SampleIndex,
                        filename = $"{registrationName}_best_{idx + 1:D2}.bmp"
                    }).ToArray()
                };

                string jsonMetadata = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(metaPath, jsonMetadata);
                Console.WriteLine($"📋 Metadatos guardados: {metaPath}");

                Console.WriteLine($"\n🏆 RESUMEN DEL REGISTRO:");
                Console.WriteLine($"   📁 Directorio: {registrationDir}");
                Console.WriteLine($"   📄 Template: {registrationName}.tml");
                Console.WriteLine($"   📸 Imágenes: {selectedImages.Count} archivos BMP");
                Console.WriteLine($"   📊 Calidad promedio: {selectedImages.Average(img => img.Quality):F2}");
                Console.WriteLine($"   🔢 ID único: {registrationName}");
            }
            else
            {
                Console.WriteLine($"\n❌ REGISTRO FALLIDO");
                Console.WriteLine($"🚫 No se pudo completar la captura: {errorMessage}");
                Console.WriteLine("\n💡 Sugerencias para el próximo intento:");
                Console.WriteLine("   • Limpie completamente el sensor con paño suave");
                Console.WriteLine("   • Asegúrese de que el dedo esté limpio y seco (no demasiado)");
                Console.WriteLine("   • Cubra toda la superficie del sensor");
                Console.WriteLine("   • Mantenga el dedo quieto durante cada captura");
                Console.WriteLine("   • Pruebe con diferente dedo si persisten los problemas");
                Environment.Exit(1);
            }
        }

        static List<CapturedImage> SelectBestImages(List<CapturedImage> allImages, int targetSamples)
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

        static double CalculateImageQuality(byte[] imageData)
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

        static void VerifyFingerprint(string registrationName, string[] args)
        {
            registrationName = SanitizeFilename(registrationName);
            string outputDir = GetStringArg(args, "--output-dir", "./registros");
            string registrationDir = Path.Combine(outputDir, registrationName);
            string templatePath = Path.Combine(registrationDir, $"{registrationName}.tml");

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"❌ No se encuentra el registro: {registrationName}");
                Console.WriteLine($"   📁 Buscado en: {templatePath}");
                Environment.Exit(1);
            }

            byte[] fileData = File.ReadAllBytes(templatePath);

            // Siempre trabajamos con formato demo
            byte[] referenceTemplate = ExtractFromDemo(fileData);
            if (referenceTemplate == null)
            {
                Console.WriteLine("❌ Error: archivo no es formato demo válido");
                Environment.Exit(1);
            }

            Console.WriteLine($"=== VERIFICACIÓN DE HUELLA ===");
            Console.WriteLine($"📁 Registro: {registrationName}");
            Console.WriteLine($"📄 Template cargado: {referenceTemplate.Length} bytes");

            var farn = GetIntArg(args, "--farn", 100);
            farn = Math.Max(10, Math.Min(1000, farn));

            int vRetries = GetIntArg(args, "--vretries", 3);
            bool vfast = GetBoolArg(args, "--vfast", false);

            Console.WriteLine($"🔧 Configuración: FARN={farn} | Reintentos={vRetries}");

            bool TryVerifyOnce(byte[] baseTemplate, out bool verified, out int resultCode, out int farnValue)
            {
                var done = new ManualResetEvent(false);
                bool localVerified = false;
                int localResultCode = 0;
                int localFarnValue = -1;

                var verifier = new FutronicVerification(baseTemplate);

                // Configuración optimizada
                TrySetProperty(verifier, "FARN", farn);
                TrySetProperty(verifier, "FastMode", vfast);
                TrySetProperty(verifier, "FakeDetection", false);
                TrySetProperty(verifier, "FFDControl", true);
                TrySetProperty(verifier, "MIOTOff", 3000);
                TrySetProperty(verifier, "DetectCore", true);
                TrySetProperty(verifier, "Version", 0x02030000);
                TrySetProperty(verifier, "ImageQuality", 30);

                verifier.OnPutOn += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("👆 Apoye el dedo para verificación...");
                };

                verifier.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("🔍 Analizando coincidencia...");
                };

                verifier.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Señal ambigua. Limpie el sensor.");
                    return true;
                };

                verifier.OnVerificationComplete += (bool success, int result, bool verificationSuccess) =>
                {
                    try
                    {
                        localResultCode = result;
                        if (success)
                        {
                            localVerified = verificationSuccess;

                            // Obtener FAR si está disponible
                            try
                            {
                                var pInfo = verifier.GetType().GetProperty("FARNValue");
                                if (pInfo != null && pInfo.CanRead)
                                {
                                    localFarnValue = (int)pInfo.GetValue(verifier, null);
                                }
                            }
                            catch { }
                        }
                    }
                    finally
                    {
                        done.Set();
                    }
                };

                verifier.Verification();
                done.WaitOne();

                verified = localVerified;
                resultCode = localResultCode;
                farnValue = localFarnValue;
                return true;
            }

            // Proceso de verificación con reintentos
            bool finalVerified = false;
            int finalCode = 0;
            int finalFarnValue = -1;

            Console.WriteLine("\n" + new string('=', 50));
            for (int attempt = 0; attempt <= vRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Console.WriteLine($"\n🔄 Intento {attempt + 1} de {vRetries + 1}");
                    Console.WriteLine("   💡 Varíe ligeramente la posición/rotación del dedo");
                    Thread.Sleep(1000);
                }

                TryVerifyOnce(referenceTemplate, out bool isVerified, out int code, out int fValue);

                if (isVerified)
                {
                    finalVerified = true;
                    finalCode = code;
                    finalFarnValue = fValue;
                    Console.WriteLine($"   ✅ ¡COINCIDENCIA CONFIRMADA! FAR: {(fValue >= 0 ? fValue.ToString() : "N/D")}");
                    break;
                }
                else
                {
                    finalCode = code;
                    if (fValue >= 0) finalFarnValue = fValue;

                    string confidence = "";
                    if (fValue >= 0)
                    {
                        if (fValue <= farn / 2)
                            confidence = " (MUY CERCA - casi coincide)";
                        else if (fValue <= farn)
                            confidence = " (CERCA - ajuste menor)";
                        else
                            confidence = " (lejano)";
                    }

                    Console.WriteLine($"   ❌ Sin coincidencia. FAR: {(fValue >= 0 ? fValue.ToString() : "N/D")}{confidence}");

                    // Continuar si hay esperanza o error de captura
                    if (code == 11 || code == 203 || code == 4 || (fValue >= 0 && fValue <= farn * 2))
                        continue;
                }
            }

            // Resultado final
            Console.WriteLine("\n" + new string('=', 50));
            if (finalVerified)
            {
                Console.WriteLine("🎉 ¡VERIFICACIÓN EXITOSA!");
                Console.WriteLine($"✅ La huella COINCIDE con el registro '{registrationName}'");
                if (finalFarnValue >= 0)
                {
                    Console.WriteLine($"📊 FAR obtenido: {finalFarnValue} (umbral: {farn})");
                    if (finalFarnValue <= farn / 10)
                        Console.WriteLine("🏆 Calidad de coincidencia: PERFECTA");
                    else if (finalFarnValue <= farn / 2)
                        Console.WriteLine("🥇 Calidad de coincidencia: EXCELENTE");
                    else
                        Console.WriteLine("🥈 Calidad de coincidencia: BUENA");
                }
            }
            else
            {
                Console.WriteLine("❌ VERIFICACIÓN FALLIDA");
                Console.WriteLine($"🚫 La huella NO coincide con '{registrationName}'");
                if (finalFarnValue >= 0)
                    Console.WriteLine($"📊 Mejor FAR obtenido: {finalFarnValue} (necesario: ≤{farn})");

                Console.WriteLine("\n💡 Sugerencias para mejorar el reconocimiento:");
                Console.WriteLine("   • Limpie completamente el sensor");
                Console.WriteLine("   • Pruebe diferentes ángulos de rotación");
                Console.WriteLine("   • Varíe la presión aplicada");
                Console.WriteLine($"   • Use un FARN más tolerante: --farn {Math.Min(farn * 2, 1000)}");
            }

            Console.WriteLine(new string('=', 50));
        }

        static byte[] ConvertBitmapToBytes(object bitmap)
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

        static byte[] ConvertToDemo(byte[] rawTemplate, string name)
        {
            var buffer = new List<byte>();

            // Header: primeros 2 bytes del template + padding
            if (rawTemplate.Length >= 2)
                buffer.AddRange(new byte[] { rawTemplate[0], rawTemplate[1] });
            else
                buffer.AddRange(new byte[] { 0x00, 0x00 });

            buffer.AddRange(new byte[] { 0x00, 0x00 }); // Padding

            // Nombre (16 bytes, null-terminated)
            byte[] nameBytes = new byte[16];
            if (!string.IsNullOrEmpty(name))
            {
                byte[] nameData = System.Text.Encoding.ASCII.GetBytes(name);
                Array.Copy(nameData, nameBytes, Math.Min(nameData.Length, 15));
            }
            buffer.AddRange(nameBytes);

            // Template completo
            buffer.AddRange(rawTemplate);

            return buffer.ToArray();
        }

        static byte[] ExtractFromDemo(byte[] demoTemplate)
        {
            if (demoTemplate.Length <= 20)
            {
                Console.WriteLine("⚠️ Archivo demasiado pequeño para formato demo");
                return null;
            }

            // Template crudo empieza en byte 20
            byte[] rawTemplate = new byte[demoTemplate.Length - 20];
            Array.Copy(demoTemplate, 20, rawTemplate, 0, rawTemplate.Length);
            return rawTemplate;
        }

        static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return "";

            foreach (char c in Path.GetInvalidFileNameChars())
                filename = filename.Replace(c, '_');

            foreach (char c in Path.GetInvalidPathChars())
                filename = filename.Replace(c, '_');

            // Eliminar espacios múltiples y trim
            filename = System.Text.RegularExpressions.Regex.Replace(filename.Trim(), @"\s+", "_");

            // Limitar longitud
            if (filename.Length > 100)
                filename = filename.Substring(0, 100);

            return filename;
        }

        static string GetErrorDescription(int errorCode)
        {
            switch (errorCode)
            {
                case 0: return "Sin error";
                case 1: return "Error de dispositivo";
                case 2: return "Dispositivo no disponible";
                case 4: return "Timeout";
                case 11: return "Calidad insuficiente";
                case 203: return "Dedo retirado muy rápido";
                case 204: return "Dedo no detectado";
                case 205: return "Señal débil";
                default: return $"Error {errorCode}";
            }
        }
    }
}