using Futronic.SDKHelper;
using System;
using System.IO;
using System.Threading;

namespace futronic_cli
{
    public class FingerprintVerificationService
    {
        public void VerifyFingerprint(string registrationName, string[] args)
        {
            registrationName = FileUtils.SanitizeFilename(registrationName);
            string outputDir = ArgumentParser.GetStringArg(args, "--output-dir", "./registros");
            string registrationDir = Path.Combine(outputDir, registrationName);
            string templatePath = Path.Combine(registrationDir, $"{registrationName}.tml");

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"❌ No se encuentra el registro: {registrationName}");
                Console.WriteLine($"   📁 Buscado en: {templatePath}");
                Environment.Exit(1);
            }

            byte[] fileData = File.ReadAllBytes(templatePath);
            byte[] referenceTemplate = TemplateUtils.ExtractFromDemo(fileData);

            if (referenceTemplate == null)
            {
                Console.WriteLine("❌ Error: archivo no es formato demo válido");
                Environment.Exit(1);
            }

            Console.WriteLine($"=== VERIFICACIÓN DE HUELLA ===");
            Console.WriteLine($"📁 Registro: {registrationName}");
            Console.WriteLine($"📄 Template cargado: {referenceTemplate.Length} bytes");

            var farn = ArgumentParser.GetIntArg(args, "--farn", 100);
            farn = Math.Max(10, Math.Min(1000, farn));

            int vRetries = ArgumentParser.GetIntArg(args, "--vretries", 3);
            bool vfast = ArgumentParser.GetBoolArg(args, "--vfast", false);

            Console.WriteLine($"🔧 Configuración: FARN={farn} | Reintentos={vRetries}");

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

                TryVerifyOnce(referenceTemplate, farn, vfast, out bool isVerified, out int code, out int fValue);

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

                    string confidence = GetConfidenceLevel(fValue, farn);
                    Console.WriteLine($"   ❌ Sin coincidencia. FAR: {(fValue >= 0 ? fValue.ToString() : "N/D")}{confidence}");

                    // Continuar si hay esperanza o error de captura
                    if (ShouldRetryVerification(code, fValue, farn))
                        continue;
                }
            }

            ShowVerificationResult(finalVerified, registrationName, finalFarnValue, farn);
        }

        private bool TryVerifyOnce(byte[] baseTemplate, int farn, bool vfast, out bool verified, out int resultCode, out int farnValue)
        {
            var done = new ManualResetEvent(false);
            var verificationResult = new VerificationResult();

            var verifier = new FutronicVerification(baseTemplate);

            // Configuración optimizada
            ReflectionHelper.TrySetProperty(verifier, "FARN", farn);
            ReflectionHelper.TrySetProperty(verifier, "FastMode", vfast);
            ReflectionHelper.TrySetProperty(verifier, "FakeDetection", false);
            ReflectionHelper.TrySetProperty(verifier, "FFDControl", true);
            ReflectionHelper.TrySetProperty(verifier, "MIOTOff", 3000);
            ReflectionHelper.TrySetProperty(verifier, "DetectCore", true);
            ReflectionHelper.TrySetProperty(verifier, "Version", 0x02030000);
            ReflectionHelper.TrySetProperty(verifier, "ImageQuality", 30);

            ConfigureVerificationEvents(verifier, verificationResult, done);

            verifier.Verification();
            done.WaitOne();

            verified = verificationResult.Verified;
            resultCode = verificationResult.ResultCode;
            farnValue = verificationResult.FarnValue;
            return true;
        }

        private void ConfigureVerificationEvents(FutronicVerification verifier, VerificationResult verificationResult, ManualResetEvent done)
        {
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
                    verificationResult.ResultCode = result;
                    verificationResult.Success = success;
                    if (success)
                    {
                        verificationResult.Verified = verificationSuccess;

                        // Obtener FAR si está disponible
                        try
                        {
                            var pInfo = verifier.GetType().GetProperty("FARNValue");
                            if (pInfo != null && pInfo.CanRead)
                            {
                                verificationResult.FarnValue = (int)pInfo.GetValue(verifier, null);
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
        }

        private string GetConfidenceLevel(int fValue, int farn)
        {
            if (fValue < 0) return "";

            if (fValue <= farn / 2)
                return " (MUY CERCA - casi coincide)";
            else if (fValue <= farn)
                return " (CERCA - ajuste menor)";
            else
                return " (lejano)";
        }

        private bool ShouldRetryVerification(int code, int fValue, int farn)
        {
            return code == 11 || code == 203 || code == 4 || (fValue >= 0 && fValue <= farn * 2);
        }

        private void ShowVerificationResult(bool finalVerified, string registrationName, int finalFarnValue, int farn)
        {
            Console.WriteLine("\n" + new string('=', 50));
            if (finalVerified)
            {
                Console.WriteLine("🎉 ¡VERIFICACIÓN EXITOSA!");
                Console.WriteLine($"✅ La huella COINCIDE con el registro '{registrationName}'");
                if (finalFarnValue >= 0)
                {
                    Console.WriteLine($"📊 FAR obtenido: {finalFarnValue} (umbral: {farn})");
                    ShowQualityLevel(finalFarnValue, farn);
                }
            }
            else
            {
                Console.WriteLine("❌ VERIFICACIÓN FALLIDA");
                Console.WriteLine($"🚫 La huella NO coincide con '{registrationName}'");
                if (finalFarnValue >= 0)
                    Console.WriteLine($"📊 Mejor FAR obtenido: {finalFarnValue} (necesario: ≤{farn})");

                ConsoleHelper.ShowVerificationSuggestions(farn);
            }

            Console.WriteLine(new string('=', 50));
        }

        private void ShowQualityLevel(int farnValue, int farn)
        {
            if (farnValue <= farn / 10)
                Console.WriteLine("🏆 Calidad de coincidencia: PERFECTA");
            else if (farnValue <= farn / 2)
                Console.WriteLine("🥇 Calidad de coincidencia: EXCELENTE");
            else
                Console.WriteLine("🥈 Calidad de coincidencia: BUENA");
        }
    }
}