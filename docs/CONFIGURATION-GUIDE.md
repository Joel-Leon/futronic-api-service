# ?? Guía Completa de Configuración - Sistema de Huellas Dactilares

## ?? Índice
1. [Resumen de Configuraciones](#resumen-de-configuraciones)
2. [Parámetros Detallados](#parámetros-detallados)
3. [Perfiles de Configuración](#perfiles-de-configuración)
4. [Cómo Configurar](#cómo-configurar)
5. [Validaciones y Restricciones](#validaciones-y-restricciones)
6. [Mejores Prácticas](#mejores-prácticas)
7. [Troubleshooting](#troubleshooting)

---

## ?? Resumen de Configuraciones

### **Tabla Completa de Parámetros**

| # | Parámetro | Tipo | Rango/Valores | Por Defecto | Categoría |
|---|-----------|------|---------------|-------------|-----------|
| 1 | `threshold` | `int` | `0-100` | `70` | Seguridad |
| 2 | `timeout` | `int` | `5000-60000` | `30000` | Rendimiento |
| 3 | `captureMode` | `string` | `screen\|file` | `screen` | Captura |
| 4 | `showImage` | `bool` | `true\|false` | `true` | Captura |
| 5 | `saveImage` | `bool` | `true\|false` | `false` | Captura |
| 6 | `detectFakeFinger` | `bool` | `true\|false` | `false` | Seguridad |
| 7 | `maxFramesInTemplate` | `int` | `1-10` | `5` | Templates |
| 8 | `disableMIDT` | `bool` | `true\|false` | `false` | Rendimiento |
| 9 | `maxRotation` | `int` | `0-199` | `199` | Seguridad |
| 10 | `templatePath` | `string` | Ruta válida | `C:/temp/fingerprints` | Almacenamiento |
| 11 | `capturePath` | `string` | Ruta válida | `C:/temp/fingerprints/captures` | Almacenamiento |
| 12 | `overwriteExisting` | `bool` | `true\|false` | `false` | Almacenamiento |
| 13 | `maxTemplatesPerIdentify` | `int` | `1-10000` | `500` | Identificación |
| 14 | `deviceCheckRetries` | `int` | `1-10` | `3` | Dispositivo |
| 15 | `deviceCheckDelayMs` | `int` | `100-5000` | `1000` | Dispositivo |
| 16 | `minQuality` | `int` | `0-100` | `50` | Calidad |
| 17 | `compressImages` | `bool` | `true\|false` | `false` | Captura |
| 18 | `imageFormat` | `string` | `bmp\|png\|jpg` | `bmp` | Captura |

---

## ?? Parámetros Detallados

### **1. ?? THRESHOLD (Umbral de Coincidencia)**

```json
{
  "threshold": 70
}
```

**Descripción:**  
Define el nivel de similitud requerido para considerar que dos huellas coinciden.

**Rango:** `0` - `100`  
**Por Defecto:** `70`  
**Unidad:** Porcentaje de similitud

**Valores Recomendados:**
- **30-50:** Muy permisivo (alto riesgo de falsos positivos)
- **60-70:** Balanceado - **RECOMENDADO para uso general**
- **80-90:** Estricto (seguridad alta)
- **90-100:** Muy estricto (puede rechazar huellas legítimas)

**Impacto:**
- ?? Valor más alto ? ? Más seguro, ? Más rechazos
- ?? Valor más bajo ? ? Más permisivo, ? Menos seguro

**Casos de Uso:**
- **Bancos/Gobierno:** `85-95`
- **Empresas:** `70-80`
- **Control de acceso físico:** `60-70`
- **Testing/Desarrollo:** `50-60`

---

### **2. ?? TIMEOUT (Tiempo Máximo de Captura)**

```json
{
  "timeout": 30000
}
```

**Descripción:**  
Tiempo máximo en milisegundos para completar una operación de captura de huella.

**Rango:** `5000` - `60000` ms  
**Por Defecto:** `30000` ms (30 segundos)  
**Unidad:** Milisegundos

**Valores Recomendados:**
- **5000-10000 ms:** Captura rápida (puede fallar con usuarios lentos)
- **15000-30000 ms:** Balanceado - **RECOMENDADO**
- **40000-60000 ms:** Paciente (usuarios con dificultades motoras)

**Consideraciones:**
- ?? Si `detectFakeFinger = true`, usar **mínimo 10000 ms**
- ?? Para aplicaciones de alta velocidad: `10000-15000 ms`
- ?? Para usuarios de tercera edad: `40000-60000 ms`

**Impacto:**
- ?? Valor más alto ? ? Más tolerante, ? Más lento
- ?? Valor más bajo ? ? Más rápido, ? Menos tolerante

---

### **3. ??? CAPTUREMODE (Modo de Captura)**

```json
{
  "captureMode": "screen"
}
```

**Descripción:**  
Define cómo se maneja la imagen capturada durante el proceso.

**Valores Permitidos:**
- `"screen"` - Captura temporal en memoria (por defecto)
- `"file"` - Guarda la imagen en disco

**Por Defecto:** `"screen"`

**Características:**

| Modo | Ventajas | Desventajas | Uso Recomendado |
|------|----------|-------------|-----------------|
| `screen` | ? Rápido<br>? No ocupa disco<br>? Más seguro (no persiste) | ? No se puede auditar<br>? No hay registro visual | Producción normal |
| `file` | ? Auditoría completa<br>? Depuración visual<br>? Análisis forense | ? Más lento<br>? Ocupa espacio<br>? Riesgo de privacidad | Testing, depuración |

**Ejemplo de Uso:**
```csharp
// Desarrollo/Testing
{ "captureMode": "file", "saveImage": true }

// Producción
{ "captureMode": "screen", "saveImage": false }
```

---

### **4. ??? SHOWIMAGE (Mostrar Imagen en Pantalla)**

```json
{
  "showImage": true
}
```

**Descripción:**  
Muestra la imagen de la huella capturada en la interfaz durante el proceso.

**Valores:** `true` | `false`  
**Por Defecto:** `true`

**Cuándo usar `true`:**
- ? Aplicaciones con UI interactiva
- ? Feedback visual al usuario
- ? Depuración y testing

**Cuándo usar `false`:**
- ? Servicios sin interfaz gráfica
- ? APIs backend
- ? Mejor rendimiento (menos procesamiento gráfico)

---

### **5. ?? SAVEIMAGE (Guardar Imagen como Archivo)**

```json
{
  "saveImage": false
}
```

**Descripción:**  
Guarda la imagen capturada como archivo BMP en disco.

**Valores:** `true` | `false`  
**Por Defecto:** `false`

**Impacto:**
- `true`: Cada captura genera un archivo `.bmp` en `capturePath`
- `false`: Las imágenes solo se mantienen en memoria temporalmente

**Consideraciones:**
- ?? Cada imagen ocupa aproximadamente **200-300 KB**
- ?? Revisar espacio en disco periódicamente si está activado
- ?? Considerar privacidad de datos biométricos

---

### **6. ?? DETECTFAKEFINGER (Detección de Liveness)**

```json
{
  "detectFakeFinger": false
}
```

**Descripción:**  
Activa la detección de dedos falsos o artificiales (liveness detection).

**Valores:** `true` | `false`  
**Por Defecto:** `false`

**Cómo Funciona:**
El SDK de Futronic analiza patrones de sudoración, temperatura y textura de la piel para detectar:
- ? Dedos de silicona
- ? Impresiones en papel
- ? Fotos de huellas
- ? Dedos sin vida (cadáveres)

**Requisitos:**
- ?? **Requiere `timeout >= 10000` ms** (10 segundos mínimo)
- ?? Aumenta el tiempo de captura en **3-5 segundos**
- ?? Puede rechazar dedos legítimos si están muy secos

**Casos de Uso:**
- ? **Alta Seguridad:** Bancos, gobierno, acceso a datos sensibles
- ? **No Recomendado:** Acceso rápido, torniquetes, control de asistencia

**Configuración Recomendada con Liveness:**
```json
{
  "detectFakeFinger": true,
  "timeout": 40000,
  "threshold": 80
}
```

---

### **7. ??? MAXFRAMESINTEMPLATE (Frames en Template)**

```json
{
  "maxFramesInTemplate": 5
}
```

**Descripción:**  
Número de frames (imágenes) que componen un template biométrico. **Este valor se usa como default cuando no se especifica `sampleCount` en el request de registro.**

**Rango:** `1` - `10`  
**Por Defecto:** `5`

**?? IMPORTANTE:**  
- Este valor se usa automáticamente en `/api/fingerprint/register-multi` si NO se especifica `sampleCount` en el request
- Si pasas `sampleCount` en el request, ese valor tiene prioridad sobre `maxFramesInTemplate`
- Ejemplo: Si `maxFramesInTemplate: 6` y no pasas `sampleCount` ? usará 6 muestras
- Ejemplo: Si `maxFramesInTemplate: 6` pero pasas `sampleCount: 3` ? usará 3 muestras

**Impacto:**

| Frames | Tamaño Template | Precisión | Velocidad | Uso Recomendado |
|--------|----------------|-----------|-----------|-----------------|
| 1-2 | ~500 bytes | Baja | ? Muy rápido | Testing rápido |
| 3-5 | ~1-2 KB | ? Buena | ?? Rápido | **RECOMENDADO** |
| 6-7 | ~2-3 KB | ?? Muy buena | ?? Medio | Alta seguridad |
| 8-10 | ~3-4 KB | ??? Excelente | ?? Lento | Máxima precisión |

**Consideraciones:**
- ?? Más frames = Mejor precisión, pero templates más grandes
- ?? Con 10,000 usuarios y 7 frames ? ~30 MB de espacio
- ?? Para identificación 1:N rápida, usar 3-5 frames

---

### **8. ?? DISABLEMIDT (Deshabilitar Detección de Movimiento)**

```json
{
  "disableMIDT": false
}
```

**Descripción:**  
MIDT (Motion Incremental Detection Technology) detecta movimientos finos del dedo durante la captura.

**Valores:** `true` | `false`  
**Por Defecto:** `false`

**Diferencias:**

| MIDT Activo (`false`) | MIDT Desactivado (`true`) |
|----------------------|--------------------------|
| ? Mayor precisión | ? Más rápido |
| ? Detecta reposicionamiento | ? Simplificado |
| ?? Más lento | ? Menos preciso |
| ?? Mejor experiencia usuario | ?? Solo detecta poner/quitar dedo |

**Cuándo Desactivar (true):**
- ?? Aplicaciones de alta velocidad
- ?? Torniquetes de acceso
- ?? Dispositivos con poco poder de cómputo

**Cuándo Mantener Activo (false):**
- ?? Bancos y entidades financieras
- ?? Acceso a sistemas críticos
- ?? Mejor experiencia de usuario

---

### **9. ?? MAXROTATION (Rotación Máxima Permitida)**

```json
{
  "maxRotation": 199
}
```

**Descripción:**  
Controla cuánta rotación angular se permite entre la huella registrada y la capturada.

**Rango:** `0` - `199`  
**Por Defecto:** `199`  
**Unidad:** Valor interno del SDK (no son grados)

**Valores de Referencia:**

| Valor | Tolerancia | Descripción | Uso Recomendado |
|-------|------------|-------------|-----------------|
| 0-100 | Muy baja | Requiere alineación casi perfecta | ? No recomendado |
| 101-165 | Baja | Alineación bastante precisa | Testing controlado |
| 166 | **Default SDK** | Balanceado | Uso general |
| 167-185 | Media-Alta | Más restrictivo | Seguridad media |
| 186-199 | Muy alta | Máxima restricción | **Alta seguridad** |

**Impacto:**
- ?? Valor más alto (199) ? ? Menos tolerante a rotación ? ? Más seguro
- ?? Valor más bajo (166) ? ? Más tolerante a rotación ? ? Menos seguro

**Recomendaciones:**
- **Usuarios Entrenados:** `185-199` (saben colocar el dedo correctamente)
- **Usuarios Generales:** `170-185` (balanceado)
- **Usuarios No Entrenados:** `166-175` (más tolerante)

**?? ADVERTENCIA del Sistema:**
Si `maxRotation < 166`, recibirás un warning:
> "MaxRotation < 166 puede permitir coincidencias con huellas muy rotadas"

---

### **10. ?? TEMPLATEPATH (Ruta de Templates)**

```json
{
  "templatePath": "C:/temp/fingerprints"
}
```

**Descripción:**  
Directorio donde se almacenan los templates biométricos (archivos `.tml`).

**Tipo:** `string` (ruta absoluta)  
**Por Defecto:** `"C:/temp/fingerprints"`

**Estructura de Directorios:**
```
templatePath/
??? 12345678/              # DNI del usuario
?   ??? index/             # Dedo: índice derecho
?   ?   ??? 12345678.tml   # Template biométrico
?   ?   ??? images/        # Imágenes capturadas
?   ?   ?   ??? 12345678_best_01.bmp
?   ?   ?   ??? 12345678_best_02.bmp
?   ?   ??? metadata.json  # Información de captura
?   ??? thumb/             # Dedo: pulgar derecho
?       ??? 12345678.tml
??? 87654321/
    ??? index/
        ??? 87654321.tml
```

**Consideraciones:**
- ?? Cada template ocupa ~1-4 KB
- ??? Cada imagen ocupa ~200-300 KB
- ?? **IMPORTANTE:** Proteger este directorio (datos biométricos sensibles)
- ?? Hacer backups periódicos
- ?? Usar SSD para mejor rendimiento en identificación 1:N

**Validación:**
- ? El directorio se crea automáticamente si no existe
- ?? Debe tener permisos de lectura/escritura

---

### **11. ?? CAPTUREPATH (Ruta de Capturas Temporales)**

```json
{
  "capturePath": "C:/temp/fingerprints/captures"
}
```

**Descripción:**  
Directorio para capturas temporales (testing, depuración).

**Tipo:** `string` (ruta absoluta)  
**Por Defecto:** `"C:/temp/fingerprints/captures"`

**Uso:**
- Capturas del endpoint `/api/fingerprint/capture` (sin DNI)
- Testing temporal de dispositivo
- Depuración de calidad de imagen

**Estructura:**
```
capturePath/
??? capture_20231219_143025/
?   ??? capture_20231219_143025.tml
?   ??? images/
?       ??? capture_20231219_143025.bmp
??? capture_20231219_143126/
    ??? ...
```

**Mantenimiento:**
- ?? Limpiar periódicamente (datos temporales)
- ?? Revisar espacio en disco
- ?? No usar para producción (solo testing)

---

### **12. ?? OVERWRITEEXISTING (Sobrescribir Huellas Existentes)**

```json
{
  "overwriteExisting": false
}
```

**Descripción:**  
Permite sobrescribir templates biométricos ya existentes.

**Valores:** `true` | `false`  
**Por Defecto:** `false`

**Comportamiento:**

| Valor | Comportamiento | Uso Recomendado |
|-------|---------------|-----------------|
| `false` | ? Error si ya existe huella<br>Código: `FILE_EXISTS` | **Producción** (evita borrados accidentales) |
| `true` | ? Sobrescribe sin preguntar<br>?? Pierde datos anteriores | Testing, actualizaciones autorizadas |

**Escenarios:**

**Producción (false):**
```json
{
  "overwriteExisting": false
}
```
Si intentas registrar un DNI que ya tiene huella:
```json
{
  "success": false,
  "message": "Ya existe una huella registrada para este DNI y dedo",
  "errorCode": "FILE_EXISTS"
}
```

**Testing/Actualización (true):**
```json
{
  "overwriteExisting": true
}
```
Sobrescribe automáticamente sin error.

**Mejores Prácticas:**
- ?? **Producción:** `false` + endpoint específico para actualización con confirmación
- ?? **Testing:** `true` para pruebas rápidas
- ?? **Auditoría:** Guardar backup antes de sobrescribir

---

### **13. ?? MAXTEMPLATESPERIDENTIFY (Límite de Comparaciones)**

```json
{
  "maxTemplatesPerIdentify": 500
}
```

**Descripción:**  
Número máximo de templates a comparar en una operación de identificación 1:N.

**Rango:** `1` - `10000`  
**Por Defecto:** `500`

**Rendimiento:**

| Templates | Tiempo Aprox. | Uso Recomendado |
|-----------|---------------|-----------------|
| 1-100 | < 1 segundo | Pequeñas organizaciones |
| 101-500 | 1-3 segundos | **Uso general** |
| 501-1000 | 3-6 segundos | Medianas empresas |
| 1001-5000 | 6-30 segundos | Grandes organizaciones |
| 5001-10000 | 30-60 segundos | ?? No recomendado (muy lento) |

**Optimizaciones:**
- ?? Usar índices/filtros previos (área geográfica, departamento, etc.)
- ?? Dividir base de datos en grupos más pequeños
- ?? Usar SSD para almacenamiento de templates
- ? Configurar `maxFramesInTemplate: 3-5` para mayor velocidad

**Ejemplo de Estrategia:**
```json
// Búsqueda en dos fases
{
  // Fase 1: Búsqueda rápida en grupo activo (500 usuarios activos)
  "maxTemplatesPerIdentify": 500
}

// Si no encuentra ? Fase 2: Búsqueda completa (todos los usuarios)
```

---

### **14. ?? DEVICECHECKRETRIES (Reintentos de Conexión)**

```json
{
  "deviceCheckRetries": 3
}
```

**Descripción:**  
Número de intentos para conectar con el dispositivo Futronic al iniciar el servicio.

**Rango:** `1` - `10`  
**Por Defecto:** `3`

**Uso:**
- Se ejecuta al arrancar el servicio
- Intenta inicializar el SDK de Futronic
- Útil cuando el dispositivo USB tarda en conectarse

**Recomendaciones:**
- ??? **PC Estables:** `2-3` reintentos
- ?? **USB Hubs:** `4-5` reintentos (pueden tardar más)
- ?? **Producción Crítica:** `5-7` reintentos

**Combinar con `deviceCheckDelayMs`:**
```json
{
  "deviceCheckRetries": 5,
  "deviceCheckDelayMs": 2000  // 2 segundos entre reintentos
}
// Total: hasta 10 segundos de espera
```

---

### **15. ? DEVICECHECKDELAYMS (Delay entre Reintentos)**

```json
{
  "deviceCheckDelayMs": 1000
}
```

**Descripción:**  
Tiempo de espera en milisegundos entre reintentos de conexión del dispositivo.

**Rango:** `100` - `5000` ms  
**Por Defecto:** `1000` ms

**Valores Recomendados:**
- **500-1000 ms:** Conexiones rápidas (USB directo)
- **1500-2000 ms:** USB Hubs o extensiones
- **2500-3000 ms:** Entornos con múltiples dispositivos USB

---

### **16. ? MINQUALITY (Calidad Mínima de Muestra)**

```json
{
  "minQuality": 50
}
```

**Descripción:**  
Calidad mínima aceptable para considerar válida una muestra capturada.

**Rango:** `0` - `100`  
**Por Defecto:** `50`

**Escala de Calidad:**

| Rango | Calidad | Descripción | Recomendación |
|-------|---------|-------------|---------------|
| 0-20 | Muy baja | Imagen borrosa, poco detalle | ? Rechazar |
| 21-40 | Baja | Detalles mínimos visibles | ?? Solo para testing |
| 41-60 | Aceptable | Detalles suficientes | ? Uso general |
| 61-80 | Buena | Buenos detalles | ? **Recomendado** |
| 81-100 | Excelente | Máximo detalle | ? Alta seguridad |

**Configuraciones por Caso:**

```json
// Alta seguridad
{ "minQuality": 70 }

// Balanceado (RECOMENDADO)
{ "minQuality": 50 }

// Tolerante (usuarios con dedos secos/dañados)
{ "minQuality": 35 }

// Testing
{ "minQuality": 20 }
```

**?? Advertencia:**
Si configuras `minQuality < 30`, recibirás:
> "MinQuality < 30 puede aceptar huellas de muy baja calidad"

---

### **17. ??? COMPRESSIMAGES (Comprimir Imágenes Base64)**

```json
{
  "compressImages": false
}
```

**Descripción:**  
Comprime las imágenes antes de convertirlas a Base64 para notificaciones SignalR.

**Valores:** `true` | `false`  
**Por Defecto:** `false`

**Impacto:**

| Valor | Tamaño | Calidad Visual | Rendimiento | Uso |
|-------|--------|----------------|-------------|-----|
| `false` | ~200-300 KB | ? Original | ?? Normal | Calidad total |
| `true` | ~50-100 KB | ?? Reducida | ? Mejor | Redes lentas |

**Cuándo Activar:**
- ?? Redes lentas o limitadas
- ?? Aplicaciones móviles
- ?? Conexiones remotas/VPN

**Cuándo Desactivar:**
- ??? Red local de alta velocidad
- ?? Necesitas máxima calidad visual
- ?? Ambiente de producción controlado

---

### **18. ?? IMAGEFORMAT (Formato de Imagen)**

```json
{
  "imageFormat": "bmp"
}
```

**Descripción:**  
Formato de las imágenes guardadas y enviadas por SignalR.

**Valores Permitidos:** `"bmp"` | `"png"` | `"jpg"`  
**Por Defecto:** `"bmp"`

**Comparación:**

| Formato | Tamaño | Calidad | Compresión | Compatibilidad | Uso Recomendado |
|---------|--------|---------|------------|----------------|-----------------|
| `bmp` | Grande (~300 KB) | ??? Perfecta | ? Ninguna | ??? Total | **Default, máxima calidad** |
| `png` | Medio (~100 KB) | ?? Muy buena | ? Sin pérdida | ?? Alta | Balance calidad/tamaño |
| `jpg` | Pequeño (~30 KB) | ? Aceptable | ?? Con pérdida | ??? Total | Redes lentas, móviles |

**Configuraciones por Escenario:**

```json
// Máxima calidad (producción local)
{ "imageFormat": "bmp", "compressImages": false }

// Balance calidad/tamaño
{ "imageFormat": "png", "compressImages": false }

// Optimizado para red lenta
{ "imageFormat": "jpg", "compressImages": true }
```

---

## ?? Perfiles de Configuración

### **Perfil 1: ?? Alta Seguridad (Bancos, Gobierno)**

```json
{
  "threshold": 90,
  "timeout": 60000,
  "captureMode": "file",
  "showImage": true,
  "saveImage": true,
  "detectFakeFinger": true,
  "maxFramesInTemplate": 7,
  "disableMIDT": false,
  "maxRotation": 199,
  "templatePath": "D:/SecureStorage/Fingerprints",
  "capturePath": "D:/SecureStorage/Captures",
  "overwriteExisting": false,
  "maxTemplatesPerIdentify": 1000,
  "deviceCheckRetries": 5,
  "deviceCheckDelayMs": 2000,
  "minQuality": 70,
  "compressImages": false,
  "imageFormat": "png"
}
```

**Características:**
- ? Máxima seguridad y precisión
- ? Detección de dedos falsos activa
- ? Auditoría completa (guarda imágenes)
- ?? Proceso más lento (60 segundos timeout)

---

### **Perfil 2: ?? Balanceado (Empresas, Oficinas)**

```json
{
  "threshold": 70,
  "timeout": 30000,
  "captureMode": "screen",
  "showImage": true,
  "saveImage": false,
  "detectFakeFinger": false,
  "maxFramesInTemplate": 5,
  "disableMIDT": false,
  "maxRotation": 185,
  "templatePath": "C:/FingerprintData/Templates",
  "capturePath": "C:/FingerprintData/Captures",
  "overwriteExisting": false,
  "maxTemplatesPerIdentify": 500,
  "deviceCheckRetries": 3,
  "deviceCheckDelayMs": 1000,
  "minQuality": 50,
  "compressImages": false,
  "imageFormat": "bmp"
}
```

**Características:**
- ? Balance perfecto entre seguridad y velocidad
- ? No guarda imágenes (más rápido)
- ? **RECOMENDADO PARA LA MAYORÍA DE CASOS**

---

### **Perfil 3: ?? Alta Velocidad (Torniquetes, Acceso Físico)**

```json
{
  "threshold": 60,
  "timeout": 15000,
  "captureMode": "screen",
  "showImage": false,
  "saveImage": false,
  "detectFakeFinger": false,
  "maxFramesInTemplate": 3,
  "disableMIDT": true,
  "maxRotation": 170,
  "templatePath": "C:/FastAccess/Templates",
  "capturePath": "C:/FastAccess/Captures",
  "overwriteExisting": false,
  "maxTemplatesPerIdentify": 300,
  "deviceCheckRetries": 2,
  "deviceCheckDelayMs": 500,
  "minQuality": 40,
  "compressImages": true,
  "imageFormat": "jpg"
}
```

**Características:**
- ? Máxima velocidad
- ? Ideal para acceso de alta frecuencia
- ?? Seguridad reducida (aceptable para control de acceso físico)

---

### **Perfil 4: ?? Testing y Desarrollo**

```json
{
  "threshold": 50,
  "timeout": 20000,
  "captureMode": "file",
  "showImage": true,
  "saveImage": true,
  "detectFakeFinger": false,
  "maxFramesInTemplate": 5,
  "disableMIDT": false,
  "maxRotation": 166,
  "templatePath": "C:/DevFingerprints/Templates",
  "capturePath": "C:/DevFingerprints/Captures",
  "overwriteExisting": true,
  "maxTemplatesPerIdentify": 100,
  "deviceCheckRetries": 3,
  "deviceCheckDelayMs": 1000,
  "minQuality": 30,
  "compressImages": false,
  "imageFormat": "bmp"
}
```

**Características:**
- ? Permite sobrescribir huellas (`overwriteExisting: true`)
- ? Guarda todas las imágenes para análisis
- ? Tolerante (threshold bajo, minQuality bajo)
- ? Ideal para pruebas rápidas

---

## ?? Cómo Configurar

### ? **IMPORTANTE: Recarga Automática de Configuración**

> **? Las configuraciones se aplican INMEDIATAMENTE sin reiniciar el servicio.**
> 
> Cuando actualizas la configuración a través de la API REST, el sistema:
> 1. Guarda la configuración en `fingerprint-config.json`
> 2. **Recarga automáticamente** la configuración en el servicio de huellas
> 3. Las próximas operaciones (captura, verificación, registro) usan la nueva configuración
> 
> **NO es necesario reiniciar el servicio** para aplicar cambios.

---

### **Método 1: API REST (Recomendado)**

#### **Actualizar Configuración Completa (PUT)**
```bash
curl -X PUT http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 80,
    "timeout": 40000,
    "detectFakeFinger": true,
    "maxRotation": 190,
    "minQuality": 60
  }'
```

#### **Actualizar Campos Específicos (PATCH)**
```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 85
  }'
```

#### **Validar Antes de Guardar**
```bash
curl -X POST http://localhost:5000/api/fingerprint/config/validate \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 120,
    "timeout": 1000
  }'

# Respuesta:
{
  "success": true,
  "data": {
    "isValid": false,
    "errors": [
      "Threshold debe estar entre 0 y 100",
      "Timeout debe estar entre 5000 y 60000"
    ],
    "warnings": []
  }
}
```

#### **Restaurar a Valores por Defecto**
```bash
curl -X POST http://localhost:5000/api/fingerprint/config/reset
```

#### **Recargar desde Archivo**
```bash
curl -X POST http://localhost:5000/api/fingerprint/config/reload
```

---

### **Método 2: Archivo JSON**

Editar manualmente `fingerprint-config.json`:

```json
{
  "threshold": 70,
  "timeout": 30000,
  "captureMode": "screen",
  "showImage": true,
  "saveImage": false,
  "detectFakeFinger": false,
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
```

Luego recargar:
```bash
curl -X POST http://localhost:5000/api/fingerprint/config/reload
```

---

### **Método 3: Desde Frontend (TypeScript)**

```typescript
import { fingerprintConfigService } from './services/fingerprintConfig.service';

// Obtener configuración actual
const config = await fingerprintConfigService.getConfiguration();

// Actualizar threshold
await fingerprintConfigService.updatePartialConfiguration({
  threshold: 85
});

// Actualizar múltiples valores
await fingerprintConfigService.updateConfiguration({
  ...config,
  threshold: 85,
  detectFakeFinger: true,
  timeout: 40000
});

// Validar antes de guardar
const validation = await fingerprintConfigService.validateConfiguration(newConfig);
if (validation.isValid) {
  await fingerprintConfigService.updateConfiguration(newConfig);
} else {
  console.error('Errores:', validation.errors);
}
```

---

## ? Validaciones y Restricciones

### **Validaciones Automáticas**

El sistema valida automáticamente todos los parámetros:

| Parámetro | Validación | Error si... |
|-----------|-----------|-------------|
| `threshold` | `[Range(0, 100)]` | < 0 o > 100 |
| `timeout` | `[Range(5000, 60000)]` | < 5000 o > 60000 |
| `captureMode` | `[RegularExpression("^(screen\|file)$")]` | Valor diferente a "screen" o "file" |
| `maxFramesInTemplate` | `[Range(1, 10)]` | < 1 o > 10 |
| `maxRotation` | `[Range(0, 199)]` | < 0 o > 199 |
| `maxTemplatesPerIdentify` | `[Range(1, 10000)]` | < 1 o > 10000 |
| `deviceCheckRetries` | `[Range(1, 10)]` | < 1 o > 10 |
| `deviceCheckDelayMs` | `[Range(100, 5000)]` | < 100 o > 5000 |
| `minQuality` | `[Range(0, 100)]` | < 0 o > 100 |
| `imageFormat` | `[RegularExpression("^(bmp\|png\|jpg)$")]` | Valor diferente a "bmp", "png" o "jpg" |

### **Advertencias (Warnings)**

Configuraciones que generan warnings pero no errores:

| Condición | Warning |
|-----------|---------|
| `maxRotation < 166` | "MaxRotation < 166 puede permitir coincidencias con huellas muy rotadas" |
| `detectFakeFinger = true` AND `timeout < 10000` | "DetectFakeFinger requiere timeout >= 10 segundos" |
| `maxFramesInTemplate > 7` | "MaxFramesInTemplate > 7 puede generar plantillas muy grandes" |
| `minQuality < 30` | "MinQuality < 30 puede aceptar huellas de muy baja calidad" |
| `!Directory.Exists(templatePath)` | "Directorio de plantillas no existe: {path}" |

### **Ejemplo de Respuesta de Validación**

```json
{
  "success": true,
  "data": {
    "isValid": true,
    "errors": [],
    "warnings": [
      "?? MaxRotation < 166 puede permitir coincidencias con huellas muy rotadas",
      "?? MinQuality < 30 puede aceptar huellas de muy baja calidad"
    ]
  }
}
```

---

## ?? Mejores Prácticas

### **1. ?? Seguridad**

```json
// ? BIEN - Alta seguridad
{
  "threshold": 85,
  "detectFakeFinger": true,
  "maxRotation": 195,
  "overwriteExisting": false
}

// ? MAL - Inseguro
{
  "threshold": 30,
  "detectFakeFinger": false,
  "maxRotation": 100,
  "overwriteExisting": true
}
```

### **2. ? Rendimiento**

```json
// ? BIEN - Balance velocidad/calidad
{
  "timeout": 30000,
  "maxFramesInTemplate": 5,
  "maxTemplatesPerIdentify": 500,
  "disableMIDT": false
}

// ? MAL - Muy lento
{
  "timeout": 60000,
  "maxFramesInTemplate": 10,
  "maxTemplatesPerIdentify": 10000,
  "disableMIDT": false
}
```

### **3. ?? Almacenamiento**

```json
// ? BIEN - Producción
{
  "captureMode": "screen",
  "saveImage": false,
  "templatePath": "D:/SecureStorage/Fingerprints"
}

// ? MAL - Ocupa mucho espacio
{
  "captureMode": "file",
  "saveImage": true,
  "templatePath": "C:/temp"  // Datos sensibles en temp
}
```

### **4. ?? Testing vs Producción**

```json
// ? DESARROLLO
{
  "threshold": 50,
  "overwriteExisting": true,
  "saveImage": true,
  "minQuality": 30
}

// ? PRODUCCIÓN
{
  "threshold": 75,
  "overwriteExisting": false,
  "saveImage": false,
  "minQuality": 55
}
```

### **5. ?? Validar Antes de Guardar**

```typescript
// ? BIEN
const validation = await validateConfiguration(newConfig);
if (validation.isValid) {
  await updateConfiguration(newConfig);
} else {
  showErrors(validation.errors);
}

// ? MAL
await updateConfiguration(newConfig); // Sin validar
```

---

## ?? Troubleshooting

### **Problema 1: Huellas Legítimas No Coinciden**

**Síntomas:**
- Usuarios válidos son rechazados
- Score muy alto en verificación

**Soluciones:**
```json
// Reducir restricciones
{
  "threshold": 65,         // Bajar de 70 a 65
  "maxRotation": 175,      // Reducir de 199 a 175
  "minQuality": 45         // Bajar de 50 a 45
}
```

---

### **Problema 2: Proceso Muy Lento**

**Síntomas:**
- Timeout frecuente
- Usuarios se quejan de espera

**Soluciones:**
```json
{
  "timeout": 20000,             // Reducir timeout
  "maxFramesInTemplate": 3,     // Menos frames
  "disableMIDT": true,          // Desactivar MIDT
  "maxTemplatesPerIdentify": 300 // Limitar comparaciones
}
```

---

### **Problema 3: Falsos Positivos (Acepta Huellas Incorrectas)**

**Síntomas:**
- Usuarios acceden con huellas de otros
- Baja seguridad

**Soluciones:**
```json
{
  "threshold": 85,              // Aumentar threshold
  "maxRotation": 195,           // Más restrictivo
  "detectFakeFinger": true,     // Activar liveness
  "minQuality": 65              // Mayor calidad requerida
}
```

---

### **Problema 4: Dispositivo No Se Conecta**

**Síntomas:**
- Error "DEVICE_NOT_CONNECTED"
- SDK no inicializa

**Soluciones:**
```json
{
  "deviceCheckRetries": 7,      // Más reintentos
  "deviceCheckDelayMs": 2500    // Más tiempo entre intentos
}
```

---

### **Problema 5: Almacenamiento Lleno**

**Síntomas:**
- Disco sin espacio
- Error al guardar templates

**Soluciones:**
```json
{
  "captureMode": "screen",      // No guardar en disco
  "saveImage": false,           // No guardar imágenes
  "compressImages": true,       // Comprimir si es necesario
  "imageFormat": "jpg"          // Formato más pequeño
}
```

---

## ?? Soporte

Para problemas o dudas sobre configuración:
1. Revisar este documento
2. Validar configuración con endpoint `/config/validate`
3. Consultar logs del servicio
4. Probar con perfil "Balanceado" primero

---

## ?? Referencias

- [Arquitectura del Sistema](./ARCHITECTURE.md)
- [Integración Backend](./BACKEND-INTEGRATION.md)
- [Integración Frontend](./FRONTEND-INTEGRATION.md)
- [Documentación del SDK Futronic](https://www.futronic-tech.com)

---

**Última Actualización:** 19 de Diciembre de 2025  
**Versión:** 1.0.0
