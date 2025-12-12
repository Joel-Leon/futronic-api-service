# ?? Guía de Prueba de SignalR - Notificaciones en Tiempo Real

## ? Estado Actual

**SignalR está HABILITADO y CONFIGURADO correctamente:**

- ? Hub registrado en `/hubs/fingerprint`
- ? `IProgressNotificationService` inyectado en el servicio
- ? Notificaciones implementadas en `EnrollFingerprintInternal`
- ? Imágenes Base64 incluidas en notificaciones
- ? Endpoint de prueba disponible en `/api/fingerprint/test-signalr`

---

## ?? Prueba Rápida (5 minutos)

### 1?? Verificar que el servicio está corriendo

```bash
# El servicio debe estar corriendo en:
http://localhost:5000
```

### 2?? Probar con el HTML de prueba

Crea un archivo `test-signalr.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Test SignalR - Futronic API</title>
    <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
        }
        .log {
            background: #1e1e1e;
            color: #d4d4d4;
            padding: 20px;
            border-radius: 8px;
            height: 400px;
            overflow-y: auto;
            font-family: 'Courier New', monospace;
            font-size: 14px;
        }
        .log-entry {
            margin: 5px 0;
            padding: 5px;
        }
        .log-entry.success { color: #4ec9b0; }
        .log-entry.error { color: #f48771; }
        .log-entry.info { color: #4fc3f7; }
        .log-entry.event { color: #c586c0; }
        .button {
            padding: 12px 24px;
            font-size: 16px;
            margin: 10px 5px;
            cursor: pointer;
            border: none;
            border-radius: 6px;
            background: #007acc;
            color: white;
        }
        .button:hover {
            background: #005a9e;
        }
        .input {
            padding: 10px;
            font-size: 16px;
            border: 2px solid #ccc;
            border-radius: 6px;
            margin: 10px 5px;
            width: 200px;
        }
    </style>
</head>
<body>
    <h1>?? Test de SignalR - Futronic API</h1>
    
    <div>
        <input type="text" id="dni" class="input" placeholder="DNI (ej: 12345678)" value="12345678">
        <button onclick="connectSignalR()" class="button">1. Conectar SignalR</button>
        <button onclick="subscribeToDni()" class="button">2. Suscribirse al DNI</button>
        <button onclick="sendTestNotification()" class="button">3. Enviar Test</button>
        <button onclick="startRegistration()" class="button">4. Iniciar Registro Real</button>
        <button onclick="clearLog()" class="button">Limpiar Log</button>
    </div>

    <h3>?? Consola de Logs:</h3>
    <div id="log" class="log"></div>

    <script>
        let connection = null;
        const API_URL = 'http://localhost:5000';

        function addLog(message, type = 'info') {
            const log = document.getElementById('log');
            const entry = document.createElement('div');
            entry.className = `log-entry ${type}`;
            const timestamp = new Date().toLocaleTimeString();
            entry.textContent = `[${timestamp}] ${message}`;
            log.appendChild(entry);
            log.scrollTop = log.scrollHeight;
        }

        function clearLog() {
            document.getElementById('log').innerHTML = '';
            addLog('Log limpiado', 'info');
        }

        async function connectSignalR() {
            try {
                addLog('?? Conectando a SignalR...', 'info');
                
                connection = new signalR.HubConnectionBuilder()
                    .withUrl(`${API_URL}/hubs/fingerprint`)
                    .withAutomaticReconnect()
                    .configureLogging(signalR.LogLevel.Information)
                    .build();

                // Manejar eventos
                connection.on('ReceiveProgress', (notification) => {
                    addLog(`?? EVENTO RECIBIDO: ${notification.eventType}`, 'event');
                    addLog(`   Mensaje: ${notification.message}`, 'info');
                    addLog(`   Data: ${JSON.stringify(notification.data, null, 2)}`, 'info');
                    console.log('Notificación completa:', notification);
                });

                connection.on('Connected', (data) => {
                    addLog(`? Conectado! ConnectionId: ${data.connectionId}`, 'success');
                });

                connection.onreconnecting(() => {
                    addLog('?? Reconectando...', 'info');
                });

                connection.onreconnected(() => {
                    addLog('? Reconectado exitosamente', 'success');
                });

                connection.onclose(() => {
                    addLog('?? Conexión cerrada', 'error');
                });

                await connection.start();
                addLog('? SignalR conectado exitosamente', 'success');
                
            } catch (error) {
                addLog(`? Error al conectar: ${error.message}`, 'error');
                console.error(error);
            }
        }

        async function subscribeToDni() {
            if (!connection) {
                addLog('? Primero conecta SignalR (botón 1)', 'error');
                return;
            }

            const dni = document.getElementById('dni').value;
            if (!dni) {
                addLog('? Ingresa un DNI', 'error');
                return;
            }

            try {
                addLog(`?? Suscribiéndose al DNI: ${dni}...`, 'info');
                await connection.invoke('SubscribeToDni', dni);
                addLog(`? Suscrito exitosamente al grupo DNI: ${dni}`, 'success');
            } catch (error) {
                addLog(`? Error al suscribirse: ${error.message}`, 'error');
                console.error(error);
            }
        }

        async function sendTestNotification() {
            const dni = document.getElementById('dni').value;
            if (!dni) {
                addLog('? Ingresa un DNI', 'error');
                return;
            }

            try {
                addLog(`?? Enviando notificación de prueba a DNI: ${dni}...`, 'info');
                
                const response = await fetch(`${API_URL}/api/fingerprint/test-signalr`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ dni: dni })
                });

                const result = await response.json();
                
                if (result.success) {
                    addLog('? Notificación de prueba enviada correctamente', 'success');
                    addLog('   Deberías recibir un evento "test" en 1-2 segundos', 'info');
                } else {
                    addLog(`? Error: ${result.message}`, 'error');
                }
                
                console.log('Respuesta del servidor:', result);
            } catch (error) {
                addLog(`? Error al enviar test: ${error.message}`, 'error');
                console.error(error);
            }
        }

        async function startRegistration() {
            const dni = document.getElementById('dni').value;
            if (!dni) {
                addLog('? Ingresa un DNI', 'error');
                return;
            }

            if (!connection) {
                addLog('? Primero conecta SignalR y suscríbete', 'error');
                return;
            }

            try {
                addLog(`?? Iniciando registro REAL con 3 muestras para DNI: ${dni}...`, 'info');
                addLog('?? IMPORTANTE: Necesitas el dispositivo Futronic conectado', 'info');
                
                const response = await fetch(`${API_URL}/api/fingerprint/register-multi`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        dni: dni,
                        dedo: 'indice-derecho',
                        sampleCount: 3,
                        includeImages: false
                    })
                });

                const result = await response.json();
                
                if (result.success) {
                    addLog('? Registro completado exitosamente', 'success');
                } else {
                    addLog(`? Error en registro: ${result.message}`, 'error');
                }
                
                console.log('Resultado del registro:', result);
            } catch (error) {
                addLog(`? Error: ${error.message}`, 'error');
                console.error(error);
            }
        }

        // Auto-connect al cargar la página
        window.addEventListener('load', () => {
            addLog('? Página cargada', 'info');
            addLog('?? Sigue los pasos en orden:', 'info');
            addLog('   1. Conectar SignalR', 'info');
            addLog('   2. Suscribirse al DNI', 'info');
            addLog('   3. Enviar Test (verifica que funciona)', 'info');
            addLog('   4. Iniciar Registro Real (requiere dispositivo)', 'info');
        });
    </script>
</body>
</html>
```

---

## ?? Instrucciones de Prueba

### Paso 1: Abrir el archivo HTML

1. Guarda el código HTML arriba como `test-signalr.html`
2. Abre el archivo en tu navegador
3. Abre la consola del navegador (F12) para ver logs adicionales

### Paso 2: Seguir los botones en orden

1. **Conectar SignalR** ? Establece conexión WebSocket
2. **Suscribirse al DNI** ? Se une al grupo del DNI
3. **Enviar Test** ? Envía notificación de prueba desde el servidor
4. **Iniciar Registro Real** ? Inicia captura real (requiere dispositivo)

---

## ?? Qué Esperar

### Después de "Conectar SignalR":
```
? SignalR conectado exitosamente
? Conectado! ConnectionId: abc123xyz
```

### Después de "Suscribirse al DNI":
```
?? Suscribiéndose al DNI: 12345678...
? Suscrito exitosamente al grupo DNI: 12345678
```

### Después de "Enviar Test":
```
?? Enviando notificación de prueba a DNI: 12345678...
? Notificación de prueba enviada correctamente
?? EVENTO RECIBIDO: test
   Mensaje: ?? Test de SignalR para DNI: 12345678
   Data: {
     "test": true,
     "message": "Esta es una notificación de prueba de SignalR",
     "timestamp": "2025-01-XX...",
     ...
   }
```

### Durante "Iniciar Registro Real":
```
?? EVENTO RECIBIDO: operation_started
   Mensaje: Iniciando registro de huella

?? EVENTO RECIBIDO: sample_started
   Mensaje: Capturando muestra 1/3

?? EVENTO RECIBIDO: sample_captured
   Mensaje: ? Muestra 1/3 capturada - Calidad: 85.5
   Data: {
     "currentSample": 1,
     "totalSamples": 3,
     "quality": 85.5,
     "progress": 33,
     "imageBase64": "..." ? ? IMAGEN EN BASE64
   }

... (se repite para muestra 2 y 3)

?? EVENTO RECIBIDO: operation_completed
   Mensaje: Registro completado exitosamente
```

---

## ?? Solución de Problemas

### Problema: "No recibo notificaciones"

**Solución:**
1. Verifica que el servicio esté corriendo: `http://localhost:5000/api/health`
2. Verifica la consola del navegador (F12) para ver errores de SignalR
3. Asegúrate de hacer los pasos en orden (conectar ? suscribir ? test)
4. Revisa los logs del servidor (consola donde corre `dotnet run`)

### Problema: "Error de CORS"

**Solución:**
El servicio debe tener CORS habilitado. Verifica en `Program.cs`:
```csharp
services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // ? IMPORTANTE para SignalR
    });
});
```

### Problema: "Connection refused"

**Solución:**
1. Verifica que el servicio esté corriendo
2. Verifica la URL: `http://localhost:5000` (no https)
3. Verifica que el Hub esté registrado en `Program.cs`:
   ```csharp
   endpoints.MapHub<FingerprintHub>("/hubs/fingerprint");
   ```

---

## ?? Logs del Servidor

Durante el registro, deberías ver en la consola del servidor:

```
?? SignalR: Client connected - ConnectionId: abc123xyz
? SignalR: Client abc123xyz subscribed to DNI group: 12345678

?? Iniciando registro con 3 muestras...
?? SignalR notification sent to DNI group '12345678': operation_started - Iniciando registro de huella

?? Muestra 1/3: Apoye el dedo firmemente.
?? SignalR notification sent to DNI group '12345678': sample_started - Capturando muestra 1/3

? ? Muestra 1 capturada. Retire el dedo completamente.
?? Imagen capturada - Muestra: 1, Calidad: 85.50
?? Notificación enviada: Muestra 1/3, Calidad: 85.50
?? SignalR notification sent to DNI group '12345678': sample_captured - ? Muestra 1/3 capturada...

... (se repite para muestras 2 y 3)

? Registro exitoso - Template: 512 bytes, Imágenes: 3
?? SignalR notification sent to DNI group '12345678': operation_completed - Registro completado...
```

---

## ? Checklist de Verificación

- [ ] Servicio corriendo en http://localhost:5000
- [ ] Abrir test-signalr.html en navegador
- [ ] Botón 1: Conectar SignalR ? ? Conectado
- [ ] Botón 2: Suscribirse al DNI ? ? Suscrito
- [ ] Botón 3: Enviar Test ? ? Recibe evento "test"
- [ ] Botón 4: Iniciar Registro ? ? Recibe eventos:
  - [ ] operation_started
  - [ ] sample_started (3 veces)
  - [ ] sample_captured (3 veces con imageBase64)
  - [ ] operation_completed

---

## ?? Siguiente Paso

Una vez que el test funcione, puedes usar el mismo código en tu aplicación Next.js:

```javascript
// En tu componente Next.js
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/fingerprint')
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveProgress', (notification) => {
    console.log('Evento recibido:', notification);
    // Actualizar tu UI aquí
});

await connection.start();
await connection.invoke('SubscribeToDni', '12345678');
```

---

**? SignalR Configurado y Listo para Usar!**

?? Última Actualización: 2025-01-XX  
?? Versión: 1.0  
? Estado: Producción Ready
