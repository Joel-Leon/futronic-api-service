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
                    Console.WriteLine("  futronic-cli.exe capture [archivo.tml] [--samples N]            - Enrolar y guardar en .tml (defecto 7 muestras)");
                    Console.WriteLine("  futronic-cli.exe verify archivo.tml [--farn X]                   - Verificar contra .tml con FAR solicitado (defecto 100)");
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
                            Console.WriteLine($"(i) No se pudo convertir valor para {propertyName}. Se omite.");
                            return;
                        }
                    }

                    p.SetValue(obj, finalValue, null);
                    Console.WriteLine($"(i) Propiedad {propertyName} establecida: {value}");
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
            int samples = GetIntArg(args, "--samples", 7); // Aumentado para mejor calidad
            if (samples < 3) samples = 3; // Mínimo recomendado
            if (samples > 10) samples = 10; // Máximo práctico

            int retries = GetIntArg(args, "--retries", 3);
            bool fast = GetBoolArg(args, "--fast", false);
            string fingerLabel = null;
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], "--finger", StringComparison.OrdinalIgnoreCase))
                    fingerLabel = args[i + 1];

            Console.WriteLine("=== MODO CAPTURA AVANZADO (Enrolamiento Robusto) ===");
            Console.WriteLine($"Muestras: {samples} | FastMode: {fast} | Reintentos: {retries}");
            if (!string.IsNullOrWhiteSpace(fingerLabel))
                Console.WriteLine($"Etiqueta de dedo: {fingerLabel}");

            byte[] capturedTemplate = null;
            string errorMessage = null;

            bool TryCaptureOnce(out byte[] templateOut, out int lastResultCodeOut)
            {
                var done = new ManualResetEvent(false);

                byte[] localTemplate = null;
                int localResultCode = 0;

                var enrollment = new FutronicEnrollment
                {
                    FakeDetection = false, // Mejor compatibilidad
                    MaxModels = samples
                };

                // Configuraciones para mejor reconocimiento
                TrySetProperty(enrollment, "FastMode", fast);
                TrySetProperty(enrollment, "FFDControl", true);
                TrySetProperty(enrollment, "FARN", 100); // Más tolerante que el default
                
                // Configuraciones adicionales para robustez
                TrySetProperty(enrollment, "Version", 0x02030000); // Usar versión compatible
                TrySetProperty(enrollment, "DetectFakeFinger", false); // Evitar falsos positivos
                TrySetProperty(enrollment, "MIOTOff", 2000); // Timeout más generoso
                TrySetProperty(enrollment, "DetectCore", true); // Mejorar detección del núcleo
                
                // Configuraciones de calidad de imagen
                TrySetProperty(enrollment, "ImageQuality", 50); // Calidad mínima más baja
                TrySetProperty(enrollment, "MaxImageSize", 0); // Sin límite de tamaño de imagen

                int currentSample = 0;

                enrollment.OnPutOn += (FTR_PROGRESS p) =>
                {
                    currentSample++;
                    Console.WriteLine($"→ Muestra {currentSample}/{samples}: Apoye el dedo firmemente cubriendo toda la superficie.");
                    Console.WriteLine("  Consejo: Centre el dedo, presione uniformemente.");
                };

                enrollment.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    if (currentSample < samples)
                    {
                        Console.WriteLine($"→ Muestra {currentSample} capturada. Retire el dedo completamente.");
                        Console.WriteLine($"  Para la siguiente muestra: rote ligeramente el dedo (5-15°) y varíe la presión.");
                    }
                    else
                    {
                        Console.WriteLine("→ Procesando template final...");
                    }
                };

                enrollment.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Señal ambigua detectada. Limpie el sensor y reposicione el dedo.");
                    return true; // Continuar a pesar de la advertencia
                };

                enrollment.OnEnrollmentComplete += (bool success, int result) =>
                {
                    try
                    {
                        localResultCode = result;
                        if (success)
                        {
                            localTemplate = enrollment.Template;
                            Console.WriteLine($"✅ Enrolamiento exitoso! Template: {localTemplate?.Length ?? 0} bytes");
                            
                            // Información adicional del template
                            try 
                            {
                                var version = enrollment.GetType().GetProperty("Version")?.GetValue(enrollment);
                                if (version != null) 
                                    Console.WriteLine($"   Versión del template: 0x{version:X8}");
                            } 
                            catch { }
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

                Console.WriteLine("\nIniciando captura de múltiples muestras...");
                Console.WriteLine("IMPORTANTE: Para cada muestra, varíe ligeramente:");
                Console.WriteLine("• Rotación del dedo (5-15 grados)");
                Console.WriteLine("• Presión aplicada (firme pero sin exceso)");
                Console.WriteLine("• Posición vertical (cubrir diferentes áreas)");
                Console.WriteLine();

                enrollment.Enrollment();
                done.WaitOne();

                templateOut = localTemplate;
                lastResultCodeOut = localResultCode;

                return (localTemplate != null && localTemplate.Length > 0);
            }

            // Intentos con manejo mejorado de errores
            int attempts = 0;
            while (attempts <= retries)
            {
                attempts++;
                Console.WriteLine($"\n{'=',50}");
                Console.WriteLine($"INTENTO {attempts} DE {retries + 1}");
                Console.WriteLine($"{'=',50}");
                
                if (TryCaptureOnce(out capturedTemplate, out int code))
                {
                    errorMessage = null;
                    break;
                }

                // Análisis más detallado de errores
                bool shouldRetry = false;
                switch (code)
                {
                    case 11: // Calidad insuficiente
                        Console.WriteLine("🔄 Calidad de imagen insuficiente. Limpie el sensor y apoye el dedo más firmemente.");
                        shouldRetry = true;
                        break;
                    case 203: // Retirada rápida
                        Console.WriteLine("🔄 Dedo retirado muy rápido. Mantenga el dedo quieto hasta que se indique retirar.");
                        shouldRetry = true;
                        break;
                    case 4: // Timeout
                        Console.WriteLine("🔄 Tiempo agotado. Reintente con movimientos más deliberados.");
                        shouldRetry = true;
                        break;
                    default:
                        errorMessage = GetErrorDescription(code);
                        break;
                }

                if (shouldRetry && attempts <= retries)
                {
                    Console.WriteLine($"🔁 Reintentando en 2 segundos... ({retries + 1 - attempts} intentos restantes)");
                    Thread.Sleep(2000);
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
                    outputFile = $"{tag}_{stamp}.tml";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Path.GetExtension(outputFile)))
                        outputFile = outputFile + ".tml";
                }

                string fullPath = Path.GetFullPath(outputFile);
                File.WriteAllBytes(fullPath, capturedTemplate);
                Console.WriteLine($"\n✅ Template guardado exitosamente: {fullPath}");

                // Metadatos expandidos
                var metaPath = Path.ChangeExtension(fullPath, ".meta.txt");
                File.WriteAllText(metaPath,
                    $"finger={fingerLabel ?? "unknown"}\n" +
                    $"samples={samples}\n" +
                    $"fastMode={fast}\n" +
                    $"templateSize={capturedTemplate.Length}\n" +
                    $"created={DateTime.Now:O}\n" +
                    $"captureQuality=enhanced\n" +
                    $"sdkVersion=robust\n");
                Console.WriteLine($"📋 Metadatos guardados: {metaPath}");
            }
            else
            {
                Console.WriteLine($"\n❌ No se pudo generar el template. {errorMessage}");
                Console.WriteLine("Sugerencias:");
                Console.WriteLine("• Limpie completamente el sensor");
                Console.WriteLine("• Asegúrese de que el dedo esté seco pero no demasiado");
                Console.WriteLine("• Cubra completamente la superficie del sensor");
                Console.WriteLine("• Mantenga el dedo quieto durante cada captura");
                Environment.Exit(1);
            }
        }

        static string SanitizeFilePart(string s)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s.Trim();
        }

        static void VerifyFingerprint(string templatePath)
        {
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"❌ No se encuentra el archivo: {templatePath}");
                Environment.Exit(1);
            }

            byte[] referenceTemplate = File.ReadAllBytes(templatePath);
            Console.WriteLine($"📁 Template de referencia cargado: {referenceTemplate.Length} bytes");

            var args = Environment.GetCommandLineArgs();
            int farn = GetIntArg(args, "--farn", 100); // Más tolerante por defecto
            
            // Rango ajustado para mejor usabilidad
            if (farn < 10) farn = 10;
            if (farn > 1000)
            {
                Console.WriteLine($"(i) --farn {farn} ajustado a 1000 (máximo permitido).");
                farn = 1000;
            }
            
            int vRetries = GetIntArg(args, "--vretries", 4); // Más intentos
            bool vfast = GetBoolArg(args, "--vfast", false);

            Console.WriteLine("=== MODO VERIFICACIÓN AVANZADO (Matcher Robusto) ===");
            Console.WriteLine($"Config: FARN={farn} (más bajo = más tolerante) | Reintentos={vRetries}");
            Console.WriteLine($"FastMode={vfast} | Algoritmo=Robusto");

            bool TryVerifyOnce(byte[] baseTemplate, out bool verified, out int resultCode, out int farnValue)
            {
                var done = new ManualResetEvent(false);

                bool localVerified = false;
                int localResultCode = 0;
                int localFarnValue = -1;

                var verifier = new FutronicVerification(baseTemplate);

                // Configuraciones para verificación robusta
                TrySetProperty(verifier, "FARN", farn);
                TrySetProperty(verifier, "FastMode", vfast);
                TrySetProperty(verifier, "FakeDetection", false); // Mejor compatibilidad
                TrySetProperty(verifier, "FFDControl", true);
                
                // Configuraciones adicionales para mayor tolerancia
                TrySetProperty(verifier, "MIOTOff", 3000); // Timeout más generoso
                TrySetProperty(verifier, "DetectCore", true); // Mejor detección del núcleo
                TrySetProperty(verifier, "Version", 0x02030000); // Versión compatible
                
                // Configuraciones de calidad más permisivas
                TrySetProperty(verifier, "ImageQuality", 30); // Calidad mínima más baja
                TrySetProperty(verifier, "MaxImageSize", 0); // Sin límite de tamaño

                verifier.OnPutOn += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("👆 Apoye el dedo para verificación...");
                    Console.WriteLine("   Consejo: No necesita ser exactamente igual que en la captura.");
                    Console.WriteLine("   El sistema es tolerante a rotaciones y variaciones de presión.");
                };

                verifier.OnTakeOff += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("🔍 Procesando verificación...");
                };

                verifier.OnFakeSource += (FTR_PROGRESS p) =>
                {
                    Console.WriteLine("⚠ Señal ambigua. Limpie el sensor si es necesario.");
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

                            // Obtener información adicional
                            try
                            {
                                var pInfo = verifier.GetType().GetProperty("FARNValue");
                                if (pInfo != null && pInfo.CanRead)
                                {
                                    localFarnValue = (int)pInfo.GetValue(verifier, null);
                                }
                            }
                            catch { localFarnValue = -1; }
                            
                            // Mostrar información de calidad si está disponible
                            try 
                            {
                                var qualityProp = verifier.GetType().GetProperty("Quality");
                                if (qualityProp != null && qualityProp.CanRead)
                                {
                                    var quality = qualityProp.GetValue(verifier, null);
                                    if (quality != null)
                                        Console.WriteLine($"   Calidad de coincidencia: {quality}");
                                }
                            }
                            catch { }
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

                verifier.Verification();
                done.WaitOne();

                verified = localVerified;
                resultCode = localResultCode;
                farnValue = localFarnValue;

                return true;
            }

            // Proceso de verificación con múltiples intentos
            bool finalVerified = false;
            int finalCode = 0;
            int finalFarnValue = -1;
            int bestFarnValue = int.MaxValue;

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("INICIANDO PROCESO DE VERIFICACIÓN");
            Console.WriteLine(new string('=', 60));

            for (int attempt = 0; attempt <= vRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Console.WriteLine($"\n🔄 Intento {attempt + 1} de {vRetries + 1}");
                    Console.WriteLine("   Sugerencias para este intento:");
                    
                    switch (attempt % 4)
                    {
                        case 1:
                            Console.WriteLine("   • Rote el dedo ligeramente hacia la izquierda");
                            break;
                        case 2:
                            Console.WriteLine("   • Rote el dedo ligeramente hacia la derecha");
                            break;
                        case 3:
                            Console.WriteLine("   • Varíe la presión (más firme o más suave)");
                            break;
                        default:
                            Console.WriteLine("   • Cambie ligeramente la posición vertical del dedo");
                            break;
                    }
                    Thread.Sleep(1000);
                }
                else
                {
                    Console.WriteLine($"🚀 Intento inicial...");
                }

                TryVerifyOnce(referenceTemplate, out bool isVerified, out int code, out int fValue);
                
                // Actualizar mejores resultados
                if (isVerified)
                {
                    finalVerified = true;
                    finalCode = code;
                    finalFarnValue = fValue;
                    Console.WriteLine($"   ✅ ¡COINCIDENCIA ENCONTRADA! FAR: {(fValue >= 0 ? fValue.ToString() : "N/D")}");
                    break;
                }
                else
                {
                    finalCode = code;
                    if (fValue >= 0 && fValue < bestFarnValue)
                    {
                        bestFarnValue = fValue;
                        finalFarnValue = fValue;
                    }
                    
                    string confidence = "";
                    if (fValue >= 0)
                    {
                        if (fValue == 0)
                            confidence = " (sin puntos de coincidencia detectados)";
                        else if (fValue <= farn / 2)
                            confidence = " (MUY CERCA - posición ligeramente diferente)";
                        else if (fValue <= farn)
                            confidence = " (CERCA - ajuste menor necesario)";
                        else if (fValue <= farn * 2)
                            confidence = " (moderadamente cerca)";
                        else if (fValue <= farn * 5)
                            confidence = " (algo lejano)";
                        else
                            confidence = " (muy lejano)";
                    }
                    
                    Console.WriteLine($"   ❌ Sin coincidencia. FAR: {(fValue >= 0 ? fValue.ToString() : "N/D")}{confidence}");
                }

                // Análisis de si vale la pena reintentar
                if (!isVerified && (code == 11 || code == 203 || code == 4 || fValue <= farn * 3))
                {
                    continue; // Reintentar por error de captura o resultado prometedor
                }
            }

            // Resultado final con análisis detallado
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("RESULTADO FINAL DE VERIFICACIÓN");
            Console.WriteLine(new string('=', 60));
            
            if (finalVerified)
            {
                Console.WriteLine("🎉 ¡VERIFICACIÓN EXITOSA!");
                Console.WriteLine("✅ Las huellas dactilares COINCIDEN");
                Console.WriteLine($"📊 FAR alcanzado: {(finalFarnValue >= 0 ? finalFarnValue.ToString() : "N/D")}");
                Console.WriteLine($"🎯 Umbral configurado: {farn}");
                
                // Análisis mejorado del FAR
                if (finalFarnValue >= 0)
                {
                    if (finalFarnValue <= farn)
                    {
                        Console.WriteLine("✨ COINCIDENCIA EXCELENTE (dentro del umbral)");
                        if (finalFarnValue <= farn / 10)
                            Console.WriteLine("🏆 Calidad de coincidencia: PERFECTA");
                        else if (finalFarnValue <= farn / 2)
                            Console.WriteLine("🥇 Calidad de coincidencia: EXCELENTE");
                        else
                            Console.WriteLine("🥈 Calidad de coincidencia: BUENA");
                    }
                    else
                    {
                        // Esto no debería pasar si finalVerified es true, pero por seguridad
                        Console.WriteLine("⚠️ Coincidencia fuera del umbral configurado");
                    }
                    
                    // Información técnica adicional
                    Console.WriteLine($"ℹ️  Interpretación FAR: Menor valor = mejor coincidencia");
                    Console.WriteLine($"ℹ️  Rango típico: 1-1000 (tu resultado: {finalFarnValue})");
                }
            }
            else
            {
                Console.WriteLine("❌ VERIFICACIÓN FALLIDA");
                Console.WriteLine("🚫 Las huellas dactilares NO coinciden");
                Console.WriteLine($"📊 Mejor FAR obtenido: {(finalFarnValue >= 0 ? finalFarnValue.ToString() : "N/D")}");
                Console.WriteLine($"🎯 Umbral requerido: {farn}");
                
                if (finalCode != 0)
                    Console.WriteLine($"🔧 Detalles técnicos: {GetErrorDescription(finalCode)}");
                
                Console.WriteLine("\n💡 SUGERENCIAS PARA MEJORAR EL RECONOCIMIENTO:");
                Console.WriteLine("• Limpie completamente el sensor con un paño suave");
                Console.WriteLine("• Asegúrese de que el dedo no esté demasiado húmedo o seco");
                Console.WriteLine("• Pruebe diferentes ángulos de rotación (±15 grados)");
                Console.WriteLine("• Varíe la presión aplicada");
                Console.WriteLine("• Centre bien el dedo en el sensor");
                Console.WriteLine($"• Considere usar un FARN más alto (actual: {farn}, pruebe: {Math.Min(farn * 2, 1000)})");
            }

            Console.WriteLine($"\n{new string('=', 60)}");
            Console.WriteLine("Presione cualquier tecla para salir...");
            Console.ReadKey(true);
        }

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
                case 0: return "Sin error";
                case 1: return "Error de dispositivo o comunicación";
                case 2: return "Dispositivo no conectado o no disponible";
                case 4: return "Timeout - operación cancelada o muy lenta";
                case 11: return "Calidad de imagen insuficiente para procesamiento";
                case 203: return "Dedo retirado demasiado rápido o señal inestable";
                case 204: return "No se detectó dedo en el sensor";
                case 205: return "Señal demasiado débil o sensor sucio";
                default: return $"Error código {errorCode} (consulte documentación SDK)";
            }
        }
    }
}