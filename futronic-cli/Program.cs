using Futronic.SDKHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace futronic_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("=== Futronic CLI ===");
                    Console.WriteLine("Uso:");
                    Console.WriteLine("  futronic-cli.exe capture [archivo.tml]    - Capturar huella");
                    Console.WriteLine("  futronic-cli.exe verify archivo.tml       - Verificar huella");
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
                            Console.WriteLine("❌ Especifica el archivo: futronic-cli.exe verify archivo.tml");
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

        static void CaptureFingerprint(string customPath = null)
        {
            var args = Environment.GetCommandLineArgs();
            int samples = GetIntArg(args, "--samples", 5);
            samples = Math.Max(3, Math.Min(10, samples));

            int retries = GetIntArg(args, "--retries", 3);
            bool fast = GetBoolArg(args, "--fast", false);

            string fingerLabel = null;
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], "--finger", StringComparison.OrdinalIgnoreCase))
                    fingerLabel = args[i + 1];

            Console.WriteLine("=== CAPTURA DE HUELLA ===");
            Console.WriteLine($"Muestras: {samples} | FastMode: {fast} | Reintentos: {retries}");
            if (!string.IsNullOrWhiteSpace(fingerLabel))
                Console.WriteLine($"Etiqueta: {fingerLabel}");

            byte[] capturedTemplate = null;
            string errorMessage = null;

            bool TryCaptureOnce(out byte[] templateOut, out int lastResultCodeOut)
            {
                var done = new ManualResetEvent(false);
                byte[] localTemplate = null;
                int localResultCode = 0;

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

                enrollment.OnPutOn += (FTR_PROGRESS p) =>
                {
                    currentSample++;
                    Console.WriteLine($"→ Muestra {currentSample}/{samples}: Apoye el dedo firmemente.");
                };

                enrollment.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    if (currentSample < samples)
                    {
                        Console.WriteLine($"→ Muestra {currentSample} OK. Retire el dedo y vuelva a apoyar.");
                    }
                    else
                    {
                        Console.WriteLine("→ Procesando template final...");
                    }
                };

                enrollment.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Señal ambigua. Limpie el sensor y reposicione el dedo.");
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
                            Console.WriteLine($"✅ Captura exitosa! Template: {localTemplate?.Length ?? 0} bytes");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Captura falló. {GetErrorDescription(result)}");
                        }
                    }
                    finally
                    {
                        done.Set();
                    }
                };

                Console.WriteLine("\nIniciando captura...");
                Console.WriteLine("Para cada muestra, varíe ligeramente la rotación y presión del dedo.");

                enrollment.Enrollment();
                done.WaitOne();

                templateOut = localTemplate;
                lastResultCodeOut = localResultCode;

                return (localTemplate != null && localTemplate.Length > 0);
            }

            // Proceso de captura con reintentos
            int attempts = 0;
            while (attempts <= retries)
            {
                attempts++;
                Console.WriteLine($"\n{'=',40}");
                Console.WriteLine($"INTENTO {attempts} DE {retries + 1}");
                Console.WriteLine($"{'=',40}");

                if (TryCaptureOnce(out capturedTemplate, out int code))
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
                        Console.WriteLine("🔄 Tiempo agotado. Reintente más deliberadamente.");
                        shouldRetry = true;
                        break;
                    default:
                        errorMessage = GetErrorDescription(code);
                        break;
                }

                if (shouldRetry && attempts <= retries)
                {
                    Console.WriteLine($"🔁 Reintentando en 2 segundos... ({retries + 1 - attempts} restantes)");
                    Thread.Sleep(2000);
                }
                else
                {
                    errorMessage = GetErrorDescription(code);
                    break;
                }
            }

            // Guardar resultado
            if (capturedTemplate != null && capturedTemplate.Length > 0)
            {
                string outputFile = customPath;
                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var tag = string.IsNullOrWhiteSpace(fingerLabel) ? "template" : SanitizeFilename(fingerLabel);
                    outputFile = $"{tag}_{stamp}.tml";
                }
                else if (string.IsNullOrWhiteSpace(Path.GetExtension(outputFile)))
                {
                    outputFile += ".tml";
                }

                // Guardar siempre en formato demo
                byte[] demoTemplate = ConvertToDemo(capturedTemplate, Path.GetFileNameWithoutExtension(outputFile));
                File.WriteAllBytes(outputFile, demoTemplate);
                Console.WriteLine($"✅ Template guardado (formato demo): {outputFile}");

                // Guardar metadatos
                var metaPath = Path.ChangeExtension(outputFile, ".meta.txt");
                File.WriteAllText(metaPath,
                    $"finger={fingerLabel ?? "unknown"}\n" +
                    $"samples={samples}\n" +
                    $"fastMode={fast}\n" +
                    $"templateSize={capturedTemplate.Length}\n" +
                    $"created={DateTime.Now:O}\n");
                Console.WriteLine($"📋 Metadatos: {metaPath}");
            }
            else
            {
                Console.WriteLine($"\n❌ No se pudo capturar. {errorMessage}");
                Console.WriteLine("Sugerencias:");
                Console.WriteLine("• Limpie completamente el sensor");
                Console.WriteLine("• Asegúrese de que el dedo esté limpio y seco");
                Console.WriteLine("• Cubra completamente la superficie del sensor");
                Environment.Exit(1);
            }
        }

        static void VerifyFingerprint(string templatePath)
        {
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"❌ No se encuentra: {templatePath}");
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

            Console.WriteLine($"📁 Template cargado: {referenceTemplate.Length} bytes");

            var args = Environment.GetCommandLineArgs();
            int farn = GetIntArg(args, "--farn", 100);
            farn = Math.Max(10, Math.Min(1000, farn));

            int vRetries = GetIntArg(args, "--vretries", 3);
            bool vfast = GetBoolArg(args, "--vfast", false);

            Console.WriteLine("=== VERIFICACIÓN DE HUELLA ===");
            Console.WriteLine($"FARN: {farn} (menor = más tolerante) | Reintentos: {vRetries}");

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
                    Console.WriteLine("🔍 Procesando verificación...");
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
                    Console.WriteLine("   Sugerencia: Varíe ligeramente la posición/rotación del dedo");
                    Thread.Sleep(1000);
                }

                TryVerifyOnce(referenceTemplate, out bool isVerified, out int code, out int fValue);

                if (isVerified)
                {
                    finalVerified = true;
                    finalCode = code;
                    finalFarnValue = fValue;
                    Console.WriteLine($"   ✅ ¡COINCIDENCIA! FAR: {(fValue >= 0 ? fValue.ToString() : "N/D")}");
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
                            confidence = " (MUY CERCA)";
                        else if (fValue <= farn)
                            confidence = " (CERCA)";
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
                Console.WriteLine("✅ Las huellas COINCIDEN");
                if (finalFarnValue >= 0)
                {
                    Console.WriteLine($"📊 FAR: {finalFarnValue} (umbral: {farn})");
                    if (finalFarnValue <= farn / 10)
                        Console.WriteLine("🏆 Calidad: PERFECTA");
                    else if (finalFarnValue <= farn / 2)
                        Console.WriteLine("🥇 Calidad: EXCELENTE");
                    else
                        Console.WriteLine("🥈 Calidad: BUENA");
                }
            }
            else
            {
                Console.WriteLine("❌ VERIFICACIÓN FALLIDA");
                Console.WriteLine("🚫 Las huellas NO coinciden");
                if (finalFarnValue >= 0)
                    Console.WriteLine($"📊 Mejor FAR: {finalFarnValue} (necesario: ≤{farn})");

                Console.WriteLine("\n💡 Sugerencias:");
                Console.WriteLine("• Limpie el sensor completamente");
                Console.WriteLine("• Pruebe diferentes ángulos de rotación");
                Console.WriteLine("• Varíe la presión aplicada");
                Console.WriteLine($"• Use un FARN más alto: --farn {Math.Min(farn * 2, 1000)}");
            }

            Console.WriteLine(new string('=', 50));
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
            foreach (char c in Path.GetInvalidFileNameChars())
                filename = filename.Replace(c, '_');
            return filename.Trim();
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