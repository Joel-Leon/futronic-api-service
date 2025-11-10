using System;
using System.Collections.Generic;
using System.Text;

namespace FutronicService.Utils
{
    public static class TemplateUtils
    {
  public static byte[] ConvertToDemo(byte[] rawTemplate, string name)
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
    byte[] nameData = Encoding.ASCII.GetBytes(name);
     Array.Copy(nameData, nameBytes, Math.Min(nameData.Length, 15));
    }
   buffer.AddRange(nameBytes);

            // Template completo
   buffer.AddRange(rawTemplate);

          return buffer.ToArray();
     }

        public static byte[] ExtractFromDemo(byte[] demoTemplate)
        {
      if (demoTemplate == null || demoTemplate.Length <= 20)
   {
      return null;
     }

            // Template crudo empieza en byte 20
     byte[] rawTemplate = new byte[demoTemplate.Length - 20];
 Array.Copy(demoTemplate, 20, rawTemplate, 0, rawTemplate.Length);
    return rawTemplate;
        }
    }
}
