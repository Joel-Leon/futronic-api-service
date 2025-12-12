# Script de Prueba - Respuestas con y sin Imágenes Base64
# Demuestra la diferencia en el tamaño de respuesta y los datos incluidos

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " PRUEBA DE RESPUESTAS CON IMÁGENES" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"
$testDni = "TEST_IMAGES_" + (Get-Random -Maximum 9999)

Write-Host "?? Verificando servicio..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method GET
    if ($health.data.deviceConnected) {
        Write-Host "? Dispositivo conectado" -ForegroundColor Green
    } else {
        Write-Host "??  Dispositivo NO conectado - Los tests mostrarán errores (esperado)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? ERROR: Servicio no disponible en $baseUrl" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " TEST 1: REGISTRO SIN IMÁGENES (DEFAULT)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n?? Enviando request SIN incluir imágenes..." -ForegroundColor Yellow

$requestWithoutImages = @{
    dni = $testDni
    dedo = "indice-derecho"
    sampleCount = 3
    includeImages = $false  # Explícitamente false
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Gray
Write-Host $requestWithoutImages -ForegroundColor White

try {
    $startTime = Get-Date
    $response1 = Invoke-RestMethod -Uri "$baseUrl/api/fingerprint/register-multi" `
        -Method POST `
        -Headers @{"Content-Type" = "application/json"} `
        -Body $requestWithoutImages `
        -ErrorAction Stop
    $endTime = Get-Date
    $duration1 = ($endTime - $startTime).TotalMilliseconds
    
    Write-Host "`n? Respuesta recibida" -ForegroundColor Green
    Write-Host "??  Tiempo: $($duration1)ms" -ForegroundColor Cyan
    
    $jsonResponse1 = $response1 | ConvertTo-Json -Depth 10
    $size1 = [System.Text.Encoding]::UTF8.GetByteCount($jsonResponse1)
    
    Write-Host "?? Tamaño de respuesta: $($size1 / 1024) KB" -ForegroundColor Cyan
    
    Write-Host "`n?? Estructura de la respuesta:" -ForegroundColor Yellow
    Write-Host "   • success: $($response1.success)" -ForegroundColor White
    Write-Host "   • message: $($response1.message)" -ForegroundColor White
    
    if ($response1.data) {
        Write-Host "`n?? Datos incluidos:" -ForegroundColor Yellow
        Write-Host "   • dni: $($response1.data.dni)" -ForegroundColor White
        Write-Host "   • dedo: $($response1.data.dedo)" -ForegroundColor White
        Write-Host "   • templatePath: $($response1.data.templatePath)" -ForegroundColor White
        Write-Host "   • imagePath: $($response1.data.imagePath)" -ForegroundColor White
        Write-Host "   • imagePaths: $($response1.data.imagePaths.Count) rutas" -ForegroundColor White
        Write-Host "   • metadataPath: $($response1.data.metadataPath)" -ForegroundColor White
        Write-Host "   • samplesCollected: $($response1.data.samplesCollected)" -ForegroundColor White
        Write-Host "   • averageQuality: $($response1.data.averageQuality)" -ForegroundColor White
        Write-Host "   • sampleQualities: $($response1.data.sampleQualities -join ', ')" -ForegroundColor White
        
        if ($response1.data.images) {
            Write-Host "   • images: $($response1.data.images.Count) imágenes en Base64" -ForegroundColor Green
        } else {
            Write-Host "   • images: NO INCLUIDAS (como se esperaba)" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "`n??  Error (esperado si dispositivo no conectado):" -ForegroundColor Yellow
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "   Error: $($errorResponse.error)" -ForegroundColor White
    Write-Host "   Mensaje: $($errorResponse.message)" -ForegroundColor White
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " TEST 2: REGISTRO CON IMÁGENES BASE64" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n?? Enviando request CON imágenes en Base64..." -ForegroundColor Yellow

$requestWithImages = @{
    dni = $testDni + "_IMG"
    dedo = "indice-derecho"
    sampleCount = 3
    includeImages = $true  # Solicitar imágenes
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Gray
Write-Host $requestWithImages -ForegroundColor White

try {
    $startTime = Get-Date
    $response2 = Invoke-RestMethod -Uri "$baseUrl/api/fingerprint/register-multi" `
        -Method POST `
        -Headers @{"Content-Type" = "application/json"} `
        -Body $requestWithImages `
        -ErrorAction Stop
    $endTime = Get-Date
    $duration2 = ($endTime - $startTime).TotalMilliseconds
    
    Write-Host "`n? Respuesta recibida" -ForegroundColor Green
    Write-Host "??  Tiempo: $($duration2)ms" -ForegroundColor Cyan
    
    $jsonResponse2 = $response2 | ConvertTo-Json -Depth 10
    $size2 = [System.Text.Encoding]::UTF8.GetByteCount($jsonResponse2)
    
    Write-Host "?? Tamaño de respuesta: $([Math]::Round($size2 / 1024, 2)) KB" -ForegroundColor Cyan
    
    Write-Host "`n?? Estructura de la respuesta:" -ForegroundColor Yellow
    Write-Host "   • success: $($response2.success)" -ForegroundColor White
    Write-Host "   • message: $($response2.message)" -ForegroundColor White
    
    if ($response2.data) {
        Write-Host "`n?? Datos incluidos:" -ForegroundColor Yellow
        Write-Host "   • dni: $($response2.data.dni)" -ForegroundColor White
        Write-Host "   • dedo: $($response2.data.dedo)" -ForegroundColor White
        Write-Host "   • templatePath: $($response2.data.templatePath)" -ForegroundColor White
        Write-Host "   • imagePath: $($response2.data.imagePath)" -ForegroundColor White
        Write-Host "   • imagePaths: $($response2.data.imagePaths.Count) rutas" -ForegroundColor White
        Write-Host "   • metadataPath: $($response2.data.metadataPath)" -ForegroundColor White
        Write-Host "   • samplesCollected: $($response2.data.samplesCollected)" -ForegroundColor White
        Write-Host "   • averageQuality: $($response2.data.averageQuality)" -ForegroundColor White
        
        if ($response2.data.images) {
            Write-Host "   • images: ? $($response2.data.images.Count) imágenes en Base64" -ForegroundColor Green
            
            Write-Host "`n???  Detalles de las imágenes:" -ForegroundColor Yellow
            foreach ($img in $response2.data.images) {
                $imgSize = [Math]::Round(($img.imageBase64.Length * 3/4) / 1024, 2)
                Write-Host "      Muestra $($img.sampleNumber):" -ForegroundColor White
                Write-Host "         • Calidad: $($img.quality)" -ForegroundColor White
                Write-Host "         • Formato: $($img.format)" -ForegroundColor White
                Write-Host "         • Tamaño Base64: ~$imgSize KB" -ForegroundColor White
                Write-Host "         • FilePath: $($img.filePath)" -ForegroundColor White
                Write-Host "         • Base64 Preview: $($img.imageBase64.Substring(0, [Math]::Min(50, $img.imageBase64.Length)))..." -ForegroundColor Gray
            }
        } else {
            Write-Host "   • images: ? NO INCLUIDAS" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "`n??  Error (esperado si dispositivo no conectado):" -ForegroundColor Yellow
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "   Error: $($errorResponse.error)" -ForegroundColor White
    Write-Host "   Mensaje: $($errorResponse.message)" -ForegroundColor White
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " COMPARACIÓN DE RESPUESTAS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($response1 -and $response2) {
    Write-Host "`n?? Estadísticas:" -ForegroundColor Yellow
    Write-Host "   Sin imágenes:" -ForegroundColor White
    Write-Host "      • Tamaño: $([Math]::Round($size1 / 1024, 2)) KB" -ForegroundColor White
    Write-Host "      • Tiempo: $([Math]::Round($duration1, 0))ms" -ForegroundColor White
    
    Write-Host "`n   Con imágenes:" -ForegroundColor White
    Write-Host "      • Tamaño: $([Math]::Round($size2 / 1024, 2)) KB" -ForegroundColor White
    Write-Host "      • Tiempo: $([Math]::Round($duration2, 0))ms" -ForegroundColor White
    
    $sizeDiff = [Math]::Round(($size2 - $size1) / 1024, 2)
    $sizePercent = [Math]::Round((($size2 - $size1) / $size1) * 100, 0)
    
    Write-Host "`n   ?? Diferencia:" -ForegroundColor Yellow
    Write-Host "      • Tamaño: +$sizeDiff KB (+$sizePercent%)" -ForegroundColor Cyan
    Write-Host "      • Tiempo: +$([Math]::Round($duration2 - $duration1, 0))ms" -ForegroundColor Cyan
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " RECOMENDACIONES DE USO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n? Usar includeImages=false (default) cuando:" -ForegroundColor Green
Write-Host "   • Solo necesitas las rutas de las imágenes"
Write-Host "   • Quieres respuestas más rápidas y ligeras"
Write-Host "   • Las imágenes se accederán desde el sistema de archivos"
Write-Host "   • Estás en una red con ancho de banda limitado"

Write-Host "`n? Usar includeImages=true cuando:" -ForegroundColor Green
Write-Host "   • Necesitas las imágenes inmediatamente en el cliente"
Write-Host "   • Quieres guardarlas en una base de datos"
Write-Host "   • El servidor no tiene acceso directo al sistema de archivos"
Write-Host "   • Implementas un sistema distribuido/cloud"

Write-Host "`n?? Ejemplo de uso con imágenes:" -ForegroundColor Cyan
Write-Host @"
{
  "dni": "12345678",
  "dedo": "indice-derecho",
  "sampleCount": 5,
  "includeImages": true  // <-- Incluir este campo
}
"@ -ForegroundColor White

Write-Host "`n? PRUEBAS COMPLETADAS`n" -ForegroundColor Green
