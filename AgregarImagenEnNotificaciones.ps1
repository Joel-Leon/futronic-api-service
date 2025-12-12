# Script para agregar imagen en notificaciones de muestra

$filePath = "FutronicService\Services\FutronicFingerprintService.cs"
Write-Host "=== AGREGANDO IMAGEN EN NOTIFICACIONES ===" -ForegroundColor Cyan

$content = Get-Content $filePath -Raw

# ============================================
# Modificar la notificación sample_captured para incluir la imagen
# ============================================
Write-Host "[1/1] Agregando imagen Base64 en notificación de muestra capturada..." -ForegroundColor Yellow

$oldNotification = @'
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

$newNotification = @'
                    // ?? Notificar muestra capturada con calidad e imagen
                    if (!string.IsNullOrEmpty(dni) && capturedImages.Count > 0)
                    {
                        var lastImage = capturedImages.LastOrDefault();
                        
                        // Convertir imagen a Base64 para enviarla en la notificación
                        string imageBase64 = null;
                        if (lastImage != null && lastImage.ImageData != null && lastImage.ImageData.Length > 0)
                        {
                            imageBase64 = Convert.ToBase64String(lastImage.ImageData);
                        }
                        
                        // Crear datos extendidos con la imagen
                        var sampleData = new
                        {
                            currentSample,
                            totalSamples = maxModels,
                            quality = lastImage?.Quality ?? 0,
                            progress = (currentSample * 100) / maxModels,
                            imageBase64 = imageBase64,
                            imageFormat = "bmp"
                        };
                        
                        _notificationService.NotifyAsync(
                            "sample_captured",
                            $"Muestra {currentSample}/{maxModels} capturada",
                            sampleData,
                            dni,
                            callbackUrl
                        ).GetAwaiter().GetResult();
                    }
'@

if ($content -match [regex]::Escape($oldNotification)) {
    $content = $content -replace [regex]::Escape($oldNotification), $newNotification
    Write-Host "  ? Notificación modificada para incluir imagen" -ForegroundColor Green
} else {
    Write-Host "  ??  Patrón no encontrado - intentando búsqueda alternativa" -ForegroundColor Yellow
    
    # Patrón alternativo más flexible
    $pattern = '// \?\? Notificar muestra capturada con calidad[\s\S]*?\.GetAwaiter\(\)\.GetResult\(\);'
    if ($content -match $pattern) {
        $content = $content -replace $pattern, $newNotification.Trim()
        Write-Host "  ? Notificación modificada (patrón alternativo)" -ForegroundColor Green
    }
}

# Guardar cambios
Set-Content $filePath -Value $content -NoNewline

Write-Host "`n? CAMBIOS APLICADOS" -ForegroundColor Green

# Compilar
Write-Host "`n?? Compilando..." -ForegroundColor Cyan
Push-Location
Set-Location "FutronicService"
$buildOutput = dotnet build 2>&1
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Compilación exitosa" -ForegroundColor Green
    Write-Host "`n?? ¡IMÁGENES AGREGADAS A LAS NOTIFICACIONES!" -ForegroundColor Green
    Write-Host "`nAhora cada evento 'sample_captured' incluirá:" -ForegroundColor Cyan
    Write-Host "  ?? quality - Calidad de la muestra" -ForegroundColor White
    Write-Host "  ?? progress - Porcentaje de progreso" -ForegroundColor White
    Write-Host "  ???  imageBase64 - Imagen en Base64 (BMP)" -ForegroundColor White
    Write-Host "  ?? imageFormat - Formato 'bmp'" -ForegroundColor White
} else {
    Write-Host "??  Error en compilación:" -ForegroundColor Red
    Write-Host $buildOutput
}
