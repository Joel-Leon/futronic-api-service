# Script para aumentar timeout de enrollment y mejorar manejo

$filePath = "FutronicService\Services\FutronicFingerprintService.cs"
Write-Host "=== MEJORANDO TIMEOUT DE ENROLLMENT ===" -ForegroundColor Cyan

$content = Get-Content $filePath -Raw

# ============================================
# 1. Calcular timeout dinámico basado en número de muestras
# ============================================
Write-Host "[1/3] Agregando cálculo dinámico de timeout..." -ForegroundColor Yellow

$oldEnrollStart = '(var enrollResult = new EnrollResult\(\);[\s\S]*?var done = new ManualResetEvent\(false\);)'
$newEnrollStart = @'
$1
                
                // Calcular timeout dinámico: dar suficiente tiempo por muestra
                // Fórmula: (timeout_por_muestra * num_muestras) + buffer
                int dynamicTimeout = Math.Max(timeout, (maxModels * 15000) + 10000); // 15s por muestra + 10s buffer
                _logger.LogInformation($"Timeout dinámico calculado: {dynamicTimeout}ms para {maxModels} muestras");
                Console.WriteLine($"?? Tiempo máximo: {dynamicTimeout / 1000} segundos para completar {maxModels} muestras\n");
'@

if ($content -match $oldEnrollStart) {
    $content = $content -replace $oldEnrollStart, $newEnrollStart
    Write-Host "  ? Timeout dinámico agregado" -ForegroundColor Green
}

# ============================================
# 2. Usar timeout dinámico en lugar del fijo
# ============================================
Write-Host "[2/3] Aplicando timeout dinámico..." -ForegroundColor Yellow

$oldWaitOne = 'if \(!done\.WaitOne\(timeout\)\)'
$newWaitOne = 'if (!done.WaitOne(dynamicTimeout))'

$content = $content -replace $oldWaitOne, $newWaitOne
Write-Host "  ? Timeout dinámico aplicado" -ForegroundColor Green

# ============================================
# 3. Agregar notificación cuando falta poco tiempo
# ============================================
Write-Host "[3/3] Agregando mensaje de urgencia..." -ForegroundColor Yellow

$oldOnPutOn = '(enrollment\.OnPutOn \+= \(FTR_PROGRESS p\) =>\s+\{\s+currentSample\+\+;)'
$newOnPutOn = @'
$1
                
                // Mostrar tiempo restante estimado
                int timeElapsed = (int)(DateTime.Now - enrollResult.StartTime).TotalMilliseconds;
                int timeRemaining = dynamicTimeout - timeElapsed;
                if (timeRemaining < 30000 && timeRemaining > 0) // Menos de 30 segundos
                {
                    Console.WriteLine($"?? Tiempo restante: ~{timeRemaining / 1000} segundos");
                }
'@

# No aplicamos esto porque enrollResult no tiene StartTime, necesitamos agregar campo

# ============================================
# Agregar StartTime a EnrollResult
# ============================================
Write-Host "[Extra] Agregando timestamp a EnrollResult..." -ForegroundColor Yellow

$oldEnrollResult = '(private class EnrollResult\s+\{[\s\S]*?public List<CapturedImage> CapturedImages \{ get; set; \} = new List<CapturedImage>\(\);)'
$newEnrollResult = @'
$1
        public DateTime StartTime { get; set; } = DateTime.Now;
'@

if ($content -match $oldEnrollResult) {
    $content = $content -replace $oldEnrollResult, $newEnrollResult
    Write-Host "  ? StartTime agregado a EnrollResult" -ForegroundColor Green
}

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
    Write-Host "`n?? TIMEOUT DE ENROLLMENT MEJORADO:" -ForegroundColor Green
    Write-Host "  ? Timeout dinámico por número de muestras" -ForegroundColor White
    Write-Host "  ? Fórmula: (muestras × 15s) + 10s buffer" -ForegroundColor White
    Write-Host "  ? Ejemplo para 5 muestras: 85 segundos (vs 30s anterior)" -ForegroundColor White
    Write-Host "`n?? Nuevos Timeouts:" -ForegroundColor Cyan
    Write-Host "  • 1 muestra: 25 segundos" -ForegroundColor White
    Write-Host "  • 3 muestras: 55 segundos" -ForegroundColor White
    Write-Host "  • 5 muestras: 85 segundos" -ForegroundColor White
    Write-Host "  • 10 muestras: 160 segundos" -ForegroundColor White
} else {
    Write-Host "??  Error en compilación:" -ForegroundColor Red
    Write-Host $buildOutput
}
