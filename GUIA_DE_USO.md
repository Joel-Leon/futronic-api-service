# ?? Guía de Uso - Futronic API Service

## ?? Inicio Rápido

### 1. Iniciar el Servicio

```powershell
cd C:\apps\futronic-api\FutronicService
dotnet run
```

**Salida esperada:**
```
=== FUTRONIC API SERVICE (.NET 8) ===
? Futronic SDK initialized successfully
?? Listening on: http://localhost:5000
```

---

## ?? Endpoints Disponibles

### ?? POST `/api/fingerprint/register-multi` - Registrar Huella (Recomendado)

**Uso:** Registra una huella con múltiples muestras para mayor precisión.

**Request:**
```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "outputPath": "C:/temp/fingerprints",
  "sampleCount": 5,
  "timeout": 30000
}
```

**Response:**
```json
{
  "success": true,
  "message": "Huella registrada exitosamente con 5 muestras",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
    "quality": 85.5,
    "samplesCollected": 5,
    "averageQuality": 82.3
  }
}
```

**Estructura de archivos creada:**
```
C:/temp/fingerprints/
??? 12345678/
    ??? indice-derecho/
        ??? 12345678.tml              (template principal)
        ??? metadata.json             (información de la captura)
        ??? images/
            ??? 12345678_best_01.bmp
            ??? 12345678_best_02.bmp
            ??? ...
```

---

### ?? POST `/api/fingerprint/verify-simple` - Verificar Identidad (1:1)

**Uso:** Verifica si la huella capturada coincide con un DNI específico.

**Request (Con ruta completa):**
```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "storedTemplatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
  "timeout": 20000
}
```

**Request (Con directorio base):** ? NUEVO
```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "storedTemplatePath": "C:/temp/fingerprints",
  "timeout": 20000
}
```
> El sistema construirá automáticamente: `C:/temp/fingerprints/12345678/indice-derecho/12345678.tml`

**Response (Exitosa):**
```json
{
  "success": true,
  "message": "Verificación exitosa para 12345678",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "verified": true,
    "score": 15,
    "threshold": 70,
    "captureQuality": 88.5,
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml"
  }
}
```

**Response (Fallida):**
```json
{
  "success": true,
  "message": "Las huellas no coinciden",
  "data": {
    "dni": "12345678",
    "verified": false,
    "score": 150,
    "threshold": 70
  }
}
```

---

### ?? POST `/api/fingerprint/identify-live` - Identificar Usuario (1:N)

**Uso:** Captura una huella y busca coincidencias en todos los templates registrados.

**Request:**
```json
{
  "templatesDirectory": "C:/temp/fingerprints",
  "timeout": 30000
}
```

**Response (Encontrado):**
```json
{
  "success": true,
  "message": "Identificado: 12345678",
  "data": {
    "matched": true,
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
    "score": 18,
    "threshold": 70,
    "matchIndex": 5,
    "totalCompared": 150
  }
}
```

**Response (No encontrado):**
```json
{
  "success": true,
  "message": "No se encontró coincidencia",
  "data": {
    "matched": false,
    "totalCompared": 150,
    "threshold": 70
  }
}
```

---

### ?? POST `/api/fingerprint/capture` - Captura Temporal

**Uso:** Captura una huella sin asociarla a ningún DNI (para testing).

**Request:**
```json
{
  "timeout": 20000
}
```

**Response:**
```json
{
  "success": true,
  "message": "Huella capturada exitosamente",
  "data": {
    "templatePath": "C:/temp/fingerprints/captures/capture_20251112_195530/capture_20251112_195530.tml",
    "imagePath": "C:/temp/fingerprints/captures/capture_20251112_195530/images/capture_20251112_195530.bmp",
    "quality": 87.2,
    "timestamp": "2025-11-12T19:55:30.123Z"
  }
}
```

---

### ?? GET `/api/fingerprint/config` - Obtener Configuración

**Response:**
```json
{
  "success": true,
  "data": {
    "threshold": 70,
    "timeout": 30000,
    "tempPath": "C:/temp/fingerprints",
    "overwriteExisting": false,
    "maxRotation": 199
  }
}
```

---

### ?? POST `/api/fingerprint/config` - Actualizar Configuración

**Request:**
```json
{
  "threshold": 60,
  "timeout": 40000,
  "maxRotation": 166
}
```

**Response:**
```json
{
  "success": true,
  "message": "Configuración obtenida",
  "data": {
    "threshold": 60,
    "timeout": 40000,
    "tempPath": "C:/temp/fingerprints",
    "overwriteExisting": false,
    "maxRotation": 166
  }
}
```

---

### ?? GET `/health` - Estado del Servicio

**Response:**
```json
{
  "success": true,
  "message": "Estado del servicio obtenido",
  "data": {
    "status": "healthy",
    "uptime": "00.02:35:42",
    "deviceConnected": true,
    "sdkInitialized": true,
    "deviceModel": "Futronic",
    "sdkVersion": "2.3.0",
    "lastError": null
  }
}
```

---

## ?? Casos de Uso

### Caso 1: Registro Inicial de Usuario

```bash
# 1. Registrar huella del índice derecho
POST /api/fingerprint/register-multi
{
  "dni": "87654321",
  "dedo": "indice-derecho",
  "sampleCount": 5
}

# 2. Registrar huella del índice izquierdo (backup)
POST /api/fingerprint/register-multi
{
  "dni": "87654321",
  "dedo": "indice-izquierdo",
  "sampleCount": 5
}
```

### Caso 2: Verificación en Sistema de Acceso

```bash
# Usuario se identifica con DNI, sistema verifica huella
POST /api/fingerprint/verify-simple
{
  "dni": "87654321",
  "dedo": "indice-derecho",
  "storedTemplatePath": "C:/temp/fingerprints"
}
```

### Caso 3: Identificación sin DNI (Modo Búsqueda)

```bash
# Usuario solo coloca el dedo, sistema busca en base de datos
POST /api/fingerprint/identify-live
{
  "templatesDirectory": "C:/temp/fingerprints"
}
```

---

## ?? Configuración Recomendada

### Para Alta Seguridad
```json
{
  "threshold": 50,        // Más restrictivo
  "maxRotation": 199      // Menos tolerancia a rotación
}
```

### Para Mejor Experiencia de Usuario
```json
{
  "threshold": 70,        // Balanceado
  "maxRotation": 166      // Más tolerancia a rotación
}
```

### Para Ambientes Ruidosos
```json
{
  "threshold": 90,        // Más permisivo
  "timeout": 40000        // Más tiempo para captura
}
```

---

## ?? Interpretación de Scores

| Score FAR | Interpretación | Acción |
|-----------|----------------|--------|
| 0-30 | ? Coincidencia excelente | Aceptar |
| 31-60 | ? Buena coincidencia | Aceptar |
| 61-90 | ?? Coincidencia aceptable | Aceptar con precaución |
| 91-150 | ? Coincidencia baja | Rechazar |
| 150+ | ? No coincide | Rechazar |

> **Nota:** Un score MÁS BAJO es mejor. El threshold por defecto es 70.

---

## ??? Solución de Problemas

### Error: "Dispositivo no conectado"

**Verificar:**
```bash
# 1. Revisar salud del servicio
GET /health

# 2. Verificar en Device Manager
# Windows > Device Manager > Imaging Devices

# 3. Reiniciar servicio
Ctrl+C (detener)
dotnet run (reiniciar)
```

### Error: "Template not found"

**Verificar estructura de archivos:**
```bash
# La estructura debe ser:
{outputPath}/{dni}/{dedo}/{dni}.tml

# Ejemplo correcto:
C:/temp/fingerprints/12345678/indice-derecho/12345678.tml

# ? Incorrecto:
C:/temp/fingerprints/12345678.tml
```

### Error: "Capture timeout"

**Soluciones:**
1. Aumentar timeout en la configuración
2. Limpiar el sensor
3. Verificar que el dedo esté completamente apoyado
4. Reintentar con mejor presión

---

## ?? Seguridad

### Protección de Archivos
```csharp
// Los templates están encriptados en formato propietario .tml
// No se pueden usar sin el SDK de Futronic
```

### Recomendaciones
1. ? Usar HTTPS en producción
2. ? Implementar autenticación de API
3. ? Limitar acceso al directorio de templates
4. ? Implementar rate limiting
5. ? Validar todas las entradas

---

## ?? Logs

El servicio genera logs detallados en consola:

```
[19:55:30 INF] === CAPTURA INTELIGENTE DE HUELLA ===
[19:55:30 INF] ?? Muestras objetivo: 5
[19:55:32 INF] ? Muestra 1/5
[19:55:34 INF] ? Muestra 2/5
[19:55:36 INF] ? Muestra 3/5
[19:55:38 INF] ? Muestra 4/5
[19:55:40 INF] ? Muestra 5/5
[19:55:42 INF] ? Registro exitoso - Template: 1024 bytes
```

---

## ?? Mejores Prácticas

### 1. Registro
- ? Usar `sampleCount: 5` para mejor calidad
- ? Registrar múltiples dedos por usuario (backup)
- ? Validar calidad promedio > 70

### 2. Verificación
- ? Siempre especificar el dedo correcto
- ? Usar directorio base para simplificar
- ? Implementar retry logic (máx 3 intentos)

### 3. Identificación
- ? Limitar búsqueda a usuarios activos
- ? Usar índices/filtros para mejorar performance
- ? Considerar timeout más alto para bases grandes

---

## ?? Soporte

Para más información, consultar:
- `README.md` - Información general del proyecto
- `ACTUALIZACION_NET8.md` - Detalles de la actualización
- `REPORTE_VERIFICACION_FINAL.md` - Estado del proyecto

---

**Última actualización:** 12 de Noviembre, 2025  
**Versión:** 2.0 (.NET 8)
