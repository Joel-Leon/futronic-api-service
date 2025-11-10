# ?? Guía de Uso con Postman - OPTIMIZADA

## ?? Inicio Rápido

### 1. Importar Colección
1. Abrir Postman
2. Click en **Import**
3. Seleccionar `Futronic_API_Postman_Collection.json`
4. ? Colección importada (5 endpoints esenciales)

### 2. Iniciar Servicio
```powershell
cd C:\apps\futronic-api\FutronicService
dotnet run
```

Espera a ver:
```
[INFO] Futronic API Service started successfully on http://localhost:5000
```

---

## ?? Endpoints Esenciales

### ? Health Check
**GET** `/health`
- Verifica servicio y dispositivo

### ? 1. Registrar Huella (RECOMENDADO)
**POST** `/api/fingerprint/register-multi`

**Body:**
```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "outputPath": "C:/SistemaHuellas/huellas/12345678/indice-derecho"
}
```

**? Por defecto: 5 muestras** (puedes cambiar con `"sampleCount": 3`)

**Dedos válidos:**
- `indice-derecho`, `pulgar-derecho`, `medio-derecho`, `anular-derecho`, `menique-derecho`
- `indice-izquierdo`, `pulgar-izquierdo`, `medio-izquierdo`, `anular-izquierdo`, `menique-izquierdo`

**Proceso:**
1. Enviar request
2. Observar consola del servicio
3. Colocar dedo cuando se indique
4. Levantar y volver a colocar 5 veces
5. Esperar template guardado

### ? 2. Verificar Identidad (RECOMENDADO)
**POST** `/api/fingerprint/verify-simple`

**Body:**
```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "timeout": 20000
}
```

**Proceso:**
1. Enviar request
2. Colocar dedo cuando se indique en consola
3. **Usa SDK de Futronic** - máxima precisión
4. Resultado: `matched: true/false`

### 3. Identificar Usuario (1:N)
**POST** `/api/fingerprint/identify`

**Body:**
```json
{
  "capturedTemplate": "C:/temp/fingerprints/capture_20250114120000.tml",
  "templates": [
    {
   "dni": "12345678",
      "dedo": "indice-derecho",
      "templatePath": "C:/SistemaHuellas/huellas/12345678/indice-derecho.tml"
    },
    {
      "dni": "87654321",
      "dedo": "pulgar-derecho",
      "templatePath": "C:/SistemaHuellas/huellas/87654321/pulgar-derecho.tml"
    }
  ]
}
```

**Identifica entre múltiples usuarios**

### 4. Configuración
**GET/POST** `/api/fingerprint/config`

**GET**: Obtener configuración actual
**POST**: Actualizar (threshold, timeout)

---

## ?? Flujo Típico de Uso

### Escenario 1: Registro y Verificación
```
1. Health Check ? Verificar servicio  
   GET /health

2. Registrar Huella ? 5 muestras automáticas
   POST /api/fingerprint/register-multi
   {
     "dni": "12345678",
     "dedo": "indice-derecho",
     "outputPath": "C:/SistemaHuellas/huellas/12345678/indice-derecho"
   }

3. Verificar Identidad ? Captura + SDK
   POST /api/fingerprint/verify-simple
   {
     "dni": "12345678",
     "dedo": "indice-derecho"
   }

? Resultado: matched: true
```

### Escenario 2: Identificar Usuario Desconocido
```
1. Primero captura con register-multi (temporal)
   o usa template existente

2. Identificar ? Comparar con base de datos
   POST /api/fingerprint/identify
   {
     "capturedTemplate": "[ruta del paso 1]",
     "templates": [lista de usuarios]
   }

? Resultado: matched: true, dni: "12345678"
```

---

## ?? Respuestas

### Exitosa (200)
```json
{
"success": true,
  "message": "Operación exitosa",
  "data": {
 "matched": true,
    "score": 35,
  "threshold": 70
  }
}
```

### Error (408/500/503)
```json
{
  "success": false,
  "message": "Descripción del error",
  "error": "ERROR_CODE"
}
```

### Códigos HTTP
- `200` - OK
- `400` - Bad Request
- `408` - Timeout
- `500` - Error interno
- `503` - Dispositivo no conectado

---

## ?? Variables de Entorno

La colección incluye:
- `{{base_url}}` = `http://localhost:5000`

Para cambiar:
1. Click en la colección
2. Variables tab
3. Editar `base_url`

---

## ?? Tips

### Observar la Consola
Siempre mira la consola del servicio:
- Cuándo colocar el dedo
- Progreso de muestras
- Errores detallados

### Configuración Óptima
- `threshold`: 70 (balance precisión/usabilidad)
- `timeout`: 30000ms por muestra
- `sampleCount`: 5 (default - mejor calidad)

### Para Máxima Velocidad
```json
{
  "sampleCount": 3,  // 3 muestras más rápido
  "timeout": 20000   // 20 segundos
}
```

---

## ? Verificación

1. **Health Check debe retornar:**
   - `serviceStatus: "running"`
   - `deviceConnected: true`

2. **Registro debe mostrar:**
   ```
   ?? Muestra 1 capturada (calidad: 85.2)
   ? Levante el dedo...
   ?? Muestra 2 capturada (calidad: 89.7)
   ...
 ? Registro completado: 5 muestras
   ```

3. **Verificación debe retornar:**
   - `matched: true` (si es correcta)
   - `score` < 70 típicamente

---

## ?? Diferencias Clave vs Versión Anterior

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| **Endpoints** | 9 | 5 (esenciales) |
| **Registro** | 1-3 muestras | 5 muestras default |
| **Verificación** | Manual (2 pasos) | Automática con SDK |
| **Complejidad** | Alta | Simplificada |
| **Producción** | Requiere ajustes | Listo para usar |

---

**? Colección optimizada para producción**
