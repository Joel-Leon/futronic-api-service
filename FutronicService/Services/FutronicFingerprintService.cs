using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Futronic.SDKHelper;
using FutronicService.Models;
using FutronicService.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReflectionHelper = FutronicService.Utils.ReflectionHelper;

namespace FutronicService.Services
{
    public class FutronicFingerprintService : IFingerprintService
    {
        private readonly ILogger<FutronicFingerprintService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IProgressNotificationService _progressNotification;
        private DateTime _serviceStartTime;
        private string _lastError;
        private bool _deviceConnected;
        private bool _sdkInitialized;
        private readonly object _deviceLock = new object();

        // Configuraciones
        private int _threshold;
        private int _timeout;
        private string _tempPath;
        private string _capturePath;
        private bool _overwriteExisting;
        private int _maxTemplatesPerIdentify;
        private int _deviceCheckRetries = 3;
        private int _deviceCheckDelayMs = 1000;
        private int _maxRotation = 199; // Valor más restrictivo (166 por defecto en SDK, 199 más estricto)

        public FutronicFingerprintService(
            ILogger<FutronicFingerprintService> logger, 
            IConfiguration configuration,
            IProgressNotificationService progressNotification)
        {
            _logger = logger;
            _configuration = configuration;
            _progressNotification = progressNotification;
            _serviceStartTime = DateTime.Now;
            LoadConfiguration();
            InitializeDevice();
        }

        private void LoadConfiguration()
        {
            _threshold = _configuration.GetValue<int>("Fingerprint:Threshold", 70);
            _timeout = _configuration.GetValue<int>("Fingerprint:Timeout", 30000);
     _tempPath = _configuration.GetValue<string>("Fingerprint:TempPath", "C:/temp/fingerprints");
  _capturePath = _configuration.GetValue<string>("Fingerprint:CapturePath", "C:/temp/fingerprints/captures");
      _overwriteExisting = _configuration.GetValue<bool>("Fingerprint:OverwriteExisting", false);
   _maxTemplatesPerIdentify = _configuration.GetValue<int>("Fingerprint:MaxTemplatesPerIdentify", 500);
        _deviceCheckRetries = _configuration.GetValue<int>("Fingerprint:DeviceCheckRetries", 3);
     _deviceCheckDelayMs = _configuration.GetValue<int>("Fingerprint:DeviceCheckDelayMs", 1000);
    _maxRotation = _configuration.GetValue<int>("Fingerprint:MaxRotation", 199); // 199 es más restrictivo, 166 es el default del SDK

     _logger.LogInformation($"Configuration loaded: Threshold={_threshold}, Timeout={_timeout}, MaxRotation={_maxRotation}, CapturePath={_capturePath}");
        }

     [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
      private void InitializeDevice()
 {
      lock (_deviceLock)
         {
    try
     {
       _logger.LogInformation("=== INITIALIZING FUTRONIC DEVICE ===");
    _sdkInitialized = false;
      _deviceConnected = false;

   // PASO 1: Verificar ensamblado del SDK
   if (!VerifySDKAssembly())
          {
     _logger.LogError("SDK assembly verification failed - cannot proceed");
             _lastError = "SDK no disponible";
   return;
        }

    // PASO 2: Verificar DLLs nativas
     if (!VerifyNativeDLLs())
  {
         _logger.LogWarning("Native DLLs verification failed - device may not work");
 _lastError = "DLLs nativas faltantes";
          }

      // PASO 3: Intentar inicializar SDK con reintentos
        _sdkInitialized = InitializeSDKWithRetries();

      if (_sdkInitialized)
  {
   _deviceConnected = true;
                 _logger.LogInformation("? Futronic SDK initialized successfully");
            _lastError = null;
      }
        else
       {
         _logger.LogError("? Failed to initialize SDK");
    _lastError = "Error de inicialización del SDK";
              }
      }
      catch (AccessViolationException avEx)
       {
       _deviceConnected = false;
        _sdkInitialized = false;
   _lastError = "Error crítico del SDK";

         _logger.LogCritical(avEx, "CRITICAL: AccessViolationException during device initialization");
         _logger.LogError("????????????????????????????????????????????");
        _logger.LogError("  FUTRONIC SDK INITIALIZATION FAILURE");
  _logger.LogError("????????????????????????????????????????????");
            _logger.LogError("Causa probable: El SDK no puede inicializar el callback interno (cbControl)");
            _logger.LogError("");
 _logger.LogError("ACCIONES REQUERIDAS:");
  _logger.LogError("  1. Verificar que el dispositivo Futronic esté conectado por USB");
  _logger.LogError("  2. Reinstalar el driver de Futronic (desde el sitio oficial)");
         _logger.LogError("  3. Copiar ftrapi.dll al directorio de la aplicación");
             _logger.LogError("  4. Reiniciar el servicio Windows");
        _logger.LogError("  5. Si persiste, reiniciar el sistema");
      _logger.LogError("????????????????????????????????????????????");
  }
           catch (Exception ex)
       {
          _deviceConnected = false;
 _sdkInitialized = false;
   _lastError = ex.Message;
 _logger.LogError(ex, "Failed to initialize Futronic device");
     }
            }
   }

    public bool IsDeviceConnected()
      {
            return _deviceConnected && _sdkInitialized;
        }

        private bool VerifySDKAssembly()
        {
try
            {
            var sdkAssembly = typeof(FutronicEnrollment).Assembly;
  _logger.LogInformation($"? SDK Assembly: {sdkAssembly.FullName}");
           _logger.LogInformation($"  Location: {sdkAssembly.Location}");
         _logger.LogInformation($"  Version: {sdkAssembly.GetName().Version}");
     return true;
 }
      catch (Exception ex)
          {
          _logger.LogError(ex, "? Failed to verify SDK assembly");
         return false;
         }
        }

   private bool VerifyNativeDLLs()
{
            try
    {
       var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        _logger.LogInformation($"Application directory: {currentDir}");

       var dllsToCheck = new[]
                {
   "ftrapi.dll",
         "FutronicSDK.dll",
          "msvcr120.dll",
        "msvcp120.dll"
};

      bool allFound = true;
       foreach (var dll in dllsToCheck)
           {
var fullPath = Path.Combine(currentDir, dll);
     if (File.Exists(fullPath))
{
    var fileInfo = new FileInfo(fullPath);
      _logger.LogInformation($"  ? {dll} ({fileInfo.Length:N0} bytes)");
  }
else
           {
      _logger.LogWarning($"  ? {dll} NOT FOUND");
            if (dll == "ftrapi.dll" || dll == "FutronicSDK.dll")
            {
     allFound = false;
   }
         }
    }

   return allFound;
  }
    catch (Exception ex)
       {
     _logger.LogError(ex, "Failed to check native DLLs");
      return false;
         }
        }

      [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
    private bool InitializeSDKWithRetries()
 {
        for (int attempt = 1; attempt <= _deviceCheckRetries; attempt++)
{
           try
           {
     _logger.LogInformation($"SDK initialization attempt {attempt}/{_deviceCheckRetries}");

      using (var testInstance = new FutronicEnrollment())
      {
             _logger.LogInformation($"  ? FutronicEnrollment instance created successfully");

        testInstance.FakeDetection = false;
         testInstance.MaxModels = 1;

             _logger.LogInformation($"? SDK properties accessible");
           return true;
             }
  }
    catch (AccessViolationException avEx)
       {
         _logger.LogError(avEx, $"  ? AccessViolationException on attempt {attempt}");

  if (attempt < _deviceCheckRetries)
            {
        _logger.LogInformation($"  ? Waiting {_deviceCheckDelayMs}ms before retry...");
          Thread.Sleep(_deviceCheckDelayMs);
      }
        }
       catch (Exception ex)
      {
 _logger.LogError(ex, $"  ? SDK initialization failed on attempt {attempt}");

               if (attempt < _deviceCheckRetries)
{
               Thread.Sleep(_deviceCheckDelayMs);
     }
        }
       }

            _logger.LogError($"SDK initialization failed after {_deviceCheckRetries} attempts");
            return false;
        }

        // ============================================
        // MÉTODOS PRINCIPALES DE LA INTERFAZ
        // ============================================

  public async Task<ApiResponse<CaptureResponseData>> CaptureAsync(CaptureRequest request)
    {
      return await Task.Run(() =>
            {
           try
   {
       if (!IsDeviceConnected())
    {
     return ApiResponse<CaptureResponseData>.ErrorResponse(
"Dispositivo no conectado o SDK no inicializado",
    "DEVICE_NOT_CONNECTED"
         );
        }

  _logger.LogInformation("Starting fingerprint capture with image...");
     Console.WriteLine($"\n{"=",-60}");
  Console.WriteLine($"=== CAPTURA DE HUELLA (SIN REGISTRO) ===");
     Console.WriteLine($"{"=",-60}");

// Usar enrollment con 1 muestra para capturar imagen
              var enrollResult = EnrollFingerprintInternal(1, request.Timeout);

      if (enrollResult == null || enrollResult.Template == null)
     {
        return ApiResponse<CaptureResponseData>.ErrorResponse(
       "Error al capturar huella",
   "CAPTURE_FAILED"
);
        }

 // Crear estructura de carpetas para la captura
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string captureId = $"capture_{timestamp}";
      string captureDir = Path.Combine(_capturePath, captureId);
          string imagesDir = Path.Combine(captureDir, "images");
     Directory.CreateDirectory(captureDir);
       Directory.CreateDirectory(imagesDir);

              // Guardar template en formato .tml
     string templatePath = Path.Combine(captureDir, $"{captureId}.tml");
       byte[] demoTemplate = TemplateUtils.ConvertToDemo(enrollResult.Template, captureId);
    File.WriteAllBytes(templatePath, demoTemplate);

        // Guardar imagen si fue capturada
       string imagePath = null;
   if (enrollResult.CapturedImages != null && enrollResult.CapturedImages.Count > 0)
           {
 var bestImage = enrollResult.CapturedImages.OrderByDescending(img => img.Quality).First();
             imagePath = Path.Combine(imagesDir, $"{captureId}.bmp");
   File.WriteAllBytes(imagePath, bestImage.ImageData);
     
           Console.WriteLine($"\n?? Imagen capturada:");
     Console.WriteLine($"   ?? Calidad: {bestImage.Quality:F2}");
        Console.WriteLine($"?? Ruta: {imagePath}");
          }

  Console.WriteLine($"\n? Huella capturada y guardada (TEMPORAL)");
    Console.WriteLine($"   ?? Template: {Path.GetFileName(templatePath)}");
  Console.WriteLine($"   ?? Directorio: {captureDir}");
 Console.WriteLine($"   ?? Tamaño: {demoTemplate.Length} bytes");
        Console.WriteLine($"   ??  Esta captura es temporal y no está asociada a ningún DNI\n");

     var responseData = new CaptureResponseData
       {
   TemplatePath = templatePath,
        ImagePath = imagePath,
   Quality = enrollResult.Quality,
Timestamp = DateTime.Now.ToString("o")
     };

       return ApiResponse<CaptureResponseData>.SuccessResponse("Huella capturada exitosamente", responseData);
      }
    catch (Exception ex)
 {
   _logger.LogError(ex, "Error in CaptureAsync");
Console.WriteLine($"? Error: {ex.Message}");
     return ApiResponse<CaptureResponseData>.ErrorResponse(
     ex.Message,
                 "CAPTURE_ERROR"
         );
    }
});
        }

        public async Task<ApiResponse<RegisterResponseData>> RegisterAsync(RegisterRequest request)
        {
            return await Task.Run(() =>
 {
          return ApiResponse<RegisterResponseData>.ErrorResponse(
          "Use el endpoint /api/fingerprint/register-multi para registro",
        "DEPRECATED_ENDPOINT"
    );
            });
      }

        public async Task<ApiResponse<VerifyResponseData>> VerifyAsync(VerifyRequest request)
        {
  return await Task.Run(() =>
    {
      return ApiResponse<VerifyResponseData>.ErrorResponse(
            "Use el endpoint /api/fingerprint/verify-simple para verificación",
         "DEPRECATED_ENDPOINT"
          );
      });
        }

        public async Task<ApiResponse<IdentifyResponseData>> IdentifyAsync(IdentifyRequest request)
        {
            return await Task.Run(() =>
        {
 return ApiResponse<IdentifyResponseData>.ErrorResponse(
    "Use el endpoint /api/fingerprint/identify-live para identificación",
          "DEPRECATED_ENDPOINT"
        );
   });
     }

        public async Task<ApiResponse<HealthResponseData>> GetHealthAsync()
     {
return await Task.Run(() =>
       {
    try
           {
            var health = new HealthResponseData
            {
       Status = IsDeviceConnected() ? "healthy" : "degraded",
  Uptime = (DateTime.Now - _serviceStartTime).ToString(@"dd\.hh\:mm\:ss"),
        DeviceConnected = _deviceConnected,
      SdkInitialized = _sdkInitialized,
             LastError = _lastError,
        DeviceModel = "Futronic",
     SdkVersion = "2.3.0"
     };

 return ApiResponse<HealthResponseData>.SuccessResponse("Estado del servicio obtenido", health);
         }
     catch (Exception ex)
   {
         _logger.LogError(ex, "Error in GetHealthAsync");
 return ApiResponse<HealthResponseData>.ErrorResponse(
              ex.Message,
     "HEALTH_CHECK_ERROR"
          );
       }
    });
        }

        public ApiResponse<ConfigResponseData> GetConfig()
        {
  try
 {
      var config = new ConfigResponseData
   {
         Threshold = _threshold,
          Timeout = _timeout,
          TempPath = _tempPath,
         OverwriteExisting = _overwriteExisting,
    MaxRotation = _maxRotation
    };

         return ApiResponse<ConfigResponseData>.SuccessResponse("Configuración obtenida", config);
     }
    catch (Exception ex)
{
           _logger.LogError(ex, "Error in GetConfig");
       return ApiResponse<ConfigResponseData>.ErrorResponse(
         ex.Message,
"CONFIG_ERROR"
      );
 }
   }

        public ApiResponse<ConfigResponseData> UpdateConfig(UpdateConfigRequest request)
        {
         try
            {
                if (request.Threshold.HasValue)
     _threshold = request.Threshold.Value;

 if (request.Timeout.HasValue)
  _timeout = request.Timeout.Value;

       if (!string.IsNullOrEmpty(request.TempPath))
        _tempPath = request.TempPath;

   if (request.OverwriteExisting.HasValue)
         _overwriteExisting = request.OverwriteExisting.Value;

    if (request.MaxRotation.HasValue)
        _maxRotation = request.MaxRotation.Value;

    _logger.LogInformation("Configuration updated successfully");

      return GetConfig();
            }
catch (Exception ex)
            {
    _logger.LogError(ex, "Error in UpdateConfig");
  return ApiResponse<ConfigResponseData>.ErrorResponse(
   ex.Message,
    "UPDATE_CONFIG_ERROR"
     );
}
    }

      public async Task<ApiResponse<VerifySimpleResponseData>> VerifySimpleAsync(VerifySimpleRequest request)
        {
 return await Task.Run(() =>
  {
     try
          {
     if (!IsDeviceConnected())
 {
          return ApiResponse<VerifySimpleResponseData>.ErrorResponse(
   "Dispositivo no conectado",
            "DEVICE_NOT_CONNECTED"
    );
               }

            _logger.LogInformation($"Starting simple verification for DNI: {request.Dni}");
Console.WriteLine($"\n{"=",-60}");
    Console.WriteLine($"=== VERIFICACIÓN DE IDENTIDAD ===");
        Console.WriteLine($"{"=",-60}");
  Console.WriteLine($"?? DNI: {request.Dni}");
  Console.WriteLine($"?? Dedo: {request.Dedo ?? "index"}");

      // Determinar ruta del template: {tempPath}/{dni}/{dedo}/{dni}.tml
         string templatePath = request.StoredTemplatePath;
        if (string.IsNullOrEmpty(templatePath))
            {
            string dedo = request.Dedo ?? "index";
       templatePath = Path.Combine(_tempPath, request.Dni, dedo, $"{request.Dni}.tml");
         }

    if (!File.Exists(templatePath))
          {
        _logger.LogWarning($"Template not found: {templatePath}");
   Console.WriteLine($"? No existe huella registrada para DNI {request.Dni}");
         return ApiResponse<VerifySimpleResponseData>.ErrorResponse(
  $"No existe huella registrada para DNI {request.Dni}",
                 "FILE_NOT_FOUND"
     );
   }

               Console.WriteLine($"?? Template cargado: {Path.GetFileName(templatePath)}");
     byte[] demoTemplate = File.ReadAllBytes(templatePath);
                 byte[] referenceTemplate = TemplateUtils.ExtractFromDemo(demoTemplate);

       if (referenceTemplate == null)
   {
    Console.WriteLine("? Error: archivo no es formato .tml válido");
          return ApiResponse<VerifySimpleResponseData>.ErrorResponse(
   "Template en formato inválido",
    "INVALID_TEMPLATE"
   );
           }

         // Capturar huella
             var captureResult = CaptureFingerprintInternal(request.Timeout ?? _timeout);
            if (captureResult == null || captureResult.Template == null)
       {
                return ApiResponse<VerifySimpleResponseData>.ErrorResponse(
         "Error al capturar huella",
        "CAPTURE_FAILED"
       );
            }

          // Verificar
        var verifyResult = VerifyTemplatesInternal(referenceTemplate, captureResult.Template);

     Console.WriteLine($"\n?? RESULTADO DE VERIFICACIÓN:");
      Console.WriteLine($"   • Coincide: {(verifyResult.Verified ? "SÍ ?" : "NO ?")}");
       Console.WriteLine($"   • Score FAR: {verifyResult.Score}");
          Console.WriteLine($"   • Umbral: {_threshold}");
            Console.WriteLine($"   • Calidad captura: {captureResult.Quality}\n");

        var responseData = new VerifySimpleResponseData
        {
   Dni = request.Dni,
           Dedo = request.Dedo ?? "index",
   Verified = verifyResult.Verified,
        Score = verifyResult.Score,
     Threshold = _threshold,
            CaptureQuality = captureResult.Quality,
        TemplatePath = templatePath
 };

                    _logger.LogInformation($"Simple verification result: Verified={verifyResult.Verified}, Score={verifyResult.Score}");
     return ApiResponse<VerifySimpleResponseData>.SuccessResponse(
  verifyResult.Verified ? $"Verificación exitosa para {request.Dni}" : "Las huellas no coinciden",
              responseData
  );
 }
                catch (Exception ex)
      {
        _logger.LogError(ex, $"Error in VerifySimpleAsync for DNI: {request.Dni}");
        Console.WriteLine($"? Error: {ex.Message}");
         return ApiResponse<VerifySimpleResponseData>.ErrorResponse(
    ex.Message,
          "VERIFICATION_ERROR"
    );
    }
            });
        }

        public async Task<ApiResponse<RegisterMultiSampleResponseData>> RegisterMultiSampleAsync(RegisterMultiSampleRequest request)
  {
       return await Task.Run(async () =>
            {
    try
                {
                    if (!IsDeviceConnected())
                    {
                        return ApiResponse<RegisterMultiSampleResponseData>.ErrorResponse(
                            "Dispositivo no conectado",
                            "DEVICE_NOT_CONNECTED"
                        );
                    }

                    // ? VERIFICAR PRIMERO SI YA EXISTE LA HUELLA (antes de capturar)
                    string outputPath = request.OutputPath ?? _tempPath;
                    string dedo = request.Dedo ?? "index";
                    string registrationDir = Path.Combine(outputPath, request.Dni, dedo);
                    string templatePath = Path.Combine(registrationDir, $"{request.Dni}.tml");

                    if (File.Exists(templatePath) && !_overwriteExisting)
                    {
                        _logger.LogWarning($"Template already exists for DNI: {request.Dni}, finger: {dedo}");
                        _logger.LogInformation($"Template path: {templatePath}");
                        
                        Console.WriteLine($"\n?? Ya existe una huella registrada para DNI {request.Dni} dedo {dedo}");
                        Console.WriteLine($"   ?? Archivo: {templatePath}");
                        Console.WriteLine($"   ?? Use la opción 'overwriteExisting' para sobrescribir\n");
                        
                        // ? Mensaje simplificado para el frontend (sin detalles técnicos)
                        return ApiResponse<RegisterMultiSampleResponseData>.ErrorResponse(
                            $"Ya existe una huella registrada para este DNI y dedo",
                            "FILE_EXISTS"
                        );
                    }

                    int sampleCount = Math.Min(request.SampleCount ?? 5, 10);
                    _logger.LogInformation($"Starting multi-sample registration for DNI: {request.Dni} with {sampleCount} samples");
                    Console.WriteLine($"\n{"=",-60}");
                    Console.WriteLine($"=== REGISTRO DE HUELLA ===");
                    Console.WriteLine($"{"=",-60}");
                    Console.WriteLine($"?? DNI: {request.Dni}");
                    Console.WriteLine($"?? Dedo: {request.Dedo ?? "index"}");
                    Console.WriteLine($"?? Muestras: {sampleCount}");

                    // ? Notificar inicio de operación por SignalR
                    await _progressNotification.NotifyStartAsync(request.Dni, "registro de huella");

                    // Usar FutronicEnrollment para crear un template de múltiples muestras con SignalR
                    var enrollResult = EnrollFingerprintInternal(sampleCount, request.Timeout ?? _timeout, request.Dni);
                    if (enrollResult == null || enrollResult.Template == null)
                    {
                        return ApiResponse<RegisterMultiSampleResponseData>.ErrorResponse(
                            "Error al registrar huella con múltiples muestras",
                            "ENROLLMENT_FAILED"
                        );
                    }

                    // Crear directorios
                    string imagesDir = Path.Combine(registrationDir, "images");
                    Directory.CreateDirectory(registrationDir);
                    Directory.CreateDirectory(imagesDir);

                    // Guardar template en formato .tml
                    byte[] demoTemplate = TemplateUtils.ConvertToDemo(enrollResult.Template, request.Dni);
                    File.WriteAllBytes(templatePath, demoTemplate);
                    _logger.LogInformation($"? Template guardado: {templatePath}");
                    Console.WriteLine($"\n? Template guardado: {templatePath}");

       // Seleccionar y guardar las mejores imágenes
    var selectedImages = ImageUtils.SelectBestImages(enrollResult.CapturedImages, sampleCount);
 _logger.LogInformation($"?? Seleccionadas {selectedImages.Count} mejores imágenes");
     Console.WriteLine($"\n?? Análisis de calidad:");
  Console.WriteLine($"   • Total capturadas: {enrollResult.CapturedImages.Count} imágenes");
    Console.WriteLine($"   • Seleccionadas: {selectedImages.Count} mejores");

       var imagePaths = new List<string>();
           for (int i = 0; i < selectedImages.Count; i++)
     {
       var img = selectedImages[i];
    string imagePath = Path.Combine(imagesDir, $"{request.Dni}_best_{i + 1:D2}.bmp");
      File.WriteAllBytes(imagePath, img.ImageData);
          imagePaths.Add(imagePath);
     Console.WriteLine($"   ?? Imagen {i + 1}: calidad {img.Quality:F2} -> {Path.GetFileName(imagePath)}");
          }

 // Guardar metadatos
      var metadataPath = Path.Combine(registrationDir, "metadata.json");
         var metadata = new
               {
         registrationName = request.Dni,
          fingerLabel = dedo,
                 captureDate = DateTime.Now.ToString("O"),
        settings = new
                  {
samples = sampleCount,
      threshold = _threshold,
         timeout = request.Timeout ?? _timeout
    },
           results = new
          {
    templateSize = enrollResult.Template.Length,
               totalImages = enrollResult.CapturedImages.Count,
  selectedImages = selectedImages.Count,
          averageQuality = selectedImages.Count > 0 ? selectedImages.Average(img => img.Quality) : 0,
   maxQuality = selectedImages.Count > 0 ? selectedImages.Max(img => img.Quality) : 0,
       minQuality = selectedImages.Count > 0 ? selectedImages.Min(img => img.Quality) : 0
             },
      images = selectedImages.Select((img, idx) => new
        {
     index = idx + 1,
    quality = img.Quality,
        sampleIndex = img.SampleIndex,
      filename = $"{request.Dni}_best_{idx + 1:D2}.bmp",
       captureTime = img.CaptureTime.ToString("O")
         }).ToArray()
  };

          string jsonMetadata = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                    File.WriteAllText(metadataPath, jsonMetadata);
        _logger.LogInformation($"?? Metadatos guardados: {metadataPath}");
  Console.WriteLine($"?? Metadatos guardados: {Path.GetFileName(metadataPath)}");

    // Resumen final
         Console.WriteLine($"\n?? RESUMEN DEL REGISTRO:");
      Console.WriteLine($"   ?? Directorio: {registrationDir}");
           Console.WriteLine($"   ?? Template: {request.Dni}.tml");
       Console.WriteLine($"   ?? Imágenes: {selectedImages.Count} archivos BMP");
    if (selectedImages.Count > 0)
             {
            Console.WriteLine($"   ?? Calidad promedio: {selectedImages.Average(img => img.Quality):F2}");
   }
           Console.WriteLine($"   ?? ID único: {request.Dni}\n");

      var responseData = new RegisterMultiSampleResponseData
         {
          Dni = request.Dni,
        Dedo = dedo,
           TemplatePath = templatePath,
      ImagePath = imagePaths.Count > 0 ? imagePaths[0] : null,
  Quality = enrollResult.Quality,
 SamplesCollected = sampleCount,
            SampleQualities = selectedImages.Select(img => img.Quality).ToList(),
    AverageQuality = selectedImages.Count > 0 ? selectedImages.Average(img => img.Quality) : 0
          };

           _logger.LogInformation($"Multi-sample registration successful for DNI: {request.Dni}");
          return ApiResponse<RegisterMultiSampleResponseData>.SuccessResponse(
         $"Huella registrada exitosamente con {sampleCount} muestras",
    responseData
      );
    }
      catch (Exception ex)
{
  _logger.LogError(ex, $"Error in RegisterMultiSampleAsync for DNI: {request.Dni}");
       Console.WriteLine($"? Error: {ex.Message}");
          return ApiResponse<RegisterMultiSampleResponseData>.ErrorResponse(
        ex.Message,
     "REGISTRATION_ERROR"
         );
          }
   });
        }

        public async Task<ApiResponse<IdentifyLiveResponseData>> IdentifyLiveAsync(IdentifyLiveRequest request)
        {
          return await Task.Run(() =>
    {
       try
    {
         if (!IsDeviceConnected())
      {
         return ApiResponse<IdentifyLiveResponseData>.ErrorResponse(
         "Dispositivo no conectado",
    "DEVICE_NOT_CONNECTED"
          );
          }

  string templatesDir = request.TemplatesDirectory ?? _tempPath;
        _logger.LogInformation($"Starting live identification from directory: {templatesDir}");
         Console.WriteLine($"\n{"=",-60}");
          Console.WriteLine($"=== IDENTIFICACIÓN AUTOMÁTICA (1:N) ===");
Console.WriteLine($"{"=",-60}");
     Console.WriteLine($"?? Directorio: {templatesDir}");

         if (!Directory.Exists(templatesDir))
          {
     return ApiResponse<IdentifyLiveResponseData>.ErrorResponse(
        $"Directorio de templates no existe: {templatesDir}",
      "DIRECTORY_NOT_FOUND"
     );
    }

       // Capturar huella
      var captureResult = CaptureFingerprintInternal(request.Timeout ?? _timeout);
   if (captureResult == null || captureResult.Template == null)
     {
        return ApiResponse<IdentifyLiveResponseData>.ErrorResponse(
              "Error al capturar huella",
          "CAPTURE_FAILED"
       );
        }

        // Cargar todos los templates .tml del directorio
                var templateFiles = Directory.GetFiles(templatesDir, "*.tml", SearchOption.AllDirectories);
            _logger.LogInformation($"Found {templateFiles.Length} template files in directory");
 Console.WriteLine($"?? Encontrados {templateFiles.Length} templates en el directorio");
                  Console.WriteLine($"?? Buscando coincidencia...\n");

    IdentifyResultInternal bestMatch = null;
   int templatesProcessed = 0;
          int matchIndex = -1;

       for (int i = 0; i < Math.Min(templateFiles.Length, _maxTemplatesPerIdentify); i++)
             {
             var templateFile = templateFiles[i];
       templatesProcessed++;

     Console.Write($"\r Procesando: {templatesProcessed}/{Math.Min(templateFiles.Length, _maxTemplatesPerIdentify)} templates...");

      try
        {
         // Extraer DNI y dedo de la ruta: {basePath}/{dni}/{dedo}/{dni}.tml
  string templateDir = Path.GetDirectoryName(templateFile);
         string dedo = Path.GetFileName(templateDir);
     string dniDir = Path.GetDirectoryName(templateDir);
      string dni = Path.GetFileName(dniDir);

       byte[] demoTemplate = File.ReadAllBytes(templateFile);
    byte[] referenceTemplate = TemplateUtils.ExtractFromDemo(demoTemplate);

  if (referenceTemplate == null)
          {
               _logger.LogWarning($"Invalid template format: {templateFile}");
           continue;
  }

 var verifyResult = VerifyTemplatesInternal(referenceTemplate, captureResult.Template);

          if (verifyResult.Verified)
       {
                 if (bestMatch == null || verifyResult.Score < bestMatch.Score) // Score más bajo es mejor en FAR
   {
     bestMatch = new IdentifyResultInternal
           {
          Dni = dni,
   Dedo = dedo,
   TemplatePath = templateFile,
        Score = verifyResult.Score
           };
          matchIndex = i;
           }
  }
     }
   catch (Exception ex)
       {
            _logger.LogWarning(ex, $"Error processing template file: {templateFile}");
     }
     }

       Console.WriteLine(); // Nueva línea después del progreso

           if (bestMatch != null)
        {
      Console.WriteLine($"\n?? ¡COINCIDENCIA ENCONTRADA!");
          Console.WriteLine($"   • DNI: {bestMatch.Dni}");
                  Console.WriteLine($"   • Dedo: {bestMatch.Dedo}");
   Console.WriteLine($"   • Score FAR: {bestMatch.Score}");
       Console.WriteLine($"   • Umbral: {_threshold}");
       Console.WriteLine($"   • Posición: {matchIndex + 1} de {templatesProcessed}\n");
          }
        else
      {
              Console.WriteLine($"\n? No se encontró coincidencia");
  Console.WriteLine($"   • Templates comparados: {templatesProcessed}");
 Console.WriteLine($"   • Ninguno alcanzó el umbral: {_threshold}\n");
       }

          var responseData = new IdentifyLiveResponseData
              {
          Matched = bestMatch != null,
     Dni = bestMatch?.Dni,
       Dedo = bestMatch?.Dedo,
            TemplatePath = bestMatch?.TemplatePath,
           Score = bestMatch?.Score ?? 0,
Threshold = _threshold,
              MatchIndex = matchIndex,
      TotalCompared = templatesProcessed
       };

    _logger.LogInformation($"Live identification result: Match={bestMatch != null}, Processed={templatesProcessed}");
   return ApiResponse<IdentifyLiveResponseData>.SuccessResponse(
   bestMatch != null ? $"Identificado: {bestMatch.Dni}" : "No se encontró coincidencia",
         responseData
      );
             }
       catch (Exception ex)
       {
      _logger.LogError(ex, "Error in IdentifyLiveAsync");
           Console.WriteLine($"? Error: {ex.Message}");
                return ApiResponse<IdentifyLiveResponseData>.ErrorResponse(
                    ex.Message,
                    "IDENTIFICATION_ERROR"
                );
     }
       });
        }

        // ============================================
   // MÉTODOS INTERNOS DE CAPTURA Y VERIFICACIÓN
        // ============================================

        private CaptureResult CaptureFingerprintInternal(int timeout)
        {
     lock (_deviceLock)
      {
           try
                {
        _logger.LogInformation("?? Iniciando captura de huella...");
     Console.WriteLine("?? Iniciando captura de huella...");
          Console.WriteLine("?? Apoye el dedo sobre el sensor cuando se indique");

       var captureResult = new CaptureResult();
      var done = new ManualResetEvent(false);

             using (var identification = new FutronicIdentification())
 {
         identification.FakeDetection = false;

         // Configuraciones optimizadas del SDK
         ReflectionHelper.TrySetProperty(identification, "FFDControl", true);
             ReflectionHelper.TrySetProperty(identification, "FARN", _threshold);
   ReflectionHelper.TrySetProperty(identification, "FastMode", false);
       ReflectionHelper.TrySetProperty(identification, "Version", 0x02030000);
           ReflectionHelper.TrySetProperty(identification, "MIOTOff", 3000);
      ReflectionHelper.TrySetProperty(identification, "DetectCore", true);

             // Eventos de progreso
                   identification.OnPutOn += (FTR_PROGRESS p) =>
              {
         _logger.LogInformation("? Apoye el dedo firmemente");
           Console.WriteLine("? Apoye el dedo firmemente sobre el sensor");
        Console.WriteLine("  ?? Mantenga presión constante");
         };

       identification.OnTakeOff += (FTR_PROGRESS p) =>
      {
       _logger.LogInformation("? Procesando...");
        Console.WriteLine("? ?? Procesando huella...");
              };

identification.OnFakeSource += (FTR_PROGRESS p) =>
        {
           _logger.LogWarning("? Señal ambigua detectada");
      Console.WriteLine("? Señal ambigua. Limpie el sensor y reposicione.");
      return true;
            };

         identification.OnGetBaseTemplateComplete += (bool success, int resultCode) =>
   {
         try
 {
          _logger.LogDebug($"Capture complete: Success={success}, ResultCode={resultCode}");

     if (success && resultCode == 0)
   {
 captureResult.Template = identification.BaseTemplate;
             captureResult.Quality = 100;
       captureResult.Success = true;

        _logger.LogInformation($"? Captura exitosa - Template: {captureResult.Template?.Length ?? 0} bytes");
       Console.WriteLine($"? ¡Captura exitosa!");
   Console.WriteLine($"   ?? Template: {captureResult.Template?.Length ?? 0} bytes");
        }
      else
            {
        captureResult.Success = false;
          captureResult.ErrorCode = resultCode;
      _logger.LogWarning($"Capture failed with code: {resultCode}");
            Console.WriteLine($"? Captura falló con código: {resultCode}");
    }
        }
    finally
       {
      done.Set();
           }
     };

       identification.GetBaseTemplate();

       if (!done.WaitOne(timeout))
{
       _logger.LogWarning("Capture timeout");
          Console.WriteLine("? Timeout - No se detectó huella a tiempo");
       return null;
   }

            return captureResult.Success ? captureResult : null;
    }
      }
      catch (Exception ex)
             {
         _logger.LogError(ex, "Error capturing fingerprint");
    Console.WriteLine($"? Error: {ex.Message}");
   return null;
 }
            }
        }

        private EnrollResult EnrollFingerprintInternal(int maxModels, int timeout, string dni = null)
        {
            lock (_deviceLock)
            {
                try
                {
                    _logger.LogInformation($"?? Iniciando registro con {maxModels} muestras...");
                    Console.WriteLine($"\n{"=",-60}");
                    Console.WriteLine($"=== CAPTURA INTELIGENTE DE HUELLA ===");
                    Console.WriteLine($"{"=",-60}");
                    Console.WriteLine($"?? Muestras objetivo: {maxModels}");
                    Console.WriteLine($"?? Siga las instrucciones en pantalla\n");

                    var enrollResult = new EnrollResult();
                    var done = new ManualResetEvent(false);
                    var capturedImages = new List<CapturedImage>();
                    int currentSample = 0;

                    using (var enrollment = new FutronicEnrollment())
                    {
                        enrollment.FakeDetection = false;
                        enrollment.MaxModels = maxModels;

                        // Configuraciones optimizadas del SDK
                        ReflectionHelper.TrySetProperty(enrollment, "FastMode", false);
                        ReflectionHelper.TrySetProperty(enrollment, "FFDControl", true);
                        ReflectionHelper.TrySetProperty(enrollment, "FARN", _threshold);
                        ReflectionHelper.TrySetProperty(enrollment, "Version", 0x02030000);
                        ReflectionHelper.TrySetProperty(enrollment, "DetectFakeFinger", false);
                        ReflectionHelper.TrySetProperty(enrollment, "MIOTOff", 2000);
                        ReflectionHelper.TrySetProperty(enrollment, "DetectCore", true);
                        ReflectionHelper.TrySetProperty(enrollment, "ImageQuality", 50);

                        // Configurar captura de imágenes pasando función para obtener currentSample actualizado
                        ConfigureImageCapture(enrollment, capturedImages, () => currentSample, maxModels, dni);

                        // Eventos de progreso
                        enrollment.OnPutOn += (FTR_PROGRESS p) =>
                        {
                            currentSample++;
                            _logger.LogInformation($"Muestra {currentSample}/{maxModels}");
                            Console.WriteLine($"Muestra {currentSample}/{maxModels}: Apoye el dedo firmemente.");
                            Console.WriteLine("  Consejo: Mantenga presión constante para mejor calidad");
                            
                            // ? Notificar inicio de captura de muestra por SignalR
                            if (!string.IsNullOrEmpty(dni))
                            {
                                _progressNotification.NotifySampleStartedAsync(dni, currentSample, maxModels).Wait();
                            }
                        };

                        enrollment.OnTakeOff += (FTR_PROGRESS p) =>
                        {
                            if (currentSample < maxModels)
                            {
                                _logger.LogInformation($"Muestra {currentSample} capturada");
                                Console.WriteLine($"Muestra {currentSample} capturada. Retire el dedo completamente.");
                                Console.WriteLine("  Para la siguiente: varíe ligeramente rotación y presión");
                            }
                            else
                            {
                                _logger.LogInformation("Procesando template final...");
                                Console.WriteLine("Procesando template final...");
                            }
                        };

                        enrollment.OnFakeSource += (FTR_PROGRESS p) =>
                        {
                            _logger.LogWarning("?? Señal ambigua detectada");
                            Console.WriteLine("?? Señal ambigua detectada. Limpie el sensor y reposicione.");
                            return true;
                        };

                        enrollment.OnEnrollmentComplete += (bool success, int resultCode) =>
                        {
                            try
                            {
                                _logger.LogDebug($"Enrollment complete: Success={success}, ResultCode={resultCode}");

                                enrollResult.ResultCode = resultCode;
                                enrollResult.Success = success;

                                if (success && resultCode == 0)
                                {
                                    enrollResult.Template = enrollment.Template;
                                    enrollResult.Quality = enrollment.Quality;
                                    enrollResult.Success = true;
                                    enrollResult.CapturedImages = capturedImages;

                                    _logger.LogInformation($"? Registro exitoso - Template: {enrollResult.Template?.Length ?? 0} bytes, Imágenes: {capturedImages.Count}");
                                    Console.WriteLine($"\n? ¡Captura exitosa!");
                                    Console.WriteLine($"   ?? Template: {enrollResult.Template?.Length ?? 0} bytes");
                                    Console.WriteLine($"   ??? Total de imágenes: {capturedImages.Count}");

                                    if (capturedImages.Count > 0)
                                    {
                                        Console.WriteLine($"   ? Calidad promedio: {capturedImages.Average(img => img.Quality):F2}");
                                        
                                        // ? Notificar operación completada por SignalR
                                        if (!string.IsNullOrEmpty(dni))
                                        {
                                            _progressNotification.NotifyCompleteAsync(
                                                dni,
                                                true,
                                                "Registro completado exitosamente",
                                                new
                                                {
                                                    samplesCollected = capturedImages.Count,
                                                    averageQuality = capturedImages.Average(img => img.Quality)
                                                }
                                            ).Wait();
                                        }
                                    }
                                }
                                else
                                {
                                    enrollResult.Success = false;
                                    enrollResult.ErrorCode = resultCode;
                                    _logger.LogWarning($"Enrollment failed with code: {resultCode}");
                                    Console.WriteLine($"? Captura falló con código: {resultCode}");
                                    
                                    // ? Notificar error por SignalR
                                    if (!string.IsNullOrEmpty(dni))
                                    {
                                        _progressNotification.NotifyErrorAsync(dni, $"Error en registro: código {resultCode}").Wait();
                                    }
                                }
                            }
                            finally
                            {
                                done.Set();
                            }
                        };

                        Console.WriteLine("\n?? Iniciando proceso de captura...");
                        Console.WriteLine("Instrucciones:");
                        Console.WriteLine("1. Apoye el dedo cuando se indique");
                        Console.WriteLine("  2. Mantenga firme hasta que se le pida retirar");
                        Console.WriteLine("  3. Retire completamente y espere siguiente indicación");
                        Console.WriteLine("  4. Varíe ligeramente la posición en cada muestra\n");

                        enrollment.Enrollment();

                        if (!done.WaitOne(timeout))
                        {
                            _logger.LogWarning("Enrollment timeout");
                            Console.WriteLine("?? Timeout - Proceso interrumpido");
                            
                            // ? Notificar timeout por SignalR
                            if (!string.IsNullOrEmpty(dni))
                            {
                                _progressNotification.NotifyErrorAsync(dni, "Timeout durante el registro").Wait();
                            }
                            
                            return null;
                        }

                        return enrollResult.Success ? enrollResult : null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during enrollment");
                    Console.WriteLine($"? Error: {ex.Message}");
                    
                    // ? Notificar excepción por SignalR
                    if (!string.IsNullOrEmpty(dni))
                    {
                        _progressNotification.NotifyErrorAsync(dni, $"Error durante registro: {ex.Message}").Wait();
                    }
                    
                    return null;
                }
            }
        }

        private void ConfigureImageCapture(FutronicEnrollment enrollment, List<CapturedImage> capturedImages, Func<int> getCurrentSample, int maxModels, string dni = null)
        {
            try
            {
                var eventInfo = enrollment.GetType().GetEvent("UpdateScreenImage");
                if (eventInfo != null)
                {
                    _logger.LogInformation("Sistema de captura de imágenes activado");
                    Console.WriteLine("Sistema de captura de imágenes activado\n");

                    Action<object> imageHandler = (bitmap) =>
                    {
                        try
                        {
                            if (bitmap != null)
                            {
                                byte[] imageData = ImageUtils.ConvertBitmapToBytes(bitmap);
                                if (imageData != null && imageData.Length > 0)
                                {
                                    double quality = ImageUtils.CalculateImageQuality(imageData);
                                    int currentSample = getCurrentSample(); // ? Obtener valor actualizado

                                    var capturedImage = new CapturedImage
                                    {
                                        ImageData = imageData,
                                        SampleIndex = currentSample,
                                        CaptureTime = DateTime.Now,
                                        Quality = quality
                                    };

                                    capturedImages.Add(capturedImage);
                                    Console.WriteLine($"   Imagen capturada - Muestra: {currentSample}, Calidad: {quality:F2}");
                                    
                                    // ? Notificar imagen capturada por SignalR con Base64
                                    if (!string.IsNullOrEmpty(dni))
                                    {
                                        _progressNotification.NotifySampleCapturedAsync(
                                            dni,
                                            currentSample,
                                            maxModels,
                                            quality,
                                            imageData  // ? Enviar imagen en Base64
                                        ).Wait();
                                        
                                        _logger.LogInformation($"Notificación SignalR enviada: Muestra {currentSample}/{maxModels}, Calidad: {quality:F2}, Imagen: {imageData.Length} bytes");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error capturando imagen");
                        }
                    };

                    var handlerType = eventInfo.EventHandlerType;
                    var convertedHandler = Delegate.CreateDelegate(handlerType, imageHandler.Target, imageHandler.Method);
                    eventInfo.AddEventHandler(enrollment, convertedHandler);
                }
                else
                {
                    _logger.LogWarning("Captura de imágenes no disponible");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configurando captura de imágenes");
            }
        }

        private VerifyResultInternal VerifyTemplatesInternal(byte[] referenceTemplate, byte[] capturedTemplate)
        {
            lock (_deviceLock)
         {
              try
  {
     var result = new VerifyResultInternal();
        var done = new ManualResetEvent(false);

 using (var verification = new FutronicVerification(referenceTemplate))
      {
              verification.FakeDetection = false;

    // Configuraciones optimizadas del SDK
           ReflectionHelper.TrySetProperty(verification, "FARN", _threshold);
      ReflectionHelper.TrySetProperty(verification, "FastMode", false);
           ReflectionHelper.TrySetProperty(verification, "FFDControl", true);
     ReflectionHelper.TrySetProperty(verification, "MIOTOff", 3000);
   ReflectionHelper.TrySetProperty(verification, "DetectCore", true);
       ReflectionHelper.TrySetProperty(verification, "Version", 0x02030000);
     ReflectionHelper.TrySetProperty(verification, "ImageQuality", 30);
        ReflectionHelper.TrySetProperty(verification, "MaxRotation", _maxRotation); // Control de rotación máxima (más restrictivo)

     verification.OnVerificationComplete += (bool success, int resultCode, bool verificationSuccess) =>
       {
             try
       {
      _logger.LogDebug($"Verification complete: Success={success}, ResultCode={resultCode}, Verified={verificationSuccess}");

  if (success)
         {
         result.Verified = verificationSuccess;
       result.Success = true;

            // Obtener FAR/score si está disponible
           try
         {
            var pInfo = verification.GetType().GetProperty("FARNValue");
          if (pInfo != null && pInfo.CanRead)
      {
                result.Score = (int)pInfo.GetValue(verification, null);
            }
     }
          catch
     {
result.Score = verificationSuccess ? 0 : 9999;
   }
        }
         else
           {
   result.Success = false;
           result.ErrorCode = resultCode;
}
  }
    finally
     {
   done.Set();
       }
};

    verification.Verification();

         if (!done.WaitOne(_timeout))
      {
           _logger.LogWarning("Verification timeout");
          return new VerifyResultInternal { Success = false, Verified = false, Score = 9999 };
    }

     return result;
         }
    }
           catch (Exception ex)
       {
      _logger.LogError(ex, "Error during verification");
      return new VerifyResultInternal { Success = false, Verified = false, Score = 9999 };
                }
      }
        }

        // ============================================
        // CLASES AUXILIARES
      // ============================================

        private class CaptureResult
        {
    public byte[] Template { get; set; }
     public uint Quality { get; set; }
       public bool Success { get; set; }
   public int ErrorCode { get; set; }
        }

        private class EnrollResult
        {
    public byte[] Template { get; set; }
         public uint Quality { get; set; }
     public bool Success { get; set; }
     public int ErrorCode { get; set; }
   public int ResultCode { get; set; }
  public List<CapturedImage> CapturedImages { get; set; } = new List<CapturedImage>();
        }

        private class VerifyResultInternal
        {
   public bool Success { get; set; }
    public bool Verified { get; set; }
 public int Score { get; set; }
            public int ErrorCode { get; set; }
        }

        private class IdentifyResultInternal
        {
            public string Dni { get; set; }
            public string Dedo { get; set; }
          public string TemplatePath { get; set; }
      public int Score { get; set; }
      }
    }
}
