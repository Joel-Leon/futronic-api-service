using System;

namespace futronic_cli
{
    public static class ConsoleHelper
    {
        public static void ShowUsage()
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
        }

        public static string GetErrorDescription(int errorCode)
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

        public static void ShowCaptureInstructions()
        {
            Console.WriteLine("📋 Instrucciones:");
            Console.WriteLine("   • Cada muestra debe cubrir completamente el sensor");
            Console.WriteLine("   • Varíe ligeramente la rotación entre muestras");
            Console.WriteLine("   • Mantenga presión firme pero no excesiva");
        }

        public static void ShowVerificationSuggestions(int farn)
        {
            Console.WriteLine("\n💡 Sugerencias para mejorar el reconocimiento:");
            Console.WriteLine("   • Limpie completamente el sensor");
            Console.WriteLine("   • Pruebe diferentes ángulos de rotación");
            Console.WriteLine("   • Varíe la presión aplicada");
            Console.WriteLine($"   • Use un FARN más tolerante: --farn {Math.Min(farn * 2, 1000)}");
        }

        public static void ShowCaptureSuggestions()
        {
            Console.WriteLine("\n💡 Sugerencias para el próximo intento:");
            Console.WriteLine("   • Limpie completamente el sensor con paño suave");
            Console.WriteLine("   • Asegúrese de que el dedo esté limpio y seco (no demasiado)");
            Console.WriteLine("   • Cubra toda la superficie del sensor");
            Console.WriteLine("   • Mantenga el dedo quieto durante cada captura");
            Console.WriteLine("   • Pruebe con diferente dedo si persisten los problemas");
        }
    }
}