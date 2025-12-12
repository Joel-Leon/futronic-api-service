# ?? Quick Start - Futronic API

## ? Inicio Rápido (5 minutos)

### 1?? Verificar que el servicio está corriendo

```powershell
# Iniciar el servicio (si no está corriendo)
cd C:\apps\futronic-api\FutronicService
dotnet run
```

### 2?? Abrir la Demo

Opción A (Más fácil):
```
1. Abre el archivo: demo-frontend.html
2. Se abrirá en tu navegador
3. ¡Listo! Prueba registrar, verificar o identificar
```

Opción B (Scripts):
```powershell
# Probar mensajes de error
.\TestMensajesErrorMejorados.ps1

# Probar respuestas con imágenes
.\TestRespuestasConImagenes.ps1
```

---

## ?? Para Integrar en Tu Frontend

### Configuración Básica

```javascript
// config.js
const API_BASE_URL = 'http://localhost:5000';
```

### Ejemplo Mínimo - Registro

```javascript
const response = await fetch(`${API_BASE_URL}/api/fingerprint/register-multi`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    dni: '12345678',
    dedo: 'indice-derecho',
    sampleCount: 5,
    includeImages: false  // true si necesitas las imágenes
  })
});

const result = await response.json();
console.log(result);
```

### Ejemplo Mínimo - Verificación

```javascript
const response = await fetch(`${API_BASE_URL}/api/fingerprint/verify-simple`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    dni: '12345678',
    dedo: 'indice-derecho',
    includeCapturedImage: true  // ? NUEVO: Incluir imagen capturada
  })
});

const result = await response.json();

if (result.success && result.data.verified) {
  console.log('? Verificado!');
  
  // Mostrar imagen capturada
  if (result.data.capturedImageBase64) {
    const img = document.createElement('img');
    img.src = `data:image/bmp;base64,${result.data.capturedImageBase64}`;
    document.body.appendChild(img);
  }
} else {
  console.log('? No verificado');
}
```

---

## ?? Nuevos Parámetros

### Registro
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `includeImages` | boolean | `false` | ? NUEVO: Incluir imágenes en Base64 |

### Verificación
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `includeCapturedImage` | boolean | `false` | ? NUEVO: Incluir imagen capturada |

---

## ?? Componentes Listos para Usar

### React Component

Copia el componente de `GUIA_INTEGRACION_FRONTEND.md`:
- `FingerprintRegistration.jsx` - Componente de registro
- `FingerprintVerification.jsx` - Componente de verificación

### HTML + JavaScript

Copia el código de `demo-frontend.html` o úsalo directamente.

---

## ?? Documentación Completa

| Archivo | Descripción |
|---------|-------------|
| `GUIA_INTEGRACION_FRONTEND.md` | Guía completa con todos los ejemplos |
| `demo-frontend.html` | Demo funcional lista para usar |
| `RESUMEN_FINAL_COMPLETO.md` | Resumen de todas las mejoras |
| `MEJORA_MANEJO_ERRORES_DESCRIPTIVOS.md` | Códigos de error descriptivos |
| `MEJORA_RESPUESTAS_CON_IMAGENES.md` | Imágenes en respuestas |

---

## ? Checklist Rápido

- [ ] Servicio corriendo en `http://localhost:5000`
- [ ] Dispositivo Futronic conectado por USB
- [ ] Archivo `demo-frontend.html` abierto en navegador
- [ ] Probar registro con 5 muestras
- [ ] Probar verificación con imagen
- [ ] Probar identificación

---

## ?? Solución de Problemas

### Problema: "Dispositivo no conectado"
```
Solución:
1. Conectar dispositivo USB
2. Reinstalar drivers de Futronic
3. Reiniciar servicio: dotnet run
4. Verificar: GET http://localhost:5000/api/health
```

### Problema: "No se puede conectar con el servicio"
```
Solución:
1. Verificar que el servicio esté corriendo
2. Verificar URL: http://localhost:5000
3. Revisar firewall/antivirus
```

### Problema: "Timeout en captura"
```
Solución:
1. Limpiar sensor con alcohol
2. Aumentar timeout en request
3. Verificar que el dedo esté seco
```

---

## ?? Tips de Rendimiento

? **Usa `includeImages: false`** (default) para respuestas más rápidas

? **Usa `includeImages: true`** solo cuando necesites las imágenes en el frontend

? **Usa `includeCapturedImage: true`** en verificación para mejor UX

? **Implementa SignalR** para notificaciones en tiempo real

---

## ?? Necesitas Ayuda?

1. Revisa `GUIA_INTEGRACION_FRONTEND.md` - Ejemplos completos
2. Abre `demo-frontend.html` - Demo funcional
3. Revisa logs del servidor - Mensajes descriptivos
4. Consulta `RESUMEN_FINAL_COMPLETO.md` - Comparación antes/después

---

**? ¡Listo! Empieza con `demo-frontend.html`**

