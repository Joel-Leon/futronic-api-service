# ??? Guía de Notificaciones con Imágenes

## ?? Evento `sample_captured` - Ahora con Imagen

Cada vez que se captura una muestra, recibirás la **imagen de la huella en Base64** para mostrarla en tiempo real.

---

## ?? Estructura del Evento

```json
{
  "eventType": "sample_captured",
  "message": "Muestra 1/5 capturada",
  "data": {
    "currentSample": 1,
    "totalSamples": 5,
    "quality": 85.5,
    "progress": 20,
    "imageBase64": "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDA...",
    "imageFormat": "bmp"
  },
  "dni": "12345678",
  "timestamp": "2025-11-12T20:00:08Z"
}
```

### Campos del Evento

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `eventType` | `string` | Siempre `"sample_captured"` |
| `message` | `string` | Mensaje descriptivo (ej: "Muestra 1/5 capturada") |
| `data.currentSample` | `int` | Número de muestra actual (1, 2, 3...) |
| `data.totalSamples` | `int` | Total de muestras a capturar (5) |
| `data.quality` | `double` | Calidad de la muestra (0-100) |
| `data.progress` | `int` | Progreso en porcentaje (20%, 40%, 60%...) |
| `data.imageBase64` | `string` | **Imagen BMP en Base64** |
| `data.imageFormat` | `string` | Formato de la imagen (`"bmp"`) |
| `dni` | `string` | DNI del usuario |
| `timestamp` | `string` | Timestamp ISO 8601 |

---

## ?? Cómo Mostrar las Imágenes

### Opción 1: JavaScript Puro

```javascript
connection.on('ReceiveProgress', (data) => {
    if (data.eventType === 'sample_captured' && data.data.imageBase64) {
        // Crear elemento img
        const imgElement = document.createElement('img');
        imgElement.src = `data:image/bmp;base64,${data.data.imageBase64}`;
        imgElement.alt = `Muestra ${data.data.currentSample}`;
        imgElement.className = 'fingerprint-sample';
        
        // Agregar al contenedor
        document.getElementById('samplesContainer').appendChild(imgElement);
        
        // Actualizar calidad
        const qualitySpan = document.createElement('span');
        qualitySpan.textContent = `Calidad: ${data.data.quality.toFixed(1)}%`;
        qualitySpan.className = data.data.quality > 80 ? 'quality-good' : 'quality-low';
        imgElement.parentElement.appendChild(qualitySpan);
    }
});
```

### Opción 2: React Component

```jsx
import React, { useState, useEffect } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import './FingerprintCapture.css';

function FingerprintCapture({ dni }) {
    const [samples, setSamples] = useState([]);
    const [progress, setProgress] = useState(0);
    const [status, setStatus] = useState('Listo');

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl('http://localhost:5000/hubs/fingerprint')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveProgress', (data) => {
            switch (data.eventType) {
                case 'operation_started':
                    setSamples([]);
                    setProgress(0);
                    setStatus(data.message);
                    break;
                    
                case 'sample_started':
                    setProgress(data.data.progress);
                    setStatus(`Capturando muestra ${data.data.currentSample}/${data.data.totalSamples}...`);
                    break;
                    
                case 'sample_captured':
                    // Agregar nueva muestra con imagen
                    setSamples(prev => [...prev, {
                        sample: data.data.currentSample,
                        quality: data.data.quality,
                        imageBase64: data.data.imageBase64,
                        timestamp: data.timestamp
                    }]);
                    setProgress(data.data.progress);
                    setStatus(`? Muestra ${data.data.currentSample} capturada (${data.data.quality.toFixed(1)}%)`);
                    break;
                    
                case 'operation_completed':
                    setProgress(100);
                    setStatus(`? ${data.message}`);
                    break;
            }
        });

        connection.start()
            .then(() => connection.invoke('SubscribeToDni', dni))
            .catch(err => console.error('Connection error:', err));

        return () => {
            connection.stop();
        };
    }, [dni]);

    const startCapture = async () => {
        const response = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                dni: dni,
                dedo: 'indice-derecho',
                sampleCount: 5
            })
        });
    };

    return (
        <div className="fingerprint-capture">
            <h2>Captura de Huella - DNI: {dni}</h2>
            
            {/* Barra de progreso */}
            <div className="progress-bar">
                <div className="progress-fill" style={{ width: `${progress}%` }}>
                    {progress}%
                </div>
            </div>
            
            {/* Estado */}
            <div className="status">{status}</div>
            
            {/* Galería de muestras con imágenes */}
            <div className="samples-gallery">
                {samples.map((sample, idx) => (
                    <div key={idx} className="sample-card">
                        <div className="sample-header">
                            <span className="sample-number">Muestra #{sample.sample}</span>
                            <span className={`quality-badge ${sample.quality > 80 ? 'good' : 'low'}`}>
                                {sample.quality.toFixed(1)}%
                            </span>
                        </div>
                        
                        {/* Imagen de la huella */}
                        {sample.imageBase64 && (
                            <img 
                                src={`data:image/bmp;base64,${sample.imageBase64}`}
                                alt={`Muestra ${sample.sample}`}
                                className="fingerprint-image"
                            />
                        )}
                        
                        <div className="sample-footer">
                            <small>{new Date(sample.timestamp).toLocaleTimeString()}</small>
                        </div>
                    </div>
                ))}
            </div>
            
            {/* Botón */}
            <button onClick={startCapture} className="capture-button">
                Iniciar Captura
            </button>
        </div>
    );
}

export default FingerprintCapture;
```

### CSS para las Muestras

```css
.fingerprint-capture {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
}

.progress-bar {
    width: 100%;
    height: 30px;
    background-color: #f0f0f0;
    border-radius: 15px;
    overflow: hidden;
    margin: 20px 0;
}

.progress-fill {
    height: 100%;
    background: linear-gradient(90deg, #4CAF50, #45a049);
    transition: width 0.3s ease;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-weight: bold;
}

.status {
    text-align: center;
    font-size: 18px;
    margin: 20px 0;
    padding: 10px;
    background-color: #f8f9fa;
    border-radius: 5px;
}

.samples-gallery {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 20px;
    margin: 30px 0;
}

.sample-card {
    background: white;
    border: 2px solid #e0e0e0;
    border-radius: 10px;
    padding: 15px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    transition: transform 0.2s, box-shadow 0.2s;
}

.sample-card:hover {
    transform: translateY(-5px);
    box-shadow: 0 4px 8px rgba(0,0,0,0.2);
}

.sample-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
}

.sample-number {
    font-weight: bold;
    font-size: 14px;
}

.quality-badge {
    padding: 5px 10px;
    border-radius: 15px;
    font-size: 12px;
    font-weight: bold;
}

.quality-badge.good {
    background-color: #4CAF50;
    color: white;
}

.quality-badge.low {
    background-color: #FF9800;
    color: white;
}

.fingerprint-image {
    width: 100%;
    height: auto;
    border: 1px solid #ddd;
    border-radius: 5px;
    background-color: #f9f9f9;
    image-rendering: crisp-edges;
}

.sample-footer {
    margin-top: 10px;
    text-align: center;
    color: #666;
}

.capture-button {
    width: 100%;
    padding: 15px;
    background-color: #2196F3;
    color: white;
    border: none;
    border-radius: 5px;
    font-size: 16px;
    font-weight: bold;
    cursor: pointer;
    transition: background-color 0.3s;
}

.capture-button:hover {
    background-color: #1976D2;
}

.capture-button:disabled {
    background-color: #ccc;
    cursor: not-allowed;
}
```

---

## ?? Ejemplo Completo con Vue.js

```vue
<template>
  <div class="fingerprint-capture">
    <h2>Captura de Huella - DNI: {{ dni }}</h2>
    
    <!-- Progreso -->
    <div class="progress-bar">
      <div class="progress-fill" :style="{ width: progress + '%' }">
        {{ progress }}%
      </div>
    </div>
    
    <div class="status">{{ status }}</div>
    
    <!-- Galería de muestras -->
    <div class="samples-gallery">
      <div 
        v-for="(sample, idx) in samples" 
        :key="idx" 
        class="sample-card"
      >
        <div class="sample-header">
          <span class="sample-number">Muestra #{{ sample.sample }}</span>
          <span 
            :class="['quality-badge', sample.quality > 80 ? 'good' : 'low']"
          >
            {{ sample.quality.toFixed(1) }}%
          </span>
        </div>
        
        <img 
          v-if="sample.imageBase64"
          :src="`data:image/bmp;base64,${sample.imageBase64}`"
          :alt="`Muestra ${sample.sample}`"
          class="fingerprint-image"
        />
        
        <div class="sample-footer">
          <small>{{ formatTime(sample.timestamp) }}</small>
        </div>
      </div>
    </div>
    
    <button @click="startCapture" :disabled="isCapturing" class="capture-button">
      {{ isCapturing ? 'Capturando...' : 'Iniciar Captura' }}
    </button>
  </div>
</template>

<script>
import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

export default {
  name: 'FingerprintCapture',
  props: {
    dni: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const samples = ref([]);
    const progress = ref(0);
    const status = ref('Listo');
    const isCapturing = ref(false);
    let connection = null;

    const setupSignalR = async () => {
      connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:5000/hubs/fingerprint')
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveProgress', (data) => {
        switch (data.eventType) {
          case 'operation_started':
            samples.value = [];
            progress.value = 0;
            status.value = data.message;
            isCapturing.value = true;
            break;
            
          case 'sample_started':
            progress.value = data.data.progress;
            status.value = `Capturando muestra ${data.data.currentSample}/${data.data.totalSamples}...`;
            break;
            
          case 'sample_captured':
            samples.value.push({
              sample: data.data.currentSample,
              quality: data.data.quality,
              imageBase64: data.data.imageBase64,
              timestamp: data.timestamp
            });
            progress.value = data.data.progress;
            status.value = `? Muestra ${data.data.currentSample} capturada`;
            break;
            
          case 'operation_completed':
            progress.value = 100;
            status.value = `? ${data.message}`;
            isCapturing.value = false;
            break;
            
          case 'error':
            status.value = `? ${data.message}`;
            isCapturing.value = false;
            break;
        }
      });

      await connection.start();
      await connection.invoke('SubscribeToDni', props.dni);
    };

    const startCapture = async () => {
      try {
        const response = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            dni: props.dni,
            dedo: 'indice-derecho',
            sampleCount: 5
          })
        });
        
        const result = await response.json();
        console.log('Result:', result);
      } catch (error) {
        console.error('Error:', error);
        status.value = `? Error: ${error.message}`;
      }
    };

    const formatTime = (timestamp) => {
      return new Date(timestamp).toLocaleTimeString();
    };

    onMounted(() => {
      setupSignalR();
    });

    onUnmounted(() => {
      if (connection) {
        connection.stop();
      }
    });

    return {
      samples,
      progress,
      status,
      isCapturing,
      startCapture,
      formatTime
    };
  }
};
</script>

<style scoped>
/* Usar el mismo CSS de arriba */
</style>
```

---

## ?? Ejemplo para HTTP Callback (Webhook)

Tu servidor recibirá el POST con la imagen:

```csharp
[HttpPost("/webhook/fingerprint")]
public async Task<IActionResult> HandleProgress([FromBody] ProgressNotification notification)
{
    if (notification.EventType == "sample_captured")
    {
        var data = JsonSerializer.Deserialize<SampleCapturedData>(notification.Data.ToString());
        
        // Convertir Base64 a imagen
        byte[] imageBytes = Convert.FromBase64String(data.ImageBase64);
        
        // Guardar imagen temporalmente
        var tempPath = Path.Combine(Path.GetTempPath(), $"sample_{notification.Dni}_{data.CurrentSample}.bmp");
        await File.WriteAllBytesAsync(tempPath, imageBytes);
        
        // Enviar a frontend vía SignalR/WebSocket
        await _hubContext.Clients.User(notification.Dni).SendAsync("SampleCaptured", new
        {
            Sample = data.CurrentSample,
            Quality = data.Quality,
            ImageUrl = $"/temp/samples/{notification.Dni}/{data.CurrentSample}.bmp"
        });
        
        // O guardar en base de datos
        await _dbContext.FingerprintSamples.AddAsync(new FingerprintSample
        {
            Dni = notification.Dni,
            SampleNumber = data.CurrentSample,
            Quality = data.Quality,
            ImageData = imageBytes,
            CapturedAt = DateTime.Parse(notification.Timestamp)
        });
        await _dbContext.SaveChangesAsync();
    }
    
    return Ok();
}

public class SampleCapturedData
{
    public int CurrentSample { get; set; }
    public int TotalSamples { get; set; }
    public double Quality { get; set; }
    public int Progress { get; set; }
    public string ImageBase64 { get; set; }
    public string ImageFormat { get; set; }
}
```

---

## ?? Tamaño de las Imágenes

### Información Técnica

- **Formato:** BMP (sin compresión)
- **Tamaño aproximado en Base64:** ~50-100 KB por imagen
- **Resolución típica:** 300x300 a 500x500 px
- **Total para 5 muestras:** ~250-500 KB

### Optimización (Opcional)

Si necesitas reducir el tamaño, puedes convertir a JPEG/PNG en el backend:

```csharp
// En ImageUtils.cs
public static string ConvertToJpegBase64(byte[] bmpData, int quality = 80)
{
    using (var ms = new MemoryStream(bmpData))
    using (var bmp = new Bitmap(ms))
    using (var outputMs = new MemoryStream())
    {
        bmp.Save(outputMs, ImageFormat.Jpeg);
        return Convert.ToBase64String(outputMs.ToArray());
    }
}
```

---

## ?? Probar las Imágenes

### 1. Iniciar el servicio
```bash
cd FutronicService
dotnet run
```

### 2. Abrir cliente de prueba
```
http://localhost:5000/signalr-client.html
```

### 3. Hacer request
```json
POST /api/fingerprint/register-multi
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "callbackUrl": "https://webhook.site/tu-id"
}
```

### 4. Ver en consola del navegador
```javascript
// Deberías ver en cada evento:
{
  eventType: "sample_captured",
  data: {
    currentSample: 1,
    quality: 85.5,
    progress: 20,
    imageBase64: "/9j/4AAQSkZJRg...", // ? Imagen aquí!
    imageFormat: "bmp"
  }
}
```

---

## ?? Beneficios

### Para Usuarios:
- ? **Feedback visual inmediato** - Ven su huella capturada
- ? **Confianza** - Pueden verificar que se capturó correctamente
- ? **Reposición rápida** - Si ven una imagen borrosa, pueden repetir

### Para Desarrolladores:
- ? **Debugging visual** - Pueden ver qué se capturó
- ? **Métricas de calidad** - Correlación imagen-calidad
- ? **Análisis post-mortem** - Guardar imágenes para revisión

### Para el Negocio:
- ? **Mejor experiencia** - UI más interactiva
- ? **Menos errores** - Usuarios ven si la captura fue buena
- ? **Soporte mejorado** - Imágenes para troubleshooting

---

**Fecha:** 12 de Noviembre, 2025  
**Versión:** 2.3 - Con imágenes en notificaciones  
**Estado:** ?? **PRODUCCIÓN READY**
