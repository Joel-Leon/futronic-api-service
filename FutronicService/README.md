# ?? Futronic Fingerprint API

> Sistema de captura, registro, verificación e identificación de huellas dactilares usando SDK Futronic FS88

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue)](https://dotnet.microsoft.com/)
[![Build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com)

---

## ?? Tabla de Contenidos

- [Características](#-características)
- [Requisitos](#-requisitos)
- [Instalación](#-instalación)
- [Inicio Rápido](#-inicio-rápido)
- [API Endpoints](#-api-endpoints)
- [Configuración](#-configuración)
- [Troubleshooting](#-troubleshooting)

---

## ? Características

- ? **Captura de Huellas** - Una o múltiples muestras
- ? **Registro de Usuarios** - Con 1-5 muestras para máxima precisión
- ? **Verificación 1:1** - Verifica identidad de usuario específico
- ? **Identificación 1:N** - Identifica usuario entre múltiples registros
- ? **Health Check** - Monitoreo de estado del dispositivo
- ? **Configuración Dinámica** - Ajustes sin reiniciar servicio

---

## ?? Requisitos

### Hardware
- Lector de huellas Futronic FS88
- Puerto USB 2.0 o superior
- Windows 10/11 o Windows Server

### Software
- .NET Framework 4.8
- Drivers de Futronic instalados

---

## ?? Instalación

```bash
# 1. Clonar repositorio
git clone https://github.com/tu-usuario/futronic-api.git
cd futronic-api/FutronicService

# 2. Restaurar paquetes
dotnet restore

# 3. Compilar
dotnet build

# 4. Ejecutar
dotnet run
```

El servicio estará disponible en: **http://localhost:5000**

---

## ?? Inicio Rápido

### Opción 1: Scripts PowerShell

```powershell
# Iniciar el servicio
.\start.ps1

# En otra ventana: Probar endpoints
.\test-all.ps1
```

### Opción 2: Postman

1. Importar `Futronic_API_Postman_Collection.json`
2. Variable `{{base_url}}` = `http://localhost:5000`
3. Ejecutar requests

---

## ?? API Endpoints

### Health Check
```http
GET /health
```

### Registrar Huella ?
```http
POST /api/fingerprint/register-multi

{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "outputPath": "C:/SistemaHuellas/huellas/12345678/indice-derecho",
"sampleCount": 5
}
```

### Verificar Identidad ?
```http
POST /api/fingerprint/verify-simple

{
  "dni": "12345678",
  "dedo": "indice-derecho"
}
```

### Identificar Usuario ?
```http
POST /api/fingerprint/identify-live

{
  "templatesDirectory": "C:/SistemaHuellas/huellas"
}
```

### Configuración
```http
GET /api/fingerprint/config
POST /api/fingerprint/config
```

---

## ?? Configuración

### appsettings.json
```json
{
  "Fingerprint": {
    "Threshold": 70,
    "Timeout": 30000,
    "StoragePath": "C:/SistemaHuellas/huellas"
  }
}
```

---

## ?? Troubleshooting

### Dispositivo no conectado
1. Verificar conexión USB
2. Revisar drivers
3. Reiniciar servicio

### Timeout en captura
1. Aumentar timeout
2. Limpiar sensor
3. Verificar colocación del dedo

### No identifica correctamente
1. Ajustar threshold (70-90)
2. Re-registrar con 5 muestras
3. Limpiar dedo y sensor

---

## ?? Documentación

- **POSTMAN_GUIDE.md** - Guía de Postman
- **PROJECT_COMPLETE.md** - Documentación completa
- **REFACTORING_GUIDE.md** - Guía de mejoras

---

## ? Estado

- ? Compilación sin errores
- ? Funcionalidad 100% operativa
- ? Listo para producción

**Versión**: 1.0.0  
**Última actualización**: Noviembre 2024
