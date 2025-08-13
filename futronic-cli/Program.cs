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
                    Console.WriteLine("  futronic-cli.exe capture [archivo.ftr] [--samples N]            - Capturar huella (enrolar) con N muestras (defecto 5)");
                    Console.WriteLine("  futronic-cli.exe verify archivo.ftr [--farn X]                   - Verificar contra template con FAR solicitado (defecto 1000)");
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

        static void TrySetProperty(object obj, string propertyName, object value)
        {
            try
            {
                var p = obj.GetType().GetProperty(propertyName);
                if (p != null && p.CanWrite)
                {
                    // Si el tipo no coincide exactamente, intenta convertir
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
                            // si no se puede convertir, no seteamos
                            Console.WriteLine($"(i) No se pudo convertir valor para {propertyName}. Se omite.");
                            return;
                        }
                    }

                    p.SetValue(obj, finalValue, null);
                    Console.WriteLine($"(i) Propiedad {propertyName} establecida.");
                }
                else
                {
                    Console.WriteLine($"(i) Propiedad {propertyName} no encontrada o no editable. Se omite.");
                }
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                Console.WriteLine($"(i) Setter {propertyName} lanzó excepción interna: {tie.InnerException?.Message ?? tie.Message}. Se omite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"(i) No se pudo establecer {propertyName}: {ex.Message}. Se omite.");
            }
        }

        static bool GetBoolArg(string[] args, string name, bool defaultValue)
        {
            // --fast  (presencia => true)
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
            // Busca "--samples", "--farn", etc.
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
            // Args
            var args = Environment.GetCommandLineArgs();
            int samples = GetIntArg(args, "--samples", 5);
            if (samples < 1) samples = 1;

            int retries = GetIntArg(args, "--retries", 2);      // reintentos si falla por calidad
            bool fast = GetBoolArg(args, "--fast", false);      // por defecto, robustez > velocidad
            string fingerLabel = null;                          // p.ej. --finger right-index
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], "--finger", StringComparison.OrdinalIgnoreCase))
                    fingerLabel = args[i + 1];

            Console.WriteLine("=== MODO CAPTURA (Enrolamiento) ===");
            Console.WriteLine($"Muestras: {samples} | FastMode: {fast} | Reintentos: {retries}");
            if (!string.IsNullOrWhiteSpace(fingerLabel))
                Console.WriteLine($"Etiqueta de dedo: {fingerLabel}");

            byte[] capturedTemplate = null;
            string errorMessage = null;

            // Función local que intenta una captura completa (con N muestras)
            bool TryCaptureOnce(out byte[] templateOut, out int lastResultCodeOut)
            {
                var done = new ManualResetEvent(false);

                // Usamos variables locales (NO out) para que la lambda pueda asignar
                byte[] localTemplate = null;
                int localResultCode = 0;

                var enrollment = new FutronicEnrollment
                {
                    FakeDetection = false,
                    MaxModels = samples
                };

                // Ajustes opcionales si existen en tu SDK
                TrySetProperty(enrollment, "FastMode", fast);
                TrySetProperty(enrollment, "FFDControl", true);

                enrollment.OnPutOn += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("→ Ponga el dedo firmemente. (muestra en progreso)");
                };

                enrollment.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("→ Retire el dedo. Reposicione ligeramente (rotar 5–10°, variar presión).");
                };

                enrollment.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Posible dedo falso. Ajuste postura/presión.");
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
                            Console.WriteLine($"✅ Enrolamiento OK. Template bytes: {localTemplate?.Length ?? 0}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Enrolamiento falló. {GetErrorDescription(result)}");
                        }
                    }
                    finally
                    {
                        done.Set();
                    }
                };

                Console.WriteLine("Iniciando captura de múltiples muestras…");
                Console.WriteLine("Sugerencia: cada muestra, levante y vuelva a apoyar variando leve orientación/posición.");
                enrollment.Enrollment();
                done.WaitOne();

                // Ahora sí, asignamos a los OUT fuera de la lambda
                templateOut = localTemplate;
                lastResultCodeOut = localResultCode;

                return (localTemplate != null && localTemplate.Length > 0);
            }


            // Intentos con reintentos en códigos típicos de baja calidad/retirada rápida
            int attempts = 0;
            while (attempts <= retries)
            {
                attempts++;
                Console.WriteLine($"\nIntento {attempts} de {retries + 1}");
                if (TryCaptureOnce(out capturedTemplate, out int code))
                {
                    errorMessage = null;
                    break;
                }

                // Reintentar sólo si es un problema típico de captura
                if (code == 11 || code == 203 || code == 4)
                {
                    errorMessage = GetErrorDescription(code);
                    Console.WriteLine("🔁 Reintentaremos la captura. Descanse la mano, limpie el sensor si es necesario.");
                    Thread.Sleep(800);
                    continue;
                }
                else
                {
                    errorMessage = GetErrorDescription(code);
                    break;
                }
            }

            if (capturedTemplate != null && capturedTemplate.Length > 0)
            {
                string outputFile = customPath;
                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var tag = string.IsNullOrWhiteSpace(fingerLabel) ? "template" : SanitizeFilePart(fingerLabel);
                    outputFile = $"{tag}_{stamp}.ftr";
                }
                string fullPath = Path.GetFullPath(outputFile);
                File.WriteAllBytes(fullPath, capturedTemplate);
                Console.WriteLine($"✅ Template guardado: {fullPath}");

                // Guarda metadatos útiles al lado (opcional)
                var metaPath = Path.ChangeExtension(fullPath, ".meta.txt");
                File.WriteAllText(metaPath,
                    $"finger={fingerLabel ?? "unknown"}\n" +
                    $"samples={samples}\n" +
                    $"fastMode={fast}\n" +
                    $"created={DateTime.Now:O}\n");
                Console.WriteLine($"🗒 Metadatos: {metaPath}");
            }
            else
            {
                Console.WriteLine($"❌ No se generó template. {errorMessage}");
                Environment.Exit(1);
            }
        }

        // Limpia nombre de archivo
        static string SanitizeFilePart(string s)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s.Trim();
        }


        static void VerifyFingerprint(string templatePath)
        {
            // 1) Validar y cargar template base de referencia
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"❌ No se encuentra el archivo: {templatePath}");
                Environment.Exit(1);
            }

            byte[] referenceTemplate = File.ReadAllBytes(templatePath);
            Console.WriteLine($"📁 Template de referencia cargado: {referenceTemplate.Length} bytes");

            // 2) Flags
            var args = Environment.GetCommandLineArgs();
            int farn = GetIntArg(args, "--farn", 1500);          // un poco más laxo que 1000
            if (farn < 1) farn = 1;
            int vRetries = GetIntArg(args, "--vretries", 2);     // reintentos de verificación
            bool vfast = GetBoolArg(args, "--vfast", false);     // por defecto robusto

            Console.WriteLine("=== MODO VERIFICACIÓN (SDK Matcher) ===");
            Console.WriteLine($"Config: FARN={farn} | Reintentos={vRetries} | FastMode={vfast}");

            // 3) Función local: corre UNA verificación
            // Corre UNA verificación
            bool TryVerifyOnce(byte[] baseTemplate, out bool verified, out int resultCode, out int farnValue)
            {
                var done = new ManualResetEvent(false);

                // <<< usar locales dentro de la lambda >>>
                bool localVerified = false;
                int localResultCode = 0;
                int localFarnValue = -1;

                var verifier = new FutronicVerification(baseTemplate);

                // Ajustes (si existen en tu SDK; TrySetProperty evita romper si no están)
                // Ajustes (minimalista para evitar crash por setters internos)
                TrySetProperty(verifier, "FARN", farn);

                // Si necesitas probar otros, habilítalos de a uno:
                // TrySetProperty(verifier, "FastMode", vfast);
                // TrySetProperty(verifier, "FakeDetection", false);
                // TrySetProperty(verifier, "FFDControl", true);


                verifier.OnPutOn += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("→ Ponga el dedo (verificación) …");
                };

                verifier.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("→ Retire el dedo. Reposicione (gire 5–10°, varíe presión, cubra el centro).");
                };

                verifier.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Posible dedo falso. Ajuste postura/presión.");
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

                            // Leer FARNValue si existe
                            var pInfo = verifier.GetType().GetProperty("FARNValue");
                            if (pInfo != null && pInfo.CanRead)
                            {
                                try { localFarnValue = (int)pInfo.GetValue(verifier, null); } catch { localFarnValue = -1; }
                            }
                        }
                        else
                        {
                            localVerified = false;
                        }
                    }
                    finally
                    {
                        done.Set();
                    }
                };

                Console.WriteLine("Iniciando verificación…");
                verifier.Verification();
                done.WaitOne();

                // <<< asignar a los OUT fuera de la lambda >>>
                verified = localVerified;
                resultCode = localResultCode;
                farnValue = localFarnValue;

                return true; // la ejecución completó (independiente de si hubo match)
            }

            // 4) Intentos
            bool finalVerified = false;
            int finalCode = 0;
            int finalFarnValue = -1;

            for (int attempt = 0; attempt <= vRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Console.WriteLine($"\n🔁 Reintento {attempt} de {vRetries} — Reposicione/rote ligeramente el dedo.");
                    Thread.Sleep(500);
                }

                TryVerifyOnce(referenceTemplate, out bool ok, out int code, out int fValue);
                finalVerified = ok;
                finalCode = code;
                finalFarnValue = fValue;

                Console.WriteLine($"Resultado intento {attempt + 1}: {(ok ? "✅ Coinciden" : "❌ No coinciden")} | FAR alcanzado: {(fValue >= 0 ? fValue.ToString() : "N/D")}");

                if (ok) break;

                // Si fue un error típico de captura, permitimos reintentar
                if (code == 11 || code == 203 || code == 4)
                {
                    continue;
                }
                // Si simplemente no coincidió, aún reintentamos hasta agotar vRetries
            }

            // 5) Resultado final
            Console.WriteLine("\n=== RESULTADO FINAL ===");
            if (finalVerified)
            {
                Console.WriteLine("✅ ¡HUELLAS COINCIDEN! (matcher SDK)");
                Console.WriteLine($"ℹ FAR alcanzado: {(finalFarnValue >= 0 ? finalFarnValue.ToString() : "N/D")}");
            }
            else
            {
                Console.WriteLine("❌ HUELLAS NO COINCIDEN (matcher SDK)");
                Console.WriteLine($"ℹ FAR alcanzado: {(finalFarnValue >= 0 ? finalFarnValue.ToString() : "N/D")}");
                if (finalCode != 0)
                    Console.WriteLine($"Detalles: {GetErrorDescription(finalCode)}");
                Console.WriteLine("Sugerencias: varíe presión, cubra el centro, rote 5–10°, limpie el sensor.");
            }

            Console.WriteLine("\nPresione una tecla para salir.");
            Console.ReadKey();
        }


        // Helper para leer FARNValue si existe; si no, muestra "N/D"
        static string GetFarnValueSafe(object verifier)
        {
            var p = verifier.GetType().GetProperty("FARNValue");
            if (p != null && p.CanRead)
            {
                try
                {
                    var v = p.GetValue(verifier, null);
                    return v?.ToString() ?? "N/D";
                }
                catch { }
            }
            return "N/D";
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