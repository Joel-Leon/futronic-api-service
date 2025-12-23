# ?? Guía de Testing de Configuración

## ? Verificación de que las Configuraciones se Aplican Correctamente

Este documento te guía paso a paso para **comprobar que las configuraciones se guardan y aplican correctamente** en el sistema.

---

## ?? Pre-requisitos

1. ? Servicio Futronic corriendo en `http://localhost:5000`
2. ? Herramienta para hacer requests HTTP (Postman, curl, o Thunder Client)
3. ? Acceso a logs del servicio

---

## ?? Test 1: Verificar Configuración Actual

### **Paso 1: Obtener configuración inicial**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

**Respuesta Esperada:**
```json
{
  "success": true,
  "message": "Configuración obtenida exitosamente",
  "data": {
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
}
```

? **Verificar:** La respuesta contiene todos los parámetros con sus valores por defecto.

---

## ?? Test 2: Actualizar un Solo Campo (PATCH)

### **Paso 1: Cambiar solo el threshold**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"threshold": 85}'
```

**Respuesta Esperada:**
```json
{
  "success": true,
  "message": "Configuración actualizada (1 campos)",
  "data": {
    "threshold": 85,  // ? ?? CAMBIÓ de 70 a 85
    "timeout": 30000,
    "maxRotation": 199,
    // ... resto de valores sin cambios
  }
}
```

### **Paso 2: Verificar que el cambio persiste**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** El `threshold` debe ser **85** (no 70).

### **Paso 3: Verificar en el archivo JSON**

Abrir el archivo `fingerprint-config.json` en el directorio de la aplicación:

```json
{
  "threshold": 85,  // ? ?? Debe estar guardado aquí
  "timeout": 30000,
  ...
}
```

? **Verificar:** El archivo contiene `"threshold": 85`.

### **Paso 4: Verificar en los logs**

Buscar en los logs del servicio:

```
?? Configuración cargada: Threshold=85, Timeout=30000, MaxRotation=199, DetectFakeFinger=False
? Configuración actualizada (1 campos) y recargada
```

? **Verificar:** Los logs muestran que la configuración fue recargada.

---

## ?? Test 3: Verificar que la Configuración se USA en Operaciones

### **Paso 1: Cambiar threshold a un valor muy bajo**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"threshold": 50}'
```

### **Paso 2: Hacer una verificación de huella**

```bash
curl -X POST http://localhost:5000/api/fingerprint/verify-simple \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "12345678",
    "dedo": "index"
  }'
```

**En los logs, buscar:**
```
?? Configuración cargada: Threshold=50, ...
   • Umbral: 50  ? ?? DEBE mostrar el nuevo valor
```

### **Paso 3: Cambiar a threshold muy alto**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"threshold": 90}'
```

### **Paso 4: Hacer otra verificación**

```bash
curl -X POST http://localhost:5000/api/fingerprint/verify-simple \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "12345678",
    "dedo": "index"
  }'
```

**En los logs, buscar:**
```
   • Umbral: 90  ? ?? DEBE mostrar el nuevo valor (no 50)
```

? **Verificar:** El umbral cambió inmediatamente, sin reiniciar el servicio.

---

## ?? Test 4: Actualizar Múltiples Campos (PUT)

### **Paso 1: Actualizar varios campos a la vez**

```bash
curl -X PUT http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 80,
    "timeout": 40000,
    "maxRotation": 185,
    "detectFakeFinger": true,
    "minQuality": 60,
    "templatePath": "C:/temp/fingerprints",
    "capturePath": "C:/temp/fingerprints/captures",
    "overwriteExisting": false,
    "maxTemplatesPerIdentify": 500,
    "deviceCheckRetries": 3,
    "deviceCheckDelayMs": 1000,
    "captureMode": "screen",
    "showImage": true,
    "saveImage": false,
    "maxFramesInTemplate": 5,
    "disableMIDT": false,
    "compressImages": false,
    "imageFormat": "bmp"
  }'
```

**Respuesta Esperada:**
```json
{
  "success": true,
  "message": "Configuración actualizada exitosamente",
  "data": {
    "threshold": 80,        // ? ?? CAMBIÓ
    "timeout": 40000,       // ? ?? CAMBIÓ
    "maxRotation": 185,     // ? ?? CAMBIÓ
    "detectFakeFinger": true, // ? ?? CAMBIÓ
    "minQuality": 60,       // ? ?? CAMBIÓ
    ...
  }
}
```

### **Paso 2: Verificar GET**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** Todos los campos actualizados persisten.

---

## ?? Test 5: Validación de Configuración Inválida

### **Paso 1: Intentar configurar un valor fuera de rango**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"threshold": 150}'
```

**Respuesta Esperada:**
```json
{
  "success": false,
  "message": "Error al actualizar configuración",
  "errorCode": "UPDATE_CONFIG_FAILED"
}
```

### **Paso 2: Verificar que NO se guardó**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** El `threshold` NO cambió (sigue en el valor anterior).

### **Paso 3: Usar endpoint de validación**

```bash
curl -X POST http://localhost:5000/api/fingerprint/config/validate \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 150,
    "timeout": 1000
  }'
```

**Respuesta Esperada:**
```json
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

? **Verificar:** El endpoint detecta los errores ANTES de guardar.

---

## ?? Test 6: Restaurar a Valores por Defecto

### **Paso 1: Modificar varios valores**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 95,
    "timeout": 50000,
    "maxRotation": 170
  }'
```

### **Paso 2: Verificar que se guardaron**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** Los valores son `threshold: 95`, `timeout: 50000`, `maxRotation: 170`.

### **Paso 3: Restaurar a valores por defecto**

```bash
curl -X POST http://localhost:5000/api/fingerprint/config/reset
```

**Respuesta Esperada:**
```json
{
  "success": true,
  "message": "Configuración restaurada a valores por defecto",
  "data": {
    "threshold": 70,        // ? ?? Volvió al default
    "timeout": 30000,       // ? ?? Volvió al default
    "maxRotation": 199,     // ? ?? Volvió al default
    ...
  }
}
```

### **Paso 4: Verificar GET**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** Todos los valores volvieron a los defaults.

---

## ?? Test 7: Recargar desde Archivo

### **Paso 1: Modificar manualmente el archivo `fingerprint-config.json`**

Editar el archivo y cambiar:
```json
{
  "threshold": 77,
  "timeout": 35000
}
```

### **Paso 2: Recargar desde el archivo**

```bash
curl -X POST http://localhost:5000/api/fingerprint/config/reload
```

**Respuesta Esperada:**
```json
{
  "success": true,
  "message": "Configuración recargada desde archivo",
  "data": {
    "threshold": 77,        // ? ?? Valor del archivo
    "timeout": 35000,       // ? ?? Valor del archivo
    ...
  }
}
```

### **Paso 3: Verificar GET**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** Los valores coinciden con los del archivo editado manualmente.

---

## ?? Test 8: Configuración Afecta Captura Real

### **Paso 1: Configurar timeout muy corto**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"timeout": 5000}'
```

### **Paso 2: Intentar capturar huella (DEBE fallar por timeout)**

```bash
curl -X POST http://localhost:5000/api/fingerprint/capture \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Resultado Esperado:**
- La captura falla por timeout después de 5 segundos (no 30)

**En los logs:**
```
? Timeout - No se detectó huella a tiempo
```

### **Paso 3: Aumentar timeout**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"timeout": 60000}'
```

### **Paso 4: Intentar capturar de nuevo**

```bash
curl -X POST http://localhost:5000/api/fingerprint/capture \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Resultado Esperado:**
- La captura tiene 60 segundos para completarse (no 5)

? **Verificar:** El timeout cambió inmediatamente.

---

## ?? Test 9: Configuración de Rutas (templatePath, capturePath)

### **Paso 1: Cambiar templatePath**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"templatePath": "C:/TestFingerprints"}'
```

### **Paso 2: Registrar una huella**

```bash
curl -X POST http://localhost:5000/api/fingerprint/register-multi \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "99999999",
    "dedo": "index",
    "sampleCount": 3
  }'
```

### **Paso 3: Verificar que se guardó en la nueva ruta**

? **Verificar:** Buscar archivo en `C:/TestFingerprints/99999999/index/99999999.tml`

**NO debe existir en:** `C:/temp/fingerprints/99999999/...`

---

## ?? Test 10: Persistencia tras Reiniciar Servicio

### **Paso 1: Configurar valores personalizados**

```bash
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{
    "threshold": 88,
    "timeout": 45000,
    "maxRotation": 180
  }'
```

### **Paso 2: Reiniciar el servicio**

Detener y volver a iniciar el servicio Futronic.

### **Paso 3: Verificar configuración después del reinicio**

```bash
curl -X GET http://localhost:5000/api/fingerprint/config
```

? **Verificar:** Los valores son `threshold: 88`, `timeout: 45000`, `maxRotation: 180`.

**En los logs del arranque:**
```
?? Configuración cargada desde: fingerprint-config.json
?? Configuración cargada: Threshold=88, Timeout=45000, MaxRotation=180, ...
```

? **Verificar:** La configuración persiste tras reiniciar.

---

## ? Checklist de Verificación

Marca cada test completado:

- [ ] **Test 1:** Obtener configuración actual funciona
- [ ] **Test 2:** PATCH actualiza un solo campo correctamente
- [ ] **Test 3:** La configuración se USA en operaciones reales
- [ ] **Test 4:** PUT actualiza múltiples campos correctamente
- [ ] **Test 5:** Validación rechaza valores inválidos
- [ ] **Test 6:** Reset restaura valores por defecto
- [ ] **Test 7:** Reload recarga desde archivo manual
- [ ] **Test 8:** Timeout afecta operaciones de captura
- [ ] **Test 9:** templatePath afecta ubicación de guardado
- [ ] **Test 10:** Configuración persiste tras reiniciar

---

## ?? Qué Buscar en los Logs

### **Logs Correctos:**

```
?? Configuración cargada: Threshold=85, Timeout=30000, MaxRotation=199, DetectFakeFinger=False
?? PUT /api/fingerprint/config
?? Configuración guardada en: fingerprint-config.json
? Configuración actualizada y recargada en todos los servicios
```

### **Logs de Error:**

```
? Error al actualizar configuración
?? Configuración inválida: Threshold debe estar entre 0 y 100
```

---

## ?? Problemas Comunes

### **Problema 1: Configuración no se aplica**

**Síntoma:** Cambias la configuración pero sigue usando valores antiguos.

**Solución:**
1. Verificar que el endpoint devuelva `success: true`
2. Buscar en logs: `? Configuración actualizada y recargada`
3. Si falta ese log, el servicio NO se recargó

### **Problema 2: Archivo JSON no se actualiza**

**Síntoma:** La configuración cambia en API pero el archivo JSON no.

**Solución:**
1. Verificar permisos de escritura en el directorio
2. Buscar en logs: `?? Configuración guardada en: fingerprint-config.json`

### **Problema 3: Configuración se pierde al reiniciar**

**Síntoma:** Tras reiniciar el servicio, vuelve a valores por defecto.

**Solución:**
1. Verificar que el archivo `fingerprint-config.json` existe
2. Verificar que el archivo tiene los valores correctos
3. Buscar en logs al arrancar: `?? Configuración cargada desde: fingerprint-config.json`

---

## ?? Soporte

Si algún test falla:
1. Revisar logs del servicio
2. Verificar permisos de archivo
3. Comprobar que el servicio es **Singleton** (no Scoped/Transient)
4. Verificar que `ReloadConfiguration()` se llama después de cada actualización

---

## ?? Resultado Esperado

Si todos los tests pasan:
- ? Las configuraciones se guardan correctamente
- ? Las configuraciones se aplican inmediatamente (sin reiniciar)
- ? Las configuraciones persisten tras reiniciar
- ? Las validaciones funcionan correctamente
- ? Los endpoints de reset y reload funcionan

**¡El sistema de configuración está funcionando correctamente!** ??
