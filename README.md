# ?? Futronic SDK - Sistema de Huellas Dactilares

Solución completa para captura, registro, verificación e identificación de huellas dactilares usando el SDK de Futronic.

## ?? Proyectos Incluidos

### 1. **futronic-cli** - Aplicación de Consola (CLI)
Aplicación original de línea de comandos para operaciones básicas con el lector de huellas.

- ? Captura de huellas dactilares
- ? Verificación 1:1 de huellas
- ? Almacenamiento de templates
- ? Procesamiento de imágenes

**Uso:**
```bash
futronic-cli.exe capture <nombre> [opciones]
futronic-cli.exe verify <nombre> [opciones]
```

**Documentación:** Ver código fuente y comentarios en el proyecto

---

### 2. **FutronicService** - Microservicio REST API ? **NUEVO**
Servicio web completo que expone toda la funcionalidad del SDK a través de endpoints REST.

- ? API REST completa con 7 endpoints
- ? Captura, registro, verificación e identificación
- ? Health checks y configuración dinámica
- ? CORS configurado para aplicaciones web
- ? Manejo de errores robusto
- ? Documentación exhaustiva

**Quick Start:**
```powershell
cd FutronicService
.\start.ps1
```

**Endpoints principales:**
- `GET /health` - Estado del servicio
- `POST /api/fingerprint/capture` - Capturar huella
- `POST /api/fingerprint/register` - Registrar huella
- `POST /api/fingerprint/verify` - Verificar huella (1:1)
- `POST /api/fingerprint/identify` - Identificar huella (1:N)

**Documentación completa:** `FutronicService/README.md`

---

## ?? ¿Cuál Usar?

| Característica | CLI | API Service |
|----------------|-----|-------------|
| **Tipo** | Aplicación de consola | Servicio web |
| **Uso** | Pruebas, scripts, operaciones simples | Integración con aplicaciones web/móvil |
| **Interface** | Línea de comandos | HTTP REST (JSON) |
| **Integración** | Scripts batch/PowerShell | Cualquier lenguaje/plataforma |
| **Recomendado para** | Testing, desarrollo, automatización local | Producción, aplicaciones empresariales |

## ?? Inicio Rápido

### CLI (Pruebas Rápidas)

```bash
# Capturar huella
cd futronic-cli
futronic-cli.exe capture test_user

# Verificar huella
futronic-cli.exe verify test_user
```

### API Service (Aplicaciones en Producción)

```powershell
# Opción 1: Script automatizado
cd FutronicService
.\start.ps1

# Opción 2: Manual
cd FutronicService
dotnet build --configuration Release
dotnet run

# Verificar que funciona
Invoke-RestMethod -Uri "http://localhost:5000/health"
```

## ?? Requisitos del Sistema

### Comunes (Ambos Proyectos)
- ? Windows 10/11 o Windows Server 2016+
- ? .NET Framework 4.8
- ? Dispositivo Futronic (FS88 o compatible)
- ? Drivers de Futronic instalados
- ? SDK de Futronic (DLLs incluidas)

### Adicionales (Solo API Service)
- ? Puerto 5000 disponible (o configurar otro)
- ? Permisos para crear reglas de firewall (opcional)

## ?? Documentación

### FutronicService (API)
- ?? **README.md** - Documentación completa de la API
- ?? **MIGRATION.md** - Guía de migración CLI ? API
- ?? **EXAMPLES_CLIENT.md** - Ejemplos JavaScript/React
- ? **DEPLOYMENT_CHECKLIST.md** - Checklist para producción
- ?? **RESUMEN_EJECUTIVO.md** - Estado del proyecto

### Scripts Útiles
- ?? **start.ps1** - Inicio rápido del servicio
- ?? **test-api.ps1** - Pruebas automatizadas

## ??? Arquitectura

```
???????????????????????????????????????????????????
?     Aplicaciones Cliente   ?
?  (Web, Móvil, Desktop, Scripts)                 ?
???????????????????????????????????????????????????
  ? HTTP/REST
           ?
???????????????????????????????????????????????????
?         FutronicService (API)  ?
?  - Controllers (Endpoints REST)        ?
?  - Services (Lógica de negocio)       ?
?  - Middleware (Errores, CORS)    ?
???????????????????????????????????????????????????
           ? Referencia de proyecto
      ?
???????????????????????????????????????????????????
?         futronic-cli (Librería)  ?
?  - FingerprintCaptureService         ?
?  - FingerprintVerificationService          ?
?- TemplateUtils, ImageUtils, etc.  ?
???????????????????????????????????????????????????
         ? P/Invoke
        ?
???????????????????????????????????????????????????
?         SDK de Futronic (DLLs nativas)     ?
?  - Captura de huellas     ?
?  - Generación de templates        ?
?  - Comparación biométrica        ?
???????????????????????????????????????????????????
               ? USB
      ?
???????????????????????????????????????????????????
?      Dispositivo Futronic FS88          ?
?      (Lector de Huellas Dactilares)    ?
???????????????????????????????????????????????????
```

## ?? Ejemplos de Uso

### Integración con JavaScript

```javascript
// Capturar huella
const response = await fetch('http://localhost:5000/api/fingerprint/capture', {
  method: 'POST',
headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ timeout: 30000 })
});
const result = await response.json();
console.log('Huella capturada:', result.data);
```

### Integración con Python

```python
import requests

# Verificar huella
response = requests.post('http://localhost:5000/api/fingerprint/verify', json={
    'storedTemplate': 'C:/path/stored.tml',
    'capturedTemplate': 'C:/path/captured.tml'
})
result = response.json()
print('Coincide:', result['data']['matched'])
```

### Integración con C#

```csharp
using System.Net.Http;
using System.Text.Json;

// Identificar persona
var client = new HttpClient();
var request = new {
    capturedTemplate = "C:/path/captured.tml",
    templates = new[] {
        new { dni = "12345678", dedo = "indice-derecho", templatePath = "C:/path/12345678.tml" }
    }
};

var response = await client.PostAsJsonAsync(
    "http://localhost:5000/api/fingerprint/identify", 
    request
);
var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
```

## ?? Seguridad

- ? CORS configurado (solo orígenes autorizados)
- ? Validación de paths (prevención de path traversal)
- ? Manejo seguro de errores (sin exposición de información sensible)
- ? Sanitización de entradas
- ?? **Recomendado para producción**: Agregar autenticación (API Keys, JWT, OAuth)

## ?? Troubleshooting

### Dispositivo no detectado
1. Verificar conexión USB
2. Instalar drivers de Futronic
3. Verificar en Administrador de Dispositivos
4. Reiniciar el servicio

### Puerto en uso
```powershell
# Ver qué está usando el puerto 5000
netstat -ano | findstr :5000

# Cambiar puerto en appsettings.json
# "Url": "http://localhost:OTRO_PUERTO"
```

### Error de compilación
1. Verificar .NET Framework 4.8 instalado
2. Restaurar paquetes NuGet
3. Limpiar y recompilar solución

## ?? Soporte

### Recursos
- ?? Documentación completa en `FutronicService/README.md`
- ?? Pruebas automatizadas: `FutronicService/test-api.ps1`
- ?? Ejemplos de cliente: `FutronicService/EXAMPLES_CLIENT.md`

### Logs
- API Service: Logs en consola (Serilog)
- CLI: Salida estándar con códigos de color

## ?? Roadmap

### Completado ?
- [x] CLI funcional
- [x] API REST completa
- [x] Documentación exhaustiva
- [x] Scripts de prueba
- [x] Ejemplos de integración

### Futuro ??
- [ ] Autenticación (API Keys, JWT)
- [ ] Rate limiting
- [ ] Swagger/OpenAPI
- [ ] Migración a .NET 6/8
- [ ] Contenedorización (Docker)
- [ ] Base de datos para templates
- [ ] WebSockets para notificaciones en tiempo real

## ?? Licencia

Este proyecto utiliza el SDK de Futronic. Consultar licencia del SDK para uso comercial.

## ?? Contribuciones

Las contribuciones son bienvenidas. Por favor:
1. Fork el repositorio
2. Crear rama feature (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

## ?? Estado del Proyecto

| Componente | Estado | Versión |
|------------|--------|---------|
| **futronic-cli** | ? Estable | 1.0 |
| **FutronicService** | ? Production Ready | 1.0 |
| **Documentación** | ? Completa | 1.0 |
| **Tests** | ? Funcional | 1.0 |

---

**Desarrollado con ?? para integración biométrica segura y confiable**
