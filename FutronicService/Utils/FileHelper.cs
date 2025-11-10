using System;
using System.IO;

namespace FutronicService.Utils
{
    public static class FileHelper
    {
        public static void CreateDirectoryIfNotExists(string path)
        {
        if (!Directory.Exists(path))
            {
      Directory.CreateDirectory(path);
     }
        }

        public static string ValidateAndSanitizePath(string path)
        {
       if (string.IsNullOrWhiteSpace(path))
       {
         throw new ArgumentException("Path cannot be empty");
   }

     // Prevenir path traversal
 string fullPath = Path.GetFullPath(path);
            
 // Validar que no contenga caracteres peligrosos
  if (path.Contains("..") || path.Contains("~"))
   {
    throw new ArgumentException("Invalid path: path traversal not allowed");
     }

 return fullPath;
      }

     public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
   {
   return fileName;
   }

   // Remover caracteres inválidos
         var invalidChars = Path.GetInvalidFileNameChars();
          foreach (var c in invalidChars)
 {
  fileName = fileName.Replace(c, '_');
   }

            return fileName;
        }

    public static bool IsValidTemplateFile(string path)
        {
    if (!File.Exists(path))
            {
     return false;
         }

            if (!path.EndsWith(".tml", StringComparison.OrdinalIgnoreCase))
            {
    return false;
            }

          try
            {
         var fileInfo = new FileInfo(path);
 return fileInfo.Length > 0 && fileInfo.Length < 10 * 1024 * 1024; // Max 10MB
         }
       catch
            {
        return false;
    }
        }
    }
}
