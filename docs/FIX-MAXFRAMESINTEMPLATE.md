# ?? Fix: maxFramesInTemplate No Se Aplicaba en Registro

## ?? **Problema Reportado**

**Síntoma:**
- El usuario configuró `maxFramesInTemplate: 6` en la configuración
- Al registrar una huella, el sistema seguía tomando **10 muestras** en lugar de 6
- La configuración parecía no aplicarse

**Evidencia:**
```
?? Configuración actualizada: maxFramesInTemplate = 6
Pero al registrar ? Captura 10 muestras en lugar de 6
```

---

## ?? **Análisis del Problema**

### **Código Original (INCORRECTO):**

**Archivo:** `FutronicService/Services/FutronicFingerprintService.cs`  
**Línea:** 603

```csharp
// ? PROBLEMA: Usaba valor hardcoded (5) en lugar de leer de la configuración
int sampleCount = Math.Min(request.SampleCount ?? 5, 10);
```

**Qué hacía:**
- Si **NO se especificaba `sampleCount`** en el request ? usaba **5** (hardcoded)
- Si **se especificaba `sampleCount`** ? usaba ese valor (hasta máximo 10)
- **NUNCA leía `maxFramesInTemplate`** de la configuración

**Por qué fallaba:**
- El usuario cambió `maxFramesInTemplate: 6` en la configuración
- Pero el código no leía ese valor
- Seguía usando el valor hardcoded `5`

---

## ? **Solución Implementada**

### **Código Corregido:**

**Archivo:** `FutronicService/Services/FutronicFingerprintService.cs`  
**Línea:** 603-611

```csharp
// ? CORRECCIÓN: Usar maxFramesInTemplate de la configuración como default
int sampleCount = request.SampleCount ?? _config.MaxFramesInTemplate;

// Validar que no exceda el límite máximo del SDK (10 muestras)
sampleCount = Math.Min(sampleCount, 10);

_logger.LogInformation($"Starting multi-sample registration for DNI: {request.Dni} with {sampleCount} samples (from request: {request.SampleCount}, config default: {_config.MaxFramesInTemplate})");

Console.WriteLine($"\n{"=",-60}");
Console.WriteLine($"=== REGISTRO DE HUELLA ===");
Console.WriteLine($"{"=",-60}");
Console.WriteLine($"?? DNI: {request.Dni}");
Console.WriteLine($"?? Dedo: {request.Dedo ?? "index"}");
Console.WriteLine($"?? Muestras: {sampleCount}");
Console.WriteLine($"?? Configuración: maxFramesInTemplate = {_config.MaxFramesInTemplate}");
```

**Cambios realizados:**
1. ? **Usa `_config.MaxFramesInTemplate`** en lugar de `5` hardcoded
2. ? **Prioridad correcta:**
   - Si se pasa `sampleCount` en el request ? usa ese valor
   - Si NO se pasa ? usa `_config.MaxFramesInTemplate`
3. ? **Validación de límite máximo (10)** se mantiene
4. ? **Log mejorado** para debugging (muestra ambos valores)

---

## ?? **Comparación Antes/Después**

### **Antes (?):**

| `maxFramesInTemplate` | `sampleCount` en request | Muestras reales |
|-----------------------|--------------------------|-----------------|
| 6 | No especificado | **5** ? (hardcoded) |
| 6 | 3 | **3** ? (del request) |
| 6 | 8 | **8** ? (del request) |
| 10 | No especificado | **5** ? (hardcoded) |

### **Después (?):**

| `maxFramesInTemplate` | `sampleCount` en request | Muestras reales |
|-----------------------|--------------------------|-----------------|
| 6 | No especificado | **6** ? (de config) |
| 6 | 3 | **3** ? (del request) |
| 6 | 8 | **8** ? (del request) |
| 10 | No especificado | **10** ? (de config) |

---

## ?? **Cómo Probar la Corrección**

### **Test 1: Configurar maxFramesInTemplate a 4**

```sh
# 1. Configurar maxFramesInTemplate
curl -X PATCH http://localhost:5000/api/fingerprint/config \
  -H "Content-Type: application/json" \
  -d '{"maxFramesInTemplate": 4}'
```

**Respuesta esperada:**
```json
{
  "success": true,
  "data": {
    "maxFramesInTemplate": 4
  }
}
```

### **Test 2: Registrar SIN especificar sampleCount**

```sh
# 2. Registrar sin sampleCount (debe usar 4 de la config)
curl -X POST http://localhost:5000/api/fingerprint/register-multi \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "12345678",
    "dedo": "index"
  }'
```

**En los logs debe aparecer:**
```
?? Muestras: 4
?? Configuración: maxFramesInTemplate = 4

Muestra 1/4: Apoye el dedo firmemente.
Muestra 2/4: Apoye el dedo firmemente.
Muestra 3/4: Apoye el dedo firmemente.
Muestra 4/4: Apoye el dedo firmemente.
```

? **Verificar:** Debe capturar **exactamente 4 muestras** (no 5, no 10)

### **Test 3: Registrar CON sampleCount específico**

```sh
# 3. Registrar con sampleCount=7 (debe usar 7, ignorando la config)
curl -X POST http://localhost:5000/api/fingerprint/register-multi \
  -H "Content-Type: application/json" \
  -d '{
    "dni": "87654321",
    "dedo": "thumb",
    "sampleCount": 7
  }'
```

**En los logs debe aparecer:**
```
?? Muestras: 7
?? Configuración: maxFramesInTemplate = 4

Muestra 1/7: Apoye el dedo firmemente.
...
Muestra 7/7: Apoye el dedo firmemente.
```

? **Verificar:** Debe capturar **7 muestras** (el request tiene prioridad sobre la config)

---

## ?? **Documentación Actualizada**

### **CONFIGURATION-GUIDE.md**

Se actualizó la sección **7. MAXFRAMESINTEMPLATE** con:

```markdown
**?? IMPORTANTE:**  
- Este valor se usa automáticamente en `/api/fingerprint/register-multi` si NO se especifica `sampleCount` en el request
- Si pasas `sampleCount` en el request, ese valor tiene prioridad sobre `maxFramesInTemplate`
- Ejemplo: Si `maxFramesInTemplate: 6` y no pasas `sampleCount` ? usará 6 muestras
- Ejemplo: Si `maxFramesInTemplate: 6` pero pasas `sampleCount: 3` ? usará 3 muestras
```

---

## ?? **Prioridad de Configuración**

```
1. sampleCount del request (si se especifica)
   ?
2. maxFramesInTemplate de la configuración (si no se especifica en request)
   ?
3. Límite máximo: Math.Min(valor, 10) (restricción del SDK)
```

**Ejemplos:**

```json
// Request 1: No especifica sampleCount
{
  "dni": "12345678",
  "dedo": "index"
}
// ? Usa maxFramesInTemplate de la config (ej: 6 muestras)

// Request 2: Especifica sampleCount
{
  "dni": "12345678",
  "dedo": "index",
  "sampleCount": 3
}
// ? Usa 3 muestras (ignora maxFramesInTemplate)

// Request 3: Especifica sampleCount > 10
{
  "dni": "12345678",
  "dedo": "index",
  "sampleCount": 15
}
// ? Usa 10 muestras (límite del SDK)
```

---

## ? **Checklist de Verificación**

Después de aplicar el fix, verificar:

- [ ] **Test 1:** Configurar `maxFramesInTemplate: 4` ? Registro sin `sampleCount` usa 4 muestras
- [ ] **Test 2:** Configurar `maxFramesInTemplate: 6` ? Registro sin `sampleCount` usa 6 muestras
- [ ] **Test 3:** Configurar `maxFramesInTemplate: 10` ? Registro sin `sampleCount` usa 10 muestras
- [ ] **Test 4:** Con `maxFramesInTemplate: 6` pero `sampleCount: 3` en request ? usa 3
- [ ] **Test 5:** Con `maxFramesInTemplate: 6` pero `sampleCount: 8` en request ? usa 8
- [ ] **Test 6:** Logs muestran ambos valores: `(from request: X, config default: Y)`
- [ ] **Test 7:** Cambiar config y verificar que se aplica inmediatamente (sin reiniciar)

---

## ?? **Cómo Aplicar el Fix**

### **Opción 1: Reiniciar el Servicio**

```sh
# 1. Detener el servicio Futronic
# (Ctrl+C o cerrar ventana)

# 2. Compilar con el fix
dotnet build

# 3. Iniciar el servicio
dotnet run
```

### **Opción 2: Usar Release Precompilada**

Si el fix ya está en producción, solo necesitas:

```sh
# 1. Detener servicio
# 2. Actualizar binarios
# 3. Reiniciar servicio
```

---

## ?? **Impacto del Fix**

### **Antes:**
- ? `maxFramesInTemplate` en config **NO se usaba**
- ? Siempre usaba 5 muestras por defecto (hardcoded)
- ? No había forma de cambiar el default sin modificar código

### **Después:**
- ? `maxFramesInTemplate` en config **SÍ se usa** como default
- ? Puedes cambiar el default sin reiniciar el servicio
- ? El request puede sobrescribir el default si es necesario
- ? Logs claros muestran qué valor se está usando

---

## ?? **Soporte**

Si el problema persiste:
1. Verificar que el servicio se reinició después del fix
2. Revisar logs: Buscar `Starting multi-sample registration for DNI`
3. Confirmar que muestra: `(from request: null, config default: X)`
4. Verificar que la configuración se guardó: `GET /api/fingerprint/config`

---

**Última Actualización:** 19 de Diciembre de 2025  
**Fix Aplicado en:** Commit XXXX  
**Versión:** 1.0.1
