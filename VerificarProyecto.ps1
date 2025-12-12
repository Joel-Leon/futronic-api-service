# Script de limpieza y verificación completa del proyecto

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LIMPIEZA Y VERIFICACIÓN DEL PROYECTO" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$rootPath = "C:\apps\futronic-api"
cd $rootPath

# ============================================
# 1. ARCHIVOS INNECESARIOS EN LA RAÍZ
# ============================================
Write-Host "[1/6] Identificando archivos innecesarios..." -ForegroundColor Yellow

$archivosInnecesarios = @(
    "App.config",                                    # Ya no se usa en .NET 8
    "BindingRedirects.xml",                          # Generado, no necesario
    "futronic-cli.sln.old",                          # Backup antiguo
    "futronic-cli.sln",                              # Solución antigua
    "ACTUALIZAR_FutronicFingerprintService_METODOS.txt",
    "AplicarSolucion.ps1",                           # Script viejo
    "CopyNativeDLLs.ps1",                            # Script viejo
    "ENDPOINT_CAPTURE_AGREGADO.md",                  # Documentación temporal
    "EVENTOS_PROGRESO_TIEMPO_REAL.md",               # Documentación temporal
    "MEJORA_MAXROTATION.md",                         # Documentación temporal
    "RECONSTRUCCION_COMPLETADA.md",                  # Documentación temporal
    "RECUPERAR_SERVICIO_URGENTE.md",                 # Documentación temporal
    "RESUMEN_FINAL.md",                              # Documentación temporal
    "RESUMEN_IMPLEMENTACION.md",                     # Documentación temporal
    "SOLUCION_FINAL_README.md"                       # Documentación temporal
)

$archivosEncontrados = @()
foreach ($archivo in $archivosInnecesarios) {
    if (Test-Path $archivo) {
        $archivosEncontrados += $archivo
        Write-Host "  ? $archivo" -ForegroundColor Red
    }
}

if ($archivosEncontrados.Count -eq 0) {
    Write-Host "  ? No se encontraron archivos innecesarios" -ForegroundColor Green
}

# ============================================
# 2. VERIFICAR IMPORTS Y REFERENCIAS
# ============================================
Write-Host "`n[2/6] Verificando imports en FutronicFingerprintService.cs..." -ForegroundColor Yellow

$serviceFile = "FutronicService\Services\FutronicFingerprintService.cs"
$content = Get-Content $serviceFile -Raw

# Verificar que no haya referencias a futronic-cli
if ($content -match "futronic_cli|futronic-cli") {
    Write-Host "  ? Se encontraron referencias a futronic-cli" -ForegroundColor Red
    $content | Select-String -Pattern "futronic" -AllMatches | ForEach-Object {
        Write-Host "    Línea: $($_.LineNumber)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ? No hay referencias a futronic-cli" -ForegroundColor Green
}

# Verificar que ReflectionHelper se usa correctamente
if ($content -match "ReflectionHelper\.TrySetProperty") {
    Write-Host "  ? ReflectionHelper se está usando" -ForegroundColor Green
} else {
    Write-Host "  ??  ReflectionHelper no parece estar en uso" -ForegroundColor Yellow
}

# ============================================
# 3. VERIFICAR ARCHIVOS UTILS
# ============================================
Write-Host "`n[3/6] Verificando archivos Utils..." -ForegroundColor Yellow

$utilsFiles = @{
    "FutronicService\Utils\ReflectionHelper.cs" = "ReflectionHelper\.TrySetProperty"
    "FutronicService\Utils\ImageUtils.cs" = "ImageUtils\.(ConvertBitmapToBytes|CalculateImageQuality|SelectBestImages)"
    "FutronicService\Utils\TemplateUtils.cs" = "TemplateUtils\.(ConvertToDemo|ExtractFromDemo)"
    "FutronicService\Utils\CapturedImage.cs" = "class CapturedImage"
    "FutronicService\Utils\ErrorCodes.cs" = "ErrorCodes|error codes"
    "FutronicService\Utils\FileHelper.cs" = "FileHelper"
}

$utilsNoUsados = @()
foreach ($util in $utilsFiles.Keys) {
    $pattern = $utilsFiles[$util]
    
    # Buscar uso en todo el proyecto (excepto en el mismo archivo)
    $usado = $false
    Get-ChildItem "FutronicService" -Recurse -Filter "*.cs" | ForEach-Object {
        if ($_.FullName -ne (Resolve-Path $util).Path) {
            $fileContent = Get-Content $_.FullName -Raw
            if ($fileContent -match $pattern) {
                $usado = $true
            }
        }
    }
    
    if ($usado) {
        Write-Host "  ? $(Split-Path $util -Leaf) - EN USO" -ForegroundColor Green
    } else {
        Write-Host "  ??  $(Split-Path $util -Leaf) - NO USADO" -ForegroundColor Yellow
        $utilsNoUsados += $util
    }
}

# ============================================
# 4. VERIFICAR MODELOS
# ============================================
Write-Host "`n[4/6] Verificando modelos..." -ForegroundColor Yellow

$modelFiles = Get-ChildItem "FutronicService\Models" -Filter "*.cs"
foreach ($model in $modelFiles) {
    $modelName = $model.BaseName
    
    # Buscar uso en Controllers y Services
    $usado = $false
    Get-ChildItem "FutronicService" -Recurse -Filter "*.cs" | ForEach-Object {
        if ($_.FullName -ne $model.FullName) {
            $fileContent = Get-Content $_.FullName -Raw
            if ($fileContent -match $modelName) {
                $usado = $true
            }
        }
    }
    
    if ($usado) {
        Write-Host "  ? $($model.Name)" -ForegroundColor Green
    } else {
        Write-Host "  ??  $($model.Name) - Posiblemente no usado" -ForegroundColor Yellow
    }
}

# ============================================
# 5. COMPILAR PROYECTO
# ============================================
Write-Host "`n[5/6] Compilando proyecto..." -ForegroundColor Yellow

$buildResult = dotnet build FutronicService.sln --verbosity quiet 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ? Proyecto compila correctamente" -ForegroundColor Green
} else {
    Write-Host "  ? Error al compilar proyecto:" -ForegroundColor Red
    Write-Host $buildResult
}

# ============================================
# 6. VERIFICAR ESTRUCTURA DE DIRECTORIOS
# ============================================
Write-Host "`n[6/6] Verificando estructura de directorios..." -ForegroundColor Yellow

$directoriosEsperados = @(
    "FutronicService\Controllers",
    "FutronicService\Models",
    "FutronicService\Services",
    "FutronicService\Utils",
    "FutronicService\Middleware",
    "FutronicService\Native"
)

foreach ($dir in $directoriosEsperados) {
    if (Test-Path $dir) {
        $fileCount = (Get-ChildItem $dir -File).Count
        Write-Host "  ? $dir ($fileCount archivos)" -ForegroundColor Green
    } else {
        Write-Host "  ? $dir - NO EXISTE" -ForegroundColor Red
    }
}

# Verificar DLLs nativas
Write-Host "`n  ?? DLLs Nativas en FutronicService\Native:" -ForegroundColor Cyan
$nativeDlls = @("FTRAPI.dll", "ftrScanAPI.dll", "ftrSDKHelper13.dll")
foreach ($dll in $nativeDlls) {
    $path = "FutronicService\Native\$dll"
    if (Test-Path $path) {
        $size = (Get-Item $path).Length / 1KB
        Write-Host "    ? $dll ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Host "    ? $dll - NO ENCONTRADA" -ForegroundColor Red
    }
}

# ============================================
# RESUMEN FINAL
# ============================================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  RESUMEN" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n?? Archivos innecesarios encontrados: $($archivosEncontrados.Count)" -ForegroundColor $(if ($archivosEncontrados.Count -gt 0) { "Yellow" } else { "Green" })
Write-Host "?? Utils no usados: $($utilsNoUsados.Count)" -ForegroundColor $(if ($utilsNoUsados.Count -gt 0) { "Yellow" } else { "Green" })

if ($archivosEncontrados.Count -gt 0) {
    Write-Host "`n¿Desea eliminar los archivos innecesarios? (S/N): " -NoNewline -ForegroundColor Yellow
    $respuesta = Read-Host
    
    if ($respuesta -eq "S" -or $respuesta -eq "s") {
        Write-Host "`nEliminando archivos..." -ForegroundColor Yellow
        foreach ($archivo in $archivosEncontrados) {
            try {
                Remove-Item $archivo -Force
                Write-Host "  ? Eliminado: $archivo" -ForegroundColor Green
            } catch {
                Write-Host "  ? Error al eliminar: $archivo" -ForegroundColor Red
            }
        }
        Write-Host "`n? Limpieza completada" -ForegroundColor Green
    } else {
        Write-Host "`n??  Limpieza omitida" -ForegroundColor Yellow
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN COMPLETADA" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
