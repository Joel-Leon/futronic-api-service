# ?? Futronic API Service - Sistema de Huellas Dactilares

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

Sistema completo de gestión de huellas dactilares usando lectores **Futronic FS88** con notificaciones en tiempo real mediante **SignalR**.

---

## ?? Características Principales

? **Registro Multi-Muestra** - Captura múltiples muestras para mejor precisión  
? **Verificación 1:1** - Verificar identidad contra una huella registrada  
? **Identificación 1:N** - Identificar automáticamente entre múltiples huellas  
? **Notificaciones en Tiempo Real** - SignalR para feedback durante captura  
? **Detección de Dedos Falsos** - Liveness detection configurable  
? **Configuración Persistente** - Personalizar comportamiento del lector  
? **Imágenes en Base64** - Recibir imágenes de huellas capturadas  
? **Prevención de Duplicados** - Verificación temprana antes de capturar  
? **API RESTful** - Endpoints HTTP simples  
? **CORS Habilitado** - Integración con frontends

---

## ?? Inicio Rápido

### Requisitos

- **.NET 8 SDK** ([Descargar](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Lector Futronic FS88** (o compatible)
- **Windows** (el SDK de Futronic requiere Windows)
- **Futronic SDK** instalado

### Instalación

1. **Clonar el repositorio**:
```bash
git clone https://github.com/Joel-Leon/futronic-api-service.git
cd futronic-api-service
```

2. **Restaurar paquetes**:
```bash
cd FutronicService
dotnet restore
```

3. **Compilar**:
```bash
dotnet build
```

4. **Ejecutar**:
```bash
dotnet run
```

El servicio estará disponible en:
- **HTTP**: `http://localhost:5000`
- **SignalR Hub**: `http://localhost:5000/hubs/fingerprint`

---

## ?? Documentación

### Guías Completas

- ?? **[Guía SignalR](GUIA_SIGNALR_COMPLETA.md)** - Integración de notificaciones en tiempo real
- ?? **[Guía de Configuración](GUIA_CONFIGURACION_API.md)** - Personalizar el comportamiento del lector
- ?? **[Guía de Integración Frontend](GUIA_INTEGRACION_FRONTEND.md)** - Ejemplos de uso desde JavaScript

### Archivos de Configuración

- `fingerprint-config.json` - Archivo de configuración (se genera automáticamente)
- `fingerprint-config.example.json` - Ejemplo de configuración
- `fingerprint-config.schema.json` - Schema JSON para validación
- `appsettings.json` - Configuración de la aplicación

---

## ?? API Endpoints

### ?? Health Check

```http
GET /api/health
```

### ?? Registro de Huellas

```http
POST /api/fingerprint/register-multi
Content-Type: application/json

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "timeout": 30000
}
```

### ? Verificación 1:1

```http
POST /api/fingerprint/verify-simple
Content-Type: application/json

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "timeout": 15000
}
```

### ?? Identificación 1:N

```http
POST /api/fingerprint/identify-live
Content-Type: application/json

{
  "templatesDirectory": "C:/temp/fingerprints",
  "timeout": 15000
}
```

### ?? Configuración

```http
GET /api/configuration              # Obtener configuración
PUT /api/configuration              # Actualizar completa
PATCH /api/configuration            # Actualizar parcial
POST /api/configuration/validate    # Validar sin guardar
POST /api/configuration/reset       # Restaurar por defecto
GET /api/configuration/schema       # Obtener schema
```

---

## ??? Configuración Persistente

El sistema permite personalizar el comportamiento del lector mediante configuración persistente.

### Parámetros Principales

| Parámetro | Tipo | Rango | Descripción |
|-----------|------|-------|-------------|
| `threshold` | int | 0-100 | Umbral de coincidencia (más alto = más estricto) |
| `timeout` | int | 5000-60000 | Timeout en ms para captura |
| `detectFakeFinger` | bool | - | Detectar dedos falsos |
| `maxFramesInTemplate` | int | 1-10 | Frames máximos en template |
| `maxRotation` | int | 0-199 | Rotación máxima permitida |
| `minQuality` | int | 0-100 | Calidad mínima aceptable |
| `disableMIDT` | bool | - | Deshabilitar detección de movimiento fino |

### Ejemplo de Configuración

```javascript
// Alta Seguridad
{
  "threshold": 90,
  "detectFakeFinger": true,
  "maxRotation": 199,
  "minQuality": 70
}

// Balance (Recomendado - Mejor UX)
{
  "threshold": 70,
  "detectFakeFinger": false,
  "maxRotation": 199,
  "minQuality": 50
}

// Velocidad
{
  "threshold": 60,
  "detectFakeFinger": false,
  "disableMIDT": true,
  "maxFramesInTemplate": 3
}
```

Ver **[GUIA_CONFIGURACION_API.md](GUIA_CONFIGURACION_API.md)** para detalles completos.

---

## ?? SignalR - Notificaciones en Tiempo Real

El sistema envía notificaciones en tiempo real durante el proceso de captura:

### Eventos

- **`operation_started`** - Operación iniciada
- **`sample_started`** - Inicio de captura de muestra
- **`sample_captured`** - Muestra capturada (incluye imagen Base64)
- **`operation_completed`** - Operación completada
- **`error`** - Error durante el proceso

### Ejemplo de Integración

```javascript
import * as signalR from '@microsoft/signalr';

// Conectar
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/fingerprint')
  .build();

// Suscribirse a notificaciones
await connection.start();
await connection.invoke('SubscribeToDni', '12345678');

// Escuchar eventos
connection.on('ReceiveProgress', (notification) => {
  console.log(notification.eventType, notification.data);
  
  if (notification.eventType === 'sample_captured') {
    // Mostrar imagen
    const img = document.createElement('img');
    img.src = `data:image/bmp;base64,${notification.data.imageBase64}`;
    document.body.appendChild(img);
  }
});
```

Ver **[GUIA_SIGNALR_COMPLETA.md](GUIA_SIGNALR_COMPLETA.md)** para ejemplos completos.

---

## ?? Configuración del Servicio

### `appsettings.json`

```json
{
  "Fingerprint": {
    "TempPath": "C:/temp/fingerprints",
    "Threshold": 70,
    "Timeout": 30000,
    "DetectFakeFinger": false,
    "MaxRotation": 199,
    "OverwriteExisting": false
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001"
    ]
  }
}
```

---

## ?? Solución de Problemas

### Problema: Muchos rechazos

**Solución**: Reducir restricciones
```http
PATCH /api/configuration
{"threshold": 65, "maxRotation": 180}
```

### Problema: Falsos positivos

**Solución**: Aumentar seguridad
```http
PATCH /api/configuration
{"threshold": 85, "detectFakeFinger": true, "minQuality": 70}
```

### Problema: Capturas muy lentas

**Solución**: Desactivar detección de dedos falsos
```http
PATCH /api/configuration
{"detectFakeFinger": false, "disableMIDT": true}
```

---

## ?? Changelog

### v3.3.1 (2025-01-24)
- ? **DetectFakeFinger desactivado por defecto** para mejor experiencia de usuario
- ? Script de verificación de configuración (`check-config.ps1`)
- ? Documentación actualizada con nuevas recomendaciones

### v3.3 (2025-01-24)
- ? Sistema de configuración persistente
- ? Verificación temprana de duplicados
- ? Más opciones de personalización (DetectFakeFinger, MaxFramesInTemplate, DisableMIDT, MaxRotation, MinQuality)
- ? Endpoints de configuración completos
- ? Validación de configuración
- ? Mejoras en mensajes de error

### v3.2 (2025-01-23)
- ? Notificaciones SignalR en tiempo real
- ? Imágenes en Base64
- ? Prevención de duplicados

---

## ?? Licencia

Este proyecto está bajo la licencia MIT.

---

## ????? Autor

**Joel León**  
GitHub: [@Joel-Leon](https://github.com/Joel-Leon)

---

**? Si este proyecto te fue útil, considera darle una estrella en GitHub ?**
