# ?? INSTRUCCIONES URGENTES - Restaurar y Actualizar FutronicFingerprintService.cs

## ?? PROBLEMA

El archivo `FutronicService\Services\FutronicFingerprintService.cs` se corrompió durante las actualizaciones automáticas.
Se ha creado un backup en: `FutronicService\Services\FutronicFingerprintService.cs.backup`

## ? SOLUCIÓN PASO A PASO

### PASO 1: Restaurar desde Backup o Repositorio

**Opción A - Si tienes backup local:**
```powershell
# Verificar si existe el backup
Test-Path "FutronicService\Services\FutronicFingerprintService.cs.backup"

# Si existe, usarlo como referencia
Copy-Item "FutronicService\Services\FutronicFingerprintService.cs.backup" "FutronicService\Services\Futronic FingerprintService_ORIGINAL.cs"
```

**Opción B - Si tienes el repositorio:**
```powershell
# Clonar nuevamente el repositorio en otra carpeta
cd C:\temp
git clone https://github.com/JoelLeonUNS/futronic-cli futronic-backup
cd futronic-backup

# Copiar el archivo original
Copy-Item "FutronicService\Services\FutronicFingerprintService.cs" "C:\apps\futronic-api\FutronicService\Services\FutronicFingerprintService_ORIGINAL.cs"
```

**Opción C - Recrear manualmente:**
Si no tienes backup, te proporcionaré el archivo completo funcional.

### PASO 2: Aplicar Solo Los Cambios Necesarios

Una vez que tengas el archivo original, aplica SOLO estos cambios específicos:

#### 2.1. Actualizar Using Statements (Líneas 1-14)

AGREGAR estas dos líneas después de `using System;`:
```csharp
using System.Runtime.ExceptionServices;
using System.Security;
```

El resultado debe ser:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;  // ? AGREGAR
using System.Security;             // ? AGREGAR
using System.Threading;
using System.Threading.Tasks;
using Futronic.SDKHelper;
using FutronicService.Models;
using FutronicService.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using futronic_cli;
```

#### 2.2. Actualizar Campos Privados (Después de línea ~20)

AGREGAR estos dos campos después de `private bool _deviceConnected;`:
```csharp
private bool _sdkInitialized;
private readonly object _deviceLock = new object();
```

Y agregar estos al final de las configuraciones:
```csharp
private int _deviceCheckRetries = 3;
private int _deviceCheckDelayMs = 1000;
```

#### 2.3. Actualizar LoadConfiguration() 

AGREGAR estas dos líneas al final del método:
```csharp
_deviceCheckRetries = _configuration.GetValue<int>("Fingerprint:DeviceCheckRetries", 3);
_deviceCheckDelayMs = _configuration.GetValue<int>("Fingerprint:DeviceCheckDelayMs", 1000);
```

Y actualizar el log:
```csharp
_logger.LogInformation($"Configuration loaded: Threshold={_threshold}, Timeout={_timeout}, Retries={_deviceCheckRetries}");
```

#### 2.4. REEMPLAZAR InitializeDevice()

Buscar el método completo `private void InitializeDevice()` y reemplazarlo con:

```csharp
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

        if (!VerifySDKAssembly())
            {
                _logger.LogError("SDK assembly verification failed - cannot proceed");
     _lastError = "SDK no disponible";
      return;
       }

  if (!VerifyNativeDLLs())
     {
                _logger.LogWarning("Native DLLs verification failed - device may not work");
        _lastError = "DLLs nativas faltantes";
    }

        _sdkInitialized = InitializeSDKWithRetries();

    if (_sdkInitialized)
     {
            _deviceConnected = CheckDeviceConnectionWithRetries();

           if (_deviceConnected)
              {
         _logger.LogInformation("? Futronic device initialized successfully");
     _lastError = null;
              }
   else
   {
            _logger.LogWarning("? SDK initialized but device not detected");
 _lastError = "Dispositivo no detectado";
     }
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
            _logger.LogError("????????????????????????????????????????????????????");
         _logger.LogError("  FUTRONIC SDK INITIALIZATION FAILURE");
      _logger.LogError("????????????????????????????????????????????????????");
     _logger.LogError("Causa probable: El SDK no puede inicializar el callback interno (cbControl)");
         _logger.LogError("");
  _logger.LogError("ACCIONES REQUERIDAS:");
  _logger.LogError("  1. Verificar que el dispositivo Futronic esté conectado por USB");
  _logger.LogError("  2. Reinstalar el driver de Futronic (desde el sitio oficial)");
    _logger.LogError("  3. Copiar ftrapi.dll al directorio de la aplicación");
        _logger.LogError("  4. Reiniciar el servicio Windows");
          _logger.LogError("  5. Si persiste, reiniciar el sistema");
            _logger.LogError("????????????????????????????????????????????????????");
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
```

#### 2.5. REEMPLAZAR CheckDeviceConnection()

Buscar el método `private bool CheckDeviceConnection()` y envolver todo el contenido con:
```csharp
[HandleProcessCorruptedStateExceptions]
[SecurityCritical]
private bool CheckDeviceConnection()
{
    // ... código existente ...
    
    // AGREGAR al final del primer try, dentro del using:
    catch (AccessViolationException avEx)
    {
      accessViolationOccurred = true;
        _logger.LogError(avEx, "AccessViolationException during test enrollment");
   _logger.LogError("This means the SDK callback (cbControl) is not initialized");
  _logger.LogError("Possible causes:");
  _logger.LogError("  1. Device driver not properly installed");
        _logger.LogError("  2. ftrapi.dll version mismatch");
        _logger.LogError("  3. Device firmware issue");
        return false;
    }
    
    // AGREGAR al catch principal:
    catch (AccessViolationException avEx)
    {
     _logger.LogError(avEx, "AccessViolationException creating FutronicEnrollment");
        return false;
    }
}
```

#### 2.6. ACTUALIZAR IsDeviceConnected()

Cambiar:
```csharp
public bool IsDeviceConnected()
{
    return _deviceConnected;
}
```

Por:
```csharp
public bool IsDeviceConnected()
{
    return _deviceConnected && _sdkInitialized;
}
```

#### 2.7. AGREGAR Métodos Nuevos

Después del método `IsDeviceConnected()`, AGREGAR estos 4 métodos nuevos:

```csharp
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
         
         _logger.LogInformation($"  ? SDK properties accessible");
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

[HandleProcessCorruptedStateExceptions]
[SecurityCritical]
private bool CheckDeviceConnectionWithRetries()
{
    for (int attempt = 1; attempt <= _deviceCheckRetries; attempt++)
    {
   try
        {
            _logger.LogInformation($"Device connection check attempt {attempt}/{_deviceCheckRetries}");
         
   bool connected = CheckDeviceConnection();
   
            if (connected)
            {
          _logger.LogInformation($"  ? Device connected on attempt {attempt}");
         return true;
  }
            else
       {
    _logger.LogWarning($"  ? Device not detected on attempt {attempt}");
       
        if (attempt < _deviceCheckRetries)
     {
      _logger.LogInformation($"  ? Waiting {_deviceCheckDelayMs}ms before retry...");
  Thread.Sleep(_deviceCheckDelayMs);
         }
    }
        }
  catch (AccessViolationException avEx)
        {
    _logger.LogError(avEx, $"  ? AccessViolationException during device check attempt {attempt}");
   
  if (attempt < _deviceCheckRetries)
            {
     Thread.Sleep(_deviceCheckDelayMs * 2);
            }
        }
        catch (Exception ex)
        {
      _logger.LogWarning(ex, $"  ? Device check failed on attempt {attempt}");
    
 if (attempt < _deviceCheckRetries)
   {
      Thread.Sleep(_deviceCheckDelayMs);
 }
        }
    }

    _logger.LogWarning($"Device not connected after {_deviceCheckRetries} attempts");
    return false;
}
```

### PASO 3: Verificar Compilación

```powershell
cd FutronicService
dotnet build
```

Si hay errores, revisa que:
1. Todos los métodos originales están presentes
2. Los using statements están completos
3. No faltan llaves de cierre `}`

### PASO 4: Probar

```powershell
dotnet run --project FutronicService
```

## ?? SI NECESITAS EL ARCHIVO COMPLETO

Si prefieres que te proporcione el archivo completo funcional, dímelo y te lo generaré.

## ? VERIFICACIÓN RÁPIDA

Después de aplicar los cambios, el archivo debe tener aproximadamente:
- ~1500-2000 líneas
- 11 métodos públicos (interfaz IFingerprintService)
- 8 métodos privados nuevos (los de verificación y reintentos)
- Todos los métodos existentes intactos (CaptureAsync, RegisterAsync, etc.)
