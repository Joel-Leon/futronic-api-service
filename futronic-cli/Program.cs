using System;

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
                    ConsoleHelper.ShowUsage();
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
                        var captureService = new FingerprintCaptureService();
                        captureService.CaptureFingerprint(args[1], args);
                        break;

                    case "verify":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Especifica el nombre del registro: futronic-cli.exe verify <nombre>");
                            return;
                        }
                        var verifyService = new FingerprintVerificationService();
                        verifyService.VerifyFingerprint(args[1], args);
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
    }
}