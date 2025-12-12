# ?? Guía de Integración Frontend - Futronic Fingerprint API

## ?? Tabla de Contenidos

1. [Introducción](#introducción)
2. [Configuración Inicial](#configuración-inicial)
3. [Registro de Huellas](#registro-de-huellas)
4. [Verificación de Huellas](#verificación-de-huellas)
5. [Identificación de Huellas](#identificación-de-huellas)
6. [Notificaciones en Tiempo Real](#notificaciones-en-tiempo-real)
7. [Manejo de Errores](#manejo-de-errores)
8. [Componentes de UI](#componentes-de-ui)
9. [Ejemplos Completos](#ejemplos-completos)

---

## ?? Introducción

Esta guía te ayudará a integrar la API de Futronic Fingerprint en tu aplicación frontend (React, Vue, Angular, o JavaScript vanilla).

### Características Principales

? **Registro** con múltiples muestras (5 recomendado)  
? **Verificación** 1:1 con imagen capturada  
? **Identificación** 1:N automática  
? **Notificaciones** en tiempo real (SignalR) con imágenes  
? **Imágenes** en Base64 **activadas por defecto** ??  
? **Mensajes de error** descriptivos  

### ?? Importante: Cambios recientes

1. **? Imágenes activadas por defecto**: `includeImages` y `includeCapturedImage` ahora son `true` por defecto
2. **? Solo SignalR**: `callbackUrl` ha sido **eliminado** completamente
3. **? Imágenes en tiempo real**: Las imágenes se envían automáticamente por SignalR en cada muestra

### ?? Convención de Nombres

La API acepta **ambos formatos** de nombres de propiedades:
- ? `dni` o `Dni`
- ? `dedo` o `Dedo`
- ? `sampleCount` o `SampleCount`

**Recomendado**: Usar **camelCase** (minúsculas) en el frontend para mantener la convención JavaScript.

---

## ?? Configuración Inicial

### Paso 1: Configurar URL Base

```javascript
// config.js
export const API_CONFIG = {
  baseURL: 'http://localhost:5000',
  endpoints: {
    register: '/api/fingerprint/register-multi',
    verify: '/api/fingerprint/verify-simple',
    identify: '/api/fingerprint/identify-live',
    health: '/api/health'
  },
  signalR: {
    hubURL: '/hubs/fingerprint'
  }
};
```

### Paso 2: Crear Cliente HTTP

```javascript
// api-client.js
import { API_CONFIG } from './config';

export class FingerprintAPI {
  constructor() {
    this.baseURL = API_CONFIG.baseURL;
  }

  async request(endpoint, method = 'GET', body = null) {
    const url = `${this.baseURL}${endpoint}`;
    const options = {
      method,
      headers: {
        'Content-Type': 'application/json'
      }
    };

    if (body) {
      options.body = JSON.stringify(body);
    }

    try {
      const response = await fetch(url, options);
      const data = await response.json();
      
      if (!data.success) {
        throw new APIError(data.error, data.message, response.status);
      }
      
      return data;
    } catch (error) {
      if (error instanceof APIError) throw error;
      throw new APIError('NETWORK_ERROR', 'No se puede conectar con el servidor', 0);
    }
  }

  // ? Método para registrar (imágenes activadas por defecto)
  async register(dni, options = {}) {
    return this.request(API_CONFIG.endpoints.register, 'POST', {
      dni,
      dedo: options.finger || 'indice-derecho',
      sampleCount: options.sampleCount || 5,
      timeout: options.timeout || 30000,
      outputPath: options.outputPath
      // ? includeImages ya es true por defecto, no es necesario enviarlo
      // Las imágenes se reciben automáticamente por SignalR
    });
  }

  // ? Método para verificar (imagen activada por defecto)
  async verify(dni, options = {}) {
    return this.request(API_CONFIG.endpoints.verify, 'POST', {
      dni,
      dedo: options.finger || 'indice-derecho',
      timeout: options.timeout || 30000
      // ? includeCapturedImage ya es true por defecto, no es necesario enviarlo
      // La imagen se devuelve automáticamente en la respuesta
    });
  }

  // Método para identificar
  async identify(options = {}) {
    return this.request(API_CONFIG.endpoints.identify, 'POST', {
      templatesDirectory: options.templatesDirectory || 'C:/temp/fingerprints',
      timeout: options.timeout || 30000
    });
  }

  // Método para verificar salud
  async checkHealth() {
    return this.request(API_CONFIG.endpoints.health, 'GET');
  }
}

// Clase de error personalizada
export class APIError extends Error {
  constructor(code, message, status) {
    super(message);
    this.code = code;
    this.status = status;
    this.name = 'APIError';
  }
}
```

---

## ?? Resumen de Parámetros

### Registro (`/api/fingerprint/register-multi`)

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `dni` | string | ? **Sí** | - | DNI del usuario |
| `dedo` | string | ? No | `"index"` | Dedo a registrar |
| `sampleCount` | number | ? No | `5` | Número de muestras (1-10) |
| `timeout` | number | ? No | `30000` | Timeout en ms |
| `outputPath` | string | ? No | `"C:/temp/fingerprints"` | Ruta de salida |
| `includeImages` | boolean | ? No | **`true`** ? | Incluir imágenes Base64 en SignalR |

**? Cambio importante**: `includeImages` ahora es **`true` por defecto**. Las imágenes se envían automáticamente por SignalR en cada muestra capturada.

**Ejemplo de payload mínimo:**

```json
{
  "dni": "12345678"
}
```

**Ejemplo de payload completo:**

```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "timeout": 30000,
  "includeImages": true
}
```

### Verificación (`/api/fingerprint/verify-simple`)

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `dni` | string | ? **Sí** | - | DNI del usuario |
| `dedo` | string | ? No | `"index"` | Dedo registrado |
| `timeout` | number | ? No | `30000` | Timeout en ms |
| `includeCapturedImage` | boolean | ? No | **`true`** ? | ?? Incluir imagen capturada en respuesta |

**? Cambio importante**: `includeCapturedImage` ahora es **`true` por defecto**. La imagen capturada se devuelve automáticamente en la respuesta.

**Ejemplo de payload mínimo:**

```json
{
  "dni": "12345678"
}
```

**Ejemplo de payload completo:**

```json
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "includeCapturedImage": true
}
```

### Identificación (`/api/fingerprint/identify-live`)

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `templatesDirectory` | string | ? No | `"C:/temp/fingerprints"` | Directorio de templates |
| `timeout` | number | ? No | `30000` | Timeout en ms |

---

## ?? Registro de Huellas con Notificaciones en Tiempo Real

### Opción 1: Registro Simple (Sin Notificaciones)

```javascript
// register-simple.js
import { FingerprintAPI } from './api-client';

const api = new FingerprintAPI();

async function registrarHuella(dni, finger = 'indice-derecho') {
  try {
    // Mostrar loading
    showLoading('Registrando huella...');
    
    const result = await api.register(dni, {
      finger: finger,
      sampleCount: 5,
      includeImages: false  // No incluir imágenes para respuesta más rápida
    });
    
    hideLoading();
    
    // Mostrar resultado
    showSuccess({
      message: result.message,
      dni: result.data.dni,
      samplesCollected: result.data.samplesCollected,
      averageQuality: result.data.averageQuality,
      templatePath: result.data.templatePath
    });
    
    console.log('Registro exitoso:', result.data);
    
  } catch (error) {
    hideLoading();
    handleError(error);
  }
}

// Uso
registrarHuella('12345678', 'indice-derecho');
```

### Opción 2: Registro con Notificaciones en Tiempo Real (RECOMENDADO)

```javascript
// register-realtime.js
import * as signalR from '@microsoft/signalr';
import { FingerprintAPI, API_CONFIG } from './api-client';

class FingerprintRegistration {
  constructor() {
    this.api = new FingerprintAPI();
    this.connection = null;
    this.samples = [];
  }

  async connect() {
    // Crear conexión SignalR
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_CONFIG.baseURL}${API_CONFIG.signalR.hubURL}`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Manejar eventos
    this.connection.on('ReceiveProgress', (notification) => {
      this.handleNotification(notification);
    });

    // Conectar
    try {
      await this.connection.start();
      console.log('? Conectado a SignalR');
    } catch (error) {
      console.error('? Error al conectar SignalR:', error);
      throw error;
    }
  }

  async register(dni, finger = 'indice-derecho', sampleCount = 5) {
    try {
      // Conectar a SignalR
      await this.connect();
      
      // Suscribirse a notificaciones del DNI
      await this.connection.invoke('SubscribeToDni', dni);
      console.log(`?? Suscrito a notificaciones de DNI: ${dni}`);
      
      // Limpiar muestras anteriores
      this.samples = [];
      
      // ? Iniciar registro (imágenes automáticas)
      const result = await this.api.register(dni, {
        finger: finger,
        sampleCount: sampleCount
        // ? No necesitas includeImages, ya es true por defecto
        // Las imágenes llegarán automáticamente por SignalR
      });
      
      // Desconectar SignalR
      await this.connection.stop();
      
      return result;
      
    } catch (error) {
      if (this.connection) {
        await this.connection.stop();
      }
      throw error;
    }
  }

  handleNotification(notification) {
    console.log('?? Notificación:', notification.eventType, notification.message);
    
    switch (notification.eventType) {
      case 'operation_started':
        this.onOperationStarted(notification);
        break;
        
      case 'sample_started':
        this.onSampleStarted(notification);
        break;
        
      case 'sample_captured':
        // ? AQUÍ RECIBES LA IMAGEN AUTOMÁTICAMENTE
        this.onSampleCaptured(notification);
        break;
        
      case 'operation_completed':
        this.onOperationCompleted(notification);
        break;
        
      case 'error':
        this.onError(notification);
        break;
    }
  }

  onSampleCaptured(notification) {
    const { currentSample, totalSamples, quality, imageBase64, progress } = notification.data;
    
    // ? La imagen llega automáticamente en imageBase64
    this.samples.push({
      number: currentSample,
      quality: quality,
      image: imageBase64  // ? Imagen en Base64
    });
    
    updateUI({
      status: `? Muestra ${currentSample} capturada`,
      message: '??? Retire el dedo y espere',
      progress: progress
    });
    
    // Mostrar imagen (ahora siempre disponible)
    if (imageBase64) {
      displayImage(imageBase64, `sample-${currentSample}`);
    }
    
    // Mostrar calidad
    displayQuality(currentSample, quality);
  }

  onOperationStarted(notification) {
    updateUI({
      status: 'Iniciando registro...',
      message: notification.message,
      progress: 0
    });
  }

  onSampleStarted(notification) {
    const { currentSample, totalSamples, progress } = notification.data;
    
    updateUI({
      status: `Capturando muestra ${currentSample} de ${totalSamples}`,
      message: '?? Coloque el dedo en el sensor',
      progress: progress
    });
  }

  onOperationCompleted(notification) {
    const { samplesCollected, averageQuality } = notification.data;
    
    updateUI({
      status: '? Registro completado',
      message: `${samplesCollected} muestras capturadas con calidad promedio de ${averageQuality.toFixed(2)}`,
      progress: 100
    });
    
    // Mostrar resumen
    displaySummary(this.samples);
  }

  onError(notification) {
    updateUI({
      status: '? Error',
      message: notification.message,
      error: true
    });
  }
}

// Funciones auxiliares de UI
function updateUI({ status, message, progress, error = false }) {
  const statusEl = document.getElementById('status');
  const messageEl = document.getElementById('message');
  const progressBar = document.getElementById('progress-bar');
  
  if (statusEl) statusEl.textContent = status;
  if (messageEl) messageEl.textContent = message;
  
  if (progressBar) {
    progressBar.style.width = `${progress}%`;
    progressBar.className = error ? 'error' : 'success';
  }
}

function displayImage(base64, containerId) {
  const img = document.createElement('img');
  img.src = `data:image/bmp;base64,${base64}`;
  img.alt = 'Huella capturada';
  img.className = 'fingerprint-image';
  
  const container = document.getElementById(containerId);
  if (container) {
    container.innerHTML = '';
    container.appendChild(img);
  }
}

function displayQuality(sampleNumber, quality) {
  const qualityBar = document.createElement('div');
  qualityBar.className = 'quality-bar';
  qualityBar.innerHTML = `
    <span>Muestra ${sampleNumber}</span>
    <div class="bar">
      <div class="fill" style="width: ${quality}%"></div>
    </div>
    <span>${quality.toFixed(1)}</span>
  `;
  
  document.getElementById('quality-list')?.appendChild(qualityBar);
}

function displaySummary(samples) {
  const avgQuality = samples.reduce((sum, s) => sum + s.quality, 0) / samples.length;
  
  const summaryEl = document.getElementById('summary');
  if (summaryEl) {
    summaryEl.innerHTML = `
      <h3>?? Resumen del Registro</h3>
      <p><strong>Total de muestras:</strong> ${samples.length}</p>
      <p><strong>Calidad promedio:</strong> ${avgQuality.toFixed(2)}</p>
      <div class="samples-grid">
        ${samples.map(s => `
          <div class="sample-card">
            <img src="data:image/bmp;base64,${s.image}" alt="Muestra ${s.number}">
            <p>Muestra ${s.number}</p>
            <p>Calidad: ${s.quality.toFixed(1)}</p>
          </div>
        `).join('')}
      </div>
    `;
  }
}

// Uso
const registration = new FingerprintRegistration();

async function iniciarRegistro(dni) {
  try {
    const result = await registration.register(dni, 'indice-derecho', 5);
    console.log('? Registro completado:', result);
  } catch (error) {
    console.error('? Error en registro:', error);
  }
}
```

---

## ? Verificación de Huellas

### Opción 1: Verificación Simple (Sin Imagen)

```javascript
// verify-simple.js
import { FingerprintAPI } from './api-client';

const api = new FingerprintAPI();

async function verificarHuella(dni, finger = 'indice-derecho') {
  try {
    showLoading('Verificando huella...');
    
    const result = await api.verify(dni, {
      finger: finger,
      includeCapturedImage: false  // No incluir imagen
    });
    
    hideLoading();
    
    if (result.data.verified) {
      // ? Huella verificada
      showSuccess({
        title: '? Verificación Exitosa',
        message: `Huella verificada correctamente para ${dni}`,
        score: result.data.score,
        threshold: result.data.threshold,
        quality: result.data.captureQuality
      });
      
      // Permitir acceso
      allowAccess(dni);
      
    } else {
      // ? Huella no coincide
      showWarning({
        title: '? Verificación Fallida',
        message: 'La huella no coincide',
        score: result.data.score,
        threshold: result.data.threshold
      });
      
      // Denegar acceso
      denyAccess();
    }
    
    return result.data.verified;
    
  } catch (error) {
    hideLoading();
    handleError(error);
    return false;
  }
}

// Uso
const esValido = await verificarHuella('12345678');
if (esValido) {
  console.log('Acceso permitido');
} else {
  console.log('Acceso denegado');
}
```

### Opción 2: Verificación con Imagen Capturada (RECOMENDADO PARA UI)

```javascript
// verify-with-image.js
import { FingerprintAPI } from './api-client';

const api = new FingerprintAPI();

async function verificarConImagen(dni, finger = 'indice-derecho') {
  try {
    showLoading('?? Coloque el dedo en el sensor...');
    
    // ? La imagen se devuelve automáticamente
    const result = await api.verify(dni, {
      finger: finger
      // ? No necesitas includeCapturedImage, ya es true por defecto
    });
    
    hideLoading();
    
    // ? La imagen SIEMPRE está disponible en la respuesta
    if (result.data.capturedImageBase64) {
      displayCapturedImage(
        result.data.capturedImageBase64, 
        result.data.capturedImageFormat || 'bmp'
      );
    }
    
    // Mostrar resultado
    showVerificationResult({
      verified: result.data.verified,
      dni: result.data.dni,
      finger: result.data.dedo,
      score: result.data.score,
      threshold: result.data.threshold,
      quality: result.data.captureQuality,
      image: result.data.capturedImageBase64  // ? Siempre disponible
    });
    
    return result.data.verified;
    
  } catch (error) {
    hideLoading();
    handleError(error);
    return false;
  }
}

function displayCapturedImage(base64, format) {
  const container = document.getElementById('captured-image');
  if (!container) return;
  
  container.innerHTML = `
    <div class="image-container">
      <img src="data:image/${format};base64,${base64}" alt="Huella capturada">
      <p>?? Huella capturada automáticamente</p>
    </div>
  `;
}

// Uso
await verificarConImagen('12345678', 'indice-derecho');
```

---

## ?? Identificación de Huellas

```javascript
// identify.js
import { FingerprintAPI } from './api-client';

const api = new FingerprintAPI();

async function identificarUsuario() {
  try {
    showLoading('?? Coloque el dedo en el sensor para identificar...');
    
    const result = await api.identify({
      templatesDirectory: 'C:/temp/fingerprints',
      timeout: 30000
    });
    
    hideLoading();
    
    if (result.data.matched) {
      // ? Usuario identificado
      showIdentificationResult({
        found: true,
        dni: result.data.dni,
        finger: result.data.dedo,
        score: result.data.score,
        threshold: result.data.threshold,
        matchIndex: result.data.matchIndex,
        totalCompared: result.data.totalCompared
      });
      
      // Cargar perfil del usuario
      loadUserProfile(result.data.dni);
      
      return {
        found: true,
        dni: result.data.dni
      };
      
    } else {
      // ? Usuario no identificado
      showIdentificationResult({
        found: false,
        totalCompared: result.data.totalCompared
      });
      
      return {
        found: false,
        dni: null
      };
    }
    
  } catch (error) {
    hideLoading();
    handleError(error);
    return { found: false, error: error.message };
  }
}

function showIdentificationResult({ found, dni, finger, score, threshold, matchIndex, totalCompared }) {
  const resultEl = document.getElementById('identification-result');
  if (!resultEl) return;
  
  if (found) {
    resultEl.innerHTML = `
      <div class="result-card success">
        <h2>? Usuario Identificado</h2>
        <div class="user-info">
          <div class="avatar">
            <span>${dni.charAt(0)}</span>
          </div>
          <p class="dni">${dni}</p>
          <p class="finger">${finger}</p>
        </div>
        <div class="metrics">
          <div class="metric">
            <span>Score FAR</span>
            <span class="value good">${score}</span>
          </div>
          <div class="metric">
            <span>Posición</span>
            <span class="value">${matchIndex + 1} / ${totalCompared}</span>
          </div>
          <div class="metric">
            <span>Umbral</span>
            <span class="value">${threshold}</span>
          </div>
        </div>
      </div>
    `;
  } else {
    resultEl.innerHTML = `
      <div class="result-card failure">
        <h2>? Usuario No Identificado</h2>
        <p>No se encontró coincidencia en la base de datos</p>
        <p class="details">Templates comparados: ${totalCompared}</p>
      </div>
    `;
  }
}

// Uso
const resultado = await identificarUsuario();
if (resultado.found) {
  console.log(`Bienvenido, ${resultado.dni}`);
} else {
  console.log('Usuario no reconocido');
}
```

---

## ? Checklist de Integración

- [ ] Instalar `@microsoft/signalr` para notificaciones en tiempo real
- [ ] Configurar URL base de la API
- [ ] Implementar cliente HTTP con manejo de errores
- [ ] Conectar a SignalR antes de registrar
- [ ] Suscribirse al DNI usando `connection.invoke('SubscribeToDni', dni)`
- [ ] Escuchar evento `ReceiveProgress` para notificaciones
- [ ] ? **Las imágenes llegan automáticamente** (no configurar nada especial)
- [ ] Agregar estilos CSS
- [ ] Probar con dispositivo conectado
- [ ] Manejar errores descriptivos

---

## ?? Ejemplos de Payloads Correctos

### Registro (Mínimo)

```json
POST /api/fingerprint/register-multi
Content-Type: application/json

{
  "dni": "12345678"
}
```

**? Nota**: Con solo el DNI, usará valores por defecto:
- `dedo`: "index"
- `sampleCount`: 5
- `timeout`: 30000
- `includeImages`: **true** (las imágenes llegan por SignalR)

### Verificación (Mínimo)

```json
POST /api/fingerprint/verify-simple
Content-Type: application/json

{
  "dni": "12345678"
}
```

**? Nota**: La imagen capturada se devuelve automáticamente en `capturedImageBase64`.

---

## ?? Beneficios de los Nuevos Defaults

1. **?? Imágenes siempre disponibles**: No necesitas configurar nada, las imágenes se envían automáticamente
2. **?? Menos código**: Payloads más simples, solo envía lo esencial
3. **?? Mejor UX**: Muestra imágenes en tiempo real sin configuración adicional
4. **? Más intuitivo**: Los defaults hacen lo que normalmente querrías

---

**?? Última Actualización:** 2025-01-XX  
**?? Versión API:** 3.1  
**? Estado:** Producción Ready  
**?? Breaking Changes:**  
  - `callbackUrl` eliminado (usar solo SignalR)  
  - `includeImages` ahora es `true` por defecto  
  - `includeCapturedImage` ahora es `true` por defecto

