# Script para copiar DLLs nativas de Futronic al directorio de salida
# Ejecutar desde la raíz del proyecto

Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "  Copiando DLLs Nativas de Futronic" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$sourcePaths = @(
    "C:\Program Files (x86)\Futronic\SDK\",
    "C:\Program Files\Futronic\SDK\",
    "$PSScriptRoot\lib\",
    "$PSScriptRoot\packages\",
    "$PSScriptRoot\FutronicService\bin\"
)

$outputPaths = @(
    "$PSScriptRoot\FutronicService\bin\Debug\",
    "$PSScriptRoot\FutronicService\bin\Release\"
)

$dllsToFind = @(
    "ftrapi.dll",
    "FutronicSDK.dll"
)

foreach ($dll in $dllsToFind) {
    $found = $false
    
    foreach ($sourcePath in $sourcePaths) {
      if (Test-Path $sourcePath) {
        $dllPath = Get-ChildItem -Path $sourcePath -Filter $dll -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            
          if ($dllPath) {
        Write-Host "? Encontrado: $dll" -ForegroundColor Green
                Write-Host "  Origen: $($dllPath.FullName)" -ForegroundColor Gray
           
            foreach ($outputPath in $outputPaths) {
         if (!(Test-Path $outputPath)) {
              New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
 }
        
   try {
   Copy-Item -Path $dllPath.FullName -Destination $outputPath -Force
          Write-Host "  ? Copiado a: $outputPath" -ForegroundColor Green
      $found = $true
  }
            catch {
     Write-Host "  ? Error al copiar a $outputPath : $_" -ForegroundColor Red
        }
 }
        break
  }
        }
    }
    
    if (-not $found) {
        Write-Host "? NO ENCONTRADO: $dll" -ForegroundColor Red
     Write-Host "  Busque manualmente en:" -ForegroundColor Yellow
        Write-Host "    - C:\Program Files (x86)\Futronic\SDK\" -ForegroundColor Yellow
        Write-Host "    - Carpeta de instalación del SDK de Futronic" -ForegroundColor Yellow
        Write-Host "  Y cópielo a: $($outputPaths[0])" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Proceso completado" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTA: Si las DLLs no se encontraron, debe:" -ForegroundColor Yellow
Write-Host "  1. Localizar ftrapi.dll y FutronicSDK.dll en su sistema" -ForegroundColor Yellow
Write-Host "  2. Crear carpeta 'lib' en la raíz del proyecto" -ForegroundColor Yellow
Write-Host "  3. Copiar las DLLs a la carpeta 'lib'" -ForegroundColor Yellow
Write-Host "  4. Ejecutar este script nuevamente" -ForegroundColor Yellow
