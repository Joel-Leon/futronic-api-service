using System;
using System.IO;
using System.Text.RegularExpressions;

namespace futronic_cli
{
    public static class FileUtils
    {
        public static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return "";

            foreach (char c in Path.GetInvalidFileNameChars())
                filename = filename.Replace(c, '_');

            foreach (char c in Path.GetInvalidPathChars())
                filename = filename.Replace(c, '_');

            // Eliminar espacios múltiples y trim
            filename = Regex.Replace(filename.Trim(), @"\s+", "_");

            // Limitar longitud
            if (filename.Length > 100)
                filename = filename.Substring(0, 100);

            return filename;
        }

        public static void CreateDirectoryStructure(string registrationDir, string imagesDir)
        {
            Directory.CreateDirectory(registrationDir);
            Directory.CreateDirectory(imagesDir);
        }
    }
}