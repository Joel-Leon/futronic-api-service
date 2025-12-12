# ? INICIO RÁPIDO - SignalR Notificaciones en Tiempo Real

## ? Todo Está Listo

SignalR ha sido implementado y está funcionando. Solo necesitas probarlo.

---

## ?? Prueba en 3 Pasos (2 minutos)

### 1?? Iniciar el Servicio

```bash
cd C:\apps\futronic-api\FutronicService
dotnet run
```

**Deberías ver:**
```
? Futronic API Service started successfully on http://localhost:5000
?? SignalR Hub: WS /hubs/fingerprint (Real-time notifications)
```

---

### 2?? Abrir el HTML de Prueba

```bash
# Abre en tu navegador:
C:\apps\futronic-api\test-signalr.html
```

O simplemente **doble clic** en el archivo `test-signalr.html`.

---

### 3?? Seguir los 4 Pasos en la Pantalla

1. **Conectar SignalR** ? Clic en botón azul
2. **Suscribirse al DNI** ? Clic en siguiente botón
3. **Enviar Test** ? Clic para probar notificación
4. (Opcional) **Iniciar Registro** ? Solo si tienes dispositivo conectado

**Resultado esperado:**
- Verás logs en tiempo real
- Recibirás notificaciones de prueba
- Si tienes dispositivo, verás imágenes de huellas en Base64

---

## ?? Integrar en Tu Frontend (5 minutos)

### Instalar SignalR

```bash
npm install @microsoft/signalr
```

### Copiar Este Código

```javascript
import * as signalR from '@microsoft/signalr';
import { useState } from 'react';

export function FingerprintRegistration({ dni }) {
  const [samples, setSamples] = useState([]);
  const [progress, setProgress] = useState(0);

  const startRegistration = async () => {
    // 1. Conectar SignalR
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/fingerprint')
      .withAutomaticReconnect()
      .build();

    // 2. Escuchar eventos
    connection.on('ReceiveProgress', (notification) => {
      console.log('Evento:', notification.eventType);
      
      if (notification.eventType === 'sample_captured') {
        // ? Aquí recibes la imagen en Base64
        setSamples(prev => [...prev, {
          number: notification.data.currentSample,
          quality: notification.data.quality,
          image: notification.data.imageBase64
        }]);
        setProgress(notification.data.progress);
      }
    });

    // 3. Conectar
    await connection.start();
    console.log('? Conectado a SignalR');

    // 4. Suscribirse al DNI
    await connection.invoke('SubscribeToDni', dni);
    console.log('? Suscrito al DNI:', dni);

    // 5. Iniciar registro
    const response = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        dni: dni,
        sampleCount: 5
      })
    });

    const result = await response.json();
    console.log('? Registro completado:', result);

    // 6. Desconectar
    await connection.stop();
  };

  return (
    <div>
      <button onClick={startRegistration}>
        Iniciar Registro
      </button>
      
      <div>Progreso: {progress}%</div>
      
      <div className="samples-grid">
        {samples.map(sample => (
          <div key={sample.number}>
            <img src={`data:image/bmp;base64,${sample.image}`} />
            <p>Muestra {sample.number}</p>
            <p>Calidad: {sample.quality.toFixed(1)}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## ?? Qué Esperar

### Durante el Registro Verás:

```
?? operation_started: "Iniciando registro de huella"

?? sample_started: "Capturando muestra 1/5"
?? sample_captured: "? Muestra 1 capturada - Calidad: 85.5"
   Data: { imageBase64: "...", quality: 85.5, progress: 20 }

?? sample_started: "Capturando muestra 2/5"
?? sample_captured: "? Muestra 2 capturada - Calidad: 90.2"
   Data: { imageBase64: "...", quality: 90.2, progress: 40 }

... (continúa para las 5 muestras)

?? operation_completed: "Registro completado exitosamente"
   Data: { samplesCollected: 5, averageQuality: 87.3 }
```

---

## ?? Documentación Completa

| Archivo | Descripción |
|---------|-------------|
| `test-signalr.html` | **EMPIEZA AQUÍ** - Prueba visual |
| `GUIA_PRUEBA_SIGNALR.md` | Guía paso a paso detallada |
| `GUIA_INTEGRACION_FRONTEND.md` | Ejemplos completos React/Vue/Angular |
| `IMPLEMENTACION_SIGNALR_COMPLETADA.md` | Detalles técnicos |

---

## ?? Solución Rápida de Problemas

### "No se conecta SignalR"
? Verifica que el servicio esté corriendo: `http://localhost:5000/api/health`

### "No recibo notificaciones"
? Asegúrate de llamar `connection.invoke('SubscribeToDni', dni)` **antes** de iniciar el registro

### "Error de CORS"
? El servicio ya tiene CORS configurado para `localhost:3000` y `localhost:3001`

### "Las imágenes no se muestran"
? Verifica que uses: `data:image/bmp;base64,${imageBase64}`

---

## ?? ¡Listo!

**Todo está implementado y funcionando.**

Solo necesitas:
1. Abrir `test-signalr.html`
2. Probar los 4 pasos
3. Copiar el código a tu app

---

**?? Tiempo estimado:** 2-5 minutos  
**?? Estado:** ? LISTO PARA USAR  
**?? Última actualización:** 2025-01-XX
