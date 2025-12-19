# Script para verificar y actualizar la configuración del servicio de huellas

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Verificador de Configuración" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$apiUrl = "http://localhost:5000/api/configuration"

try {
    # Verificar si el servicio está corriendo
    Write-Host "?? Verificando servicio..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri $apiUrl -Method GET -ErrorAction Stop
    
    if ($response.success) {
        Write-Host "? Servicio conectado correctamente" -ForegroundColor Green
        Write-Host ""
        
        # Mostrar configuración actual
        Write-Host "?? Configuración actual:" -ForegroundColor Cyan
        Write-Host "----------------------------------------" -ForegroundColor Gray
        
        $config = $response.data
        
        # Parámetros clave
        Write-Host "  Threshold:          $($config.threshold)" -ForegroundColor White
        Write-Host "  Timeout:            $($config.timeout) ms" -ForegroundColor White
        Write-Host "  DetectFakeFinger:   $($config.detectFakeFinger)" -ForegroundColor $(if ($config.detectFakeFinger) { "Yellow" } else { "Green" })
        Write-Host "  MaxRotation:        $($config.maxRotation)" -ForegroundColor White
        Write-Host "  MinQuality:         $($config.minQuality)" -ForegroundColor White
        Write-Host "  MaxFramesInTemplate:$($config.maxFramesInTemplate)" -ForegroundColor White
        Write-Host "  DisableMIDT:        $($config.disableMIDT)" -ForegroundColor White
        Write-Host ""
        
        # Advertencias
        if ($config.detectFakeFinger) {
            Write-Host "??  ADVERTENCIA: DetectFakeFinger está ACTIVADO" -ForegroundColor Yellow
            Write-Host "   Esto puede causar rechazos frecuentes y tiempos más lentos." -ForegroundColor Yellow
            Write-Host ""
            
            $response = Read-Host "¿Desea desactivarlo? (S/N)"
            if ($response -eq "S" -or $response -eq "s") {
                Write-Host ""
                Write-Host "?? Desactivando DetectFakeFinger..." -ForegroundColor Yellow
                
                $body = @{
                    detectFakeFinger = $false
                } | ConvertTo-Json
                
                $updateResponse = Invoke-RestMethod -Uri $apiUrl -Method PATCH -ContentType "application/json" -Body $body
                
                if ($updateResponse.success) {
                    Write-Host "? DetectFakeFinger desactivado correctamente" -ForegroundColor Green
                } else {
                    Write-Host "? Error al actualizar: $($updateResponse.message)" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "? DetectFakeFinger está DESACTIVADO (recomendado)" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host "?? Para ver todas las opciones: http://localhost:5000/api/configuration" -ForegroundColor Cyan
        Write-Host "?? Documentación completa: GUIA_CONFIGURACION_API.md" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "? Error al conectar con el servicio" -ForegroundColor Red
    Write-Host "   Asegúrese de que el servicio esté corriendo en http://localhost:5000" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   Ejecute: cd FutronicService && dotnet run" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
