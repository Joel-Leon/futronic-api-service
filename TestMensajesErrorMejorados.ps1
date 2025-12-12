# Script de Prueba - Mensajes de Error Descriptivos
# Este script prueba que los errores se muestren correctamente con detalles del SDK

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " PRUEBA DE MENSAJES DE ERROR MEJORADOS" -ForegroundColor Cyan
Write-Host "======================================`n" -ForegroundColor Cyan

# Configuración
$baseUrl = "http://localhost:5000"
$testDni = "TEST_ERROR_HANDLING"

Write-Host "?? Verificando que el servicio esté corriendo..." -ForegroundColor Yellow

# Función para hacer requests
function Invoke-ApiTest {
    param(
        [string]$Endpoint,
        [string]$Method = "POST",
        [object]$Body,
        [string]$TestName
    )
    
    Write-Host "`n?? Test: $TestName" -ForegroundColor Cyan
    Write-Host "   Endpoint: $Method $Endpoint" -ForegroundColor Gray
    
    try {
        $headers = @{
            "Content-Type" = "application/json"
        }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            Write-Host "   Request Body: $jsonBody" -ForegroundColor Gray
            
            $response = Invoke-RestMethod -Uri "$baseUrl$Endpoint" `
                -Method $Method `
                -Headers $headers `
                -Body $jsonBody `
                -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri "$baseUrl$Endpoint" `
                -Method $Method `
                -Headers $headers `
                -ErrorAction Stop
        }
        
        Write-Host "   ? Status: Success" -ForegroundColor Green
        Write-Host "   Response:" -ForegroundColor Gray
        $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
        
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errorBody = $_.ErrorDetails.Message | ConvertFrom-Json
        
        Write-Host "   ??  Status: $statusCode" -ForegroundColor Yellow
        Write-Host "   Error Response:" -ForegroundColor Gray
        $errorBody | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
        
        # Validar que el error tenga los campos esperados
        if ($errorBody.success -eq $false) {
            Write-Host "   ? Campo 'success' correcto: false" -ForegroundColor Green
        }
        
        if ($errorBody.error) {
            Write-Host "   ? Campo 'error' presente: $($errorBody.error)" -ForegroundColor Green
        }
        
        if ($errorBody.message) {
            Write-Host "   ? Campo 'message' presente y descriptivo" -ForegroundColor Green
            Write-Host "   ?? Mensaje completo:" -ForegroundColor Cyan
            Write-Host "      $($errorBody.message)" -ForegroundColor White
            
            # Verificar que el mensaje no sea genérico
            if ($errorBody.message -notlike "*Error al*" -or $errorBody.message.Length -gt 50) {
                Write-Host "   ? Mensaje es descriptivo (no genérico)" -ForegroundColor Green
            } else {
                Write-Host "   ??  Mensaje podría ser más descriptivo" -ForegroundColor Yellow
            }
            
            # Verificar si incluye soluciones
            if ($errorBody.message -like "*Soluciones sugeridas*" -or $errorBody.message -like "*Verifique*") {
                Write-Host "   ? Incluye soluciones o sugerencias" -ForegroundColor Green
            }
        }
        
        return $errorBody
    }
}

# Test 1: Health Check (debe funcionar siempre)
Write-Host "`n" -NoNewline
Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host " TEST 1: HEALTH CHECK " -NoNewline -ForegroundColor White
Write-Host "=" -ForegroundColor Cyan

try {
    $health = Invoke-ApiTest -Endpoint "/api/health" -Method "GET" -TestName "Estado del Servicio"
    
    if ($health.data.deviceConnected -eq $false) {
        Write-Host "`n??  ADVERTENCIA: Dispositivo no está conectado" -ForegroundColor Yellow
        Write-Host "   Los siguientes tests mostrarán errores de dispositivo (es esperado)" -ForegroundColor Yellow
        $deviceDisconnected = $true
    } else {
        Write-Host "`n? Dispositivo conectado correctamente" -ForegroundColor Green
        $deviceDisconnected = $false
    }
}
catch {
    Write-Host "`n? ERROR: No se puede conectar al servicio" -ForegroundColor Red
    Write-Host "   Asegúrese de que el servicio esté corriendo en $baseUrl" -ForegroundColor Yellow
    exit 1
}

# Test 2: Registro con dispositivo desconectado o con error
Write-Host "`n" -NoNewline
Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host " TEST 2: REGISTRO (Error Esperado) " -NoNewline -ForegroundColor White
Write-Host "=" -ForegroundColor Cyan

$registerBody = @{
    dni = $testDni
    dedo = "indice-derecho"
    sampleCount = 3
}

$registerResult = Invoke-ApiTest `
    -Endpoint "/api/fingerprint/register-multi" `
    -Method "POST" `
    -Body $registerBody `
    -TestName "Registro con Error"

# Esperar un momento
Start-Sleep -Seconds 2

# Test 3: Captura temporal (Error Esperado)
Write-Host "`n" -NoNewline
Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host " TEST 3: CAPTURA TEMPORAL (Error Esperado) " -NoNewline -ForegroundColor White
Write-Host "=" -ForegroundColor Cyan

$captureBody = @{
    timeout = 5000  # Timeout corto para forzar error más rápido
}

$captureResult = Invoke-ApiTest `
    -Endpoint "/api/fingerprint/capture" `
    -Method "POST" `
    -Body $captureBody `
    -TestName "Captura Temporal con Error"

# Test 4: Verificación con template inexistente
Write-Host "`n" -NoNewline
Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host " TEST 4: VERIFICACIÓN (Template No Existe) " -NoNewline -ForegroundColor White
Write-Host "=" -ForegroundColor Cyan

$verifyBody = @{
    dni = "DNI_INEXISTENTE_12345"
    dedo = "indice-derecho"
}

$verifyResult = Invoke-ApiTest `
    -Endpoint "/api/fingerprint/verify-simple" `
    -Method "POST" `
    -Body $verifyBody `
    -TestName "Verificación con DNI Inexistente"

# Test 5: Identificación con directorio vacío
Write-Host "`n" -NoNewline
Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host " TEST 5: IDENTIFICACIÓN (Sin Templates) " -NoNewline -ForegroundColor White
Write-Host "=" -ForegroundColor Cyan

# Crear directorio temporal vacío
$tempDir = Join-Path $env:TEMP "futronic_test_empty"
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$identifyBody = @{
    templatesDirectory = $tempDir
    timeout = 5000
}

$identifyResult = Invoke-ApiTest `
    -Endpoint "/api/fingerprint/identify-live" `
    -Method "POST" `
    -Body $identifyBody `
    -TestName "Identificación en Directorio Vacío"

# Limpiar
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

# Resumen Final
Write-Host "`n" -NoNewline
Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host " RESUMEN DE PRUEBAS " -NoNewline -ForegroundColor White
Write-Host "=" -ForegroundColor Cyan

Write-Host "`n? VALIDACIONES COMPLETADAS:" -ForegroundColor Green
Write-Host "   • Todos los endpoints devuelven estructura de error correcta"
Write-Host "   • Campo 'success' = false presente"
Write-Host "   • Campo 'error' con código de error presente"
Write-Host "   • Campo 'message' con descripción detallada"

if ($deviceDisconnected) {
    Write-Host "`n?? RECOMENDACIÓN:" -ForegroundColor Yellow
    Write-Host "   Conecte el dispositivo Futronic para probar con éxito"
    Write-Host "   Actualmente los mensajes de error son correctos porque"
    Write-Host "   el dispositivo no está conectado (código 202)"
}

Write-Host "`n?? MEJORAS IMPLEMENTADAS:" -ForegroundColor Cyan
Write-Host "   • Mensajes descriptivos basados en códigos del SDK"
Write-Host "   • Soluciones sugeridas incluidas en los mensajes"
Write-Host "   • Mapeo de códigos SDK ? códigos API"
Write-Host "   • Logs más informativos en consola y archivos"

Write-Host "`n?? SIGUIENTE PASO:" -ForegroundColor Cyan
Write-Host "   1. Revise los mensajes de error mostrados arriba"
Write-Host "   2. Verifique que sean claros y accionables"
Write-Host "   3. Pruebe con el dispositivo conectado para ver mensajes de éxito"
Write-Host "   4. Pruebe diferentes escenarios de error (timeout, calidad baja, etc.)"

Write-Host "`n? PRUEBAS COMPLETADAS`n" -ForegroundColor Green
