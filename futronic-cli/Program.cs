using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Futronic.SDKHelper;

namespace futronic_cli
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        static void Main(string[] args)
        {
            try
            {
                // SetDllDirectory(@"C:\Program Files (x86)\Futronic\SDK 4.2\Bin\x64");

                if (args.Length == 0)
                {
                    Console.WriteLine("=== Futronic CLI ===");
                    Console.WriteLine("Uso:");
                    Console.WriteLine("  futronic-cli.exe capture [archivo.ftr]  - Capturar huella");
                    Console.WriteLine("  futronic-cli.exe verify archivo.ftr     - Verificar contra template");
                    return;
                }

                string command = args[0].ToLower();

                switch (command)
                {
                    case "capture":
                        string outputPath = args.Length > 1 ? args[1] : null;
                        CaptureFingerprint(outputPath);
                        break;

                    case "verify":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Especifica el archivo template: futronic-cli.exe verify archivo.ftr");
                            return;
                        }
                        VerifyFingerprint(args[1]);
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

        static void CaptureFingerprint(string customPath = null)
        {
            var done = new ManualResetEvent(false);
            byte[] capturedTemplate = null;
            string errorMessage = null;

            var enrollment = new FutronicEnrollment
            {
                FakeDetection = false,
                MaxModels = 1,
                MIOTControlOff = true
            };

            enrollment.OnPutOn += (FTR_PROGRESS progress) =>
            {
                Console.WriteLine("→ Ponga el dedo...");
            };

            enrollment.OnTakeOff += (FTR_PROGRESS progress) =>
            {
                Console.WriteLine("→ Retire el dedo...");
            };

            enrollment.OnFakeSource += (FTR_PROGRESS progress) =>
            {
                Console.WriteLine("⚠ Dedo falso detectado");
                return true;
            };

            enrollment.OnEnrollmentComplete += (bool success, int result) =>
            {
                try
                {
                    if (success)
                    {
                        capturedTemplate = enrollment.Template;
                        Console.WriteLine($"✅ Captura OK. Bytes: {capturedTemplate?.Length ?? 0}");
                    }
                    else
                    {
                        errorMessage = GetErrorDescription(result);
                        Console.WriteLine($"❌ Captura falló. {errorMessage}");
                    }
                }
                finally
                {
                    done.Set();
                }
            };

            Console.WriteLine("=== MODO CAPTURA ===");
            Console.WriteLine("Iniciando captura...");
            enrollment.Enrollment();
            done.WaitOne();

            if (capturedTemplate != null && capturedTemplate.Length > 0)
            {
                string outputFile = customPath ?? $"template_{DateTime.Now:yyyyMMdd_HHmmss}.ftr";
                string fullPath = Path.GetFullPath(outputFile);

                File.WriteAllBytes(fullPath, capturedTemplate);
                Console.WriteLine($"✅ Template guardado: {fullPath}");
            }
            else
            {
                Console.WriteLine($"❌ No se generó template. {errorMessage}");
                Environment.Exit(1);
            }
        }

        static void VerifyFingerprint(string templatePath)
        {
            // Verificar que existe el template de referencia
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"❌ No se encuentra el archivo: {templatePath}");
                Environment.Exit(1);
            }

            byte[] referenceTemplate = File.ReadAllBytes(templatePath);
            Console.WriteLine($"📁 Template de referencia cargado: {referenceTemplate.Length} bytes");

            // Capturar huella actual para comparar
            var captureComplete = new ManualResetEvent(false);
            byte[] currentTemplate = null;
            string errorMessage = null;

            var enrollment = new FutronicEnrollment
            {
                FakeDetection = false,
                MaxModels = 1,
                MIOTControlOff = true
            };

            enrollment.OnPutOn += (FTR_PROGRESS progress) =>
            {
                Console.WriteLine("→ Ponga el dedo para verificar...");
            };

            enrollment.OnTakeOff += (FTR_PROGRESS progress) =>
            {
                Console.WriteLine("→ Retire el dedo...");
            };

            enrollment.OnEnrollmentComplete += (bool success, int result) =>
            {
                try
                {
                    if (success)
                    {
                        currentTemplate = enrollment.Template;
                        Console.WriteLine($"✅ Huella actual capturada: {currentTemplate?.Length ?? 0} bytes");
                    }
                    else
                    {
                        errorMessage = GetErrorDescription(result);
                        Console.WriteLine($"❌ Error capturando huella actual: {errorMessage}");
                    }
                }
                finally
                {
                    captureComplete.Set();
                }
            };

            Console.WriteLine("=== MODO VERIFICACIÓN ===");
            Console.WriteLine("Capture su huella para verificar...");
            enrollment.Enrollment();
            captureComplete.WaitOne();

            if (currentTemplate != null && currentTemplate.Length > 0)
            {
                // Realizar verificación comparando templates directamente
                try
                {
                    Console.WriteLine("🔍 Comparando huellas...");

                    // Comparar directamente los templates
                    bool templatesMatch = CompareTemplates(currentTemplate, referenceTemplate);

                    Console.WriteLine("=== RESULTADO ===");
                    if (templatesMatch)
                    {
                        Console.WriteLine("✅ ¡HUELLAS COINCIDEN! ✅");
                        Console.WriteLine("La huella verificada pertenece al mismo dedo.");
                    }
                    else
                    {
                        Console.WriteLine("❌ HUELLAS NO COINCIDEN");
                        Console.WriteLine("La huella no pertenece al mismo dedo.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en verificación: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"❌ No se pudo capturar huella actual. {errorMessage}");
            }

            Console.WriteLine("\nPresione una tecla para salir.");
            Console.ReadKey();
        }

        // Función auxiliar para comparar templates (aproximación básica)
        static bool CompareTemplates(byte[] template1, byte[] template2)
        {
            if (template1.Length != template2.Length)
            {
                Console.WriteLine($"🔍 Tamaños diferentes: {template1.Length} vs {template2.Length}");
                return false;
            }

            // Comparación exacta (para templates idénticos)
            bool exactMatch = true;
            for (int i = 0; i < template1.Length; i++)
            {
                if (template1[i] != template2[i])
                {
                    exactMatch = false;
                    break;
                }
            }

            if (exactMatch)
            {
                Console.WriteLine("🔍 Coincidencia exacta: 100%");
                return true;
            }

            // Comparación por similitud (contamos diferencias)
            int differences = 0;
            for (int i = 0; i < template1.Length; i++)
            {
                if (template1[i] != template2[i])
                    differences++;
            }

            // Si menos del 10% de bytes son diferentes, consideramos que coincide
            double similarity = 1.0 - (double)differences / template1.Length;
            Console.WriteLine($"🔍 Similitud: {similarity:P1} ({differences} diferencias de {template1.Length})");

            return similarity > 0.9; // 90% de similitud
        }

        static string GetErrorDescription(int errorCode)
        {
            switch (errorCode)
            {
                case 203: return "Código 203: Dedo retirado demasiado rápido o calidad insuficiente";
                case 1: return "Código 1: Error de dispositivo";
                case 2: return "Código 2: Dispositivo no conectado";
                case 4: return "Código 4: Timeout - operación cancelada";
                case 11: return "Código 11: Calidad de imagen insuficiente";
                default: return $"Código {errorCode}: Error desconocido";
            }
        }
    }
}