# Script para agregar notificaciones muestra por muestra

$filePath = "FutronicService\Services\FutronicFingerprintService.cs"
Write-Host "=== AGREGANDO NOTIFICACIONES EN TIEMPO REAL POR MUESTRA ===" -ForegroundColor Cyan

$content = Get-Content $filePath -Raw

# ============================================
# 1. Modificar firma de EnrollFingerprintInternal para recibir dni y callbackUrl
# ============================================
Write-Host "[1/4] Modificando firma de EnrollFingerprintInternal..." -ForegroundColor Yellow

$oldSignature = 'private EnrollResult EnrollFingerprintInternal\(int maxModels, int timeout\)'
$newSignature = 'private EnrollResult EnrollFingerprintInternal(int maxModels, int timeout, string dni = null, string callbackUrl = null)'

$content = $content -replace $oldSignature, $newSignature
Write-Host "  ? Firma modificada" -ForegroundColor Green

# ============================================
# 2. Agregar notificación en OnPutOn (cuando empieza una muestra)
# ============================================
Write-Host "[2/4] Agregando notificación en OnPutOn..." -ForegroundColor Yellow

$onPutOnPattern = '(enrollment\.OnPutOn \+= \(FTR_PROGRESS p\) =>\s+\{\s+currentSample\+\+;[\s\S]*?Console\.WriteLine\("  \?\? Consejo: Mantenga presión constante para mejor calidad"\);)'
$onPutOnReplacement = @'
$1
                
                // ?? Notificar que la muestra está en proceso
                if (!string.IsNullOrEmpty(dni))
                {
                    _notificationService.NotifyAsync(
                        "sample_started",
                        $"Capturando muestra {currentSample}/{maxModels}",
                        new { currentSample, totalSamples = maxModels, progress = (currentSample * 100) / maxModels },
                        dni,
                        callbackUrl
                    ).GetAwaiter().GetResult();
                }
'@

$content = $content -replace $onPutOnPattern, $onPutOnReplacement
Write-Host "  ? Notificación en OnPutOn agregada" -ForegroundColor Green

# ============================================
# 3. Agregar notificación en OnTakeOff (cuando se captura una muestra)
# ============================================
Write-Host "[3/4] Agregando notificación en OnTakeOff..." -ForegroundColor Yellow

$onTakeOffPattern = '(_logger\.LogInformation\(\$"\? Muestra \{currentSample\} capturada"\);[\s\S]*?Console\.WriteLine\("  \?\? Para la siguiente: varíe ligeramente rotación y presión"\);)'
$onTakeOffReplacement = @'
$1
                    
                    // ?? Notificar muestra capturada con calidad
                    if (!string.IsNullOrEmpty(dni) && capturedImages.Count > 0)
                    {
                        var lastImage = capturedImages.LastOrDefault();
                        _notificationService.NotifySampleCapturedAsync(
                            dni,
                            currentSample,
                            maxModels,
                            lastImage?.Quality ?? 0,
                            callbackUrl
                        ).GetAwaiter().GetResult();
                    }
'@

$content = $content -replace $onTakeOffPattern, $onTakeOffReplacement
Write-Host "  ? Notificación en OnTakeOff agregada" -ForegroundColor Green

# ============================================
# 4. Actualizar llamadas a EnrollFingerprintInternal
# ============================================
Write-Host "[4/4] Actualizando llamadas a EnrollFingerprintInternal..." -ForegroundColor Yellow

# En RegisterMultiSampleAsync
$registerCallPattern = 'var enrollResult = EnrollFingerprintInternal\(sampleCount, request\.Timeout \?\? _timeout\);'
$registerCallReplacement = 'var enrollResult = EnrollFingerprintInternal(sampleCount, request.Timeout ?? _timeout, request.Dni, request.CallbackUrl);'
$content = $content -replace $registerCallPattern, $registerCallReplacement

# En CaptureAsync (sin dni ni callback)
$captureCallPattern = 'var enrollResult = EnrollFingerprintInternal\(1, request\.Timeout\);'
$captureCallReplacement = 'var enrollResult = EnrollFingerprintInternal(1, request.Timeout, null, null);'
$content = $content -replace $captureCallPattern, $captureCallReplacement

Write-Host "  ? Llamadas actualizadas" -ForegroundColor Green

# Guardar cambios
Set-Content $filePath -Value $content -NoNewline

Write-Host "`n? CAMBIOS APLICADOS" -ForegroundColor Green

# Verificar
$verification = Get-Content $filePath -Raw
$notifyCount = ([regex]::Matches($verification, "NotifyAsync|NotifySampleCapturedAsync")).Count
Write-Host "?? Total de llamadas a notificaciones: $notifyCount" -ForegroundColor Cyan

# Compilar
Write-Host "`n?? Compilando..." -ForegroundColor Cyan
Push-Location
Set-Location "FutronicService"
$buildOutput = dotnet build 2>&1
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Compilación exitosa" -ForegroundColor Green
    Write-Host "`n?? ¡NOTIFICACIONES POR MUESTRA IMPLEMENTADAS!" -ForegroundColor Green
    Write-Host "`nAhora recibirás notificaciones:" -ForegroundColor Cyan
    Write-Host "  ?? operation_started - Al iniciar" -ForegroundColor White
    Write-Host "  ?? sample_started - Al empezar cada muestra (1/5, 2/5, etc)" -ForegroundColor White
    Write-Host "  ?? sample_captured - Al capturar cada muestra con su calidad" -ForegroundColor White
    Write-Host "  ?? operation_completed - Al terminar exitosamente" -ForegroundColor White
    Write-Host "  ?? error - En caso de error" -ForegroundColor White
} else {
    Write-Host "??  Error en compilación:" -ForegroundColor Red
    Write-Host $buildOutput
}
