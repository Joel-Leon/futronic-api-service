using Futronic.SDKHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.Json;

namespace futronic_cli
{
    public class FingerprintCaptureService
    {
        public void CaptureFingerprint(string registrationName, string[] args)
        {
            // Validar nombre del registro
            registrationName = FileUtils.SanitizeFilename(registrationName);
            if (string.IsNullOrWhiteSpace(registrationName))
            {
                Console.WriteLine("❌ Nombre de registro inválido");
                Environment.Exit(1);
            }

            // Configuración
            int samples = ArgumentParser.GetIntArg(args, "--samples", 5);
            samples = Math.Max(3, Math.Min(10, samples));
            int retries = ArgumentParser.GetIntArg(args, "--retries", 3);
            bool fast = ArgumentParser.GetBoolArg(args, "--fast", false);
            string fingerLabel = ArgumentParser.GetStringArg(args, "--finger", "unknown");
            string outputDir = ArgumentParser.GetStringArg(args, "--output-dir", "./registros");

            // Crear estructura de directorios
            string registrationDir = Path.Combine(outputDir, registrationName);
            string imagesDir = Path.Combine(registrationDir, "images");
            FileUtils.CreateDirectoryStructure(registrationDir, imagesDir);

            Console.WriteLine("=== CAPTURA INTELIGENTE DE HUELLA ===");
            Console.WriteLine($"📁 Registro: {registrationName}");
            Console.WriteLine($"📂 Directorio: {registrationDir}");
            Console.WriteLine($"🔬 Muestras objetivo: {samples} | Modo rápido: {fast}");
            Console.WriteLine($"👆 Dedo: {fingerLabel}");

            byte[] capturedTemplate = null;
            List<CapturedImage> allImages = new List<CapturedImage>();
            string errorMessage = null;

            // Proceso de captura con reintentos
            int attempts = 0;
            while (attempts <= retries)
            {
                attempts++;
                Console.WriteLine($"\n{'=',60}");
                Console.WriteLine($"🎯 INTENTO {attempts} DE {retries + 1}");
                Console.WriteLine($"{'=',60}");

                if (TryCaptureOnce(samples, fast, out capturedTemplate, out int code, out allImages))
                {
                    errorMessage = null;
                    break;
                }

                bool shouldRetry = ShouldRetryCapture(code, attempts, retries);
                if (!shouldRetry)
                {
                    errorMessage = ConsoleHelper.GetErrorDescription(code);
                    break;
                }
            }

            // Procesar y guardar resultados
            if (capturedTemplate != null && capturedTemplate.Length > 0)
            {
                SaveCaptureResults(registrationName, registrationDir, imagesDir, capturedTemplate, allImages, samples, fast, retries, fingerLabel);
            }
            else
            {
                ShowCaptureFailure(errorMessage);
                Environment.Exit(1);
            }
        }

        private bool TryCaptureOnce(int samples, bool fast, out byte[] templateOut, out int lastResultCodeOut, out List<CapturedImage> imagesOut)
        {
            var done = new ManualResetEvent(false);
            var captureResult = new CaptureResult();
            List<CapturedImage> localImages = new List<CapturedImage>();

            var enrollment = new FutronicEnrollment
            {
                FakeDetection = false,
                MaxModels = samples
            };

            // Configuraciones optimizadas
            ReflectionHelper.TrySetProperty(enrollment, "FastMode", fast);
            ReflectionHelper.TrySetProperty(enrollment, "FFDControl", true);
            ReflectionHelper.TrySetProperty(enrollment, "FARN", 100);
            ReflectionHelper.TrySetProperty(enrollment, "Version", 0x02030000);
            ReflectionHelper.TrySetProperty(enrollment, "DetectFakeFinger", false);
            ReflectionHelper.TrySetProperty(enrollment, "MIOTOff", 2000);
            ReflectionHelper.TrySetProperty(enrollment, "DetectCore", true);
            ReflectionHelper.TrySetProperty(enrollment, "ImageQuality", 50);

            // Configurar captura de imágenes
            ConfigureImageCapture(enrollment, localImages, captureResult);

            // Configurar eventos
            ConfigureEnrollmentEvents(enrollment, samples, captureResult, done, localImages);

            Console.WriteLine("\n🚀 Iniciando proceso de captura...");
            ConsoleHelper.ShowCaptureInstructions();
            Console.WriteLine();

            enrollment.Enrollment();
            done.WaitOne();

            templateOut = captureResult.Template;
            lastResultCodeOut = captureResult.ResultCode;
            imagesOut = localImages;

            return (captureResult.Template != null && captureResult.Template.Length > 0);
        }

        private void ConfigureImageCapture(FutronicEnrollment enrollment, List<CapturedImage> localImages, CaptureResult captureResult)
        {
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
                                byte[] imageData = ImageUtils.ConvertBitmapToBytes(bitmap);
                                if (imageData != null && imageData.Length > 0)
                                {
                                    double quality = ImageUtils.CalculateImageQuality(imageData);

                                    var capturedImage = new CapturedImage
                                    {
                                        ImageData = imageData,
                                        SampleIndex = captureResult.CurrentSample,
                                        CaptureTime = DateTime.Now,
                                        Quality = quality
                                    };

                                    localImages.Add(capturedImage);
                                    Console.WriteLine($"📸 Imagen capturada - Muestra: {captureResult.CurrentSample}, Calidad: {quality:F2}");
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
        }

        private void ConfigureEnrollmentEvents(FutronicEnrollment enrollment, int samples,
            CaptureResult captureResult, ManualResetEvent done, List<CapturedImage> localImages)
        {
            enrollment.OnPutOn += (FTR_PROGRESS p) =>
            {
                captureResult.CurrentSample++;
                Console.WriteLine($"→ Muestra {captureResult.CurrentSample}/{samples}: Apoye el dedo firmemente.");
                Console.WriteLine("  💡 Consejo: Mantenga presión constante para mejor calidad");
            };

            enrollment.OnTakeOff += (FTR_PROGRESS p) =>
            {
                if (captureResult.CurrentSample < samples)
                {
                    Console.WriteLine($"→ ✅ Muestra {captureResult.CurrentSample} capturada. Retire el dedo completamente.");
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
                    captureResult.ResultCode = result;
                    captureResult.Success = success;
                    if (success)
                    {
                        captureResult.Template = enrollment.Template;
                        Console.WriteLine($"✅ ¡Captura exitosa!");
                        Console.WriteLine($"   📊 Template: {captureResult.Template?.Length ?? 0} bytes");
                        Console.WriteLine($"   📸 Total de imágenes: {localImages.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Captura falló: {ConsoleHelper.GetErrorDescription(result)}");
                    }
                }
                finally
                {
                    done.Set();
                }
            };
        }

        private bool ShouldRetryCapture(int code, int attempts, int retries)
        {
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
                    break;
            }

            if (shouldRetry && attempts <= retries)
            {
                Console.WriteLine($"🔁 Reintentando en 3 segundos... ({retries + 1 - attempts} intentos restantes)");
                Thread.Sleep(3000);
                return true;
            }

            return false;
        }

        private void SaveCaptureResults(string registrationName, string registrationDir, string imagesDir,
            byte[] capturedTemplate, List<CapturedImage> allImages, int samples, bool fast, int retries, string fingerLabel)
        {
            Console.WriteLine($"\n🎉 ¡REGISTRO COMPLETADO EXITOSAMENTE!");
            Console.WriteLine($"{'=',60}");

            // Seleccionar las mejores imágenes
            var selectedImages = ImageUtils.SelectBestImages(allImages, samples);
            Console.WriteLine($"📊 Análisis de calidad:");
            Console.WriteLine($"   • Total capturadas: {allImages.Count} imágenes");
            Console.WriteLine($"   • Seleccionadas: {selectedImages.Count} mejores");

            // Guardar template
            string templatePath = Path.Combine(registrationDir, $"{registrationName}.tml");
            byte[] demoTemplate = TemplateUtils.ConvertToDemo(capturedTemplate, registrationName);
            File.WriteAllBytes(templatePath, demoTemplate);
            Console.WriteLine($"✅ Template guardado: {templatePath}");

            // Guardar imágenes seleccionadas
            SaveSelectedImages(imagesDir, registrationName, selectedImages);

            // Guardar metadatos completos
            SaveMetadata(registrationDir, registrationName, fingerLabel, capturedTemplate, allImages, selectedImages, samples, fast, retries);

            ShowCaptureSuccess(registrationDir, registrationName, selectedImages);
        }

        private void SaveSelectedImages(string imagesDir, string registrationName, List<CapturedImage> selectedImages)
        {
            Console.WriteLine($"\n📸 Guardando imágenes seleccionadas:");
            for (int i = 0; i < selectedImages.Count; i++)
            {
                var img = selectedImages[i];
                string imagePath = Path.Combine(imagesDir, $"{registrationName}_best_{i + 1:D2}.bmp");
                File.WriteAllBytes(imagePath, img.ImageData);
                Console.WriteLine($"   📷 Imagen {i + 1}: calidad {img.Quality:F2} -> {Path.GetFileName(imagePath)}");
            }
        }

        private void SaveMetadata(string registrationDir, string registrationName, string fingerLabel,
            byte[] capturedTemplate, List<CapturedImage> allImages, List<CapturedImage> selectedImages,
            int samples, bool fast, int retries)
        {
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
                    averageQuality = selectedImages.Count > 0 ? selectedImages.Average(img => img.Quality) : 0
                },
                images = selectedImages.Select((img, idx) => new
                {
                    index = idx + 1,
                    quality = img.Quality,
                    sampleIndex = img.SampleIndex,
                    filename = $"{registrationName}_best_{idx + 1:D2}.bmp"
                }).ToArray()
            };

            string jsonMetadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(metaPath, jsonMetadata);
            Console.WriteLine($"📋 Metadatos guardados: {metaPath}");
        }

        private void ShowCaptureSuccess(string registrationDir, string registrationName, List<CapturedImage> selectedImages)
        {
            Console.WriteLine($"\n🏆 RESUMEN DEL REGISTRO:");
            Console.WriteLine($"   📁 Directorio: {registrationDir}");
            Console.WriteLine($"   📄 Template: {registrationName}.tml");
            Console.WriteLine($"   📸 Imágenes: {selectedImages.Count} archivos BMP");
            Console.WriteLine($"   📊 Calidad promedio: {(selectedImages.Count > 0 ? selectedImages.Average(img => img.Quality) : 0):F2}");
            Console.WriteLine($"   🔢 ID único: {registrationName}");
        }

        private void ShowCaptureFailure(string errorMessage)
        {
            Console.WriteLine($"\n❌ REGISTRO FALLIDO");
            Console.WriteLine($"🚫 No se pudo completar la captura: {errorMessage}");
            ConsoleHelper.ShowCaptureSuggestions();
        }
    }
}