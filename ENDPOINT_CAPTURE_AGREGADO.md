# ? Endpoint /api/fingerprint/capture Agregado

## ?? Problema Resuelto

**Problema**: El endpoint `POST /api/fingerprint/capture` devolvía 404 Not Found porque **no estaba agregado al controlador**.

**Estado**: ? **SOLUCIONADO**

---

## ?? Cambios Realizados

### 1. Endpoint Agregado al Controlador

**Archivo**: `FutronicService\Controllers\FingerprintController.cs`

```csharp
/// <summary>
/// POST /api/fingerprint/capture
/// Captura una huella temporal sin asociarla a DNI (para testing)
/// </summary>
[HttpPost("capture")]
public async Task<IActionResult> Capture([FromBody] CaptureRequest request)
{
    _logger.LogInformation("Capture endpoint called");

    if (request == null)
    {
        request = new CaptureRequest(); // Usar valores por defecto
    }

    var result = await _fingerprintService.CaptureAsync(request);

    if (!result.Success)
    {
        return GetErrorStatusCode(result);
    }

    return Ok(result);
}
```

### 2. Carpeta Separada para Capturas Temporales

**Problema identificado**: Las capturas temporales se mezclaban con los registros permanentes.

**Solución**: Nueva configuración `CapturePath`

**Archivo**: `appsettings.json`

```json
{
  "Fingerprint": {
    "TempPath": "C:/temp/fingerprints",          // Para registros permanentes
    "CapturePath": "C:/temp/fingerprints/captures", // Para capturas temporales ? NUEVO
  "Threshold": 70,
    "MaxRotation": 199
  }
}
```

### 3. Servicio Actualizado

**Archivo**: `FutronicFingerprintService.cs`

- ? Variable `_capturePath` agregada
- ? Configuración cargada en `LoadConfiguration()`
- ? Método `CaptureAsync` actualizado para usar `_capturePath`

---

## ?? Estructura de Carpetas

### Antes:
```
C:/temp/fingerprints/
    capture_20250115_143022.tml  ? mezclado con registros
    12345678/
 indice-derecho/
  12345678.tml
```

### Ahora:
```
C:/temp/fingerprints/
  captures/ ? Carpeta separada
capture_20250115_143022/
   capture_20250115_143022.tml
            images/
     capture_20250115_143022.bmp  ? IMAGEN INCLUIDA
        capture_20250115_143155/
capture_20250115_143155.tml
            images/
       capture_20250115_143155.bmp
    12345678/   ? Registros permanentes
        indice-derecho/
            12345678.tml
   metadata.json
   images/
        12345678_best_01.bmp
```

---

## ?? Uso del Endpoint

### Request (Body Opcional)

```http
POST /api/fingerprint/capture
Content-Type: application/json

{
  "timeout": 30000
}
```

O simplemente:

```http
POST /api/fingerprint/capture
Content-Type: application/json

{}
```

### Response Exitosa

```json
{
  "success": true,
  "message": "Huella capturada exitosamente",
  "data": {
    "templatePath": "C:/temp/fingerprints/captures/capture_20250115_143022/capture_20250115_143022.tml",
    "imagePath": "C:/temp/fingerprints/captures/capture_20250115_143022/images/capture_20250115_143022.bmp",
  "quality": 88.5,
    "timestamp": "2025-01-15T14:30:22.1234567-05:00"
  }
}
```

### Logging en Consola

```
?? Iniciando captura de huella...
?? Apoye el dedo sobre el sensor cuando se indique
? Apoye el dedo firmemente sobre el sensor
  ?? Mantenga presión constante
   ?? Imagen capturada - Muestra: 1, Calidad: 88.45
? ?? Procesando huella...
? ¡Captura exitosa!
 ?? Template: 1024 bytes
   ?? Total de imágenes: 1

?? Imagen capturada:
   ?? Calidad: 88.45
   ?? Ruta: C:/temp/fingerprints/captures/capture_20250115_143022/images/capture_20250115_143022.bmp

? Huella capturada y guardada (TEMPORAL)
   ?? Template: capture_20250115_143022.tml
 ?? Directorio: C:/temp/fingerprints/captures/capture_20250115_143022
   ?? Tamaño: 1044 bytes
   ??  Esta captura es temporal y no está asociada a ningún DNI
```

---

## ?? Diferencias con /api/fingerprint/register-multi

| Aspecto | `/capture` | `/register-multi` |
|---------|------------|-------------------|
| **Requiere DNI** | ? No | ? Sí |
| **Requiere Dedo** | ? No | ? Sí |
| **Muestras** | 1 captura | 3-10 muestras |
| **Carpeta** | `CapturePath/{captureId}/` | `TempPath/{dni}/{dedo}/` |
| **Imágenes BMP** | ? Sí (1 imagen) ? | ? Sí (mejores 1-3) |
| **Metadatos JSON** | ? No | ? Sí |
| **Propósito** | Testing rápido | Registro permanente |
| **Uso en producción** | ? No recomendado | ? Recomendado |
| **Estructura** | `{captureId}/{captureId}.tml + images/` | `{dni}/{dedo}/{dni}.tml + images/` |

---

## ?? Casos de Uso

### 1. Testing del Dispositivo

```http
POST /api/fingerprint/capture
{}
```

**Uso**: Verificar rápidamente que el dispositivo Futronic está funcionando.

### 2. Pruebas de Calidad

```http
POST /api/fingerprint/capture
{
  "timeout": 30000
}
```

**Uso**: Capturar varias huellas para comparar calidad sin crear registros permanentes.

### 3. Verificación Manual

```http
# 1. Capturar huella de referencia
POST /api/fingerprint/capture
# Respuesta: "templatePath": "captures/capture_001.tml"

# 2. Capturar huella a verificar
POST /api/fingerprint/capture
# Respuesta: "templatePath": "captures/capture_002.tml"

# 3. Verificar manualmente (si implementas comparación por path)
POST /api/fingerprint/verify
{
  "storedTemplate": "<contenido_base64_de_capture_001>",
  "capturedTemplate": "<contenido_base64_de_capture_002>"
}
```

---

## ?? Importante

### Limpieza de Capturas Temporales

Las capturas en `CapturePath` son **temporales**. Considera implementar:

1. **Limpieza automática**: Borrar archivos mayores a X días
2. **Límite de almacenamiento**: Máximo N capturas
3. **Notificación**: Alertar cuando la carpeta esté llena

### Ejemplo de Script de Limpieza

```powershell
# Borrar capturas mayores a 7 días
$capturePath = "C:/temp/fingerprints/captures"
$daysOld = 7
Get-ChildItem $capturePath -Filter "*.tml" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$daysOld) } | 
    Remove-Item -Force
```

---

## ?? Colección de Postman Actualizada

La descripción del endpoint ha sido actualizada:

```json
{
  "name": "Capturar Huella (sin registro)",
  "request": {
    "method": "POST",
    "url": "{{base_url}}/api/fingerprint/capture",
    "description": "Captura una huella temporal sin asociarla a DNI. Se guarda en CapturePath (C:/temp/fingerprints/captures). Útil para testing y pruebas rápidas del dispositivo. NO requiere DNI ni dedo."
  }
}
```

---

## ?? Próximos Pasos

1. **Reiniciar el servicio** para aplicar los cambios
2. **Probar el endpoint** en Postman:
   ```
   POST http://localhost:5000/api/fingerprint/capture
   ```
3. **Verificar carpeta**: Comprobar que se crea `C:/temp/fingerprints/captures/`
4. **Implementar limpieza**: Agregar tarea programada para borrar capturas antiguas

---

## ? Resumen

| Cambio | Archivo | Estado |
|--------|---------|--------|
| Endpoint agregado | `FingerprintController.cs` | ? |
| CapturePath configurado | `appsettings.json` | ? |
| Variable _capturePath | `FutronicFingerprintService.cs` | ? |
| LoadConfiguration actualizado | `FutronicFingerprintService.cs` | ? |
| CaptureAsync actualizado | `FutronicFingerprintService.cs` | ? |
| Postman actualizado | `Futronic_API_Postman_Collection.json` | ? |

**Estado**: ? **Compilación exitosa - Listo para reiniciar y probar**

---

## ?? Para Aplicar los Cambios

Si el servicio está en ejecución:

1. **Detener el servicio**
2. **Reiniciar** (los cambios se aplicarán automáticamente)
3. **Probar**: `POST http://localhost:5000/api/fingerprint/capture`

O usar **Hot Reload** si está disponible (Visual Studio 2022).
