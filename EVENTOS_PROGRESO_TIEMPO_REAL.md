# ?? Eventos de Progreso en Tiempo Real - Guía de Implementación

## ?? Problema

Cuando un cliente hace una solicitud a los endpoints de captura/registro/verificación, la respuesta solo llega al final del proceso. Durante la captura, el usuario no recibe las instrucciones que se muestran en la consola del servidor:

- "Apoye el dedo firmemente"
- "Retire el dedo completamente"
- "Muestra X/Y capturada"
- Etc.

## ?? Soluciones Disponibles

### **Solución 1: HTTP Callbacks (Implementada) ?**

El cliente proporciona una URL de callback donde recibirá eventos de progreso mediante HTTP POST.

**Ventajas:**
- ? Simple de implementar
- ? Compatible con cualquier cliente HTTP
- ? No requiere librerías especiales
- ? Funciona con frameworks antiguos

**Desventajas:**
- ? Requiere que el cliente tenga un servidor HTTP
- ? No es bidireccional en tiempo real

---

### **Solución 2: Server-Sent Events (SSE)**

El servidor envía eventos al cliente a través de una conexión HTTP persistente.

**Ventajas:**
- ? Unidireccional servidor?cliente
- ? Conexión persistente
- ? Soportado nativamente en navegadores

**Desventajas:**
- ? Requiere modificaciones significativas al código
- ? No funciona bien con proxies/firewalls antiguos

---

### **Solución 3: SignalR / WebSockets**

Comunicación bidireccional en tiempo real.

**Ventajas:**
- ? Bidireccional
- ? Baja latencia
- ? Manejo de reconexión automático

**Desventajas:**
- ? Complejo de implementar en .NET Framework 4.8
- ? Conflictos de dependencias
- ? Requiere librerías adicionales en el cliente

---

### **Solución 4: Polling**

El cliente consulta periódicamente un endpoint de estado.

**Ventajas:**
- ? Muy simple
- ? Compatible con todo

**Desventajas:**
- ? Ineficiente (muchas peticiones)
- ? Latencia alta
- ? Consume más recursos

---

## ?? Implementación Recomendada: HTTP Callbacks

### Cómo Funciona

1. El cliente inicia un servidor HTTP simple en un puerto local
2. El cliente envía el request con `callbackUrl`
3. El servidor Futronic envía eventos de progreso a esa URL mediante POST
4. El cliente muestra los mensajes al usuario en tiempo real

### Ejemplo de Uso

#### 1. Request con Callback

```http
POST /api/fingerprint/register-multi
Content-Type: application/json

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "callbackUrl": "http://localhost:8080/progress"
}
```

#### 2. Eventos que Recibirá el Cliente

El servidor enviará POST requests a `http://localhost:8080/progress` con este formato:

```json
{
  "eventType": "PUT_ON",
  "message": "Muestra 1/5: Apoye el dedo firmemente",
  "data": {
    "currentSample": 1,
    "totalSamples": 5
  },
  "timestamp": "2025-01-15T14:30:22Z"
}
```

```json
{
  "eventType": "IMAGE_CAPTURED",
  "message": "Imagen capturada - Calidad: 88.45",
  "data": {
    "sampleIndex": 1,
    "quality": 88.45
  },
  "timestamp": "2025-01-15T14:30:23Z"
}
```

```json
{
  "eventType": "TAKE_OFF",
  "message": "Muestra 1 capturada. Retire el dedo completamente",
  "data": {
    "currentSample": 1,
    "totalSamples": 5
  },
  "timestamp": "2025-01-15T14:30:24Z"
}
```

```json
{
  "eventType": "COMPLETE",
  "message": "¡Captura exitosa!",
  "data": {
    "totalImages": 5,
    "averageQuality": 89.5
  },
  "timestamp": "2025-01-15T14:30:40Z"
}
```

### Tipos de Eventos

| EventType | Descripción | Cuándo se Envía |
|-----------|-------------|-----------------|
| `PUT_ON` | Apoye el dedo | Al inicio de cada muestra |
| `TAKE_OFF` | Retire el dedo | Al completar cada muestra |
| `IMAGE_CAPTURED` | Imagen capturada | Al capturar imagen BMP |
| `FAKE_SOURCE` | Señal ambigua | Al detectar problema |
| `COMPLETE` | Proceso completado | Al finalizar exitosamente |
| `ERROR` | Error ocurrido | Si hay algún error |

---

## ?? Implementación del Cliente

### Ejemplo en Node.js/Express

```javascript
const express = require('express');
const axios = require('axios');
const app = express();

app.use(express.json());

// Endpoint para recibir callbacks de progreso
app.post('/progress', (req, res) => {
    const { eventType, message, data } = req.body;
    
  // Mostrar al usuario
    console.log(`[${eventType}] ${message}`);
    
    // Actualizar UI (ejemplo con WebSocket al frontend)
    if (io) {
        io.emit('fingerprint-progress', { eventType, message, data });
    }
  
    res.status(200).send('OK');
});

app.listen(8080, () => {
    console.log('Servidor de callbacks en http://localhost:8080');
});

// Hacer request al servicio Futronic
async function registrarHuella() {
    try {
   const response = await axios.post('http://localhost:5000/api/fingerprint/register-multi', {
     dni: '12345678',
         dedo: 'indice-derecho',
    sampleCount: 5,
  callbackUrl: 'http://localhost:8080/progress'
        });
     
console.log('Registro completado:', response.data);
    } catch (error) {
  console.error('Error:', error.message);
    }
}

registrarHuella();
```

### Ejemplo en Python/Flask

```python
from flask import Flask, request, jsonify
import requests
import threading

app = Flask(__name__)

@app.route('/progress', methods=['POST'])
def progress_callback():
    data = request.json
    event_type = data.get('eventType')
    message = data.get('message')
    
    print(f"[{event_type}] {message}")
    
  # Actualizar UI (enviar a WebSocket, actualizar ventana, etc.)
    
    return jsonify({'status': 'ok'}), 200

def start_callback_server():
    app.run(port=8080)

# Iniciar servidor en thread separado
threading.Thread(target=start_callback_server, daemon=True).start()

# Hacer request al servicio Futronic
response = requests.post('http://localhost:5000/api/fingerprint/register-multi', json={
    'dni': '12345678',
    'dedo': 'indice-derecho',
    'sampleCount': 5,
    'callbackUrl': 'http://localhost:8080/progress'
})

print('Resultado:', response.json())
```

### Ejemplo en C# (Cliente .NET)

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    static HttpListener listener;
    
    static async Task Main(string[] args)
    {
 // Iniciar servidor de callbacks
     listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Servidor de callbacks iniciado en http://localhost:8080");
        
        // Escuchar callbacks en background
    _ = Task.Run(() => ListenForCallbacks());
        
        // Hacer request al servicio Futronic
await RegistrarHuella();
        
     Console.WriteLine("Presione Enter para salir...");
        Console.ReadLine();
        listener.Stop();
    }
    
    static async Task ListenForCallbacks()
    {
      while (listener.IsListening)
        {
 try
       {
        var context = await listener.GetContextAsync();
  var request = context.Request;
           
       using (var reader = new StreamReader(request.InputStream))
             {
              string json = await reader.ReadToEndAsync();
          var data = JsonConvert.DeserializeObject<dynamic>(json);
      
           Console.WriteLine($"[{data.eventType}] {data.message}");
      
          // Aquí actualizas tu UI
                }
   
    var response = context.Response;
     response.StatusCode = 200;
       response.Close();
      }
            catch (Exception ex)
         {
   Console.WriteLine($"Error en callback: {ex.Message}");
    }
        }
    }
    
    static async Task RegistrarHuella()
    {
        using (var client = new HttpClient())
        {
      var payload = new
        {
             dni = "12345678",
   dedo = "indice-derecho",
                sampleCount = 5,
   callbackUrl = "http://localhost:8080/progress"
            };
          
 var json = JsonConvert.SerializeObject(payload);
          var content = new StringContent(json, Encoding.UTF8, "application/json");
    
            var response = await client.PostAsync("http://localhost:5000/api/fingerprint/register-multi", content);
            var result = await response.Content.ReadAsStringAsync();
    
            Console.WriteLine("Resultado final: " + result);
        }
    }
}
```

---

## ?? Ejemplo de UI con HTML/JavaScript

```html
<!DOCTYPE html>
<html>
<head>
    <title>Captura de Huella</title>
    <style>
        .progress-message {
        margin: 10px 0;
            padding: 10px;
   border-radius: 5px;
     animation: fadeIn 0.3s;
        }
        @keyframes fadeIn {
       from { opacity: 0; }
        to { opacity: 1; }
        }
    </style>
</head>
<body>
    <h1>Registro de Huella Digital</h1>
    <button onclick="registrarHuella()">Iniciar Captura</button>
    <div id="progress"></div>
    <div id="result"></div>

    <script>
        // Conectar al servidor de callbacks (Socket.IO, WebSocket, etc.)
        const socket = io('http://localhost:3000');
        
        socket.on('fingerprint-progress', (data) => {
        const { eventType, message } = data;
        const progressDiv = document.getElementById('progress');
      
            const messageDiv = document.createElement('div');
   messageDiv.className = 'progress-message';
         messageDiv.textContent = message;
      
          // Colorear según tipo de evento
 switch(eventType) {
   case 'PUT_ON':
        messageDiv.style.backgroundColor = '#e3f2fd';
break;
   case 'TAKE_OFF':
    messageDiv.style.backgroundColor = '#e8f5e9';
         break;
       case 'IMAGE_CAPTURED':
      messageDiv.style.backgroundColor = '#fff3e0';
      break;
        case 'COMPLETE':
           messageDiv.style.backgroundColor = '#c8e6c9';
          break;
        case 'ERROR':
     messageDiv.style.backgroundColor = '#ffcdd2';
          break;
       }
   
         progressDiv.appendChild(messageDiv);
        });
        
        async function registrarHuella() {
   document.getElementById('progress').innerHTML = '';
          
     const response = await fetch('http://localhost:5000/api/fingerprint/register-multi', {
      method: 'POST',
         headers: { 'Content-Type': 'application/json' },
           body: JSON.stringify({
    dni: '12345678',
                    dedo: 'indice-derecho',
  sampleCount: 5,
       callbackUrl: 'http://localhost:3000/progress' // Tu servidor Node.js
     })
            });
     
            const result = await response.json();
            document.getElementById('result').innerHTML = `
             <h3>Resultado:</h3>
       <pre>${JSON.stringify(result, null, 2)}</pre>
          `;
        }
    </script>
</body>
</html>
```

---

## ?? Configuración en Postman (Para Testing)

1. **Usar un servidor mock**:
   - Utiliza Postman Mock Server
   - O usa https://webhook.site/ para recibir callbacks
   - O usa ngrok para exponer un servidor local

2. **Request de ejemplo**:
```json
POST http://localhost:5000/api/fingerprint/register-multi

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "callbackUrl": "https://webhook.site/tu-id-unico"
}
```

3. **Ver los eventos**:
   - Ve a https://webhook.site/tu-id-unico
   - Verás todos los eventos POST que el servidor envía

---

## ?? Testing sin Callback

Si no proporcionas `callbackUrl`, el comportamiento es el mismo de antes:
- Los mensajes solo se muestran en la consola del servidor
- El cliente recibe la respuesta final al terminar el proceso

---

## ?? Comparación de Soluciones

| Característica | HTTP Callbacks | SSE | SignalR | Polling |
|----------------|----------------|-----|---------|---------|
| **Complejidad** | Baja | Media | Alta | Muy Baja |
| **Rendimiento** | Bueno | Excelente | Excelente | Malo |
| **Compatibilidad** | Alta | Media | Media | Alta |
| **Tiempo real** | ~100ms delay | Tiempo real | Tiempo real | 1-5s delay |
| **Bidireccional** | ? | ? | ? | ? |
| **Requiere servidor cliente** | ? | ? | ? | ? |

---

## ?? Recomendación

Para tu caso de uso:
- **Desarrollo/Testing**: Usa **HTTP Callbacks** con webhook.site
- **Producción con UI web**: Usa **HTTP Callbacks** + WebSocket entre tu backend y frontend
- **App de escritorio**: Usa **HTTP Callbacks** con servidor local
- **App móvil**: Usa **HTTP Callbacks** con servidor local en el dispositivo

---

## ?? Próximos Pasos

1. ? HTTP Callbacks implementado (opcional)
2. ?? Implementar SSE si es necesario
3. ?? Implementar WebSockets/SignalR si se requiere bidireccionalidad
4. ?? Crear librería cliente para facilitar integración

---

## ?? Alternativa Simple: Mostrar en UI del Servidor

Si tu cliente está en la misma máquina que el servidor, puedes crear una UI simple en el servidor mismo usando:
- **Windows Forms** (aplicación de escritorio)
- **WPF** (aplicación moderna de escritorio)
- **Console con colores** (para terminal)

Esto elimina la necesidad de callbacks HTTP.

¿Quieres que implemente alguna de estas alternativas?
