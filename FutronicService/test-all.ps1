# Script de Pruebas Completas - Futronic API
# Prueba todos los endpoints de la API de forma interactiva

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   FUTRONIC API - PRUEBAS COMPLETAS" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000"

# Función para hacer requests HTTP
function Invoke-ApiRequest {
    param(
     [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null
    )
    
    try {
  $headers = @{
  "Content-Type" = "application/json"
 }
        
        $params = @{
  Uri = "$baseUrl$Endpoint"
    Method = $Method
            Headers = $headers
        }
        
        if ($Body) {
       $params.Body = ($Body | ConvertTo-Json -Depth 10)
      }
        
   $response = Invoke-RestMethod @params
      return $response
  }
    catch {
        Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
  $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
         $responseBody = $reader.ReadToEnd()
     Write-Host "Detalles: $responseBody" -ForegroundColor Yellow
        }
    return $null
    }
}

# Verificar que el servicio está corriendo
Write-Host "?? Verificando conexión con el servicio..." -ForegroundColor Yellow
try {
    $health = Invoke-ApiRequest -Method GET -Endpoint "/health"
    if ($health) {
        Write-Host "? Servicio conectado correctamente" -ForegroundColor Green
        Write-Host "   Estado: $($health.data.serviceStatus)" -ForegroundColor Gray
        Write-Host "   Dispositivo: $($health.data.deviceConnected)" -ForegroundColor Gray
        Write-Host ""
    } else {
Write-Host "? No se pudo conectar con el servicio" -ForegroundColor Red
      Write-Host "   Asegúrese de que el servicio está ejecutándose en $baseUrl" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "? Error al conectar con $baseUrl" -ForegroundColor Red
    Write-Host "   Inicie el servicio primero con: .\start.ps1" -ForegroundColor Yellow
    exit 1
}

# Menú principal
do {
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "   MENÚ DE PRUEBAS" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Health Check" -ForegroundColor White
    Write-Host "2. Captura Temporal" -ForegroundColor White
    Write-Host "3. Registro Simple (1 muestra)" -ForegroundColor White
    Write-Host "4. Registro Multi-Muestra (5 muestras) ?" -ForegroundColor White
Write-Host "5. Verificación Simple (1:1) ?" -ForegroundColor White
    Write-Host "6. Identificación en Vivo (1:N) ?" -ForegroundColor White
    Write-Host "7. Ver Configuración" -ForegroundColor White
    Write-Host "8. Actualizar Configuración" -ForegroundColor White
    Write-Host "9. Prueba Completa (Registrar + Verificar + Identificar)" -ForegroundColor Yellow
    Write-Host "0. Salir" -ForegroundColor White
    Write-Host ""
    
    $option = Read-Host "Seleccione una opción (0-9)"
    Write-Host ""
    
    switch ($option) {
  "1" {
      # Health Check
   Write-Host "?? Health Check" -ForegroundColor Cyan
      Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
            
    $response = Invoke-ApiRequest -Method GET -Endpoint "/health"
            if ($response) {
     Write-Host "? $($response.message)" -ForegroundColor Green
        Write-Host "   Estado: $($response.data.serviceStatus)" -ForegroundColor White
             Write-Host "   Dispositivo: $($response.data.deviceConnected)" -ForegroundColor White
    Write-Host "   Modelo: $($response.data.deviceModel)" -ForegroundColor White
        Write-Host "   SDK: $($response.data.sdkVersion)" -ForegroundColor White
   Write-Host "   Uptime: $($response.data.uptime)" -ForegroundColor White
            }
        }
        
        "2" {
   # Captura Temporal
 Write-Host "?? Captura Temporal" -ForegroundColor Cyan
       Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
    
            $timeout = Read-Host "Timeout en ms (Enter = 30000)"
            if ([string]::IsNullOrWhiteSpace($timeout)) { $timeout = 30000 }
            
   $body = @{
                timeout = [int]$timeout
            }
  
            Write-Host "?? Coloque su dedo en el lector..." -ForegroundColor Yellow
         $response = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/capture" -Body $body
        
      if ($response -and $response.success) {
    Write-Host "? $($response.message)" -ForegroundColor Green
         Write-Host "   Template: $($response.data.templatePath)" -ForegroundColor White
    Write-Host "   Imagen: $($response.data.imagePath)" -ForegroundColor White
           Write-Host "   Calidad: $($response.data.quality)" -ForegroundColor White
      }
        }
  
        "3" {
         # Registro Simple
          Write-Host "?? Registro Simple (1 muestra)" -ForegroundColor Cyan
 Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
         
        $dni = Read-Host "DNI (8 dígitos)"
 $dedo = Read-Host "Dedo (ej: indice-derecho)"
     $outputPath = Read-Host "Ruta de salida (Enter = C:/SistemaHuellas/huellas/$dni/$dedo)"
            
            if ([string]::IsNullOrWhiteSpace($outputPath)) {
              $outputPath = "C:/SistemaHuellas/huellas/$dni/$dedo"
}
  
            $body = @{
    dni = $dni
                dedo = $dedo
         outputPath = $outputPath
            }
            
       Write-Host "?? Coloque su dedo en el lector..." -ForegroundColor Yellow
  $response = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/register" -Body $body
            
      if ($response -and $response.success) {
 Write-Host "? $($response.message)" -ForegroundColor Green
      Write-Host "   DNI: $($response.data.dni)" -ForegroundColor White
    Write-Host "   Dedo: $($response.data.dedo)" -ForegroundColor White
             Write-Host "   Calidad: $($response.data.quality)" -ForegroundColor White
          Write-Host "   Template: $($response.data.templatePath)" -ForegroundColor White
        }
        }
 
     "4" {
   # Registro Multi-Muestra
    Write-Host "?? Registro Multi-Muestra (5 muestras) ?" -ForegroundColor Cyan
            Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
        
 $dni = Read-Host "DNI (8 dígitos)"
            $dedo = Read-Host "Dedo (ej: indice-derecho)"
         $samples = Read-Host "Número de muestras (Enter = 5)"
   if ([string]::IsNullOrWhiteSpace($samples)) { $samples = 5 }
        
      $outputPath = "C:/SistemaHuellas/huellas/$dni/$dedo"
            
   $body = @{
    dni = $dni
      dedo = $dedo
       outputPath = $outputPath
                sampleCount = [int]$samples
  }
        
         Write-Host ""
  Write-Host "?? Coloque su dedo en el lector $samples veces..." -ForegroundColor Yellow
   Write-Host "   Levante y vuelva a colocar el dedo entre cada muestra" -ForegroundColor Gray
            Write-Host ""
            
            $response = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/register-multi" -Body $body
      
            if ($response -and $response.success) {
                Write-Host "? $($response.message)" -ForegroundColor Green
         Write-Host "   DNI: $($response.data.dni)" -ForegroundColor White
    Write-Host "   Dedo: $($response.data.dedo)" -ForegroundColor White
            Write-Host "   Muestras: $($response.data.samplesCollected)" -ForegroundColor White
     Write-Host "   Calidad Promedio: $($response.data.averageQuality)" -ForegroundColor White
 Write-Host "   Template: $($response.data.templatePath)" -ForegroundColor White
     }
        }
        
     "5" {
   # Verificación Simple
         Write-Host "?? Verificación Simple (1:1) ?" -ForegroundColor Cyan
      Write-Host "?????????????????????????????????????????" -ForegroundColor Gray

            $dni = Read-Host "DNI a verificar"
            $dedo = Read-Host "Dedo (ej: indice-derecho)"
            
            $body = @{
         dni = $dni
            dedo = $dedo
   }
    
            Write-Host ""
            Write-Host "?? Coloque su dedo en el lector para verificar..." -ForegroundColor Yellow
     $response = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/verify-simple" -Body $body
 
            if ($response) {
  if ($response.success -and $response.data.matched) {
  Write-Host "? $($response.message)" -ForegroundColor Green
        Write-Host "   Verificado: SÍ ?" -ForegroundColor Green
  } else {
         Write-Host "? $($response.message)" -ForegroundColor Yellow
      Write-Host "   Verificado: NO ?" -ForegroundColor Red
          }
       Write-Host "   Score: $($response.data.score)" -ForegroundColor White
   Write-Host "   Threshold: $($response.data.threshold)" -ForegroundColor White
      }
        }
        
        "6" {
       # Identificación en Vivo
  Write-Host "?? Identificación en Vivo (1:N) ?" -ForegroundColor Cyan
            Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
            
   $directory = Read-Host "Directorio de templates (Enter = C:/SistemaHuellas/huellas)"
            if ([string]::IsNullOrWhiteSpace($directory)) {
          $directory = "C:/SistemaHuellas/huellas"
            }
       
            if (!(Test-Path $directory)) {
   Write-Host "? El directorio no existe: $directory" -ForegroundColor Red
       continue
       }
            
     $body = @{
  templatesDirectory = $directory
            }
      
            Write-Host ""
   Write-Host "?? Coloque su dedo en el lector para identificar..." -ForegroundColor Yellow
        $response = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/identify-live" -Body $body
            
if ($response) {
                if ($response.success -and $response.data.matched) {
  Write-Host "? $($response.message)" -ForegroundColor Green
             Write-Host " DNI Identificado: $($response.data.dni)" -ForegroundColor Green
     Write-Host "   Dedo: $($response.data.dedo)" -ForegroundColor White
     Write-Host "   Índice: $($response.data.matchIndex)" -ForegroundColor White
        } else {
     Write-Host "? No se encontró coincidencia" -ForegroundColor Yellow
                }
            Write-Host "   Score: $($response.data.score)" -ForegroundColor White
          Write-Host "   Templates Comparados: $($response.data.totalCompared)" -ForegroundColor White
          }
    }
      
        "7" {
            # Ver Configuración
        Write-Host "?? Configuración Actual" -ForegroundColor Cyan
            Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
  
            $response = Invoke-ApiRequest -Method GET -Endpoint "/api/fingerprint/config"
          if ($response) {
         Write-Host "? Configuración:" -ForegroundColor Green
     Write-Host "   Threshold: $($response.data.threshold)" -ForegroundColor White
          Write-Host "   Timeout: $($response.data.timeout) ms" -ForegroundColor White
  Write-Host " Temp Path: $($response.data.tempPath)" -ForegroundColor White
     Write-Host "   Overwrite: $($response.data.overwriteExisting)" -ForegroundColor White
            }
        }
      
        "8" {
            # Actualizar Configuración
   Write-Host "?? Actualizar Configuración" -ForegroundColor Cyan
         Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
            
      $threshold = Read-Host "Nuevo Threshold (0-100, Enter para mantener actual)"
        $timeout = Read-Host "Nuevo Timeout en ms (1000-60000, Enter para mantener actual)"
        
 $body = @{}
            if (![string]::IsNullOrWhiteSpace($threshold)) {
            $body.threshold = [int]$threshold
            }
       if (![string]::IsNullOrWhiteSpace($timeout)) {
              $body.timeout = [int]$timeout
      }
  
if ($body.Count -gt 0) {
       $response = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/config" -Body $body
     if ($response) {
        Write-Host "? Configuración actualizada" -ForegroundColor Green
        Write-Host "   Threshold: $($response.data.threshold)" -ForegroundColor White
              Write-Host "   Timeout: $($response.data.timeout) ms" -ForegroundColor White
         }
    } else {
         Write-Host "?? No se realizaron cambios" -ForegroundColor Yellow
       }
   }
        
        "9" {
        # Prueba Completa
     Write-Host "?? Prueba Completa del Sistema" -ForegroundColor Cyan
            Write-Host "?????????????????????????????????????????" -ForegroundColor Gray
            Write-Host ""
      
     $testDni = "99999999"
         $testDedo = "indice-derecho"
          $testPath = "C:/SistemaHuellas/huellas/$testDni/$testDedo"
         
     Write-Host "?? Paso 1: Registrando usuario de prueba..." -ForegroundColor Yellow
   Write-Host "   DNI: $testDni" -ForegroundColor Gray
   Write-Host "   Dedo: $testDedo" -ForegroundColor Gray
            Write-Host ""
            Write-Host "?? Coloque su dedo en el lector 5 veces..." -ForegroundColor Yellow
    
            $registerBody = @{
                dni = $testDni
    dedo = $testDedo
          outputPath = $testPath
     sampleCount = 5
 }
         
      $registerResponse = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/register-multi" -Body $registerBody
   
       if (!$registerResponse -or !$registerResponse.success) {
    Write-Host "? Falló el registro. Abortando prueba." -ForegroundColor Red
         continue
            }
            
   Write-Host "? Registro exitoso" -ForegroundColor Green
  Write-Host ""
         Start-Sleep -Seconds 2
            
       # Verificación
       Write-Host "?? Paso 2: Verificando identidad..." -ForegroundColor Yellow
            Write-Host "?? Coloque su dedo en el lector de nuevo..." -ForegroundColor Yellow
         
   $verifyBody = @{
          dni = $testDni
        dedo = $testDedo
   }
   
            $verifyResponse = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/verify-simple" -Body $verifyBody
          
        if ($verifyResponse -and $verifyResponse.data.matched) {
     Write-Host "? Verificación exitosa" -ForegroundColor Green
            } else {
           Write-Host "? Verificación falló" -ForegroundColor Red
            }
   Write-Host ""
            Start-Sleep -Seconds 2
  
            # Identificación
       Write-Host "?? Paso 3: Identificando usuario..." -ForegroundColor Yellow
  Write-Host "?? Coloque su dedo en el lector una vez más..." -ForegroundColor Yellow
            
  $identifyBody = @{
     templatesDirectory = "C:/SistemaHuellas/huellas"
   }
      
     $identifyResponse = Invoke-ApiRequest -Method POST -Endpoint "/api/fingerprint/identify-live" -Body $identifyBody
         
  if ($identifyResponse -and $identifyResponse.data.matched -and $identifyResponse.data.dni -eq $testDni) {
         Write-Host "? Identificación exitosa" -ForegroundColor Green
    Write-Host "   Usuario identificado correctamente: $($identifyResponse.data.dni)" -ForegroundColor Green
        } else {
    Write-Host "? Identificación falló" -ForegroundColor Red
      }
  Write-Host ""
   
     # Resumen
     Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
            Write-Host "   RESUMEN DE PRUEBA COMPLETA" -ForegroundColor Cyan
          Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host " Registro: $(if ($registerResponse.success) { '?' } else { '?' })" -ForegroundColor White
 Write-Host "   Verificación: $(if ($verifyResponse.data.matched) { '?' } else { '?' })" -ForegroundColor White
            Write-Host "   Identificación: $(if ($identifyResponse.data.matched) { '?' } else { '?' })" -ForegroundColor White
        Write-Host "????????????????????????????????????????" -ForegroundColor Cyan
        }
        
     "0" {
Write-Host "?? Saliendo..." -ForegroundColor Gray
            break
   }
        
    default {
      Write-Host "? Opción inválida" -ForegroundColor Red
        }
    }
    
    if ($option -ne "0") {
   Write-Host ""
        Write-Host "Presione Enter para continuar..." -ForegroundColor Gray
   Read-Host
        Write-Host ""
    }
    
} while ($option -ne "0")

Write-Host ""
Write-Host "? Pruebas finalizadas" -ForegroundColor Green
Write-Host ""
