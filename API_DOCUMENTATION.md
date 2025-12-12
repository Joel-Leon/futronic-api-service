# ?? API REST - Futronic Fingerprint Service

## ?? URL Base

```
http://localhost:5000
```

---

## ?? Tabla de Contenidos

1. [Autenticación](#-autenticación)
2. [Endpoints Principales](#-endpoints-principales)
3. [Endpoints de Registro](#-registro-de-huellas)
4. [Endpoints de Verificación](#-verificación-de-huellas)
5. [Endpoints de Identificación](#-identificación-de-huellas)
6. [Endpoints de Configuración](#?-configuración)
7. [Endpoints de Salud](#-salud-del-servicio)
8. [Sistema de Notificaciones](#-sistema-de-notificaciones)
9. [Webhooks](#-webhooks-http-callbacks)
10. [Códigos de Error](#-códigos-de-error)
11. [Ejemplos de Uso](#-ejemplos-completos)

---

## ?? Autenticación

**Estado Actual:** No requiere autenticación

**Futuras Versiones:** Se implementará autenticación mediante:
- API Keys
- JWT Tokens
- OAuth 2.0

---

## ?? Endpoints Principales

### Resumen Rápido

| Método | Endpoint | Descripción | Uso Principal |
|--------|----------|-------------|---------------|
| `POST` | `/api/fingerprint/register-multi` | ? Registro con múltiples muestras | **RECOMENDADO** |
| `POST` | `/api/fingerprint/verify-simple` | ? Verificación 1:1 automática | **RECOMENDADO** |
| `POST` | `/api/fingerprint/identify-live` | ? Identificación 1:N automática | **RECOMENDADO** |
| `POST` | `/api/fingerprint/capture` | Captura temporal | Testing |
| `GET` | `/api/fingerprint/config` | Ver configuración | Admin |
| `POST` | `/api/fingerprint/config` | Actualizar configuración | Admin |
| `GET` | `/api/health` | Estado del servicio | Monitoreo |

---

## ?? Registro de Huellas

### `POST /api/fingerprint/register-multi`

**Descripción:** Registra una huella dactilar capturando **múltiples muestras** (5 por defecto) para mayor precisión.

**? Este es el endpoint RECOMENDADO para registro**

#### Request Body

```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "outputPath": "C:/temp/fingerprints",
  "timeout": 30000,
  "callbackUrl": "https://mi-servidor.com/api/webhook/fingerprint"
}
```

#### Parámetros

| Campo | Tipo | Requerido | Default | Descripción |
|-------|------|-----------|---------|-------------|
| `dni` | `string` | ? Sí | - | DNI o identificador único del usuario |
| `dedo` | `string` | ? No | `"index"` | Dedo a registrar (ej: "pulgar-derecho", "indice-izquierdo") |
| `sampleCount` | `int` | ? No | `5` | Número de muestras (1-10, recomendado: 5) |
| `outputPath` | `string` | ? No | `C:/temp/fingerprints` | Directorio base para guardar |
| `timeout` | `int` | ? No | `30000` | Timeout global en ms |
| `callbackUrl` | `string` | ? No | `null` | URL para notificaciones HTTP |

#### Response Success (200 OK)

```json
{
  "success": true,
  "message": "Huella registrada exitosamente con 5 muestras",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
    "imagePath": "C:/temp/fingerprints/12345678/indice-derecho/images/12345678_best_01.bmp",
    "quality": 90.5,
    "samplesCollected": 5,
    "sampleQualities": [85.2, 87.5, 90.5, 89.1, 88.3],
    "averageQuality": 88.12
  },
  "error": null
}
```

#### Response Error (500 Internal Server Error)

```json
{
  "success": false,
  "message": "Error al registrar huella con múltiples muestras",
  "data": null,
  "error": "ENROLLMENT_FAILED"
}
```

#### Estructura de Archivos Generados

```
C:/temp/fingerprints/
??? 12345678/
    ??? indice-derecho/
        ??? 12345678.tml                    ? Template principal
        ??? metadata.json                   ? Metadatos del registro
        ??? images/
            ??? 12345678_best_01.bmp       ? Mejor imagen (calidad más alta)
            ??? 12345678_best_02.bmp
            ??? 12345678_best_03.bmp
            ??? 12345678_best_04.bmp
            ??? 12345678_best_05.bmp
```

#### Notificaciones en Tiempo Real

Durante el registro, el sistema envía notificaciones de progreso:

##### 1. Inicio del Proceso
```json
{
  "eventType": "operation_started",
  "message": "Iniciando registro de huella",
  "data": null,
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:00Z"
}
```

##### 2. Inicio de Captura de Muestra
```json
{
  "eventType": "sample_started",
  "message": "Capturando muestra 1/5",
  "data": {
    "currentSample": 1,
    "totalSamples": 5,
    "progress": 20
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:02Z"
}
```

##### 3. Muestra Capturada (con Imagen)
```json
{
  "eventType": "sample_captured",
  "message": "? Muestra 1/5 capturada - RETIRE EL DEDO",
  "data": {
    "currentSample": 1,
    "totalSamples": 5,
    "quality": 85.5,
    "progress": 20,
    "imageBase64": "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDA...",
    "imageFormat": "bmp"
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:05Z"
}
```

##### 4. Completado
```json
{
  "eventType": "operation_completed",
  "message": "Huella registrada exitosamente con 5 muestras",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
    "samplesCollected": 5,
    "averageQuality": 88.12
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:30Z"
}
```

#### Ejemplo cURL

```bash
curl -X POST http://localhost:5000/api/fingerprint/register-multi \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "12345678",
    "dedo": "indice-derecho",
    "sampleCount": 5,
    "callbackUrl": "https://webhook.site/tu-id"
  }'
```

#### Ejemplo JavaScript

```javascript
const response = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    dni: '12345678',
    dedo: 'indice-derecho',
    sampleCount: 5,
    callbackUrl: 'https://mi-servidor.com/webhook'
  })
});

const result = await response.json();
console.log(result);
```

---

## ? Verificación de Huellas

### `POST /api/fingerprint/verify-simple`

**Descripción:** Verifica que la huella capturada del dispositivo coincida con una huella previamente registrada (verificación 1:1).

**? Este es el endpoint RECOMENDADO para verificación**

#### Request Body

```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "storedTemplatePath": "C:/temp/fingerprints",
  "timeout": 30000,
  "callbackUrl": "https://mi-servidor.com/api/webhook/verify"
}
```

#### Parámetros

| Campo | Tipo | Requerido | Default | Descripción |
|-------|------|-----------|---------|-------------|
| `dni` | `string` | ? Sí | - | DNI del usuario a verificar |
| `dedo` | `string` | ? No | `"index"` | Dedo registrado |
| `storedTemplatePath` | `string` | ? No | Auto | Ruta del template o directorio base |
| `timeout` | `int` | ? No | `30000` | Timeout en ms |
| `callbackUrl` | `string` | ? No | `null` | URL para notificaciones |

**Nota:** Si no se proporciona `storedTemplatePath`, el sistema construye la ruta automáticamente:
```
{tempPath}/{dni}/{dedo}/{dni}.tml
```

#### Response Success (200 OK) - Verificado

```json
{
  "success": true,
  "message": "Verificación exitosa para 12345678",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "verified": true,
    "score": 25,
    "threshold": 70,
    "captureQuality": 100,
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml"
  },
  "error": null
}
```

#### Response Success (200 OK) - No Verificado

```json
{
  "success": true,
  "message": "Las huellas no coinciden",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "verified": false,
    "score": 150,
    "threshold": 70,
    "captureQuality": 100,
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml"
  },
  "error": null
}
```

#### Interpretación del Score

- **Score FAR (False Acceptance Rate)**
  - Score **< 70** ? ? **Verificado** (huellas coinciden)
  - Score **? 70** ? ? **No Verificado** (huellas no coinciden)
  - Score más bajo = Mayor similitud
  - Umbral configurable (default: 70)

#### Response Error (404 Not Found)

```json
{
  "success": false,
  "message": "No existe huella registrada para DNI 12345678",
  "data": null,
  "error": "FILE_NOT_FOUND"
}
```

#### Ejemplo cURL

```bash
curl -X POST http://localhost:5000/api/fingerprint/verify-simple \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "12345678",
    "dedo": "indice-derecho"
  }'
```

---

## ?? Identificación de Huellas

### `POST /api/fingerprint/identify-live`

**Descripción:** Identifica a qué usuario pertenece la huella capturada del dispositivo, buscando en un directorio de templates (identificación 1:N).

**? Este es el endpoint RECOMENDADO para identificación**

#### Request Body

```json
{
  "templatesDirectory": "C:/temp/fingerprints",
  "timeout": 30000
}
```

#### Parámetros

| Campo | Tipo | Requerido | Default | Descripción |
|-------|------|-----------|---------|-------------|
| `templatesDirectory` | `string` | ? No | `C:/temp/fingerprints` | Directorio con templates |
| `timeout` | `int` | ? No | `30000` | Timeout en ms |

#### Response Success (200 OK) - Coincidencia Encontrada

```json
{
  "success": true,
  "message": "Identificado: 12345678",
  "data": {
    "matched": true,
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
    "score": 28,
    "threshold": 70,
    "matchIndex": 42,
    "totalCompared": 150
  },
  "error": null
}
```

#### Response Success (200 OK) - Sin Coincidencia

```json
{
  "success": true,
  "message": "No se encontró coincidencia",
  "data": {
    "matched": false,
    "dni": null,
    "dedo": null,
    "templatePath": null,
    "score": 0,
    "threshold": 70,
    "matchIndex": -1,
    "totalCompared": 150
  },
  "error": null
}
```

#### Campos de Response

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `matched` | `bool` | Si se encontró coincidencia |
| `dni` | `string` | DNI identificado (si matched=true) |
| `dedo` | `string` | Dedo que coincidió |
| `templatePath` | `string` | Ruta del template que coincidió |
| `score` | `int` | Score FAR de la coincidencia |
| `threshold` | `int` | Umbral usado |
| `matchIndex` | `int` | Posición en la que se encontró (0-based) |
| `totalCompared` | `int` | Total de templates comparados |

#### Ejemplo cURL

```bash
curl -X POST http://localhost:5000/api/fingerprint/identify-live \
  -H "Content-Type: application/json" \
  -d '{
    "templatesDirectory": "C:/temp/fingerprints"
  }'
```

#### Rendimiento

- **Velocidad:** ~50-100 comparaciones/segundo
- **Máximo recomendado:** 500 templates (configurable)
- **Tiempo típico:** 3-10 segundos para 150 templates

---

## ?? Captura Temporal

### `POST /api/fingerprint/capture`

**Descripción:** Captura una huella temporal sin asociarla a ningún DNI. Útil para testing o captura sin registro.

#### Request Body

```json
{
  "timeout": 30000
}
```

#### Parámetros

| Campo | Tipo | Requerido | Default | Descripción |
|-------|------|-----------|---------|-------------|
| `timeout` | `int` | ? No | `30000` | Timeout en ms |

#### Response Success (200 OK)

```json
{
  "success": true,
  "message": "Huella capturada exitosamente",
  "data": {
    "templatePath": "C:/temp/fingerprints/captures/capture_20251117_100530/capture_20251117_100530.tml",
    "imagePath": "C:/temp/fingerprints/captures/capture_20251117_100530/images/capture_20251117_100530.bmp",
    "quality": 90.5,
    "timestamp": "2025-11-17T10:05:30.1234567Z"
  },
  "error": null
}
```

---

## ?? Configuración

### `GET /api/fingerprint/config`

**Descripción:** Obtiene la configuración actual del servicio.

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Configuración obtenida",
  "data": {
    "threshold": 70,
    "timeout": 30000,
    "tempPath": "C:/temp/fingerprints",
    "overwriteExisting": false,
    "maxRotation": 199
  },
  "error": null
}
```

### `POST /api/fingerprint/config`

**Descripción:** Actualiza la configuración en runtime (sin reiniciar el servicio).

#### Request Body

```json
{
  "threshold": 80,
  "timeout": 45000,
  "tempPath": "C:/huellas",
  "overwriteExisting": true,
  "maxRotation": 166
}
```

#### Parámetros

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `threshold` | `int` | Umbral FAR (0-9999, recomendado: 70) |
| `timeout` | `int` | Timeout global en ms |
| `tempPath` | `string` | Directorio base para templates |
| `overwriteExisting` | `bool` | Si sobrescribir templates existentes |
| `maxRotation` | `int` | Rotación máxima permitida (166-199) |

**Nota:** Todos los parámetros son opcionales. Solo envía los que quieras actualizar.

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Configuración obtenida",
  "data": {
    "threshold": 80,
    "timeout": 45000,
    "tempPath": "C:/huellas",
    "overwriteExisting": true,
    "maxRotation": 166
  },
  "error": null
}
```

---

## ?? Salud del Servicio

### `GET /api/health`

**Descripción:** Verifica el estado del servicio y el dispositivo.

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Estado del servicio obtenido",
  "data": {
    "status": "healthy",
    "uptime": "02.15:30:45",
    "deviceConnected": true,
    "sdkInitialized": true,
    "lastError": null,
    "deviceModel": "Futronic",
    "sdkVersion": "2.3.0"
  },
  "error": null
}
```

#### Estados Posibles

| Status | Descripción |
|--------|-------------|
| `healthy` | ? Servicio y dispositivo funcionando correctamente |
| `degraded` | ?? Servicio funcionando pero dispositivo desconectado |
| `unhealthy` | ? Servicio con errores críticos |

---

## ?? Sistema de Notificaciones

El servicio soporta dos tipos de notificaciones en tiempo real:

### 1. SignalR (Recomendado para Web)

**Hub URL:** `http://localhost:5000/hubs/fingerprint`

#### Conectar al Hub

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/fingerprint')
    .withAutomaticReconnect()
    .build();

await connection.start();
```

#### Suscribirse a DNI

```javascript
// Suscribirse a notificaciones de un DNI específico
await connection.invoke('SubscribeToDni', '12345678');
```

#### Recibir Notificaciones

```javascript
connection.on('ReceiveProgress', (notification) => {
    console.log('Evento:', notification.eventType);
    console.log('Mensaje:', notification.message);
    console.log('Data:', notification.data);
    console.log('DNI:', notification.dni);
    console.log('Timestamp:', notification.timestamp);
    
    switch (notification.eventType) {
        case 'operation_started':
            console.log('Iniciando operación...');
            break;
            
        case 'sample_started':
            console.log(`Capturando muestra ${notification.data.currentSample}`);
            break;
            
        case 'sample_captured':
            console.log('Muestra capturada!');
            console.log('Calidad:', notification.data.quality);
            console.log('Progreso:', notification.data.progress + '%');
            
            // Mostrar imagen capturada
            if (notification.data.imageBase64) {
                const img = document.createElement('img');
                img.src = `data:image/bmp;base64,${notification.data.imageBase64}`;
                document.body.appendChild(img);
            }
            break;
            
        case 'operation_completed':
            console.log('Operación completada!');
            break;
            
        case 'error':
            console.error('Error:', notification.message);
            break;
    }
});
```

### 2. HTTP Callbacks (Webhooks)

**Descripción:** El servicio hace una petición POST a tu URL cuando ocurre un evento.

#### Configurar Webhook

Simplemente incluye `callbackUrl` en tus requests:

```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "callbackUrl": "https://mi-servidor.com/api/webhook/fingerprint"
}
```

#### Estructura del Webhook POST

```http
POST https://mi-servidor.com/api/webhook/fingerprint
Content-Type: application/json

{
  "eventType": "sample_captured",
  "message": "? Muestra 1/5 capturada - RETIRE EL DEDO",
  "data": {
    "currentSample": 1,
    "totalSamples": 5,
    "quality": 85.5,
    "progress": 20,
    "imageBase64": "/9j/4AAQSkZJRgAB...",
    "imageFormat": "bmp"
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:05Z"
}
```

#### Implementar Endpoint Webhook

##### Node.js + Express

```javascript
app.post('/api/webhook/fingerprint', (req, res) => {
    const notification = req.body;
    
    console.log('Evento recibido:', notification.eventType);
    console.log('DNI:', notification.dni);
    console.log('Mensaje:', notification.message);
    
    // Procesar según el tipo de evento
    switch (notification.eventType) {
        case 'sample_captured':
            // Guardar imagen, actualizar UI, etc.
            if (notification.data.imageBase64) {
                const imageBuffer = Buffer.from(notification.data.imageBase64, 'base64');
                // Guardar en BD o enviar a cliente vía WebSocket
            }
            break;
            
        case 'operation_completed':
            // Marcar proceso como completado
            break;
    }
    
    // IMPORTANTE: Responder rápidamente
    res.status(200).json({ received: true });
});
```

##### C# + ASP.NET Core

```csharp
[HttpPost("/api/webhook/fingerprint")]
public async Task<IActionResult> HandleFingerprintWebhook([FromBody] ProgressNotification notification)
{
    _logger.LogInformation($"Webhook recibido: {notification.EventType} para DNI: {notification.Dni}");
    
    switch (notification.EventType)
    {
        case "sample_captured":
            var data = JsonSerializer.Deserialize<SampleCapturedData>(notification.Data.ToString());
            
            // Convertir Base64 a imagen
            if (!string.IsNullOrEmpty(data.ImageBase64))
            {
                byte[] imageBytes = Convert.FromBase64String(data.ImageBase64);
                // Guardar o procesar imagen
            }
            
            // Notificar a cliente vía SignalR
            await _hubContext.Clients.User(notification.Dni).SendAsync("SampleCaptured", notification);
            break;
            
        case "operation_completed":
            // Marcar como completado
            break;
    }
    
    return Ok(new { received = true });
}
```

##### Python + Flask

```python
@app.route('/api/webhook/fingerprint', methods=['POST'])
def handle_fingerprint_webhook():
    notification = request.json
    
    print(f"Evento: {notification['eventType']}")
    print(f"DNI: {notification['dni']}")
    
    if notification['eventType'] == 'sample_captured':
        data = notification['data']
        
        # Decodificar imagen Base64
        if data.get('imageBase64'):
            import base64
            image_bytes = base64.b64decode(data['imageBase64'])
            # Guardar o procesar
    
    return jsonify({'received': True}), 200
```

---

## ?? Eventos de Notificación

### Tipos de Eventos

| EventType | Cuándo se Dispara | Incluye Imagen |
|-----------|-------------------|----------------|
| `operation_started` | Al iniciar operación | ? No |
| `sample_started` | Al iniciar captura de muestra | ? No |
| `sample_captured` | **Al capturar imagen** | ? Sí |
| `operation_completed` | Al completar operación | ? No |
| `error` | Al ocurrir un error | ? No |

### Estructura de Datos por Evento

#### operation_started

```json
{
  "eventType": "operation_started",
  "message": "Iniciando registro de huella",
  "data": null,
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:00Z"
}
```

#### sample_started

```json
{
  "eventType": "sample_started",
  "message": "Capturando muestra 1/5",
  "data": {
    "currentSample": 1,
    "totalSamples": 5,
    "progress": 20
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:02Z"
}
```

#### sample_captured (Con Imagen)

```json
{
  "eventType": "sample_captured",
  "message": "? Muestra 1/5 capturada - RETIRE EL DEDO",
  "data": {
    "currentSample": 1,
    "totalSamples": 5,
    "quality": 85.5,
    "progress": 20,
    "imageBase64": "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8U...",
    "imageFormat": "bmp"
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:05Z"
}
```

**Tamaño de Imagen:**
- Formato: BMP sin compresión
- Tamaño en Base64: ~50-100 KB
- Resolución típica: 300x300 a 500x500 px

#### operation_completed

```json
{
  "eventType": "operation_completed",
  "message": "Huella registrada exitosamente con 5 muestras",
  "data": {
    "dni": "12345678",
    "dedo": "indice-derecho",
    "templatePath": "C:/temp/fingerprints/12345678/indice-derecho/12345678.tml",
    "samplesCollected": 5,
    "averageQuality": 88.12
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:30Z"
}
```

#### error

```json
{
  "eventType": "error",
  "message": "Error al capturar huella",
  "data": {
    "errorCode": "CAPTURE_FAILED",
    "details": "Timeout en captura"
  },
  "dni": "12345678",
  "timestamp": "2025-11-17T10:00:15Z"
}
```

---

## ?? Códigos de Error

### HTTP Status Codes

| Status | Descripción |
|--------|-------------|
| `200 OK` | Operación exitosa |
| `400 Bad Request` | Request inválido |
| `404 Not Found` | Recurso no encontrado |
| `408 Request Timeout` | Timeout en captura |
| `500 Internal Server Error` | Error del servidor |
| `503 Service Unavailable` | Dispositivo no conectado |

### Error Codes

| Error Code | Descripción | Solución |
|------------|-------------|----------|
| `DEVICE_NOT_CONNECTED` | Dispositivo Futronic no conectado | Verificar conexión USB y drivers |
| `CAPTURE_FAILED` | Error al capturar huella | Reintentar, limpiar sensor |
| `CAPTURE_TIMEOUT` | Timeout en captura | Aumentar timeout o verificar dispositivo |
| `ENROLLMENT_FAILED` | Error en registro | Verificar que se completen todas las muestras |
| `FILE_NOT_FOUND` | Template no existe | Verificar que el DNI esté registrado |
| `INVALID_INPUT` | Parámetros inválidos | Verificar formato del request |
| `INVALID_TEMPLATE` | Template corrupto | Volver a registrar la huella |
| `REGISTRATION_ERROR` | Error genérico de registro | Ver logs del servidor |
| `VERIFICATION_ERROR` | Error genérico de verificación | Ver logs del servidor |
| `IDENTIFICATION_ERROR` | Error genérico de identificación | Ver logs del servidor |

### Ejemplo de Manejo de Errores

```javascript
try {
    const response = await fetch('http://localhost:5000/api/fingerprint/verify-simple', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ dni: '12345678' })
    });
    
    const result = await response.json();
    
    if (!result.success) {
        switch (result.error) {
            case 'DEVICE_NOT_CONNECTED':
                alert('El lector de huellas no está conectado. Por favor, verifique la conexión.');
                break;
            case 'FILE_NOT_FOUND':
                alert('No existe registro de huella para este DNI.');
                break;
            case 'CAPTURE_TIMEOUT':
                alert('Tiempo agotado. Por favor, coloque el dedo en el sensor.');
                break;
            default:
                alert('Error: ' + result.message);
        }
        return;
    }
    
    // Procesar resultado exitoso
    if (result.data.verified) {
        console.log('? Huella verificada correctamente');
    } else {
        console.log('? Huella no coincide');
    }
    
} catch (error) {
    console.error('Error de red:', error);
    alert('No se puede conectar con el servicio de huellas.');
}
```

---

## ?? Ejemplos Completos

### Ejemplo 1: Registro Completo con Notificaciones

```javascript
// 1. Conectar a SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/fingerprint')
    .withAutomaticReconnect()
    .build();

await connection.start();

// 2. Suscribirse al DNI
const dni = '12345678';
await connection.invoke('SubscribeToDni', dni);

// 3. Escuchar eventos
const samples = [];

connection.on('ReceiveProgress', (notification) => {
    switch (notification.eventType) {
        case 'operation_started':
            console.log('?? Iniciando registro...');
            updateUI('Iniciando proceso de registro...');
            break;
            
        case 'sample_started':
            console.log(`?? Coloque el dedo para muestra ${notification.data.currentSample}/${notification.data.totalSamples}`);
            updateUI(`Capturando muestra ${notification.data.currentSample}...`);
            updateProgress(notification.data.progress);
            break;
            
        case 'sample_captured':
            console.log(`? Muestra ${notification.data.currentSample} capturada`);
            
            // Guardar muestra
            samples.push({
                number: notification.data.currentSample,
                quality: notification.data.quality,
                imageBase64: notification.data.imageBase64
            });
            
            // Mostrar imagen en la UI
            displayImage(notification.data.imageBase64);
            
            // Actualizar progreso
            updateProgress(notification.data.progress);
            updateUI(`Muestra ${notification.data.currentSample} capturada. Retire el dedo.`);
            break;
            
        case 'operation_completed':
            console.log('?? Registro completado!');
            updateUI('Registro completado exitosamente');
            displaySummary(samples);
            break;
            
        case 'error':
            console.error('? Error:', notification.message);
            updateUI('Error: ' + notification.message, 'error');
            break;
    }
});

// 4. Iniciar registro
const registerResponse = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        dni: dni,
        dedo: 'indice-derecho',
        sampleCount: 5
    })
});

const result = await registerResponse.json();

if (result.success) {
    console.log('? Registro final:', result.data);
    console.log('?? Calidad promedio:', result.data.averageQuality);
    console.log('?? Template guardado en:', result.data.templatePath);
} else {
    console.error('? Error en registro:', result.message);
}

// Funciones auxiliares
function updateUI(message, type = 'info') {
    document.getElementById('status').textContent = message;
    document.getElementById('status').className = type;
}

function updateProgress(percent) {
    document.getElementById('progress-bar').style.width = percent + '%';
    document.getElementById('progress-text').textContent = percent + '%';
}

function displayImage(base64) {
    const img = document.createElement('img');
    img.src = `data:image/bmp;base64,${base64}`;
    img.className = 'sample-image';
    document.getElementById('samples-container').appendChild(img);
}

function displaySummary(samples) {
    const avgQuality = samples.reduce((sum, s) => sum + s.quality, 0) / samples.length;
    document.getElementById('summary').innerHTML = `
        <h3>Resumen del Registro</h3>
        <p>Total de muestras: ${samples.length}</p>
        <p>Calidad promedio: ${avgQuality.toFixed(2)}</p>
    `;
}
```

### Ejemplo 2: Verificación Simple

```javascript
async function verificarHuella(dni) {
    try {
        const response = await fetch('http://localhost:5000/api/fingerprint/verify-simple', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                dni: dni,
                dedo: 'indice-derecho'
            })
        });
        
        const result = await response.json();
        
        if (!result.success) {
            throw new Error(result.message);
        }
        
        if (result.data.verified) {
            console.log('? Huella VERIFICADA correctamente');
            console.log('Score FAR:', result.data.score);
            console.log('Umbral:', result.data.threshold);
            return true;
        } else {
            console.log('? Huella NO VERIFICADA');
            console.log('Score FAR:', result.data.score, '(supera umbral de', result.data.threshold + ')');
            return false;
        }
        
    } catch (error) {
        console.error('Error en verificación:', error);
        return false;
    }
}

// Uso
const esValido = await verificarHuella('12345678');
if (esValido) {
    // Permitir acceso
} else {
    // Denegar acceso
}
```

### Ejemplo 3: Identificación en Vivo

```javascript
async function identificarUsuario() {
    console.log('?? Iniciando identificación...');
    console.log('?? Coloque el dedo en el sensor');
    
    try {
        const response = await fetch('http://localhost:5000/api/fingerprint/identify-live', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                templatesDirectory: 'C:/temp/fingerprints'
            })
        });
        
        const result = await response.json();
        
        if (!result.success) {
            throw new Error(result.message);
        }
        
        if (result.data.matched) {
            console.log('? Usuario IDENTIFICADO:');
            console.log('  • DNI:', result.data.dni);
            console.log('  • Dedo:', result.data.dedo);
            console.log('  • Score FAR:', result.data.score);
            console.log('  • Posición:', result.data.matchIndex + 1, 'de', result.data.totalCompared);
            
            return {
                found: true,
                dni: result.data.dni
            };
        } else {
            console.log('? Usuario NO IDENTIFICADO');
            console.log('  • Templates comparados:', result.data.totalCompared);
            
            return {
                found: false,
                dni: null
            };
        }
        
    } catch (error) {
        console.error('Error en identificación:', error);
        return { found: false, error: error.message };
    }
}

// Uso
const resultado = await identificarUsuario();
if (resultado.found) {
    console.log(`Bienvenido, usuario ${resultado.dni}`);
} else {
    console.log('Usuario no reconocido');
}
```

### Ejemplo 4: Con Webhook (Backend Node.js)

```javascript
// Backend - Recibir notificaciones
const express = require('express');
const app = express();

app.use(express.json());

// Estado global de procesos
const activeProcesses = new Map();

app.post('/api/webhook/fingerprint', (req, res) => {
    const notification = req.body;
    const dni = notification.dni;
    
    // Obtener o crear estado del proceso
    if (!activeProcesses.has(dni)) {
        activeProcesses.set(dni, {
            samples: [],
            startTime: Date.now()
        });
    }
    
    const process = activeProcesses.get(dni);
    
    switch (notification.eventType) {
        case 'operation_started':
            console.log(`[${dni}] Proceso iniciado`);
            break;
            
        case 'sample_captured':
            const sample = {
                number: notification.data.currentSample,
                quality: notification.data.quality,
                timestamp: notification.timestamp
            };
            
            process.samples.push(sample);
            
            console.log(`[${dni}] Muestra ${sample.number} capturada (calidad: ${sample.quality})`);
            
            // Guardar imagen en base de datos
            if (notification.data.imageBase64) {
                saveImageToDatabase(dni, sample.number, notification.data.imageBase64);
            }
            
            // Notificar a frontend vía WebSocket
            io.to(dni).emit('sample_captured', sample);
            break;
            
        case 'operation_completed':
            const duration = Date.now() - process.startTime;
            console.log(`[${dni}] Proceso completado en ${duration}ms`);
            console.log(`[${dni}] Total de muestras: ${process.samples.length}`);
            
            // Notificar a frontend
            io.to(dni).emit('registration_completed', {
                dni: dni,
                samples: process.samples.length,
                averageQuality: process.samples.reduce((sum, s) => sum + s.quality, 0) / process.samples.length
            });
            
            // Limpiar estado
            activeProcesses.delete(dni);
            break;
            
        case 'error':
            console.error(`[${dni}] Error: ${notification.message}`);
            io.to(dni).emit('error', { message: notification.message });
            activeProcesses.delete(dni);
            break;
    }
    
    // Responder rápidamente
    res.json({ received: true, timestamp: Date.now() });
});

function saveImageToDatabase(dni, sampleNumber, imageBase64) {
    // Implementar guardado en DB
    const buffer = Buffer.from(imageBase64, 'base64');
    
    // Ejemplo con MongoDB
    db.collection('fingerprint_images').insertOne({
        dni: dni,
        sampleNumber: sampleNumber,
        imageData: buffer,
        createdAt: new Date()
    });
}

app.listen(3000, () => {
    console.log('Webhook listener en puerto 3000');
});
```

---

## ?? Troubleshooting

### Problema: Dispositivo No Conectado

**Error:** `DEVICE_NOT_CONNECTED`

**Soluciones:**
1. Verificar que el dispositivo Futronic esté conectado por USB
2. Reinstalar drivers de Futronic
3. Verificar que `ftrapi.dll` esté en el directorio de la aplicación
4. Reiniciar el servicio: `dotnet run`
5. Verificar estado: `GET /api/health`

### Problema: Timeout en Captura

**Error:** `CAPTURE_TIMEOUT` o Error Code `08`

**Soluciones:**
1. Aumentar timeout en el request
2. Limpiar el sensor con alcohol isopropílico
3. Instruir al usuario sobre posición correcta del dedo
4. Verificar que el dedo no esté muy seco o muy húmedo

### Problema: Template No Encontrado

**Error:** `FILE_NOT_FOUND`

**Soluciones:**
1. Verificar que el DNI esté registrado
2. Verificar la ruta del template
3. Verificar que el dedo especificado coincida con el registrado

---

## ?? Notas Importantes

### Seguridad

- ?? **No exponer el servicio directamente a Internet** sin autenticación
- ? Implementar API Gateway con autenticación (JWT, API Keys)
- ? Usar HTTPS en producción
- ? Validar todos los inputs en el backend
- ? Limitar rate limiting para prevenir abuso

### Rendimiento

- ?? **Registro:** 30-60 segundos para 5 muestras
- ?? **Verificación:** 3-5 segundos
- ?? **Identificación:** 3-10 segundos (150 templates)
- ?? **Máximo concurrente:** 1 operación por dispositivo

### Mejores Prácticas

1. **Siempre usar `register-multi`** con 5 muestras para mejor precisión
2. **Implementar retry automático** para timeouts
3. **Validar calidad de muestras** (mínimo 70-80)
4. **Guardar metadata** con cada registro
5. **Implementar notificaciones** para mejor UX
6. **Limpiar sensor** regularmente
7. **Usar SignalR** para aplicaciones web en tiempo real
8. **Usar Webhooks** para integración backend-to-backend

---

**?? Última Actualización:** 17 de Noviembre, 2025  
**?? Versión API:** 2.7  
**?? Repositorio:** https://github.com/Joel-Leon/futronic-api-service  
**? Estado:** ?? Producción Ready
