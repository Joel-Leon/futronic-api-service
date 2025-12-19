# ??? Guía de Configuración de la API de Huellas Dactilares

## ?? Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [Parámetros de Configuración](#parámetros-de-configuración)
4. [Ejemplos de Uso](#ejemplos-de-uso)
5. [Mejores Prácticas](#mejores-prácticas)

---

## ?? Descripción General

El sistema de configuración permite personalizar el comportamiento del lector de huellas dactilares Futronic. La configuración se guarda en un archivo `fingerprint-config.json` y persiste entre reinicios del servicio.

### Características Principales

- ? **Configuración Persistente**: Los cambios se guardan automáticamente
- ? **Validación Automática**: Validación de rangos y tipos de datos
- ? **Valores por Defecto**: Configuración óptima incluida
- ? **Actualización en Caliente**: Sin necesidad de reiniciar el servicio
- ? **Rollback Fácil**: Restaurar valores por defecto cuando sea necesario

---

## ?? Endpoints Disponibles

### 1. Obtener Configuración Actual

```http
GET /api/configuration
```

**Respuesta:**
```json
{
  "success": true,
  "message": "Configuración obtenida correctamente",
  "data": {
    "threshold": 70,
    "timeout": 30000,
    "captureMode": "screen",
    "showImage": true,
    "saveImage": false,
    "detectFakeFinger": true,
    "maxFramesInTemplate": 5,
    "disableMIDT": false,
    "maxRotation": 199,
    "templatePath": "C:/temp/fingerprints",
    "capturePath": "C:/temp/fingerprints/captures",
    "overwriteExisting": false,
    "maxTemplatesPerIdentify": 500,
    "deviceCheckRetries": 3,
    "deviceCheckDelayMs": 1000,
    "minQuality": 50,
    "compressImages": false,
    "imageFormat": "bmp"
  }
}
```

### 2. Actualizar Configuración Completa

```http
PUT /api/configuration
Content-Type: application/json
```

**Body:**
```json
{
  "threshold": 80,
  "timeout": 30000,
  "captureMode": "screen",
  "showImage": true,
  "saveImage": true,
  "detectFakeFinger": true,
  "maxFramesInTemplate": 7,
  "disableMIDT": false,
  "maxRotation": 199,
  "templatePath": "C:/temp/fingerprints",
  "capturePath": "C:/temp/fingerprints/captures",
  "overwriteExisting": false,
  "maxTemplatesPerIdentify": 500,
  "deviceCheckRetries": 3,
  "deviceCheckDelayMs": 1000,
  "minQuality": 60,
  "compressImages": false,
  "imageFormat": "bmp"
}
```

**Respuesta:**
```json
{
  "success": true,
  "message": "? Configuración actualizada correctamente",
  "data": { /* configuración actualizada */ }
}
```

### 3. Actualizar Configuración Parcial (PATCH)

```http
PATCH /api/configuration
Content-Type: application/json
```

**Body (solo los valores a cambiar):**
```json
{
  "threshold": 85,
  "detectFakeFinger": false,
  "maxRotation": 180
}
```

**Respuesta:**
```json
{
  "success": true,
  "message": "? 3 valores actualizados correctamente",
  "data": { /* configuración completa actualizada */ }
}
```

### 4. Validar Configuración (sin guardar)

```http
POST /api/configuration/validate
Content-Type: application/json
```

**Body:**
```json
{
  "threshold": 95,
  "maxRotation": 150,
  "detectFakeFinger": true
}
```

**Respuesta:**
```json
{
  "success": true,
  "message": "? Configuración válida",
  "data": {
    "isValid": true,
    "errors": [],
    "warnings": [
      "?? MaxRotation < 166 puede permitir coincidencias con huellas muy rotadas"
    ]
  }
}
```

### 5. Recargar Configuración desde Archivo

```http
POST /api/configuration/reload
```

**Respuesta:**
```json
{
  "success": true,
  "message": "?? Configuración recargada correctamente",
  "data": { /* configuración recargada */ }
}
```

### 6. Restaurar Configuración por Defecto

```http
POST /api/configuration/reset
```

**Respuesta:**
```json
{
  "success": true,
  "message": "?? Configuración restaurada a valores por defecto",
  "data": { /* configuración por defecto */ }
}
```

### 7. Obtener Schema de Configuración

```http
GET /api/configuration/schema
```

**Respuesta:**
```json
{
  "success": true,
  "message": "Schema de configuración",
  "data": {
    "properties": {
      "threshold": {
        "type": "integer",
        "min": 0,
        "max": 100,
        "default": 70,
        "description": "Umbral de coincidencia (más alto = más estricto)"
      },
      /* ... más propiedades ... */
    }
  }
}
```

---

## ?? Parámetros de Configuración

### 1. `threshold` (integer, 0-100, default: 70)
**Descripción**: Umbral de coincidencia para verificación de huellas.

- **Valores bajos (30-50)**: Más permisivo, mayor tasa de aceptación, mayor riesgo de falsos positivos
- **Valores medios (60-80)**: Balanceado, recomendado para la mayoría de casos
- **Valores altos (85-100)**: Más estricto, menor tasa de aceptación, menor riesgo de falsos positivos

**Ejemplo de uso**:
```json
{ "threshold": 75 }  // Moderadamente estricto
```

---

### 2. `timeout` (integer, 5000-60000 ms, default: 30000)
**Descripción**: Tiempo máximo de espera para captura de huella.

- **5-10 segundos**: Para usuarios experimentados
- **20-30 segundos**: Recomendado para uso general
- **40-60 segundos**: Para usuarios con dificultades o detección de dedos falsos

**Ejemplo de uso**:
```json
{ "timeout": 25000 }  // 25 segundos
```

---

### 3. `captureMode` (string, "screen"|"file", default: "screen")
**Descripción**: Modo de captura de imagen.

- **"screen"**: Imagen temporal en memoria (más rápido)
- **"file"**: Guardar imagen en disco (para auditoría)

**Ejemplo de uso**:
```json
{ "captureMode": "file" }  // Guardar todas las capturas
```

---

### 4. `showImage` (boolean, default: true)
**Descripción**: Mostrar imagen de huella durante captura.

**Ejemplo de uso**:
```json
{ "showImage": true }  // Mostrar feedback visual
```

---

### 5. `saveImage` (boolean, default: false)
**Descripción**: Guardar imagen de huella como archivo BMP.

**Ejemplo de uso**:
```json
{ "saveImage": true }  // Guardar para auditoría
```

---

### 6. `detectFakeFinger` (boolean, default: false)
**Descripción**: Activar detección de dedos artificiales/falsos (liveness detection).

?? **Importante**: 
- Requiere sensor compatible y aumenta el tiempo de captura
- Puede causar rechazos con dedos fríos, húmedos o secos
- Por defecto está **desactivado** para mejor experiencia de usuario
- Activar solo si se requiere alta seguridad

**Ejemplo de uso**:
```json
{ "detectFakeFinger": true }  // Mayor seguridad (pero más rechazos)
{ "detectFakeFinger": false } // Mejor experiencia de usuario (default)
```

---

### 7. `maxFramesInTemplate` (integer, 1-10, default: 5)
**Descripción**: Número máximo de frames/muestras en el template.

- **1-3 frames**: Templates más pequeños, menos precisión
- **4-6 frames**: Balance óptimo (recomendado)
- **7-10 frames**: Mayor precisión, templates más grandes

**Ejemplo de uso**:
```json
{ "maxFramesInTemplate": 5 }  // Balance óptimo
```

---

### 8. `disableMIDT` (boolean, default: false)
**Descripción**: Deshabilitar detección de movimiento incremental del dedo.

- **false**: Detecta movimiento fino del dedo (más preciso pero más lento)
- **true**: Solo detecta dedo colocado/retirado (más rápido)

**Ejemplo de uso**:
```json
{ "disableMIDT": false }  // Máxima precisión
```

---

### 9. `maxRotation` (integer, 0-199, default: 199)
**Descripción**: Control de rotación máxima permitida para matching.

- **0-165**: Muy tolerante a rotación (puede generar falsos positivos)
- **166-180**: Tolerancia moderada
- **181-199**: Estricto (requiere mejor alineación)

?? **Nota**: El valor del SDK por defecto es **166**. Valores más altos = **menor tolerancia** a rotación.

**Ejemplo de uso**:
```json
{ "maxRotation": 199 }  // Muy estricto (recomendado para seguridad)
```

---

### 10. `minQuality` (integer, 0-100, default: 50)
**Descripción**: Calidad mínima aceptable para una muestra.

- **20-40**: Baja calidad aceptada (no recomendado)
- **50-70**: Calidad media (recomendado)
- **80-100**: Alta calidad requerida (puede rechazar muchas capturas)

**Ejemplo de uso**:
```json
{ "minQuality": 60 }  // Calidad media-alta
```

---

### 11. `templatePath` (string, default: "C:/temp/fingerprints")
**Descripción**: Ruta de almacenamiento de plantillas/templates.

**Ejemplo de uso**:
```json
{ "templatePath": "D:/biometrics/templates" }
```

---

### 12. `capturePath` (string, default: "C:/temp/fingerprints/captures")
**Descripción**: Ruta de almacenamiento de imágenes capturadas.

**Ejemplo de uso**:
```json
{ "capturePath": "D:/biometrics/captures" }
```

---

### 13. `overwriteExisting` (boolean, default: false)
**Descripción**: Permitir sobrescribir huellas existentes.

?? **Importante**: Si es `false`, intentar registrar una huella existente retornará error.

**Ejemplo de uso**:
```json
{ "overwriteExisting": true }  // Permitir sobrescribir
```

---

### 14. `maxTemplatesPerIdentify` (integer, 1-10000, default: 500)
**Descripción**: Máximo de plantillas a comparar en identificación 1:N.

**Ejemplo de uso**:
```json
{ "maxTemplatesPerIdentify": 1000 }  // Búsqueda en más templates
```

---

### 15. `compressImages` (boolean, default: false)
**Descripción**: Habilitar compresión de imágenes en Base64.

**Ejemplo de uso**:
```json
{ "compressImages": true }  // Menor tamaño de transmisión
```

---

### 16. `imageFormat` (string, "bmp"|"png"|"jpg", default: "bmp")
**Descripción**: Formato de imagen a retornar.

- **bmp**: Sin compresión, mejor calidad
- **png**: Compresión sin pérdida
- **jpg**: Compresión con pérdida, menor tamaño

**Ejemplo de uso**:
```json
{ "imageFormat": "png" }  // Balance calidad/tamaño
```

---

## ?? Ejemplos de Uso

### Ejemplo 1: Configuración para Alta Seguridad

```javascript
// JavaScript/TypeScript
const highSecurityConfig = {
  threshold: 90,                 // Muy estricto
  detectFakeFinger: true,        // Detectar dedos falsos
  maxRotation: 199,              // Rotación muy limitada
  minQuality: 70,                // Alta calidad requerida
  maxFramesInTemplate: 7,        // Más muestras para mejor template
  overwriteExisting: false       // No permitir sobrescribir
};

const response = await fetch('http://localhost:5000/api/configuration', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(highSecurityConfig)
});

const result = await response.json();
console.log(result.message);
```

---

### Ejemplo 2: Configuración para Velocidad

```javascript
const fastConfig = {
  threshold: 60,                 // Menos estricto
  detectFakeFinger: false,       // Desactivar detección (más rápido)
  disableMIDT: true,             // Deshabilitar detección fina
  maxFramesInTemplate: 3,        // Menos muestras
  timeout: 10000                 // Timeout corto
};

const response = await fetch('http://localhost:5000/api/configuration', {
  method: 'PATCH',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(fastConfig)
});
```

---

### Ejemplo 3: Configuración Balanceada (Recomendada)

```javascript
const balancedConfig = {
  threshold: 70,                 // Balance
  detectFakeFinger: false,       // Desactivado por defecto (mejor UX)
  maxRotation: 199,              // Estricto en rotación
  minQuality: 50,                // Calidad media
  maxFramesInTemplate: 5,        // Balance
  disableMIDT: false,            // Precisión
  timeout: 30000,                // 30 segundos
  saveImage: false,              // No guardar imágenes
  overwriteExisting: false       // No sobrescribir
};
```

---

### Ejemplo 4: Actualizar Solo Threshold

```bash
# cURL
curl -X PATCH http://localhost:5000/api/configuration \
  -H "Content-Type: application/json" \
  -d '{"threshold": 85}'
```

```javascript
// JavaScript
await fetch('http://localhost:5000/api/configuration', {
  method: 'PATCH',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ threshold: 85 })
});
```

---

### Ejemplo 5: Validar Antes de Aplicar

```javascript
// Primero validar
const configToTest = {
  threshold: 95,
  maxRotation: 150,
  detectFakeFinger: true
};

const validationResponse = await fetch('http://localhost:5000/api/configuration/validate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(configToTest)
});

const validation = await validationResponse.json();

if (validation.data.isValid) {
  if (validation.data.warnings.length > 0) {
    console.warn('?? Advertencias:', validation.data.warnings);
    // Preguntar al usuario si desea continuar
  }
  
  // Aplicar configuración
  await fetch('http://localhost:5000/api/configuration', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(configToTest)
  });
} else {
  console.error('? Errores:', validation.data.errors);
}
```

---

### Ejemplo 6: Restaurar Configuración por Defecto

```javascript
// Restaurar a valores por defecto
const resetResponse = await fetch('http://localhost:5000/api/configuration/reset', {
  method: 'POST'
});

const result = await resetResponse.json();
console.log(result.message); // "?? Configuración restaurada a valores por defecto"
```

---

## ?? Mejores Prácticas

### 1. **Validar Antes de Aplicar**
Siempre use el endpoint `/validate` antes de aplicar cambios importantes:

```javascript
// ? Buena práctica
const validation = await validateConfig(newConfig);
if (validation.isValid && confirm(`Advertencias: ${validation.warnings}`)) {
  await updateConfig(newConfig);
}
```

---

### 2. **Usar PATCH para Cambios Pequeños**
No es necesario enviar toda la configuración si solo cambia un valor:

```javascript
// ? Eficiente
await fetch('/api/configuration', {
  method: 'PATCH',
  body: JSON.stringify({ threshold: 80 })
});

// ? Ineficiente
await fetch('/api/configuration', {
  method: 'PUT',
  body: JSON.stringify(entireConfig)  // Envía 16 campos cuando solo necesitas 1
});
```

---

### 3. **Balance Seguridad vs Usabilidad**

| Caso de Uso | Threshold | MaxRotation | DetectFakeFinger | MinQuality |
|-------------|-----------|-------------|------------------|------------|
| ?? **Bancos** | 85-95 | 199 | true | 70-80 |
| ?? **Empresas** | 70-80 | 199 | true | 60-70 |
| ?? **Escuelas** | 60-70 | 180 | false | 50-60 |
| ?? **Hogar** | 50-60 | 166 | false | 40-50 |

---

### 4. **Monitoreo de Configuración**
Implemente logging para cambios de configuración:

```javascript
function logConfigChange(oldConfig, newConfig) {
  const changes = Object.keys(newConfig)
    .filter(key => oldConfig[key] !== newConfig[key])
    .map(key => `${key}: ${oldConfig[key]} ? ${newConfig[key]}`);
  
  console.log(`?? Configuración actualizada:\n${changes.join('\n')}`);
}
```

---

### 5. **Backup de Configuración**
Guarde copias de configuraciones importantes:

```javascript
// Obtener configuración actual
const currentConfig = await fetch('/api/configuration').then(r => r.json());

// Guardar backup
localStorage.setItem('config-backup-2025-01-24', JSON.stringify(currentConfig.data));

// Restaurar desde backup si es necesario
const backup = JSON.parse(localStorage.getItem('config-backup-2025-01-24'));
await fetch('/api/configuration', {
  method: 'PUT',
  body: JSON.stringify(backup)
});
```

---

### 6. **Configuración por Entorno**

```javascript
const configs = {
  development: {
    threshold: 60,
    detectFakeFinger: false,
    timeout: 15000
  },
  production: {
    threshold: 80,
    detectFakeFinger: true,
    timeout: 30000
  }
};

const env = process.env.NODE_ENV || 'development';
await updateConfig(configs[env]);
```

---

## ?? Solución de Problemas

### Problema: "Configuration inválida"
**Solución**: Verificar rangos de valores con `/schema`:
```javascript
const schema = await fetch('/api/configuration/schema').then(r => r.json());
console.log(schema.data.properties);
```

---

### Problema: Cambios no se aplican
**Solución**: Recargar configuración:
```javascript
await fetch('/api/configuration/reload', { method: 'POST' });
```

---

### Problema: Muchos rechazos de huellas
**Solución**: Reducir `threshold` y `maxRotation`:
```javascript
await fetch('/api/configuration', {
  method: 'PATCH',
  body: JSON.stringify({
    threshold: 65,      // Más tolerante
    maxRotation: 180    // Más tolerante a rotación
  })
});
```

---

### Problema: Falsos positivos
**Solución**: Aumentar seguridad:
```javascript
await fetch('/api/configuration', {
  method: 'PATCH',
  body: JSON.stringify({
    threshold: 85,           // Más estricto
    detectFakeFinger: true,  // Detectar falsificaciones
    minQuality: 70           // Mayor calidad requerida
  })
});
```

---

## ?? Referencias

- **Documentación Futronic SDK**: Consulte el manual del SDK para detalles técnicos
- **Archivo de configuración**: `fingerprint-config.json` (se crea automáticamente)
- **Valores por defecto**: Ver código en `FingerprintConfiguration.cs`

---

**?? Última Actualización:** 2025-01-24  
**?? Versión:** 1.0  
**? Estado:** Producción Ready
