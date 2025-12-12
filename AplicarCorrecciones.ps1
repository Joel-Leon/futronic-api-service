# Script para aplicar todas las correcciones a FutronicFingerprintService.cs
$filePath = "FutronicService\Services\FutronicFingerprintService.cs"

Write-Host "=== APLICANDO CORRECCIONES ===" -ForegroundColor Cyan
Write-Host "Archivo: $filePath`n" -ForegroundColor Yellow

# Leer el archivo
$content = Get-Content $filePath -Raw

# 1. Eliminar la línea de using futronic_cli
Write-Host "[1/3] Eliminando referencia a futronic_cli..." -ForegroundColor Yellow
$content = $content -replace 'using ReflectionHelper = futronic_cli\.ReflectionHelper;[\r\n]+', ''

# 2. Corregir construcción de rutas en VerifySimpleAsync
Write-Host "[2/3] Corrigiendo construcción de rutas en VerifySimpleAsync..." -ForegroundColor Yellow
$oldPattern = @'
      // Determinar ruta del template: \{tempPath\}/\{dni\}/\{dedo\}/\{dni\}\.tml
         string templatePath = request\.StoredTemplatePath;
        if \(string\.IsNullOrEmpty\(templatePath\)\)
            \{
            string dedo = request\.Dedo \?\? "index";
       templatePath = Path\.Combine\(_tempPath, request\.Dni, dedo, \$"\{request\.Dni\}\.tml"\);
         \}

    if \(!File\.Exists\(templatePath\)\)
'@

$newPattern = @'
      // Determinar ruta del template
        string templatePath = request.StoredTemplatePath;
        string dedo = request.Dedo ?? "index";
        
        if (string.IsNullOrEmpty(templatePath))
            {
            // Usar ruta por defecto: {_tempPath}/{dni}/{dedo}/{dni}.tml
       templatePath = Path.Combine(_tempPath, request.Dni, dedo, $"{request.Dni}.tml");
         }
        else if (Directory.Exists(templatePath))
        {
            // Si es un directorio, construir la ruta completa: {path}/{dni}/{dedo}/{dni}.tml
            templatePath = Path.Combine(templatePath, request.Dni, dedo, $"{request.Dni}.tml");
            _logger.LogInformation($"Constructed full path from directory: {templatePath}");
        }
        // Si ya es un archivo, usar tal cual

    if (!File.Exists(templatePath))
'@

$content = $content -replace $oldPattern, $newPattern

# 3. Actualizar mensaje de error para mostrar la ruta buscada
Write-Host "[3/3] Mejorando mensajes de error..." -ForegroundColor Yellow
$content = $content -replace '   Console\.WriteLine\(\$"\? No existe huella registrada para DNI \{request\.Dni\}"\);[\r\n]+         return ApiResponse', @'
   Console.WriteLine($"? No existe huella registrada para DNI {request.Dni}");
   Console.WriteLine($"   Buscado en: {templatePath}");
         return ApiResponse
'@

# Cambiar "Template cargado" por "Template encontrado"
$content = $content -replace 'Console\.WriteLine\(\$"\?\? Template cargado: \{Path\.GetFileName\(templatePath\)\}"\);', 'Console.WriteLine($"? Template encontrado: {templatePath}");'

# Guardar los cambios
Set-Content $filePath -Value $content -NoNewline

Write-Host "`n? CORRECCIONES APLICADAS EXITOSAMENTE" -ForegroundColor Green
Write-Host "`nVerificando cambios..." -ForegroundColor Cyan

# Verificar que los cambios se aplicaron
$verification = Get-Content $filePath -Raw

$checks = @(
    @{ Name = "Eliminación de futronic_cli"; Pattern = "futronic_cli\.ReflectionHelper"; ShouldExist = $false },
    @{ Name = "Nueva construcción de rutas"; Pattern = "else if \(Directory\.Exists\(templatePath\)\)"; ShouldExist = $true },
    @{ Name = "Mensaje 'Buscado en'"; Pattern = 'Console\.WriteLine\(\$"   Buscado en:'; ShouldExist = $true }
)

$allPassed = $true
foreach ($check in $checks) {
    $found = $verification -match $check.Pattern
    $expected = $check.ShouldExist
    $passed = ($found -eq $expected)
    
    if ($passed) {
        Write-Host "  ? $($check.Name)" -ForegroundColor Green
    } else {
        Write-Host "  ? $($check.Name)" -ForegroundColor Red
        $allPassed = $false
    }
}

if ($allPassed) {
    Write-Host "`n?? Todas las correcciones verificadas correctamente" -ForegroundColor Green
} else {
    Write-Host "`n??  Algunas verificaciones fallaron - revisar manualmente" -ForegroundColor Yellow
}

Write-Host "`nCompilando proyecto..." -ForegroundColor Cyan
dotnet build ..\FutronicService.csproj
