# Futronic Fingerprint API - Guía de Uso

## ?? Descripción

API REST para captura, registro, verificación e identificación de huellas dactilares usando dispositivos Futronic y el SDK oficial de Futronic.

## ?? Características

- **Captura de huellas** desde dispositivo Futronic
- **Registro multi-muestra** (3-10 capturas para mayor precisión)
- **Verificación 1:1** (comparar huella capturada vs registrada)
- **Identificación 1:N** (buscar en múltiples registros)
- **Identificación automática** (captura + búsqueda en directorio)
- **Configuración dinámica** (sin reiniciar servicio)

## ?? Configuración

### appsettings.json

```json
{
  "Fingerprint": {
    "Threshold": 70,        // Umbral FAR para coincidencia (10-1000)
 "Timeout": 30000, // Timeout en ms para captura
    "TempPath": "C:/temp/fingerprints",
    "OverwriteExisting": false,
    "MaxTemplatesPerIdentify": 500,
    "DeviceCheckRetries": 3,
    "DeviceCheckDelayMs": 1000
  }
}
```

## ?? Endpoints

### 1. Health Check

Verifica el estado del servicio y dispositivo.

```http
GET /health
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "message": "Estado del servicio obtenido",
  "data": {
    "status": "healthy",
    "deviceConnected": true,
    "sdkInitialized": true,
    "deviceModel": "Futronic",
    "sdkVersion": "2.3.0",
    "uptime": "00.01:23:45",
    "lastError": null
}
}
```

---

### 2. Registrar Huella (1 muestra)

Captura y guarda una huella con una sola muestra.

```http
POST /api/fingerprint/register
Content-Type: application/json

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "outputPath": "C:/temp/fingerprints"  // Opcional
}
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "message": "Huella registrada exitosamente",
  "data": {
    "dni": "12345678",
  "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678_indice-derecho.dat",
    "imagePath": null,
    "quality": 85.5
}
}
```

---

### 3. Registrar Huella Multi-Muestra (? RECOMENDADO)

Captura múltiples muestras para crear un template más robusto.

```http
POST /api/fingerprint/register-multi
Content-Type: application/json

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "outputPath": "C:/temp/fingerprints",  // Opcional
  "sampleCount": 5,         // 3-10, default 5
  "timeout": 30000     // Opcional
}
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "message": "Huella registrada exitosamente con 5 muestras",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678_indice-derecho.dat",
    "quality": 92.3,
    "samplesCollected": 5,
    "sampleQualities": [90.2, 91.5, 93.0, 92.8, 94.0],
    "averageQuality": 92.3
  }
}
```

---

### 4. Verificar Identidad (? RECOMENDADO)

Verifica la identidad capturando automáticamente del dispositivo.

```http
POST /api/fingerprint/verify-simple
Content-Type: application/json

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "storedTemplatePath": null,  // Opcional: si null busca en tempPath
  "timeout": 20000           // Opcional
}
```

**Respuesta exitosa (coincide):**
```json
{
  "success": true,
  "message": "Verificación exitosa para 12345678",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "verified": true,
    "score": 45,
    "threshold": 70,
 "captureQuality": 88.5,
    "templatePath": "C:/temp/fingerprints/12345678_indice-derecho.dat"
  }
}
```

**Respuesta sin coincidencia:**
```json
{
  "success": true,
  "message": "Las huellas no coinciden",
  "data": {
    "verified": false,
    "score": 150
  }
}
```

---

### 5. Identificar Usuario en Vivo (? 1:N AUTOMÁTICO)

Captura huella y busca coincidencia en todos los templates de un directorio.

```http
POST /api/fingerprint/identify-live
Content-Type: application/json

{
  "templatesDirectory": "C:/temp/fingerprints",  // Opcional: default tempPath
  "timeout": 30000 // Opcional
}
```

**Respuesta con coincidencia:**
```json
{
  "success": true,
  "message": "Identificado: 12345678",
  "data": {
    "matched": true,
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678_indice-derecho.dat",
    "score": 42,
    "threshold": 70,
    "matchIndex": 5,
    "totalCompared": 125
  }
}
```

---

### 6. Capturar Huella Temporal

Captura una huella sin asociarla a un DNI específico.

```http
POST /api/fingerprint/capture
Content-Type: application/json

{
  "timeout": 30000
}
```

**Respuesta:**
```json
{
  "success": true,
  "message": "Huella capturada exitosamente",
  "data": {
    "templatePath": "C:/temp/fingerprints/capture_20250115_143022.dat",
    "imagePath": null,
    "quality": 87.2,
    "timestamp": "2025-01-15T14:30:22.1234567Z"
  }
}
```

---

### 7. Verificar con Templates (Manual)

Compara dos templates ya capturados.

```http
POST /api/fingerprint/verify
Content-Type: application/json

{
  "storedTemplate": "base64_encoded_template",     // Base64 del template registrado
  "capturedTemplate": "base64_encoded_template"    // Base64 del template capturado (null = captura del dispositivo)
}
```

---

### 8. Identificar Huella (1:N Manual)

Identifica a qué usuario pertenece una huella entre múltiples registros.

```http
POST /api/fingerprint/identify
Content-Type: application/json

{
  "capturedTemplate": null,  // null = captura del dispositivo
"templates": [
    {
      "dni": "12345678",
      "dedo": "indice-derecho",
      "templatePath": "C:/temp/fingerprints/12345678_indice-derecho.dat"
    },
    {
      "dni": "87654321",
      "dedo": "pulgar-derecho",
      "templatePath": "C:/temp/fingerprints/87654321_pulgar-derecho.dat"
  }
  ]
}
```

---

### 9. Obtener Configuración

```http
GET /api/fingerprint/config
```

**Respuesta:**
```json
{
  "success": true,
  "message": "Configuración obtenida",
  "data": {
    "threshold": 70,
    "timeout": 30000,
    "tempPath": "C:/temp/fingerprints",
  "overwriteExisting": false
  }
}
```

---

### 10. Actualizar Configuración

Actualiza la configuración en runtime sin reiniciar el servicio.

```http
POST /api/fingerprint/config
Content-Type: application/json

{
  "threshold": 80,// Opcional
  "timeout": 25000,// Opcional
  "tempPath": "D:/huellas",  // Opcional
  "overwriteExisting": true  // Opcional
}
```

---

## ?? Códigos de Error

| Código | Descripción |
|--------|-------------|
| `DEVICE_NOT_CONNECTED` | Dispositivo Futronic no conectado o SDK no inicializado |
| `CAPTURE_FAILED` | Error al capturar huella del dispositivo |
| `CAPTURE_TIMEOUT` | Timeout esperando captura de huella |
| `FILE_NOT_FOUND` | Template no encontrado en la ruta especificada |
| `FILE_EXISTS` | Ya existe un template registrado (y overwrite=false) |
| `INVALID_TEMPLATE` | Template en formato inválido o corrupto |
| `INVALID_INPUT` | Request con datos inválidos o faltantes |
| `ENROLLMENT_FAILED` | Error durante el proceso de enrollment multi-muestra |
| `VERIFICATION_ERROR` | Error durante el proceso de verificación |
| `IDENTIFICATION_ERROR` | Error durante el proceso de identificación |

**Ejemplo de respuesta de error:**
```json
{
  "success": false,
  "message": "Dispositivo no conectado",
  "data": null,
  "error": "DEVICE_NOT_CONNECTED"
}
```

---

## ?? Mejores Prácticas

### Para Registro (Enrollment)

? **RECOMENDADO:**
```json
POST /api/fingerprint/register-multi
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5
}
```

? **NO RECOMENDADO para producción:**
```json
POST /api/fingerprint/register  // Solo 1 muestra
```

### Para Verificación (1:1)

? **RECOMENDADO:**
```json
POST /api/fingerprint/verify-simple
{
  "dni": "12345678",
  "dedo": "indice-derecho"
}
```

### Para Identificación (1:N)

? **RECOMENDADO:**
```json
POST /api/fingerprint/identify-live
{
  "templatesDirectory": "C:/SistemaHuellas/huellas"
}
```

---

## ?? Seguridad

1. **Templates almacenados localmente**: Los archivos `.dat` contienen datos biométricos sensibles
2. **Permisos del sistema**: El servicio necesita acceso de lectura/escritura a `TempPath`
3. **Dispositivo USB**: El dispositivo Futronic debe estar conectado y con drivers instalados

---

## ?? Troubleshooting

### Dispositivo no detectado

1. Verificar conexión USB
2. Reinstalar drivers de Futronic
3. Verificar que `ftrapi.dll` esté en el directorio de la aplicación
4. Revisar logs en `/health` endpoint

### Errores de captura

- **Timeout**: Aumentar `timeout` en el request
- **Calidad baja**: Limpiar el sensor del dispositivo
- **FAR alto**: Ajustar `threshold` en la configuración

### Templates no encontrados

- Verificar que `TempPath` tenga permisos de lectura/escritura
- Verificar nomenclatura: `{dni}_{dedo}.dat`
- Usar rutas absolutas en Windows: `C:/temp/fingerprints`

---

## ?? Formato de Templates

Los templates se guardan en formato binario `.dat`:
- **Nomenclatura**: `{dni}_{dedo}.dat`
- **Ejemplo**: `12345678_indice-derecho.dat`
- **Contenido**: Datos biométricos procesados por el SDK de Futronic

---

## ?? SDK de Futronic

Este servicio utiliza el **Futronic SDK 4.2** con las siguientes configuraciones optimizadas:

### FutronicIdentification (Captura simple)
- `FakeDetection`: false
- `FFDControl`: true
- `FARN`: threshold configurado
- `MIOTOff`: 3000ms
- `DetectCore`: true
- `Version`: 0x02030000

### FutronicEnrollment (Multi-muestra)
- `MaxModels`: 3-10 muestras
- `FastMode`: false (mayor precisión)
- `FFDControl`: true
- `DetectFakeFinger`: false
- `MIOTOff`: 2000ms
- `ImageQuality`: 50

### FutronicVerification (Comparación)
- `FARN`: threshold configurado
- `FastMode`: false
- `FFDControl`: true
- `MIOTOff`: 3000ms

---

## ?? Soporte

Para más información sobre el SDK de Futronic, visitar:
- [Sitio oficial de Futronic](http://www.futronic-tech.com/)
- Documentación del SDK incluida en la instalación

---

## ?? Licencia

Este servicio utiliza el SDK propietario de Futronic. Verificar los términos de licencia del SDK.
