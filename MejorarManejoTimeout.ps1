# Script para mejorar manejo de timeouts y errores 08

$filePath = "FutronicService\Services\FutronicFingerprintService.cs"
Write-Host "=== MEJORANDO MANEJO DE TIMEOUTS Y ERROR 08 ===" -ForegroundColor Cyan

$content = Get-Content $filePath -Raw

# ============================================
# 1. Aumentar MIOTOff en CaptureFingerprintInternal
# ============================================
Write-Host "[1/4] Aumentando MIOTOff en CaptureFingerprintInternal..." -ForegroundColor Yellow

$oldMiotCapture = 'ReflectionHelper\.TrySetProperty\(identification, "MIOTOff", 3000\);'
$newMiotCapture = 'ReflectionHelper.TrySetProperty(identification, "MIOTOff", 5000); // Aumentado para evitar timeout prematuro'

if ($content -match $oldMiotCapture) {
    $content = $content -replace $oldMiotCapture, $newMiotCapture
    Write-Host "  ? MIOTOff aumentado a 5000ms en capture" -ForegroundColor Green
}

# ============================================
# 2. Aumentar MIOTOff en EnrollFingerprintInternal
# ============================================
Write-Host "[2/4] Aumentando MIOTOff en EnrollFingerprintInternal..." -ForegroundColor Yellow

$oldMiotEnroll = 'ReflectionHelper\.TrySetProperty\(enrollment, "MIOTOff", 2000\);'
$newMiotEnroll = 'ReflectionHelper.TrySetProperty(enrollment, "MIOTOff", 4000); // Aumentado para dar más tiempo entre muestras'

if ($content -match $oldMiotEnroll) {
    $content = $content -replace $oldMiotEnroll, $newMiotEnroll
    Write-Host "  ? MIOTOff aumentado a 4000ms en enrollment" -ForegroundColor Green
}

# ============================================
# 3. Agregar mensaje de instrucción antes de captura
# ============================================
Write-Host "[3/4] Mejorando mensajes de instrucción..." -ForegroundColor Yellow

$oldCaptureMsg = '(_logger\.LogInformation\("?? Iniciando captura de huella\.\.\."\);[\s\S]*?Console\.WriteLine\("?? Apoye el dedo sobre el sensor cuando se indique"\);)'
$newCaptureMsg = @'
$1
                
                // Notificación de instrucción para reducir timeouts
                Console.WriteLine("\n? IMPORTANTE:");
                Console.WriteLine("   • Asegúrese de que el sensor esté limpio");
                Console.WriteLine("   • Apoye el dedo con presión firme y constante");
                Console.WriteLine("   • Mantenga el dedo quieto hasta que se indique\n");
'@

if ($content -match $oldCaptureMsg) {
    $content = $content -replace $oldCaptureMsg, $newCaptureMsg
    Write-Host "  ? Mensajes de instrucción mejorados" -ForegroundColor Green
}

# ============================================
# 4. Agregar retry automático en caso de error 08
# ============================================
Write-Host "[4/4] Agregando retry automático para error 08..." -ForegroundColor Yellow

# Modificar CaptureFingerprintInternal para agregar retry
$oldCaptureMethod = 'private CaptureResult CaptureFingerprintInternal\(int timeout\)'
$newCaptureMethod = @'
private CaptureResult CaptureFingerprintInternal(int timeout, int retries = 2)
'@

$content = $content -replace $oldCaptureMethod, $newCaptureMethod

# Agregar lógica de retry al final del método
$oldCaptureEnd = '(return captureResult\.Success \? captureResult : null;[\s\S]*?catch \(Exception ex\))'
$newCaptureEnd = @'
// Retry automático en caso de timeout (error 08)
                if (!captureResult.Success && captureResult.ErrorCode == 8 && retries > 0)
                {
                    _logger.LogWarning($"Timeout detectado (error 08), reintentando... ({retries} intentos restantes)");
                    Console.WriteLine($"??  Timeout - Reintentando captura ({retries} intentos restantes)...");
                    Thread.Sleep(1000); // Pequeña pausa antes de reintentar
                    return CaptureFingerprintInternal(timeout, retries - 1);
                }
                
                return captureResult.Success ? captureResult : null;
    }
      }
      catch (Exception ex)
'@

$content = $content -replace $oldCaptureEnd, $newCaptureEnd

Write-Host "  ? Retry automático agregado" -ForegroundColor Green

# Guardar cambios
Set-Content $filePath -Value $content -NoNewline

Write-Host "`n? MEJORAS APLICADAS" -ForegroundColor Green

# Compilar
Write-Host "`n?? Compilando..." -ForegroundColor Cyan
Push-Location
Set-Location "FutronicService"
$buildOutput = dotnet build 2>&1
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Compilación exitosa" -ForegroundColor Green
    Write-Host "`n?? MEJORAS PARA ERROR 08 IMPLEMENTADAS:" -ForegroundColor Green
    Write-Host "  ? MIOTOff aumentado (3s ? 5s en capture, 2s ? 4s en enrollment)" -ForegroundColor White
    Write-Host "  ? Mensajes de instrucción mejorados" -ForegroundColor White
    Write-Host "  ? Retry automático (hasta 2 reintentos)" -ForegroundColor White
    Write-Host "  ? Pausa de 1s entre reintentos" -ForegroundColor White
    Write-Host "`n?? Recomendaciones adicionales:" -ForegroundColor Cyan
    Write-Host "  • Limpiar el sensor regularmente" -ForegroundColor White
    Write-Host "  • Verificar que el dispositivo USB esté bien conectado" -ForegroundColor White
    Write-Host "  • Instruir al usuario sobre presión adecuada" -ForegroundColor White
} else {
    Write-Host "??  Error en compilación:" -ForegroundColor Red
    Write-Host $buildOutput
}
