# ?? ACCIÓN INMEDIATA - Lo Que Debes Hacer AHORA

## ? SignalR Está LISTO - Solo Falta Probar

---

## ? Prueba en 60 Segundos

### Paso 1: Iniciar Servicio (10 segundos)

```powershell
cd C:\apps\futronic-api\FutronicService
dotnet run
```

**Espera ver:**
```
? Futronic API Service started successfully on http://localhost:5000
?? SignalR Hub: WS /hubs/fingerprint
```

---

### Paso 2: Abrir HTML de Prueba (5 segundos)

Haz **doble clic** en:
```
C:\apps\futronic-api\test-signalr.html
```

Se abrirá en tu navegador predeterminado.

---

### Paso 3: Ejecutar 4 Pasos (45 segundos)

En la página HTML:

1. **Clic en "1. Conectar SignalR"**
   - Debe decir: `? SignalR conectado exitosamente`

2. **Clic en "2. Suscribirse al DNI"**
   - Debe decir: `? Suscrito exitosamente al grupo DNI: 12345678`

3. **Clic en "3. Enviar Test"**
   - Debe decir: `? Notificación de prueba enviada correctamente`
   - Luego: `?? EVENTO: test`
   - **Si ves esto, ¡SignalR funciona al 100%!** ?

4. **(Opcional) Clic en "4. Iniciar Registro"**
   - Solo si tienes dispositivo Futronic conectado
   - Verás: `?? EVENTO: sample_captured` con imágenes

---

## ?? ¿Qué Esperar?

### En la Consola HTML (Navegador):

```
[10:30:15] ? Página de prueba cargada
[10:30:20] ?? Conectando a SignalR...
[10:30:21] ? SignalR conectado exitosamente
[10:30:21] ? Conectado! ConnectionId: abc123xyz

[10:30:25] ?? Suscribiéndose al DNI: 12345678...
[10:30:25] ? Suscrito exitosamente al grupo DNI: 12345678

[10:30:30] ?? Enviando notificación de prueba a DNI: 12345678...
[10:30:30] ? Notificación de prueba enviada correctamente
[10:30:31] ?? EVENTO: test
[10:30:31]    Mensaje: ?? Test de SignalR para DNI: 12345678
[10:30:31]    ??? Imagen recibida (0.03 KB)
```

### En la Consola del Servidor (PowerShell):

```
?? SignalR: Client connected - ConnectionId: abc123xyz789
? SignalR: Client abc123xyz789 subscribed to DNI group: 12345678
?? SignalR notification sent to DNI group '12345678': test - ?? Test de SignalR...
```

---

## ? Si Todo Funciona

**¡Felicidades!** SignalR está funcionando correctamente.

**Siguiente paso:** Integra en tu aplicación Next.js con el código de `GUIA_INTEGRACION_FRONTEND.md`

---

## ? Si No Funciona

### Problema 1: "No se puede conectar al servidor"

**Solución:**
```powershell
# Verifica que el servicio esté corriendo
curl http://localhost:5000/api/health

# Debe devolver:
# {"success":true,"data":{"status":"healthy",...}}
```

### Problema 2: "No recibo notificaciones"

**Solución:**
1. Abre DevTools del navegador (F12)
2. Ve a la pestaña Console
3. Busca errores en rojo
4. Verifica que hayas hecho los pasos 1 y 2 antes del 3

### Problema 3: "Error de CORS"

**Solución:** Ya está configurado, pero verifica en `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // ? IMPORTANTE
    });
});
```

---

## ?? Integrar en Next.js (5 minutos)

### 1. Instalar Dependencia

```bash
npm install @microsoft/signalr
```

### 2. Crear Hook Personalizado

```typescript
// hooks/useFingerprint.ts
import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

export function useFingerprint(dni: string) {
  const [samples, setSamples] = useState([]);
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('idle');

  const startRegistration = async () => {
    setStatus('connecting');
    
    // Conectar SignalR
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/fingerprint')
      .withAutomaticReconnect()
      .build();

    // Escuchar eventos
    connection.on('ReceiveProgress', (notification) => {
      if (notification.eventType === 'sample_captured') {
        setSamples(prev => [...prev, {
          number: notification.data.currentSample,
          quality: notification.data.quality,
          image: notification.data.imageBase64
        }]);
        setProgress(notification.data.progress);
      }
      
      if (notification.eventType === 'operation_completed') {
        setStatus('completed');
      }
    });

    await connection.start();
    await connection.invoke('SubscribeToDni', dni);
    
    setStatus('registering');

    // Iniciar registro
    const response = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dni, sampleCount: 5 })
    });

    const result = await response.json();
    await connection.stop();
    
    return result;
  };

  return { samples, progress, status, startRegistration };
}
```

### 3. Usar en Componente

```tsx
// components/FingerprintRegistration.tsx
'use client';

import { useFingerprint } from '@/hooks/useFingerprint';

export function FingerprintRegistration({ dni }: { dni: string }) {
  const { samples, progress, status, startRegistration } = useFingerprint(dni);

  return (
    <div>
      <button onClick={startRegistration} disabled={status !== 'idle'}>
        Iniciar Registro
      </button>

      {status === 'registering' && (
        <div>
          <p>Progreso: {progress}%</p>
          <div className="samples-grid">
            {samples.map((sample: any) => (
              <div key={sample.number}>
                <img src={`data:image/bmp;base64,${sample.image}`} />
                <p>Muestra {sample.number}</p>
                <p>Calidad: {sample.quality.toFixed(1)}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {status === 'completed' && (
        <p>? Registro completado!</p>
      )}
    </div>
  );
}
```

---

## ?? Resumen de Archivos

### Para Probar AHORA:
- **`test-signalr.html`** ? Abre esto

### Para Integrar:
- **`GUIA_INTEGRACION_FRONTEND.md`** ? Lee esto
- **Código arriba** ? Copia esto

### Para Referencia:
- **`INICIO_RAPIDO_SIGNALR.md`** ? Guía rápida
- **`GUIA_PRUEBA_SIGNALR.md`** ? Guía detallada
- **`IMPLEMENTACION_SIGNALR_COMPLETADA.md`** ? Detalles técnicos

---

## ?? Acción Inmediata

**1. Ejecuta:**
```powershell
cd C:\apps\futronic-api\FutronicService
dotnet run
```

**2. Abre:**
```
test-signalr.html
```

**3. Haz clic en:**
- Botón 1
- Botón 2
- Botón 3

**4. Verifica que funciona**

**5. ¡Listo! Integra en tu app**

---

**?? Tiempo total:** 60 segundos de prueba + 5 minutos de integración  
**? Estado:** LISTO PARA USAR  
**?? Siguiente:** Copiar código a tu proyecto Next.js
