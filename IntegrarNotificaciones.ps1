# Script para integrar notificaciones en FutronicFingerprintService.cs

$filePath = "FutronicService\Services\FutronicFingerprintService.cs"
Write-Host "=== INTEGRANDO NOTIFICACIONES EN TIEMPO REAL ===" -ForegroundColor Cyan

$content = Get-Content $filePath -Raw

# ============================================
# 1. INTEGRAR EN RegisterMultiSampleAsync
# ============================================
Write-Host "[1/3] Integrando notificaciones en RegisterMultiSampleAsync..." -ForegroundColor Yellow

# Agregar notificación de inicio después de la validación
$pattern1 = '(Console\.WriteLine\(\$"\\n\{"=",\-60\}"\);\s+Console\.WriteLine\(\$"=== REGISTRO DE HUELLA ==="\);)'
$replacement1 = @'
$1

                // Notificar inicio de operación
                await _notificationService.NotifyStartAsync(
                    request.Dni,
                    "registro de huella",
                    request.CallbackUrl
                );
'@
$content = $content -replace $pattern1, $replacement1

# Agregar notificación en el evento OnPutOn (dentro de EnrollFingerprintInternal)
$pattern2 = '(enrollment\.OnPutOn \+= \(FTR_PROGRESS p\) =>\s+\{\s+currentSample\+\+;)'
$replacement2 = @'
$1
                
                // Notificar muestra iniciada
                _notificationService.NotifySampleCapturedAsync(
                    request?.Dni ?? "unknown",
                    currentSample,
                    maxModels,
                    0,
                    request?.CallbackUrl
                ).GetAwaiter().GetResult();
'@
# No aplicamos este porque EnrollFingerprintInternal no tiene acceso al request

# Agregar notificación al completar exitosamente
$pattern3 = '(_logger\.LogInformation\(\$"Multi-sample registration successful for DNI: \{request\.Dni\}"\);)'
$replacement3 = @'
$1

                // Notificar éxito
                await _notificationService.NotifyCompleteAsync(
                    request.Dni,
                    success: true,
                    message: $"Huella registrada exitosamente con {sampleCount} muestras",
                    data: responseData,
                    callbackUrl: request.CallbackUrl
                );
'@
$content = $content -replace $pattern3, $replacement3

# Agregar notificación en caso de error
$pattern4 = '(_logger\.LogError\(ex, \$"Error in RegisterMultiSampleAsync for DNI: \{request\.Dni\}"\);)'
$replacement4 = @'
$1

                // Notificar error
                await _notificationService.NotifyErrorAsync(
                    request.Dni,
                    error: ex.Message,
                    callbackUrl: request.CallbackUrl
                );
'@
$content = $content -replace $pattern4, $replacement4

# ============================================
# 2. INTEGRAR EN VerifySimpleAsync
# ============================================
Write-Host "[2/3] Integrando notificaciones en VerifySimpleAsync..." -ForegroundColor Yellow

# Notificación de inicio
$pattern5 = '(_logger\.LogInformation\(\$"Starting simple verification for DNI: \{request\.Dni\}"\);)'
$replacement5 = @'
$1

                // Notificar inicio de verificación
                await _notificationService.NotifyStartAsync(
                    request.Dni,
                    "verificación de identidad",
                    request.CallbackUrl
                );
'@
$content = $content -replace $pattern5, $replacement5

# Notificación al completar
$pattern6 = '(_logger\.LogInformation\(\$"Simple verification result: Verified=\{verifyResult\.Verified\}, Score=\{verifyResult\.Score\}"\);)'
$replacement6 = @'
$1

                // Notificar resultado
                await _notificationService.NotifyCompleteAsync(
                    request.Dni,
                    success: verifyResult.Verified,
                    message: verifyResult.Verified ? $"Verificación exitosa para {request.Dni}" : "Las huellas no coinciden",
                    data: responseData,
                    callbackUrl: request.CallbackUrl
                );
'@
$content = $content -replace $pattern6, $replacement6

# ============================================
# 3. INTEGRAR EN IdentifyLiveAsync
# ============================================
Write-Host "[3/3] Integrando notificaciones en IdentifyLiveAsync..." -ForegroundColor Yellow

# Notificación de inicio
$pattern7 = '(_logger\.LogInformation\(\$"Starting live identification from directory: \{templatesDir\}"\);)'
$replacement7 = @'
$1

                // Notificar inicio de identificación
                await _notificationService.NotifyStartAsync(
                    "identify",
                    "identificación automática (1:N)",
                    null
                );
'@
$content = $content -replace $pattern7, $replacement7

# Notificación de progreso durante búsqueda
$pattern8 = '(templatesProcessed\+\+;\s+Console\.Write)'
$replacement8 = @'
$1

                    // Notificar progreso cada 10 templates
                    if (templatesProcessed % 10 == 0)
                    {
                        _notificationService.NotifyAsync(
                            "search_progress",
                            $"Procesando: {templatesProcessed}/{Math.Min(templateFiles.Length, _maxTemplatesPerIdentify)} templates",
                            new { processed = templatesProcessed, total = Math.Min(templateFiles.Length, _maxTemplatesPerIdentify) }
                        ).GetAwaiter().GetResult();
                    }

                    Console.Write
'@
$content = $content -replace $pattern8, $replacement8

# Notificación al completar
$pattern9 = '(_logger\.LogInformation\(\$"Live identification result: Match=\{bestMatch != null\}, Processed=\{templatesProcessed\}"\);)'
$replacement9 = @'
$1

                // Notificar resultado
                await _notificationService.NotifyCompleteAsync(
                    bestMatch?.Dni ?? "unknown",
                    success: bestMatch != null,
                    message: bestMatch != null ? $"Identificado: {bestMatch.Dni}" : "No se encontró coincidencia",
                    data: responseData,
                    callbackUrl: null
                );
'@
$content = $content -replace $pattern9, $replacement9

# Guardar cambios
Set-Content $filePath -Value $content -NoNewline

Write-Host "`n? INTEGRACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "`nVerificando cambios..." -ForegroundColor Cyan

# Verificar que se aplicaron los cambios
$verification = Get-Content $filePath -Raw
$notifyCount = ([regex]::Matches($verification, "NotifyStartAsync|NotifyCompleteAsync|NotifyErrorAsync|NotifySampleCapturedAsync")).Count

Write-Host "  ? Métodos de notificación agregados: $notifyCount" -ForegroundColor Green

Write-Host "`nCompilando proyecto..." -ForegroundColor Cyan
cd ..
dotnet build FutronicService.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n?? ¡Integración exitosa y proyecto compilado!" -ForegroundColor Green
    Write-Host "`nAhora los clientes recibirán notificaciones en tiempo real:" -ForegroundColor Cyan
    Write-Host "  ?? Durante registro de huellas (inicio, progreso, completado)" -ForegroundColor White
    Write-Host "  ?? Durante verificación (inicio, resultado)" -ForegroundColor White
    Write-Host "  ?? Durante identificación (inicio, progreso, resultado)" -ForegroundColor White
} else {
    Write-Host "`n??  Hay errores de compilación - revisar código" -ForegroundColor Yellow
}
