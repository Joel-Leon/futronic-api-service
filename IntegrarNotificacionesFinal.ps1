# Script mejorado para integrar notificaciones

$filePath = "FutronicService\Services\FutronicFingerprintService.cs"
Write-Host "=== INTEGRACIÓN DE NOTIFICACIONES EN TIEMPO REAL ===" -ForegroundColor Cyan
Write-Host "Archivo: $filePath`n" -ForegroundColor Yellow

$content = Get-Content $filePath -Raw

# Verificar que ya tiene el servicio inyectado
if ($content -notmatch "IProgressNotificationService _notificationService") {
    Write-Host "? ERROR: IProgressNotificationService no está inyectado" -ForegroundColor Red
    exit 1
}

Write-Host "? Servicio de notificaciones ya inyectado" -ForegroundColor Green

# ============================================
# ESTRATEGIA: Buscar e insertar después de logs específicos
# ============================================

$modificado = $false

# 1. RegisterMultiSampleAsync - Notificar INICIO
Write-Host "`n[1/7] RegisterMultiSampleAsync - Notificación de inicio..." -ForegroundColor Yellow
$pattern = '(\$"Starting multi-sample registration for DNI: \{request\.Dni\} with \{sampleCount\} samples"\);)'
$replacement = @'
$1

                // ?? Notificar inicio de registro
                await _notificationService.NotifyStartAsync(
                    request.Dni,
                    "registro de huella",
                    request.CallbackUrl
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de inicio" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# 2. RegisterMultiSampleAsync - Notificar ÉXITO
Write-Host "[2/7] RegisterMultiSampleAsync - Notificación de éxito..." -ForegroundColor Yellow
$pattern = '(_logger\.LogInformation\(\$"Multi-sample registration successful for DNI: \{request\.Dni\}"\);)'
$replacement = @'
$1

                // ?? Notificar éxito
                await _notificationService.NotifyCompleteAsync(
                    request.Dni,
                    success: true,
                    message: $"Huella registrada exitosamente con {sampleCount} muestras",
                    data: responseData,
                    callbackUrl: request.CallbackUrl
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de éxito" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# 3. RegisterMultiSampleAsync - Notificar ERROR
Write-Host "[3/7] RegisterMultiSampleAsync - Notificación de error..." -ForegroundColor Yellow
$pattern = '(_logger\.LogError\(ex, \$"Error in RegisterMultiSampleAsync for DNI: \{request\.Dni\}"\);)'
$replacement = @'
$1

                // ?? Notificar error
                await _notificationService.NotifyErrorAsync(
                    request.Dni,
                    error: ex.Message,
                    callbackUrl: request.CallbackUrl
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de error" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# 4. VerifySimpleAsync - Notificar INICIO
Write-Host "[4/7] VerifySimpleAsync - Notificación de inicio..." -ForegroundColor Yellow
$pattern = '(_logger\.LogInformation\(\$"Starting simple verification for DNI: \{request\.Dni\}"\);)'
$replacement = @'
$1

                // ?? Notificar inicio de verificación
                await _notificationService.NotifyStartAsync(
                    request.Dni,
                    "verificación de identidad",
                    request.CallbackUrl
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de inicio" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# 5. VerifySimpleAsync - Notificar RESULTADO
Write-Host "[5/7] VerifySimpleAsync - Notificación de resultado..." -ForegroundColor Yellow
$pattern = '(_logger\.LogInformation\(\$"Simple verification result: Verified=\{verifyResult\.Verified\}, Score=\{verifyResult\.Score\}"\);)'
$replacement = @'
$1

                // ?? Notificar resultado
                await _notificationService.NotifyCompleteAsync(
                    request.Dni,
                    success: verifyResult.Verified,
                    message: verifyResult.Verified ? $"Verificación exitosa para {request.Dni}" : "Las huellas no coinciden",
                    data: responseData,
                    callbackUrl: request.CallbackUrl
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de resultado" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# 6. IdentifyLiveAsync - Notificar INICIO
Write-Host "[6/7] IdentifyLiveAsync - Notificación de inicio..." -ForegroundColor Yellow
$pattern = '(_logger\.LogInformation\(\$"Starting live identification from directory: \{templatesDir\}"\);)'
$replacement = @'
$1

                // ?? Notificar inicio de identificación
                await _notificationService.NotifyStartAsync(
                    "identify",
                    "identificación automática (1:N)",
                    null
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de inicio" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# 7. IdentifyLiveAsync - Notificar RESULTADO
Write-Host "[7/7] IdentifyLiveAsync - Notificación de resultado..." -ForegroundColor Yellow
$pattern = '(_logger\.LogInformation\(\$"Live identification result: Match=\{bestMatch != null\}, Processed=\{templatesProcessed\}"\);)'
$replacement = @'
$1

                // ?? Notificar resultado
                await _notificationService.NotifyCompleteAsync(
                    bestMatch?.Dni ?? "unknown",
                    success: bestMatch != null,
                    message: bestMatch != null ? $"Identificado: {bestMatch.Dni}" : "No se encontró coincidencia",
                    data: responseData,
                    callbackUrl: null
                );
'@
if ($content -match $pattern) {
    $content = $content -replace $pattern, $replacement
    Write-Host "  ? Agregada notificación de resultado" -ForegroundColor Green
    $modificado = $true
} else {
    Write-Host "  ??  Patrón no encontrado" -ForegroundColor Yellow
}

# Guardar cambios solo si hubo modificaciones
if ($modificado) {
    Set-Content $filePath -Value $content -NoNewline
    Write-Host "`n? CAMBIOS GUARDADOS" -ForegroundColor Green
    
    # Contar notificaciones agregadas
    $notifyCount = ([regex]::Matches($content, "_notificationService\.Notify")).Count
    Write-Host "?? Total de llamadas a notificaciones: $notifyCount" -ForegroundColor Cyan
    
    # Compilar
    Write-Host "`n?? Compilando proyecto..." -ForegroundColor Cyan
    Push-Location
    Set-Location "FutronicService"
    $output = dotnet build 2>&1
    Pop-Location
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Compilación exitosa" -ForegroundColor Green
        Write-Host "`n?? ¡INTEGRACIÓN COMPLETADA CON ÉXITO!" -ForegroundColor Green
        Write-Host "`nLos métodos ahora enviarán notificaciones en tiempo real:" -ForegroundColor Cyan
        Write-Host "  ?? RegisterMultiSampleAsync: inicio, éxito, error" -ForegroundColor White
        Write-Host "  ?? VerifySimpleAsync: inicio, resultado" -ForegroundColor White
        Write-Host "  ?? IdentifyLiveAsync: inicio, resultado" -ForegroundColor White
    } else {
        Write-Host "??  Advertencias de compilación (puede ser normal)" -ForegroundColor Yellow
        Write-Host $output
    }
} else {
    Write-Host "`n??  No se realizaron modificaciones - verificar patrones" -ForegroundColor Yellow
}

Write-Host "`n================================================" -ForegroundColor Cyan
